using MudBlazor;
using MudBlazor.Services;
using BlazorServerRenderKinopoisk.Components;
using BlazorServerRenderKinopoisk.Services;
using BlazorServerRenderKinopoisk.Models;
using Flurl.Http;

namespace BlazorServerRenderKinopoisk
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddMudServices();
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            var apiUrl = builder.Configuration.GetSection("ApiClient")["ApiUrl"] ?? "http://screeny.ddns.net";

            builder.Services.AddScoped<TokenRefreshHandler>(sp =>
                new TokenRefreshHandler(sp, apiUrl));
            builder.Services.AddScoped<IFlurlClient>(sp =>
            {
                var handler = sp.GetRequiredService<TokenRefreshHandler>();
                handler.InnerHandler = new HttpClientHandler();
                var httpClient = new HttpClient(handler) { BaseAddress = new Uri(apiUrl) };
                return new FlurlClient(httpClient);
            });
            builder.Services.AddScoped<TokenStore>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<CatalogService>();
            builder.Services.AddHttpClient("catalog", client =>
            {
                client.BaseAddress = new Uri(apiUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            builder.Services.AddHttpClient("media", client =>
            {
                client.BaseAddress = new Uri(apiUrl);
                client.Timeout = Timeout.InfiniteTimeSpan;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStatusCodePagesWithReExecute("/not-found");
            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapGet("/api/catalog/poster/{size}/{**imagePath}", async (string size, string imagePath, IHttpClientFactory factory) =>
            {
                var client = factory.CreateClient("catalog");
                var response = await client.GetAsync($"/api/catalog/poster/{size}/{imagePath}");
                if (!response.IsSuccessStatusCode) return Results.NotFound();
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
                return Results.File(bytes, contentType);
            });
            app.MapGet("/api/catalog/avatar/{userId:guid}", async (Guid userId, IHttpClientFactory factory) =>
            {
                var client = factory.CreateClient("catalog");
                var response = await client.GetAsync($"/api/catalog/avatar/{userId}");
                if (!response.IsSuccessStatusCode) return Results.NotFound();
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
                return Results.File(bytes, contentType);
            });
            app.MapGet("/api/jellyfin/Media/hls/{mediaId:guid}/{**rest}", async (Guid mediaId, string? rest, HttpContext context, IHttpClientFactory factory) =>
            {
                var path = string.IsNullOrEmpty(rest) ? "" : $"/{rest}";
                await ProxyMediaAsync(factory, context, $"/api/jellyfin/Media/hls/{mediaId}{path}{context.Request.QueryString}");
            });
            app.MapGet("/api/jellyfin/Media/stream/{mediaId:guid}", async (Guid mediaId, HttpContext context, IHttpClientFactory factory) =>
            {
                await ProxyMediaAsync(factory, context, $"/api/jellyfin/Media/stream/{mediaId}{context.Request.QueryString}");
            });
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }

        private static async Task ProxyMediaAsync(IHttpClientFactory factory, HttpContext context, string target)
        {
            var client = factory.CreateClient("media");
            using var request = new HttpRequestMessage(HttpMethod.Get, target);
            if (context.Request.Headers.TryGetValue("Range", out var range) &&
                System.Net.Http.Headers.RangeHeaderValue.TryParse(range.FirstOrDefault(), out var parsed))
                request.Headers.Range = parsed;
            try
            {
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
                context.Response.StatusCode = (int)response.StatusCode;
                foreach (var header in response.Headers)
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                foreach (var header in response.Content.Headers)
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                context.Response.Headers.Remove("transfer-encoding");
                await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                if (!context.Response.HasStarted)
                    context.Response.StatusCode = 502;
            }
        }
    }
}
