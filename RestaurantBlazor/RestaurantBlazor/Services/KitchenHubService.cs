using Blazored.LocalStorage;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;

namespace RestaurantBlazor.Services
{
    public class KitchenHubService : IAsyncDisposable
    {
        private HubConnection? _hub;
        private readonly ILocalStorageService _localStorage;
        private readonly ILogger<KitchenHubService> _logger;
        private readonly IConfiguration _configuration;
        private readonly SemaphoreSlim _connectionGate = new(1, 1);

        private bool _joinKitchen;
        private bool _joinWaiters;
        private bool _joinTableObservers;
        private bool _joinCouriers;
        private bool _joinSupportOperators;
        private bool _joinSupportUser;
        private readonly HashSet<int> _trackedOrders = new();

        public event Action<object>? OnNewOrder;
        public event Action<object>? OnNewDelivery;
        public event Action? OnOrderUpdated;
        public event Action<string>? OnWaiterNotification;
        public event Action<string>? OnDeliveryStatusChanged;

        public static string GetDeliveryNotice(object data)
        {
            try
            {
                using var document = JsonDocument.Parse(JsonSerializer.Serialize(data));
                if (document.RootElement.TryGetProperty("message", out var message) && !string.IsNullOrWhiteSpace(message.GetString()))
                    return message.GetString()!;
                if (document.RootElement.TryGetProperty("pickupAddress", out var pickup) && !string.IsNullOrWhiteSpace(pickup.GetString()))
                    return $"Новый заказ: забрать в филиале «{pickup.GetString()}» и отвезти клиенту.";
            }
            catch { }
            return "Вам назначена новая доставка";
        }
        public event Action<string>? OnOrderStatusChanged;
        public event Action? OnTablesUpdated;
        public event Action<int>? OnSupportTicketCreated;
        public event Action<int>? OnSupportMessageReceived;
        public event Action<int, string>? OnSupportReply;

        public bool IsConnected => _hub?.State == HubConnectionState.Connected;

        public KitchenHubService(
            ILocalStorageService localStorage,
            ILogger<KitchenHubService> logger,
            IConfiguration configuration)
        {
            _localStorage = localStorage;
            _logger = logger;
            _configuration = configuration;
        }

        public Task StartAsync()
        {
            var baseUrl = (_configuration["ApiBaseUrl"] ?? "http://localhost:5161").TrimEnd('/');
            return ConnectAsync(baseUrl);
        }

        public Task StopAsync() => DisposeAsync().AsTask();

        public async Task ConnectAsync(string baseUrl)
        {
            var acquired = false;
            try
            {
                await _connectionGate.WaitAsync();
                acquired = true;
                if (_hub is { State: HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting }) return;

                if (_hub != null)
                {
                    await _hub.DisposeAsync();
                    _hub = null;
                }

                var token = await _localStorage.GetItemAsync<string>("auth_token");
                if (string.IsNullOrWhiteSpace(token)) return;

                var hub = new HubConnectionBuilder()
                    .WithUrl($"{baseUrl}/hubs/kitchen", options => options.AccessTokenProvider = () => Task.FromResult<string?>(token))
                    .WithAutomaticReconnect()
                    .Build();
                RegisterHandlers(hub);
                hub.Reconnected += async _ => await RestoreGroupsAsync();
                _hub = hub;
                await hub.StartAsync();
                await RestoreGroupsAsync();
                _logger.LogInformation("KitchenHub connected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KitchenHub connection failed");
            }
            finally
            {
                if (acquired) _connectionGate.Release();
            }
        }

