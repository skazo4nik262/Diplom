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

    [HttpGet("{tmdbId:int}/status")]
    public async Task<IActionResult> GetUserMovieStatus(int tmdbId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var (rating, status, isFavorite, lastPosition, duration) = await _postgres.GetUserMovieExtendedStatusAsync(userId, tmdbId);
        return Ok(new { rating, status, isFavorite, lastPositionSeconds = lastPosition, durationSeconds = duration });
    }

    [HttpGet("{tmdbId:int}/progress")]
    public async Task<IActionResult> GetUserMovieProgress(int tmdbId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var (_, _, isFavorite, lastPosition, duration) = await _postgres.GetUserMovieExtendedStatusAsync(userId, tmdbId);
        return Ok(new { lastPositionSeconds = lastPosition, durationSeconds = duration });
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
        await _postgres.RecordActivityAsync(userId, "movie_rated", tmdbId);
        return Ok();
    }

    [HttpPost("{tmdbId:int}/status")]
    public async Task<IActionResult> SetStatus(int tmdbId, [FromBody] StatusRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _postgres.SetMovieStatusAsync(userId, tmdbId, request.Status);
        var eventType = request.Status switch
        {
            "watching" => "movie_watching",
            "watched" => "movie_watched",
            "planned" => "movie_planned",
            "dropped" => "movie_dropped",
            _ => (string?)null
        };
        if (eventType is not null)
            await _postgres.RecordActivityAsync(userId, eventType, tmdbId);
        return Ok();
    }

    [HttpGet("batch-status")]
    public async Task<IActionResult> GetBatchStatus([FromQuery] string? movieIds)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var parsedIds = movieIds?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(id => int.TryParse(id, out var g) ? g : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList() ?? [];

        var result = await _postgres.GetUserMoviesStatusBatchAsync(userId, parsedIds);
        var dto = result.ToDictionary(kv => kv.Key, kv => new { kv.Value.Rating, kv.Value.Status, kv.Value.IsFavorite, lastPositionSeconds = kv.Value.LastPosition, durationSeconds = kv.Value.Duration });
        return Ok(dto);
    }

    [HttpDelete("{tmdbId:int}")]
    public async Task<IActionResult> RemoveMovie(int tmdbId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _postgres.RemoveUserMovieAsync(userId, tmdbId);
        return Ok();
    }

    [HttpPost("{tmdbId:int}/favorite")]
    public async Task<IActionResult> SetFavorite(int tmdbId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _postgres.SetFavoriteAsync(userId, tmdbId);
        await _postgres.RecordActivityAsync(userId, "favorite_added", tmdbId);
        return Ok();
    }

    [HttpDelete("{tmdbId:int}/favorite")]
    public async Task<IActionResult> RemoveFavorite(int tmdbId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _postgres.RemoveFavoriteAsync(userId, tmdbId);
        return Ok();
    }

    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavorites([FromQuery] int page = 1)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var movies = await _postgres.GetUserMoviesAsync(userId, "favorite", page);
        return Ok(movies);
    }

    [HttpPut("{tmdbId:int}/progress")]
    public async Task<IActionResult> SetProgress(int tmdbId, [FromBody] ProgressRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _postgres.SetMovieProgressAsync(userId, tmdbId, request.Position, request.Duration);
        return Ok();
    }

    [HttpGet("continue-watching")]
    public async Task<IActionResult> GetContinueWatching([FromQuery] int page = 1)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var movies = await _postgres.GetContinueWatchingAsync(userId, page);
        return Ok(movies);
    }
}

public record RateRequest(int Rating);
public record StatusRequest(string Status);
public record ProgressRequest(double Position, double Duration);
