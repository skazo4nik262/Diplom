using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;

namespace BlazorServerRenderKinopoisk.Services;

public class TokenStore
{
    private readonly IJSRuntime _js;

    public TokenStore(IJSRuntime js)
    {
        _js = js;
    }

    public string? Token { get; private set; }
    public string? RefreshToken { get; private set; }
    public bool IsAuthenticated => Token != null;

    public async Task SaveAsync(string token, string? refreshToken = null)
    {
        Token = token;
        if (refreshToken is not null)
            RefreshToken = refreshToken;
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", "auth_token", token);
            if (refreshToken is not null)
                await _js.InvokeVoidAsync("localStorage.setItem", "auth_refresh_token", refreshToken);
        }
        catch { }
    }

    public async Task SaveRefreshTokenAsync(string refreshToken)
    {
        RefreshToken = refreshToken;
        try { await _js.InvokeVoidAsync("localStorage.setItem", "auth_refresh_token", refreshToken); } catch { }
    }

    public async Task LoadFromStorageAsync()
    {
        try
        {
            var token = await _js.InvokeAsync<string?>("localStorage.getItem", "auth_token");
            if (!string.IsNullOrEmpty(token))
                Token = token;

            var refresh = await _js.InvokeAsync<string?>("localStorage.getItem", "auth_refresh_token");
            if (!string.IsNullOrEmpty(refresh))
                RefreshToken = refresh;
        }
        catch { }
    }

    public async Task ClearAsync()
    {
        Token = null;
        RefreshToken = null;
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", "auth_token");
            await _js.InvokeVoidAsync("localStorage.removeItem", "auth_refresh_token");
        }
        catch { }
    }

    public DateTime? GetTokenExpiry()
    {
        var payload = DecodePayload();
        if (!payload.HasValue) return null;

        if (payload.Value.TryGetProperty("exp", out var expProp))
        {
            var expUnix = expProp.GetInt64();
            return DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
        }
        return null;
    }

    public bool IsAdmin
    {
        get
        {
            var payload = DecodePayload();
            if (!payload.HasValue) return false;

            var role = GetRoleClaim(payload.Value);
            if (role is null) return false;

            if (role.Value.ValueKind == JsonValueKind.Number)
                return role.Value.GetInt32() == 0;

            var raw = role.Value.GetString();
            return int.TryParse(raw, out var roleVal) && roleVal == 0;
        }
    }

    private static JsonElement? GetRoleClaim(JsonElement payload)
    {
        if (payload.TryGetProperty("role", out var role))
            return role;
        if (payload.TryGetProperty("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", out role))
            return role;
        return null;
    }

    private JsonElement? DecodePayload()
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
            return JsonDocument.Parse(json).RootElement;
        }
        catch { return null; }
    }
}
