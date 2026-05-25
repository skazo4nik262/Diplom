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
}
