using CatalogService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

[Route("api/catalog/people")]
[ApiController]
public class PeopleController : ControllerBase
{
    private readonly IPostgresService _postgres;
    private readonly ITmdbService _tmdb;

    public PeopleController(IPostgresService postgres, ITmdbService tmdb)
    {
        _postgres = postgres;
        _tmdb = tmdb;
    }

    [HttpGet("{personId:int}")]
    public async Task<IActionResult> GetPerson(int personId)
    {
        var person = await _postgres.GetPersonAsync(personId);
        if (person is not null) return Ok(person);

        var tmdbPerson = await _tmdb.GetPersonFullDetailsAsync(personId);
        if (tmdbPerson is null) return NotFound();

        await _postgres.AddPerson(tmdbPerson);
        person = await _postgres.GetPersonAsync(personId);
        return Ok(person);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Query parameter is required.");

        var person = await _postgres.SearchPersonAsync(query);
        return Ok(person);
    }
}
