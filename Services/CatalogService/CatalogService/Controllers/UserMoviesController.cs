using CatalogService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

[Route("api/catalog/user-movies")]
[ApiController]
public class UserMoviesController : ControllerBase
{
    private readonly IPostgresService _postgres;

    public UserMoviesController(IPostgresService postgres)
    {
        _postgres = postgres;
    }

    private Guid GetUserId()
    {
        if (Request.Headers.TryGetValue("X-User-Id", out var value) && Guid.TryParse(value, out var userId))
            return userId;
        return Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetUserMovies([FromQuery] string? status, [FromQuery] int page = 1)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var movies = await _postgres.GetUserMoviesAsync(userId, status, page);
        return Ok(movies);
    }

    [HttpPost("{tmdbId:int}/rate")]
    public async Task<IActionResult> RateMovie(int tmdbId, [FromBody] RateRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _postgres.RateMovieAsync(userId, tmdbId, request.Rating);
        return Ok();
    }

    [HttpPost("{tmdbId:int}/status")]
    public async Task<IActionResult> SetStatus(int tmdbId, [FromBody] StatusRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _postgres.SetMovieStatusAsync(userId, tmdbId, request.Status);
        return Ok();
    }

    [HttpDelete("{tmdbId:int}")]
    public async Task<IActionResult> RemoveMovie(int tmdbId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _postgres.RemoveUserMovieAsync(userId, tmdbId);
        return Ok();
    }
}

public record RateRequest(int Rating);
public record StatusRequest(string Status);
