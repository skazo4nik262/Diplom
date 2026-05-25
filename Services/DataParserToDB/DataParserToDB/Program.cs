using Microsoft.EntityFrameworkCore;
using DataParserToDB.Data;
using Npgsql;
using Pgvector;

var connectionString = Environment.GetEnvironmentVariable("TMDB_CONNECTION_STRING")
    ?? "Host=localhost:54320;Database=mydb;Username=skazo4nik;Password=qaz123wsx";

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.UseVector();
var dataSource = dataSourceBuilder.Build();

var options = new DbContextOptionsBuilder<TmdbDbContext>()
    .UseNpgsql(dataSource)
    .Options;

await using var db = new TmdbDbContext(options);

Console.WriteLine("Deleting database schema");
await db.Database.EnsureDeletedAsync();
Console.WriteLine("Database schema deleted successfully");

Console.WriteLine("Creating database schema");
await db.Database.EnsureCreatedAsync();
Console.WriteLine("Database schema created successfully!");
