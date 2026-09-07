using System.Text.Json.Serialization;
using CatalogService.Services;
using CatalogService.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pgvector;

namespace CatalogService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Environment.SetEnvironmentVariable("PG_GSSENCMODE", "disable");
            var builder = WebApplication.CreateBuilder(args);

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(
                    builder.Configuration.GetConnectionString("Default"));
            dataSourceBuilder.UseVector();
            var dataSource = dataSourceBuilder.Build();


            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

            builder.Services.AddDbContext<TmdbDbContext>(options =>
                options.UseNpgsql(dataSource, o =>
                {
                    o.UseVector();
                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                }));

            builder.Services.AddScoped<ITmdbService, TmdbService>();
            builder.Services.AddScoped<IPostgresService, PostgresService>();
            builder.Services.AddSingleton<EmbeddingGeneratorService>();
            builder.Services.AddHttpClient();

            var embeddingUrl = builder.Configuration.GetValue<string>("EmbeddingService:Url") ?? "http://localhost:5005";
            builder.Services.AddScoped<IEmbeddingClient>(_ => new HttpEmbeddingClient(embeddingUrl));

            var cacheImageUrl = builder.Configuration.GetValue<string>("CacheImageService:Url") ?? "http://localhost:5007";
            builder.Services.AddScoped<ICacheImageClient>(_ => new CacheImageClient(cacheImageUrl));

            builder.Logging.AddConsole();
            builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Model.Validation", LogLevel.Error);
            builder.Logging.AddFilter("Npgsql", LogLevel.Warning);

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<TmdbDbContext>();
                    await db.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE \"Movies\" ADD COLUMN IF NOT EXISTS \"RefreshedAt\" timestamptz NULL");
                }
                catch { }
            }

            app.UseAuthorization();

            app.MapControllers();

            app.Run("http://0.0.0.0:5003");
        }
    }
}
