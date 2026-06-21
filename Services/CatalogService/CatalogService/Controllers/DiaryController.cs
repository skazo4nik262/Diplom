using CatalogService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

[Route("api/catalog/diary")]
[ApiController]
public class DiaryController : ControllerBase
{
    private readonly IPostgresService _postgres;

    public DiaryController(IPostgresService postgres)
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
    public async Task<IActionResult> GetDiary([FromQuery] int? year, [FromQuery] int? month, [FromQuery] int page = 1)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var entries = await _postgres.GetDiaryAsync(userId, year, month, page);
        return Ok(entries.Select(e => new
        {
            e.MovieId,
            e.Rating,
            e.WatchedAt,
            e.CreatedAt,
            Movie = new
            {
                e.Movie.Id,
                e.Movie.Title,
                e.Movie.PosterPath,
                e.Movie.ReleaseDate,
                e.Movie.VoteAverage,
                e.Movie.Runtime,
                Genres = e.Movie.Genres!.Select(g => new { g.Id, g.Name })
            }
        }));
    }

    [HttpPost]
    public async Task<IActionResult> AddDiaryEntry([FromBody] AddDiaryEntryRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _postgres.SetMovieStatusAsync(userId, request.MovieId, "watched");
        if (request.Rating > 0)
            await _postgres.RateMovieAsync(userId, request.MovieId, request.Rating);

        await _postgres.RecordActivityAsync(userId, "movie_watched", request.MovieId);
        return Ok();
    }
}

public record AddDiaryEntryRequest(int MovieId, DateTime? WatchedAt, int Rating);
