using BlazorServerRenderKinopoisk.Models;
using Flurl.Http;

namespace BlazorServerRenderKinopoisk.Services
{
    public class AuthService
    {
        private readonly IFlurlClient _flurl;
        private readonly TokenStore _tokenStore;

        public AuthService(IFlurlClient flurl, TokenStore tokenStore)
        {
            _flurl = flurl;
            _tokenStore = tokenStore;
        }

        public async Task<(AuthResponse? Result, string? Error)> LoginAsync(string login, string password)
        {
            try
            {
                var result = await _flurl.Request("api/auth/login")
                    .PostJsonAsync(new LoginRequest(login, password))
                    .ReceiveJson<AuthResponse>();
                return (result, null);
            }
            catch (FlurlHttpException ex)
            {
                if (ex.StatusCode == 401) return (null, "Invalid credentials");
                try
                {
                    var error = await ex.GetResponseJsonAsync<ErrorBody>();
                    return (null, error?.Error ?? "Login failed");
                }
                catch { return (null, "Login failed"); }
            }
        }

        public async Task<(RegisterResponse? Result, string? Error)> RegisterAsync(string login, string password, string? username = null)
        {
            try
            {
                var result = await _flurl.Request("api/auth/register")
                    .PostJsonAsync(new RegisterRequest(login, password, username))
                    .ReceiveJson<RegisterResponse>();
                return (result, null);
            }
            catch (FlurlHttpException ex)
            {
                if (ex.StatusCode == 409) return (null, "Login already exists");
                try
                {
                    var error = await ex.GetResponseJsonAsync<ErrorBody>();
                    return (null, error?.Error ?? "Registration failed");
                }
                catch { return (null, "Registration failed"); }
            }
        }

        public async Task<RefreshResponse?> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                return await _flurl.Request("api/auth/refresh")
                    .PostJsonAsync(new RefreshRequest(refreshToken))
                    .ReceiveJson<RefreshResponse>();
            }
            catch { return null; }
        }

        public async Task<ProfileResponse?> GetProfileAsync()
        {
            try
            {
                return await _flurl.Request("api/auth/profile")
                    .WithOAuthBearerToken(_tokenStore.Token ?? "")
                    .PostJsonAsync(new { })
                    .ReceiveJson<ProfileResponse>();
            }
            catch { return null; }
        }

        public async Task<ProfileResponse?> UpdateProfileAsync(string? username, string? bio, DateTime? birthday, string? avatarUrl)
        {
            try
            {
                return await _flurl.Request("api/auth/profile")
                    .WithOAuthBearerToken(_tokenStore.Token ?? "")
                    .PostJsonAsync(new UpdateProfileRequest(username, bio, birthday, avatarUrl))
                    .ReceiveJson<ProfileResponse>();
            }
            catch { return null; }
        }

        public async Task<List<UserBriefDto>> SearchUsersAsync(string query, bool includeInactive = false)
        {
            try
            {
                var req = _flurl.Request("api/auth/users/search")
                    .WithOAuthBearerToken(_tokenStore.Token ?? "")
                    .SetQueryParam("query", query);
                if (includeInactive)
                    req = req.SetQueryParam("includeInactive", true);
                return await req.GetJsonAsync<List<UserBriefDto>>();
            }
            catch { return []; }
        }

        public async Task<bool> DeactivateUserAsync(Guid userId)
        {
            try
            {
                await _flurl.Request($"api/auth/users/{userId}")
                    .WithOAuthBearerToken(_tokenStore.Token ?? "")
                    .DeleteAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> ReactivateUserAsync(Guid userId)
        {
            try
            {
                await _flurl.Request($"api/auth/users/{userId}/reactivate")
                    .WithOAuthBearerToken(_tokenStore.Token ?? "")
                    .PatchAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> SetUserRoleAsync(Guid userId, int role)
        {
            try
            {
                await _flurl.Request($"api/auth/users/{userId}/role")
                    .WithOAuthBearerToken(_tokenStore.Token ?? "")
                    .PatchJsonAsync(new { role });
                return true;
            }
            catch { return false; }
        }

        public async Task<ProfileResponse?> GetUserByIdAsync(Guid userId)
        {
            try
            {
                return await _flurl.Request($"api/auth/users/id/{userId}")
                    .WithOAuthBearerToken(_tokenStore.Token ?? "")
                    .GetJsonAsync<ProfileResponse>();
            }
            catch { return null; }
        }
    }
    public record ErrorBody(string Error);
}
