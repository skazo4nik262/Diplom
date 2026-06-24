namespace CacheImageService.Services;

public class ImageCacheService
{
    private readonly string _cacheDir;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ImageCacheService> _logger;

    public ImageCacheService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<ImageCacheService> logger)
    {
        _cacheDir = config.GetValue<string>("ImageCache:Path") ?? "./cache/images";
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        Directory.CreateDirectory(_cacheDir);
    }

    public async Task<byte[]> GetOrDownloadImageAsync(string size, string imagePath, CancellationToken ct = default)
    {
        var cachePath = GetCachePath(size, imagePath);

        if (File.Exists(cachePath))
            return await File.ReadAllBytesAsync(cachePath, ct);

        var url = $"https://image.tmdb.org/t/p/{size}/{imagePath}";
        var client = _httpClientFactory.CreateClient("tmdb");
        var bytes = await client.GetByteArrayAsync(url, ct);

        var dir = Path.GetDirectoryName(cachePath);
        if (dir is not null) Directory.CreateDirectory(dir);

        await File.WriteAllBytesAsync(cachePath, bytes, ct);
        return bytes;
    }

    public async Task<byte[]?> GetOrCacheAvatarAsync(Guid userId)
    {
        var avatarCacheDir = Path.Combine(_cacheDir, "avatars");
        Directory.CreateDirectory(avatarCacheDir);
        var cachePath = Path.Combine(avatarCacheDir, $"{userId}.cache");

        if (File.Exists(cachePath))
            return await File.ReadAllBytesAsync(cachePath);

        var client = _httpClientFactory.CreateClient("identity");
        var extensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        foreach (var ext in extensions)
        {
            try
            {
                var response = await client.GetAsync($"/avatars/{userId}{ext}");
                if (!response.IsSuccessStatusCode) continue;

                var bytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(cachePath, bytes);
                return bytes;
            }
            catch
            {
                continue;
            }
        }

        return null;
    }

    private string GetCachePath(string size, string imagePath)
    {
        var safeName = imagePath.Replace("/", "_").Replace("\\", "_");
        return Path.Combine(_cacheDir, size, safeName);
    }
}
