using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Attributes;
using RestaurantApi.Database;
using RestaurantApi.Hubs;
using RestaurantApi.Models;
using RestaurantApi.Request;
using RestaurantApi.Interface;

namespace RestaurantApi.Controllers;

public sealed class PaymentsController(ContextDb db, IOrderService orders, IHubContext<KitchenHub> hub, IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<PaymentsController> logger) : ControllerBase
{
    private int UserID => int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value, out var id) ? id : 0;
    private int RoleID => int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "role")?.Value, out var id) ? id : 0;

    [HttpPost, Route("/payments/request"), RoleAuthorizeAttributes([4, 6])]
    public async Task<IActionResult> RequestPayment([FromBody] StartPaymentRequest request, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.OrderID == request.OrderID && o.WaiterID == UserID, ct);
        if (order == null) return NotFound(new { status = false, message = "Заказ не найден." });
        if (order.Status != "Ready") return BadRequest(new { status = false, message = "Оплатить можно только готовый заказ." });
        if (request.Method is not ("Cash" or "CardTerminal")) return BadRequest(new { status = false, message = "Выберите оплату наличными или картой у официанта." });
        var existing = await db.PaymentTransactions.FirstOrDefaultAsync(p => p.OrderID == order.OrderID && p.Status == "Pending" && p.Method != "YooKassa", ct);
        if (existing != null) return Ok(new { status = true, paymentID = existing.PaymentTransactionID, message = "Ожидаем подтверждения оплаты официантом." });
        var payment = new PaymentTransaction { OrderID = order.OrderID, UserID = UserID, Amount = order.TotalPrice, Method = request.Method };
        db.PaymentTransactions.Add(payment); await db.SaveChangesAsync(ct);
        await hub.Clients.Group("Waiters").SendAsync("PaymentRequested", new { paymentID = payment.PaymentTransactionID, orderID = order.OrderID, amount = order.TotalPrice, method = payment.Method }, ct);
        return Ok(new { status = true, paymentID = payment.PaymentTransactionID, message = "Оплатите заказ выбранным способом официанту. Статус обновится после подтверждения." });
    }

    [HttpGet, Route("/payments/pending"), RoleAuthorizeAttributes([1, 2])]
    public async Task<IActionResult> Pending(CancellationToken ct)
    {
        var items = await db.PaymentTransactions.AsNoTracking().Where(p => p.Status == "Pending" && p.Method != "YooKassa").OrderBy(p => p.CreatedAt)
            .Select(p => new { p.PaymentTransactionID, p.OrderID, p.Amount, p.Method, p.CreatedAt }).ToListAsync(ct);
        return Ok(new { status = true, items });
    }

    [HttpPost, Route("/payments/{id:int}/confirm"), RoleAuthorizeAttributes([1, 2])]
    public async Task<IActionResult> Confirm(int id, CancellationToken ct)
    {
        var payment = await db.PaymentTransactions.FirstOrDefaultAsync(p => p.PaymentTransactionID == id, ct);
        if (payment == null) return NotFound(new { status = false, message = "Ожидающий платёж не найден." });
        if (payment.Status != "Pending") return Conflict(new { status = false, message = "Этот платёж уже обработан." });
        if (payment.Method == "YooKassa") return Conflict(new { status = false, message = "Онлайн-платёж подтверждается ЮKassa автоматически." });
        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderID == payment.OrderID, ct);
        if (order == null) return NotFound(new { status = false, message = "Заказ для оплаты не найден." });
        if (order.Status == "Cancelled")
        {
            payment.Status = "Cancelled";
            await db.SaveChangesAsync(ct);
            return Conflict(new { status = false, message = "Нельзя подтвердить оплату отменённого заказа." });
        }
        var result = await orders.UpdateStatusAsync(new UpdateStatusRequest { OrderID = payment.OrderID, Status = "Paid", ActorUserID = UserID, ActorRole = RoleID });
        if (result is not Microsoft.AspNetCore.Mvc.OkObjectResult) return result;
        payment.Status = "Confirmed"; payment.ConfirmedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { status = true, message = "Оплата подтверждена, заказ закрыт." });
    }

    [HttpPost, Route("/payments/online/create"), RoleAuthorizeAttributes([4, 6])]
    public async Task<IActionResult> CreateOnlinePayment([FromBody] StartPaymentRequest request, CancellationToken ct)
    {
        if (!TryGetCredentials(out var shopId, out var secretKey, out var returnUrl))
            return StatusCode(503, new { status = false, message = "ЮKassa не настроена. Укажите ShopId, SecretKey и ReturnUrl." });

        var order = await db.Orders.FirstOrDefaultAsync(o => o.OrderID == request.OrderID && o.WaiterID == UserID, ct);
        if (order == null) return NotFound(new { status = false, message = "Заказ не найден." });
        if (order.Status != "Ready") return BadRequest(new { status = false, message = "Онлайн-оплата доступна после приготовления заказа." });

        var payment = await db.PaymentTransactions
            .Where(p => p.OrderID == order.OrderID && p.Method == "YooKassa" && (p.Status == "Pending" || p.Status == "Creating"))
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (payment?.Status == "Pending" && !string.IsNullOrWhiteSpace(payment.ConfirmationUrl))
            return Ok(new { status = true, paymentID = payment.PaymentTransactionID, confirmationUrl = payment.ConfirmationUrl, message = "Продолжите оплату на странице ЮKassa." });

        if (payment == null)
        {
            payment = new PaymentTransaction
            {
                OrderID = order.OrderID,
                UserID = UserID,
                Amount = order.TotalPrice,
                Method = "YooKassa",
                Status = "Creating",
                IdempotenceKey = Guid.NewGuid().ToString()
            };
            db.PaymentTransactions.Add(payment);
            await db.SaveChangesAsync(ct);
        }
        else if (string.IsNullOrWhiteSpace(payment.IdempotenceKey))
        {
            payment.IdempotenceKey = Guid.NewGuid().ToString();
            await db.SaveChangesAsync(ct);
        }

        try
        {
            var confirmationReturnUrl = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(returnUrl, "paymentID", payment.PaymentTransactionID.ToString(CultureInfo.InvariantCulture));
            using var response = await SendYooKassaAsync(HttpMethod.Post, "/v3/payments", new
            {
                amount = new { value = payment.Amount.ToString("0.00", CultureInfo.InvariantCulture), currency = "RUB" },
                capture = true,
                confirmation = new { type = "redirect", return_url = confirmationReturnUrl },
                description = $"Заказ ресторана №{order.OrderID}",
                metadata = new { order_id = order.OrderID.ToString(CultureInfo.InvariantCulture), payment_transaction_id = payment.PaymentTransactionID.ToString(CultureInfo.InvariantCulture) }
            }, shopId, secretKey, payment.IdempotenceKey!, ct);

            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("ЮKassa вернула {StatusCode} при создании платежа: {Body}", (int)response.StatusCode, body);
                await db.SaveChangesAsync(ct);
                return StatusCode(502, new { status = false, message = "ЮKassa не смогла создать платёж. Попробуйте ещё раз." });
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            payment.ProviderPaymentID = root.GetProperty("id").GetString();
            payment.ConfirmationUrl = root.GetProperty("confirmation").GetProperty("confirmation_url").GetString();
            payment.Status = "Pending";
            await db.SaveChangesAsync(ct);
            return Ok(new { status = true, paymentID = payment.PaymentTransactionID, confirmationUrl = payment.ConfirmationUrl, message = "Перейдите на защищённую страницу ЮKassa для оплаты." });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            logger.LogError(ex, "Не удалось создать платёж ЮKassa для заказа {OrderID}", order.OrderID);
            await db.SaveChangesAsync(ct);
            return StatusCode(502, new { status = false, message = "Не удалось связаться с ЮKassa. Попробуйте ещё раз." });
        }
    }

    [HttpGet, Route("/payments/online/{id:int}/status"), RoleAuthorizeAttributes([4, 6])]
    public async Task<IActionResult> OnlinePaymentStatus(int id, CancellationToken ct)
    {
        var payment = await db.PaymentTransactions.FirstOrDefaultAsync(p => p.PaymentTransactionID == id && p.UserID == UserID && p.Method == "YooKassa", ct);
        if (payment == null) return NotFound(new { status = false, paymentStatus = "NotFound", message = "Онлайн-платёж не найден." });
        if (payment.Status == "Confirmed") return Ok(new { status = true, paymentStatus = "succeeded", message = "Оплата подтверждена." });
        return await RefreshOnlinePayment(payment, ct);
    }

    [HttpPost, Route("/payments/yookassa/webhook"), AllowAnonymous]
    public async Task<IActionResult> YooKassaWebhook(CancellationToken ct)
    {
        JsonDocument notification;
        try { notification = await JsonDocument.ParseAsync(Request.Body, cancellationToken: ct); }
        catch (JsonException) { return BadRequest(); }
        using (notification)
        {
            var root = notification.RootElement;
            if (!root.TryGetProperty("event", out var eventElement) || !root.TryGetProperty("object", out var paymentObject) || !paymentObject.TryGetProperty("id", out var idElement))
                return BadRequest();

            var eventName = eventElement.GetString() ?? "";
            if (eventName is not ("payment.succeeded" or "payment.canceled")) return Ok();
            var providerId = idElement.GetString();
            if (string.IsNullOrWhiteSpace(providerId)) return BadRequest();
            var payment = await db.PaymentTransactions.FirstOrDefaultAsync(p => p.ProviderPaymentID == providerId && p.Method == "YooKassa", ct);
            if (payment == null) return Ok();
            var result = await RefreshOnlinePayment(payment, ct);
            return result is OkObjectResult ? Ok() : StatusCode(500);
        }
    }

    private async Task<IActionResult> RefreshOnlinePayment(PaymentTransaction payment, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(payment.ProviderPaymentID) || !TryGetCredentials(out var shopId, out var secretKey, out _))
            return StatusCode(503, new { status = false, paymentStatus = payment.Status, message = "Платёж ещё создаётся или ЮKassa не настроена." });

        try
        {
            using var response = await SendYooKassaAsync(HttpMethod.Get, $"/v3/payments/{Uri.EscapeDataString(payment.ProviderPaymentID)}", null, shopId, secretKey, null, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("ЮKassa вернула {StatusCode} при проверке платежа {PaymentID}: {Body}", (int)response.StatusCode, payment.ProviderPaymentID, body);
                return StatusCode(502, new { status = false, paymentStatus = payment.Status, message = "Не удалось проверить платёж в ЮKassa." });
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var providerStatus = root.TryGetProperty("status", out var statusElement) ? statusElement.GetString() ?? "pending" : "pending";
            if (providerStatus == "succeeded")
            {
                var paid = root.TryGetProperty("paid", out var paidElement) && paidElement.GetBoolean();
                var amountElement = root.GetProperty("amount");
                var amountValue = amountElement.GetProperty("value").GetString();
                var currency = amountElement.GetProperty("currency").GetString();
                if (!paid || currency != "RUB" || !decimal.TryParse(amountValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount != payment.Amount)
                    return Conflict(new { status = false, paymentStatus = "verification_failed", message = "Параметры оплаты не совпали с заказом." });

                if (payment.Status != "Confirmed")
                {
                    var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderID == payment.OrderID, ct);
                    if (order == null || order.Status != "Ready")
                    {
                        payment.Status = "NeedsReview";
                        await db.SaveChangesAsync(ct);
                        return Conflict(new { status = false, paymentStatus = "needs_review", message = "Платёж успешен, но заказ изменил статус. Обратитесь к администратору." });
                    }

                    var update = await orders.UpdateStatusAsync(new UpdateStatusRequest { OrderID = payment.OrderID, Status = "Paid", ActorRole = 1 });
                    if (update is not OkObjectResult) return Conflict(new { status = false, paymentStatus = "needs_review", message = "Платёж получен, но заказ не удалось закрыть. Обратитесь к администратору." });
                    payment.Status = "Confirmed";
                    payment.ConfirmedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                }
            }
            else if (providerStatus == "canceled" && payment.Status is "Pending" or "Creating")
            {
                payment.Status = "Cancelled";
                await db.SaveChangesAsync(ct);
            }

            return Ok(new { status = true, paymentStatus = providerStatus, message = providerStatus == "succeeded" ? "Оплата подтверждена." : providerStatus == "canceled" ? "Платёж отменён." : "Ожидаем подтверждения оплаты от ЮKassa." });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            logger.LogError(ex, "Не удалось проверить платёж ЮKassa {PaymentID}", payment.PaymentTransactionID);
            return StatusCode(502, new { status = false, paymentStatus = payment.Status, message = "Не удалось проверить платёж. Попробуйте позже." });
        }
    }

    private async Task<HttpResponseMessage> SendYooKassaAsync(HttpMethod method, string path, object? body, string shopId, string secretKey, string? idempotenceKey, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("YooKassa");
        using var request = new HttpRequestMessage(method, path);
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{shopId}:{secretKey}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        if (!string.IsNullOrWhiteSpace(idempotenceKey)) request.Headers.TryAddWithoutValidation("Idempotence-Key", idempotenceKey);
        if (body != null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    private bool TryGetCredentials(out string shopId, out string secretKey, out string returnUrl)
    {
        shopId = configuration["YooKassa:ShopId"] ?? "";
        secretKey = configuration["YooKassa:SecretKey"] ?? "";
        returnUrl = configuration["YooKassa:ReturnUrl"] ?? "";
        return !string.IsNullOrWhiteSpace(shopId) && !string.IsNullOrWhiteSpace(secretKey) && Uri.IsWellFormedUriString(returnUrl, UriKind.Absolute);
    }
}
