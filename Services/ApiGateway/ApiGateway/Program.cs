namespace ApiGateway
{
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.IdentityModel.Tokens;
    using System.Text;
    using System.Security.Claims;
    using System.Collections.Concurrent;

    public class Program
    {
        private static readonly ConcurrentDictionary<Guid, (int Version, DateTime CachedAt)> _tokenVersionCache = new();

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddHttpClient("IdentityService", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["IdentityService:Url"] ?? "http://identity-service:5001");
            });

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

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                            var tokenVersionClaim = context.Principal?.FindFirst("tokenVersion")?.Value;

                            if (userIdClaim is null || tokenVersionClaim is null) return;

                            if (!Guid.TryParse(userIdClaim, out var userId)) return;
                            if (!int.TryParse(tokenVersionClaim, out var tokenVersion)) return;

                            var cacheKey = userId;
                            if (_tokenVersionCache.TryGetValue(cacheKey, out var cached))
                            {
                                if (DateTime.UtcNow - cached.CachedAt < TimeSpan.FromMinutes(5) && cached.Version == tokenVersion)
                                    return;
                            }

                            try
                            {
                                var factory = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                                var client = factory.CreateClient("IdentityService");
                                var response = await client.GetAsync($"/api/auth/users/{userId}/token-version");

                                if (!response.IsSuccessStatusCode)
                                {
                                    context.Fail("Token validation failed: user not found");
                                    return;
                                }

                                var result = await response.Content.ReadFromJsonAsync<TokenVersionResponse>();
                                if (result is null || result.TokenVersion != tokenVersion)
                                {
                                    context.Fail("Token validation failed: token version mismatch");
                                    return;
                                }

                                _tokenVersionCache[cacheKey] = (tokenVersion, DateTime.UtcNow);
                            }
                            catch
                            {
                                context.Fail("Token validation failed: identity service unavailable");
                            }
                        }
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
            RouteId = "auth-refresh",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch
            {
                Path = "/api/auth/refresh"
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
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "catalog-avatar",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch
            {
                Path = "/api/catalog/avatar/{**catch-all}"
            },
            ClusterId = "catalog-cluster",
        },

        // Открытый стриминг (ID фильма уже защищён аутентификацией при получении) \\
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "jellyfin-stream",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch
            {
                Path = "/api/jellyfin/Media/stream/{**catch-all}"
            },
            ClusterId = "jellyfin-cluster",
        },
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "jellyfin-hls",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch
            {
                Path = "/api/jellyfin/Media/hls/{**catch-all}"
            },
            ClusterId = "jellyfin-cluster",
        },

        // Защищённые маршруты \\
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "auth-avatar",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch
            {
                Path = "/api/auth/avatar"
            },
            ClusterId = "identity-cluster",
            AuthorizationPolicy = "Authenticated"
        },
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
            RouteId = "auth-users-reactivate",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch
            {
                Path = "/api/auth/users/{**catch-all}"
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
        },
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "admin",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch
            {
                Path = "/api/admin/{**catch-all}"
            },
            ClusterId = "admin-cluster",
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
        },
        new Yarp.ReverseProxy.Configuration.ClusterConfig
        {
            ClusterId = "admin-cluster",
            Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>(StringComparer.OrdinalIgnoreCase)
            {
                ["destination1"] = new Yarp.ReverseProxy.Configuration.DestinationConfig
                {
                    Address = "http://admin-service:5009"
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

    public record TokenVersionResponse(int TokenVersion);
}
