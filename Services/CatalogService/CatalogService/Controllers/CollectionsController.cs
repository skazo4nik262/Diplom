using CatalogService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

[Route("api/catalog/collections")]
[ApiController]
public class CollectionsController : ControllerBase
{
    private readonly IPostgresService _postgres;

    public CollectionsController(IPostgresService postgres)
    {
        _postgres = postgres;
    }

    [HttpGet("{collectionId:int}")]
    public async Task<IActionResult> GetCollection(int collectionId)
    {
        var collection = await _postgres.GetCollectionAsync(collectionId);
        if (collection is null) return NotFound();
        return Ok(collection);
    }
}
