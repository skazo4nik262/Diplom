using AdminService.Data;
using AdminService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AdminService.Services;

public class LibraryScanner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LibraryScanner> _logger;

    private ScanState _state = new();
    private readonly object _lock = new();

    public LibraryScanner(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<LibraryScanner> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string StartScan()
    {
        lock (_lock)
        {
            if (_state.IsRunning)
                return "Сканирование уже выполняется";

            _state = new ScanState { IsRunning = true, StartedAt = DateTime.UtcNow };
        }

        _ = ScanAsync();
        return "Сканирование запущено";
    }

    public ScanState GetStatus() { lock (_lock) return _state with { }; }

    private async Task ScanAsync()
    {
        var videoExts = new[] { ".mkv", ".mp4", ".avi", ".mov", ".m4v", ".webm", ".wmv" };

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
            using var client = _httpClientFactory.CreateClient("Jellyfin");

            var mediaDir = "/media";
            if (!Directory.Exists(mediaDir))
            {
                _logger.LogWarning("Media directory '{Dir}' not found", mediaDir);
                SetCompleted(0, 0, 1, ["Директория /media не найдена"]);
                return;
            }

            var files = Directory.GetFiles(mediaDir)
                .Where(f => videoExts.Contains(Path.GetExtension(f)?.ToLowerInvariant()))
                .ToList();

            lock (_lock) _state.FilesFound = files.Count;

            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                var tmdbId = TryParseTmdbId(fileName);

                if (tmdbId == null)
                {
                    AddError($"Не удалось извлечь TmdbId из имени: {fileName}");
                    IncrementProcessed();
                    continue;
                }

                try
                {
                    var exists = await db.MovieFiles.AnyAsync(f =>
                        f.TmdbId == tmdbId.Value && f.FilePath == filePath);

                    if (exists)
                    {
                        IncrementProcessed();
                        continue;
                    }

                    var fileInfo = new FileInfo(filePath);

                    var movieFile = new MovieFileEntity
                    {
                        TmdbId = tmdbId.Value,
                        FileName = fileName,
                        FilePath = filePath,
                        SizeBytes = fileInfo.Length,
                        IsReady = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    db.MovieFiles.Add(movieFile);

                    var itemId = await FindJellyfinItemId(client, tmdbId.Value, fileName, filePath);
                    if (itemId.HasValue)
                    {
                        movieFile.JellyfinItemId = itemId;
                        movieFile.IsReady = true;
                    }

                    await db.SaveChangesAsync();

                    lock (_lock) _state.NewRecords++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing file {File}", filePath);
                    AddError($"Ошибка обработки {fileName}: {ex.Message}");
                }

                IncrementProcessed();
            }

            SetCompleted(files.Count, _state.NewRecords, _state.Errors.Count, null);
            _logger.LogInformation("Scan completed: {Found} files, {New} new records, {Errors} errors",
                _state.FilesFound, _state.NewRecords, _state.Errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan failed");
            SetCompleted(0, 0, 1, [$"Ошибка сканирования: {ex.Message}"]);
        }
    }

    private static int? TryParseTmdbId(string fileName)
    {
        var parts = fileName.Split(" - ", 2);
        if (parts.Length < 2) return null;

        if (int.TryParse(parts[0].Trim(), out var id))
            return id;

        return null;
    }

    private async Task<Guid?> FindJellyfinItemId(HttpClient client, int tmdbId, string fileName, string filePath)
    {
        var tmdbIdStr = tmdbId.ToString();

        string[] urls =
        [
            $"/Items?Recursive=true&SearchTerm={tmdbIdStr}&IncludeItemTypes=Movie&Fields=Path,ProviderIds&Limit=5",
            "/Items?Recursive=true&IncludeItemTypes=Movie&Fields=Path,ProviderIds&Limit=10&SortBy=DateCreated&SortOrder=Descending"
        ];

        foreach (var url in urls)
        {
            try
            {
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) continue;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var items = doc.RootElement.GetProperty("Items");

                foreach (var item in items.EnumerateArray())
                {
                    if (!ItemMatchesFile(item, tmdbIdStr, filePath, fileName, _logger))
                        continue;

                    var idStr = item.GetProperty("Id").GetString();
                    if (Guid.TryParse(idStr, out var guid))
                        return guid;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Jellyfin query failed: {Url}", url);
            }
        }

        return null;
    }

    private static bool ItemMatchesFile(JsonElement item, string tmdbIdStr, string filePath, string fileName, ILogger logger)
    {
        if (item.TryGetProperty("Path", out var pathProp))
        {
            var path = pathProp.GetString() ?? "";
            if (path.Contains(tmdbIdStr))
                return true;
        }

        if (item.TryGetProperty("Name", out var nameProp))
        {
            var name = nameProp.GetString() ?? "";
            if (name.Contains(tmdbIdStr))
                return true;
        }

        if (item.TryGetProperty("ProviderIds", out var providers) && providers.ValueKind == JsonValueKind.Object)
        {
            if (providers.TryGetProperty("Tmdb", out var tmdbIdProp))
            {
                if (tmdbIdProp.GetString() == tmdbIdStr)
                    return true;
            }
        }

        return false;
    }

    private void IncrementProcessed()
    {
        lock (_lock) _state.Processed++;
    }

    private void AddError(string error)
    {
        lock (_lock)
        {
            _state.Errors.Add(error);
        }
    }

    private void SetCompleted(int filesFound, int newRecords, int errorCount, List<string>? extraErrors)
    {
        lock (_lock)
        {
            _state.IsRunning = false;
            _state.IsComplete = true;
            _state.CompletedAt = DateTime.UtcNow;
            if (filesFound > 0) _state.FilesFound = filesFound;
            if (newRecords > 0) _state.NewRecords = newRecords;
            if (extraErrors != null)
                _state.Errors.AddRange(extraErrors);
        }
    }
}

public record ScanState
{
    public bool IsRunning { get; set; }
    public bool IsComplete { get; set; }
    public int FilesFound { get; set; }
    public int Processed { get; set; }
    public int NewRecords { get; set; }
    public List<string> Errors { get; set; } = [];
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
