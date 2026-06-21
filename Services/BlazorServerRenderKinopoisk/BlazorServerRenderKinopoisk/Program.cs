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
            builder.Services.AddSingleton<IFlurlClient>(new FlurlClient(apiUrl));
            builder.Services.AddScoped<TokenStore>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<CatalogService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStatusCodePagesWithReExecute("/not-found");
            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
