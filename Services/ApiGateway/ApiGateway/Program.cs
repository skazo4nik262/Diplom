namespace ApiGateway
{
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.IdentityModel.Tokens;
    using System.Text;

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);



            builder.Services.AddControllers();

            // Add CORS support for web clients
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
                        )
                    };
                });
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
            });
            builder.Services.AddReverseProxy()
                .LoadFromMemory(new[]
    #region маршруты
    {
        // Открытые маршруты \\
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "auth-login",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch
            {
                Path = "/api/auth/login"
            },
            ClusterId = "identity-cluster"
        },
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "auth-register",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch
            {
                Path = "/api/auth/register"
            },
            ClusterId = "identity-cluster"
        },
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "catalog-poster",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch
            {
                Path = "/api/catalog/poster/{**catch-all}"
            },
            ClusterId = "catalog-cluster",
        },



        // Защищённые маршруты \\
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "auth-profile",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch
            {
                Path = "/api/auth/profile"
            },
            ClusterId = "identity-cluster",
            AuthorizationPolicy = "Authenticated"
        },
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "auth-users-search",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch
            {
                Path = "/api/auth/users/search"
            },
            ClusterId = "identity-cluster",
            AuthorizationPolicy = "Authenticated"
        },
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "auth-users-id",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch
            {
                Path = "/api/auth/users/id/{**catch-all}"
            },
            ClusterId = "identity-cluster",
            AuthorizationPolicy = "Authenticated"
        },
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "jellyfin",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch
            {
                Path = "/api/jellyfin/{**catch-all}"
            },
            ClusterId = "jellyfin-cluster",
            AuthorizationPolicy = "Authenticated",
        },
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "catalog",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch
            {
                Path = "/api/catalog/{**catch-all}"
            },
            ClusterId = "catalog-cluster",
            AuthorizationPolicy = "Authenticated"
        },
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "catalog-users",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch
            {
                Path = "/api/catalog/users/{**catch-all}"
            },
            ClusterId = "catalog-cluster",
            AuthorizationPolicy = "Authenticated"
        }
    },
    #endregion
    #region кластеры
    new[]
    {
        new Yarp.ReverseProxy.Configuration.ClusterConfig
        {
            ClusterId = "identity-cluster",
            Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>(StringComparer.OrdinalIgnoreCase)
            {
                ["destination1"] = new Yarp.ReverseProxy.Configuration.DestinationConfig
                {
                    Address = "http://identity-service:5001"
                }
            }
        },
        new Yarp.ReverseProxy.Configuration.ClusterConfig
        {
            ClusterId = "jellyfin-cluster",
            Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>(StringComparer.OrdinalIgnoreCase)
            {
                ["destination1"] = new Yarp.ReverseProxy.Configuration.DestinationConfig
                {
                    Address = "http://jellyfin-service:5002"
                }
            }
        },
        new Yarp.ReverseProxy.Configuration.ClusterConfig
        {
            ClusterId = "catalog-cluster",
            Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>(StringComparer.OrdinalIgnoreCase)
            {
                ["destination1"] = new Yarp.ReverseProxy.Configuration.DestinationConfig
                {
                    Address = "http://catalog-service:5003"
                }
            }
        }
    }
    #endregion
    );

            var app = builder.Build();

            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseMiddleware<UserIdForwardingMiddleware>();

            app.MapControllers();
            app.MapReverseProxy();

            app.Run("http://0.0.0.0:5000");
        }
    }
}
