using Blazored.LocalStorage;
using RestaurantBlazor.Models;

namespace RestaurantBlazor.Services
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(string login, string password);
        Task<bool> RegisterAsync(RegisterRequest request);
        Task LogoutAsync();
        bool IsAuthenticated();
        int GetCurrentUserId();
        int GetCurrentRole();
        int GetCurrentBranchId();
        string GetCurrentUserName();
        string GetRoleName();
        Task InitializeAsync();
        event Action? Changed;
    }

    public class AuthService : IAuthService
    {
        private readonly ApiService _api;
        private readonly ILocalStorageService _localStorage;

        private string? _token;
        private int _userId;
        private int _role;
        private int _branchId = 1;
        private string _userName = "";

        public event Action? Changed;

        public static readonly Dictionary<int, string> RoleNames = new()
        {
            { 1, "Администратор" },
            { 2, "Официант" },
            { 3, "Повар" },
            { 4, "Гость" },
            { 5, "Курьер" },
            { 6, "Оператор поддержки" },
            { 7, "Поставщик" }
        };

        public AuthService(ApiService api, ILocalStorageService localStorage)
        {
            _api = api;
            _localStorage = localStorage;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _token = await _localStorage.GetItemAsync<string>("auth_token");
                _userId = await _localStorage.GetItemAsync<int>("auth_userId");
                _role = await _localStorage.GetItemAsync<int>("auth_role");
                _branchId = await _localStorage.GetItemAsync<int>("auth_branchId");
                _userName = await _localStorage.GetItemAsync<string>("auth_userName") ?? "";

                if (!string.IsNullOrEmpty(_token))
                {
                    _api.SetToken(_token);
                    Changed?.Invoke();
                }
            }
            catch
            {
            }
        }

        public async Task<bool> LoginAsync(string login, string password)
        {
            try
            {
                var result = await _api.LoginAsync(new AuthRequest { Login = login, Password = password });
                if (result is { Status: true })
                {
                    _token = result.Token;
                    _userId = result.UserId;
                    _role = result.Role;
                    _branchId = result.BranchId;
                    _userName = string.IsNullOrWhiteSpace(result.UserName) ? login : result.UserName;

                    _api.SetToken(_token);

                    await _localStorage.SetItemAsync("auth_token", _token);
                    await _localStorage.SetItemAsync("auth_userId", _userId);
                    await _localStorage.SetItemAsync("auth_role", _role);
                    await _localStorage.SetItemAsync("auth_branchId", _branchId);
                    await _localStorage.SetItemAsync("auth_userName", _userName);

                    Changed?.Invoke();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var result = await _api.RegisterAsync(request);
                return result?.Status == true;
            }
            catch
            {
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            _token = null;
            _userId = 0;
            _role = 0;
            _branchId = 1;
            _userName = "";
            _api.SetToken(null);

            await _localStorage.RemoveItemAsync("auth_token");
            await _localStorage.RemoveItemAsync("auth_userId");
            await _localStorage.RemoveItemAsync("auth_role");
            await _localStorage.RemoveItemAsync("auth_branchId");
            await _localStorage.RemoveItemAsync("auth_userName");

            Changed?.Invoke();
        }

        public bool IsAuthenticated() => !string.IsNullOrEmpty(_token);
        public int GetCurrentUserId() => _userId;
        public int GetCurrentRole() => _role;
        public int GetCurrentBranchId() => _branchId;
        public string GetCurrentUserName() => _userName;
        public string GetRoleName() => RoleNames.TryGetValue(_role, out var n) ? n : "Гость";
    }
}
