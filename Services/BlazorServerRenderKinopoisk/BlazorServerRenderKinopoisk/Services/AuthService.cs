using BlazorServerRenderKinopoisk.Models;
using Flurl.Http;
using Microsoft.AspNetCore.Identity.Data;

namespace BlazorServerRenderKinopoisk.Services
{
    public class AuthService
    {
        private readonly IFlurlClient _flurl;

        public AuthService(IFlurlClient flurl)
        {
            _flurl = flurl;
        }

        public async Task<(AuthResponse? Result, string? Error)> LoginAsync(string login, string password)
        {
            try
            {
                var result = await _flurl.Request("api/auth/login")
                    .PostJsonAsync(new Models.LoginRequest(login, password))
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
            catch (Exception ex)
            {
                return (null, $"Connection error: {ex.Message}");
            }
        }

        public async Task<(RegisterResponse? Result, string? Error)> RegisterAsync(string login, string password)
        {
            try
            {
                var result = await _flurl.Request("api/auth/register")
                    .PostJsonAsync(new Models.RegisterRequest(login, password))
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
            catch (Exception ex)
            {
                return (null, $"Connection error: {ex.Message}");
            }
        }
    }
    public record ErrorBody(string Error);
}
