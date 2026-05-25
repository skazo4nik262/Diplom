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
        public static void Main(string[] args)
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
                options.UseNpgsql(dataSource, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

            builder.Services.AddScoped<ITmdbService, TmdbService>();
            builder.Services.AddScoped<IPostgresService, PostgresService>();

            var embeddingUrl = builder.Configuration.GetValue<string>("EmbeddingService:Url") ?? "http://localhost:5005";
            builder.Services.AddScoped<IEmbeddingClient>(_ => new HttpEmbeddingClient(embeddingUrl));

            builder.Logging.AddConsole();
            builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Model.Validation", LogLevel.Error);
            builder.Logging.AddFilter("Npgsql", LogLevel.Warning);

            var app = builder.Build();

            app.UseAuthorization();

            app.MapControllers();

            app.Run("http://0.0.0.0:5003");
        }
    }
}
