using Flurl.Http;

namespace CatalogService.Services;

public interface ICacheImageClient
{
    Task<byte[]> GetImageAsync(string path, string size = "w500");
    Task<byte[]?> GetAvatarAsync(Guid userId);
}

public class CacheImageClient(string baseUrl) : ICacheImageClient
{
    private static readonly FlurlClient Client = new(
        new HttpClient(new SocketsHttpHandler
        {
            UseProxy = false,
            Proxy = null
        })
    );

    public async Task<byte[]> GetImageAsync(string path, string size = "w500")
    {
        return await Client
            .Request($"{baseUrl}/api/cache/image")
            .SetQueryParams(new { path, size })
            .GetBytesAsync();
    }

    public async Task<byte[]?> GetAvatarAsync(Guid userId)
    {
        try
        {
            return await Client
                .Request($"{baseUrl}/api/cache/avatar/{userId}")
                .GetBytesAsync();
        }
        catch
        {
            return null;
        }
    }
}
