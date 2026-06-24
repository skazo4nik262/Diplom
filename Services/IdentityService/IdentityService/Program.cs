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
            builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();

            builder.Services.AddControllers();

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
            });

            var app = builder.Build();

            var avatarsDir = Path.Combine(app.Environment.ContentRootPath, "avatars");
            Directory.CreateDirectory(avatarsDir);

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(avatarsDir),
                RequestPath = "/avatars",
                ServeUnknownFileTypes = false,
                ContentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider
                {
                    Mappings =
                    {
                        [".jpg"] = "image/jpeg",
                        [".jpeg"] = "image/jpeg",
                        [".png"] = "image/png",
                        [".gif"] = "image/gif",
                        [".webp"] = "image/webp",
                    }
                }
            });

            app.UseAuthorization();

            app.MapControllers();

            app.Run("http://0.0.0.0:5001");
        }
    }
}
