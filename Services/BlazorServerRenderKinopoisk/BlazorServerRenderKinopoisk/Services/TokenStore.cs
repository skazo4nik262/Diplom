using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;

namespace BlazorServerRenderKinopoisk.Services
{
    public class TokenStore
    {
        private readonly IJSRuntime _js;

        public TokenStore(IJSRuntime js)
        {
            _js = js;
        }

        public string? Token { get; private set; }
        public bool IsAuthenticated => Token != null;

        public async Task SaveAsync(string token)
        {
            Token = token;
            try { await _js.InvokeVoidAsync("localStorage.setItem", "auth_token", token); } catch { }
        }

        public async Task LoadFromStorageAsync()
        {
            try
            {
                var result = await _js.InvokeAsync<string?>("localStorage.getItem", "auth_token");
                if (!string.IsNullOrEmpty(result))
                    Token = result;
            }
            catch { }
        }

        public async Task ClearAsync()
        {
            Token = null;
            try { await _js.InvokeVoidAsync("localStorage.removeItem", "auth_token"); } catch { }
        }

        public DateTime? GetTokenExpiry()
        {
            if (Token == null) return null;

            var parts = Token.Split('.');
            if (parts.Length != 3) return null;

            var payload = parts[1];
            var padded = (payload.Length % 4) switch
            {
                2 => payload + "==",
                3 => payload + "=",
                _ => payload
            };
            padded = padded.Replace('-', '+').Replace('_', '/');

            try
            {
                var bytes = Convert.FromBase64String(padded);
                var json = Encoding.UTF8.GetString(bytes);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("exp", out var expProp))
                {
                    var expUnix = expProp.GetInt64();
                    return DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
                }
            }
            catch { }

            return null;
        }
    }
}
