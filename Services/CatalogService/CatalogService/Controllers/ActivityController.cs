using CatalogService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

[Route("api/catalog/activity")]
[ApiController]
public class ActivityController : ControllerBase
{
    private readonly IPostgresService _postgres;

    public ActivityController(IPostgresService postgres)
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
    public async Task<IActionResult> GetFeed([FromQuery] int page = 1)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var events = await _postgres.GetFeedAsync(userId, page);
        return Ok(events.Select(e => new
        {
            e.Id,
            e.EventType,
            e.MovieId,
            e.ReviewId,
            e.PlaylistId,
            e.CreatedAt,
            User = new { e.User.Id, e.User.Login, e.User.Username, e.User.AvatarUrl },
            Movie = e.Movie is null ? null : new
            {
                e.Movie.Id,
                e.Movie.Title,
                e.Movie.PosterPath,
                e.Movie.ReleaseDate,
                e.Movie.VoteAverage
            }
        }));
    }
}
