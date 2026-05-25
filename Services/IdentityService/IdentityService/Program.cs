using IdentityService.Data;
using IdentityService.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

            builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
            builder.Services.AddScoped<IIdentity, Identity>();
            builder.Services.AddScoped<ITokenService, TokenService>();

            builder.Services.AddControllers();

            var app = builder.Build();

            app.UseAuthorization();


            app.MapControllers();

            app.Run("http://0.0.0.0:5001");
        }
    }
}
