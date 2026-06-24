using CatalogService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

[ApiController]
[Route("api/catalog/poster")]
public class PosterController : ControllerBase
{
    private readonly ICacheImageClient _cache;

    public PosterController(ICacheImageClient cache)
    {
        _cache = cache;
    }

    [HttpGet("{size}/{**imagePath}")]
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
}
