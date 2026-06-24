using Microsoft.EntityFrameworkCore;
using AdminService.Data;
using AdminService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient("Jellyfin", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Jellyfin:Url"] ??
        throw new InvalidOperationException("Jellyfin:Url not configured"));
    client.DefaultRequestHeaders.Add("X-Emby-Token",
        builder.Configuration["Jellyfin:ApiKey"] ?? "");
});

builder.Services.AddHttpClient<TransmissionClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Transmission:Url"] ??
        throw new InvalidOperationException("Transmission:Url not configured"));
    var user = builder.Configuration["Transmission:User"] ?? "transmission";
    var pass = builder.Configuration["Transmission:Pass"] ?? "transmission";
    var auth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{user}:{pass}"));
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
});

builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ??
        throw new InvalidOperationException("Connection string not configured")));

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});

builder.Services.AddSingleton<LibraryScanner>();
builder.Services.AddHostedService<DownloadTracker>();

var app = builder.Build();

app.UseCors();
app.MapControllers();
app.Run("http://0.0.0.0:5009");
