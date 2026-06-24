using CacheImageService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CacheImageService.Controllers;

[ApiController]
[Route("api/cache/image")]
public class CacheController : ControllerBase
{
    private readonly ImageCacheService _cache;

    public CacheController(ImageCacheService cache)
    {
        _cache = cache;
    }

    [HttpGet]
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
}
