using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BlazorServerRenderKinopoisk.Services;

public class TokenRefreshHandler : DelegatingHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _apiBaseUrl;

    public TokenRefreshHandler(IServiceProvider serviceProvider, string apiBaseUrl)
    {
        _serviceProvider = serviceProvider;
        _apiBaseUrl = apiBaseUrl;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var tokenStore = _serviceProvider.GetRequiredService<TokenStore>();
            var refreshToken = tokenStore.RefreshToken;

            if (refreshToken is not null)
            {
                var newTokens = await TryRefreshTokenAsync(refreshToken, cancellationToken);

                if (newTokens is not null)
                {
                    await tokenStore.SaveAsync(newTokens.Token, newTokens.RefreshToken);

                    var clone = await CloneRequestAsync(request);
                    clone.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newTokens.Token);
                    response = await base.SendAsync(clone, cancellationToken);
                }
                else
                {
                    await tokenStore.ClearAsync();
                }
            }
        }

        return response;
    }

    private async Task<TokenRefreshResult?> TryRefreshTokenAsync(string refreshToken, CancellationToken ct)
    {
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(_apiBaseUrl) };
            var payload = JsonSerializer.Serialize(new { refreshToken });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/auth/refresh", content, ct);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            return new TokenRefreshResult(
                doc.RootElement.GetProperty("token").GetString()!,
                doc.RootElement.GetProperty("refreshToken").GetString()!,
                doc.RootElement.GetProperty("refreshTokenExpiresAt").GetDateTime()
            );
        }
        catch
        {
            return null;
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        if (request.Content is not null)
        {
            var body = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(body);
            if (request.Content.Headers.ContentType is not null)
                clone.Content.Headers.ContentType = request.Content.Headers.ContentType;
        }

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }

    private record TokenRefreshResult(string Token, string RefreshToken, DateTime RefreshTokenExpiresAt);
}