        private void RegisterHandlers(HubConnection hub)
        {
            hub.On<object>("NewOrder", order =>
            {
                _logger.LogInformation("SignalR: NewOrder received");
                OnNewOrder?.Invoke(order);
                OnOrderUpdated?.Invoke();
            });

            hub.On<object>("OrderReady", data =>
            {
                var orderId = GetIntFromData(data, "orderID");
                var tableId = GetIntFromData(data, "tableID");
                OnWaiterNotification?.Invoke(tableId > 0
                    ? $"Заказ #{orderId} для стола #{tableId} готов"
                    : $"Заказ #{orderId} готов");
                OnOrderUpdated?.Invoke();
            });

            hub.On<object>("WaiterCalled", data =>
            {
                var tableNumber = GetIntFromData(data, "tableNumber");
                var reason = GetMessageFromData(data, "reason");
                OnWaiterNotification?.Invoke($"Стол {tableNumber}: {reason}");
            });

            hub.On<object>("BillRequested", data =>
            {
                var orderId = GetIntFromData(data, "orderID");
                var tableId = GetIntFromData(data, "tableID");
                OnWaiterNotification?.Invoke($"Запрошен счёт по заказу #{orderId}" + (tableId > 0 ? $" (стол #{tableId})" : ""));
            });

            hub.On<object>("NewDelivery", delivery =>
            {
                _logger.LogInformation("SignalR: NewDelivery received");
                OnNewDelivery?.Invoke(delivery);
            });

            hub.On<object>("DeliveryAssigned", data =>
            {
                var msg = GetMessageFromData(data, "message");
                OnDeliveryStatusChanged?.Invoke(msg);
            });

            hub.On<object>("DeliveryStatusChanged", data =>
            {
                var msg = GetMessageFromData(data, "message");
                OnDeliveryStatusChanged?.Invoke(msg);
                OnOrderUpdated?.Invoke();
            });

            hub.On<object>("OrderStatusChanged", data =>
            {
                var msg = GetMessageFromData(data, "message");
                OnOrderStatusChanged?.Invoke(msg);
                OnOrderUpdated?.Invoke();
            });

            hub.On("OrderUpdated", () => OnOrderUpdated?.Invoke());
            hub.On("TablesUpdated", () => OnTablesUpdated?.Invoke());
            hub.On("StockUpdated", () => OnOrderUpdated?.Invoke());
            hub.On<object>("PaymentRequested", data => OnWaiterNotification?.Invoke($"Запрошена оплата заказа #{GetIntFromData(data, "orderID")}"));
            hub.On<object>("SupportTicketCreated", data => OnSupportTicketCreated?.Invoke(GetIntFromData(data, "conversationID")));
            hub.On<object>("SupportMessageReceived", data => OnSupportMessageReceived?.Invoke(GetIntFromData(data, "conversationID")));
            hub.On<object>("SupportReply", data => OnSupportReply?.Invoke(GetIntFromData(data, "conversationID"), GetMessageFromData(data, "message")));
        }

        private async Task RestoreGroupsAsync()
        {
            if (_hub is not { State: HubConnectionState.Connected }) return;

            try
            {
                if (_joinKitchen) await _hub.InvokeAsync("JoinKitchen");
                if (_joinWaiters) await _hub.InvokeAsync("JoinWaiters");
                if (_joinTableObservers) await _hub.InvokeAsync("JoinTableObservers");
                if (_joinCouriers) await _hub.InvokeAsync("JoinCouriers");
                if (_joinSupportOperators) await _hub.InvokeAsync("JoinSupportOperators");
                if (_joinSupportUser) await _hub.InvokeAsync("JoinSupportUser");
                foreach (var orderId in _trackedOrders.ToArray())
                    await _hub.InvokeAsync("JoinOrderTracking", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось восстановить SignalR-группы");
            }
        }

        private static string GetMessageFromData(object data, string key)
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty(key, out var val))
                    return val.GetString() ?? "";
            }
            catch { }
            return "";
        }

        private static int GetIntFromData(object data, string key)
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty(key, out var val) && val.TryGetInt32(out var number))
                    return number;
            }
            catch { }
            return 0;
        }

        public async Task JoinKitchenAsync()
        {
            _joinKitchen = true;
            if (_hub is { State: HubConnectionState.Connected })
                await _hub.InvokeAsync("JoinKitchen");
        }

        public async Task JoinWaitersAsync()
        {
            _joinWaiters = true;
            if (_hub is { State: HubConnectionState.Connected })
                await _hub.InvokeAsync("JoinWaiters");
        }

        public async Task JoinTableObserversAsync()
        {
            _joinTableObservers = true;
            if (_hub is { State: HubConnectionState.Connected })
                await _hub.InvokeAsync("JoinTableObservers");
        }

        public async Task JoinCouriersAsync()
        {
            _joinCouriers = true;
            if (_hub is { State: HubConnectionState.Connected })
                await _hub.InvokeAsync("JoinCouriers");
        }

        public async Task JoinSupportOperatorsAsync()
        {
            _joinSupportOperators = true;
            if (_hub is { State: HubConnectionState.Connected }) await _hub.InvokeAsync("JoinSupportOperators");
        }

        public async Task JoinSupportUserAsync()
        {
            _joinSupportUser = true;
            if (_hub is { State: HubConnectionState.Connected }) await _hub.InvokeAsync("JoinSupportUser");
        }

        public async Task JoinOrderTrackingAsync(int orderId)
        {
            if (orderId <= 0) return;
            _trackedOrders.Add(orderId);
            if (_hub is { State: HubConnectionState.Connected })
                await _hub.InvokeAsync("JoinOrderTracking", orderId);
        }

        public async Task LeaveOrderTrackingAsync(int orderId)
        {
            _trackedOrders.Remove(orderId);
            if (_hub is { State: HubConnectionState.Connected })
                await _hub.InvokeAsync("LeaveOrderTracking", orderId);
        }

        public async ValueTask DisposeAsync()
        {
            _joinKitchen = false;
            _joinWaiters = false;
            _joinTableObservers = false;
            _joinCouriers = false;
            _joinSupportOperators = false;
            _joinSupportUser = false;
            _trackedOrders.Clear();

            if (_hub != null)
            {
                await _hub.DisposeAsync();
                _hub = null;
            }
        }
    }
}
