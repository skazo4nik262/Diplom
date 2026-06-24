using CatalogService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

[Route("api/catalog/genres")]
[ApiController]
public class GenresController : ControllerBase
{
    private readonly IPostgresService _postgres;

    public GenresController(IPostgresService postgres)
    {
        _postgres = postgres;
    }

    [HttpGet]
    public async Task<IActionResult> GetGenres()
    {
        var genres = await _postgres.GetGenresAsync();
        return Ok(genres);
    }

    [HttpGet("popular")]
    public async Task<IActionResult> GetPopularGenres([FromQuery] int count = 10)
    {
        var genres = await _postgres.GetPopularGenresAsync(count);
        return Ok(genres);
    }
}
