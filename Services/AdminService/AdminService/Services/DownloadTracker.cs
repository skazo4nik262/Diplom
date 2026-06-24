using AdminService.Data;
using AdminService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AdminService.Services;

public class DownloadTracker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DownloadTracker> _logger;

    public DownloadTracker(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<DownloadTracker> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDownloads(stoppingToken);
                await ResolveMovieFiles(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DownloadTracker error");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private async Task ProcessDownloads(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        var transmissionClient = scope.ServiceProvider.GetRequiredService<TransmissionClient>();

        var activeJobs = await db.DownloadJobs
            .Where(j => j.Status == "downloading" || j.Status == "error")
            .ToListAsync(ct);

        if (activeJobs.Count == 0) return;

        List<TorrentInfo> allTorrents = [];
        try
        {
            allTorrents = await transmissionClient.GetTorrentsAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get torrents from Transmission");
            return;
        }

        foreach (var job in activeJobs)
        {
            var torrent = allTorrents.FirstOrDefault(t => t.Id == job.TransmissionId);

            if (torrent == null)
            {
                job.Status = "error";
                _logger.LogWarning("Torrent {Id} not found for job {JobId}", job.TransmissionId, job.Id);
                continue;
            }

            if (!torrent.IsComplete) continue;

            var videoExts = new[] { ".mkv", ".mp4", ".avi", ".mov", ".m4v", ".webm", ".wmv" };

            try
            {
                foreach (var file in torrent.Files)
                {
                    if (file.BytesCompleted < file.Length) continue;

                    var ext = Path.GetExtension(file.Name)?.ToLowerInvariant();
                    if (ext == null || !videoExts.Contains(ext)) continue;

                    var actualFileName = Path.GetFileName(file.Name);
                    var srcPath = Path.Combine("/media", "torrents", file.Name);
                    var destFileName = $"{job.TmdbId} - {actualFileName}";
                    var destPath = Path.Combine("/media", destFileName);

                    if (System.IO.File.Exists(destPath))
                    {
                        _logger.LogInformation("File already exists: {Path}", destPath);
                        continue;
                    }

                    var dir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);

                    System.IO.File.Move(srcPath, destPath);
                    _logger.LogInformation("Moved {Src} -> {Dst}", srcPath, destPath);

                    var fileInfo = new FileInfo(destPath);

                    var movieFile = new MovieFileEntity
                    {
                        TmdbId = job.TmdbId,
                        FileName = destFileName,
                        FilePath = destPath,
                        SizeBytes = fileInfo.Length,
                        IsReady = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    db.MovieFiles.Add(movieFile);
                }

                job.Status = "done";

                using var client = _httpClientFactory.CreateClient("Jellyfin");
                await client.PostAsync("/Library/Refresh", null, ct);

                await transmissionClient.RemoveTorrentAsync(job.TransmissionId!.Value, false, ct);

                _logger.LogInformation("Download complete for TmdbId={TmdbId}", job.TmdbId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process download for TmdbId={TmdbId}", job.TmdbId);
                job.Status = "error";
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task ResolveMovieFiles(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        var pending = await db.MovieFiles
            .Where(f => !f.IsReady)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        using var client = _httpClientFactory.CreateClient("Jellyfin");

        foreach (var file in pending)
        {
            var itemId = await FindJellyfinItemId(client, file, ct);
            if (itemId.HasValue)
            {
                file.JellyfinItemId = itemId;
                file.IsReady = true;
                _logger.LogInformation("Resolved TmdbId={TmdbId} -> JellyfinItemId={ItemId}", file.TmdbId, itemId);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<Guid?> FindJellyfinItemId(HttpClient client, MovieFileEntity file, CancellationToken ct)
    {
        string[] urls =
        [
            $"/Items?Recursive=true&SearchTerm={file.TmdbId}&IncludeItemTypes=Movie&Fields=Path,ProviderIds&Limit=5",
            "/Items?Recursive=true&IncludeItemTypes=Movie&Fields=Path,ProviderIds&Limit=10&SortBy=DateCreated&SortOrder=Descending"
        ];

        foreach (var url in urls)
        {
            try
            {
                var response = await client.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode) continue;

                var json = await response.Content.ReadAsStringAsync(ct);
                _logger.LogInformation("Jellyfin response: {Json}", json);
                using var doc = JsonDocument.Parse(json);
                var items = doc.RootElement.GetProperty("Items");

                foreach (var item in items.EnumerateArray())
                {
                    if (!ItemMatchesFile(item, file, _logger)) continue;

                    var idStr = item.GetProperty("Id").GetString();
                    _logger.LogInformation("Matched item, Id='{Id}'", idStr);
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

    private static bool ItemMatchesFile(JsonElement item, MovieFileEntity file, ILogger logger = null)
    {
        var tmdbIdStr = file.TmdbId.ToString();

        if (item.TryGetProperty("Path", out var pathProp))
        {
            var path = pathProp.GetString() ?? "";
            if (path.Contains(tmdbIdStr))
                return true;
            logger?.LogDebug("Path '{Path}' does not contain '{TmdbId}'", path, tmdbIdStr);
        }

        if (item.TryGetProperty("Name", out var nameProp))
        {
            var name = nameProp.GetString() ?? "";
            if (name.Contains(tmdbIdStr))
                return true;
            logger?.LogDebug("Name '{Name}' does not contain '{TmdbId}'", name, tmdbIdStr);
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
}
