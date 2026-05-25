using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

[ApiController]
[Route("api/catalog/poster")]
public class PosterController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public PosterController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("{size}/{**imagePath}")]
    public async Task<IActionResult> GetPoster(string size, string imagePath)
    {
        var url = $"https://image.tmdb.org/t/p/{size}/{imagePath}";
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode);

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
        var stream = await response.Content.ReadAsStreamAsync();
        return File(stream, contentType);
    }
}
