using CacheImageService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddHttpClient("tmdb", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(5)
});

var identityUrl = builder.Configuration.GetValue<string>("IdentityService:Url") ?? "http://identity-service:5001";
builder.Services.AddHttpClient("identity", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.BaseAddress = new Uri(identityUrl);
});

builder.Services.AddSingleton<ImageCacheService>();

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.Run("http://0.0.0.0:5007");
