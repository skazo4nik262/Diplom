using CatalogService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

[ApiController]
[Route("api/catalog")]
public class PosterController : ControllerBase
{
    private readonly ICacheImageClient _cache;

    public PosterController(ICacheImageClient cache)
    {
        _cache = cache;
    }

    [HttpGet("poster/{size}/{**imagePath}")]
    public async Task<IActionResult> GetPoster(string size, string imagePath)
    {
        try
        {
            var bytes = await _cache.GetImageAsync(imagePath, size);
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
            var bytes = await _cache.GetAvatarAsync(userId);
            if (bytes is null) return NotFound();
            return File(bytes, "image/jpeg");
        }
        catch
        {
            return NotFound();
        }
    }
}
