using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;

namespace JellyfinService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services.AddHttpClient("JellyfinClient", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["Jellyfin:Url"]);
                client.DefaultRequestHeaders.Add("X-Emby-Token", builder.Configuration["Jellyfin:ApiKey"]);
            });


            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            });

            var app = builder.Build();


            app.UseHttpsRedirection();


            app.MapControllers();

            app.Run("http://0.0.0.0:5002");
        }
    }
}
