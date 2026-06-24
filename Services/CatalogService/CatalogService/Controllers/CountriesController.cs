using CatalogService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

[Route("api/catalog/countries")]
[ApiController]
public class CountriesController : ControllerBase
{
    private readonly IPostgresService _postgres;

    public CountriesController(IPostgresService postgres)
    {
        _postgres = postgres;
    }

    [HttpGet]
    public async Task<IActionResult> GetCountries()
    {
        var countries = await _postgres.GetCountriesAsync();
        return Ok(countries);
    }
}
