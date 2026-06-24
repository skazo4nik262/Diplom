using CacheImageService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CacheImageService.Controllers;

[ApiController]
[Route("api/cache")]
public class CacheController : ControllerBase
{
    private readonly ImageCacheService _cache;

    public CacheController(ImageCacheService cache)
    {
        _cache = cache;
    }

    [HttpGet("image")]
    public async Task<IActionResult> GetImage([FromQuery] string path, [FromQuery] string size = "w500")
    {
        try
        {
            var bytes = await _cache.GetOrDownloadImageAsync(size, path);
            return File(bytes, "image/jpeg");
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpGet("avatar/{userId:guid}")]
    public async Task<IActionResult> GetAvatar(Guid userId)
    {
        try
        {
            var bytes = await _cache.GetOrCacheAvatarAsync(userId);
            if (bytes is null) return NotFound();
            return File(bytes, GetContentType(bytes));
        }
        catch
        {
            return NotFound();
        }
    }

    private static string GetContentType(byte[] bytes)
    {
        if (bytes.Length < 4) return "image/jpeg";
        if (bytes[0] == 0xFF && bytes[1] == 0xD8) return "image/jpeg";
        if (bytes[0] == 0x89 && bytes[1] == 0x50) return "image/png";
        if (bytes[0] == 0x47 && bytes[1] == 0x49) return "image/gif";
        if (bytes[0] == 0x52 && bytes[1] == 0x49) return "image/webp";
        return "image/jpeg";
    }
}
