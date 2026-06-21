using CatalogService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

[Route("api/catalog/users")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IPostgresService _postgres;

    public UsersController(IPostgresService postgres)
    {
        _postgres = postgres;
    }

    private Guid GetUserId()
    {
        if (Request.Headers.TryGetValue("X-User-Id", out var value) && Guid.TryParse(value, out var userId))
            return userId;
        return Guid.Empty;
    }

    [HttpGet("{userId:guid}/profile")]
    public async Task<IActionResult> GetUserProfile(Guid userId)
    {
        var user = await _postgres.GetUserByIdAsync(userId);
        if (user is null) return NotFound();

        var reviews = await _postgres.GetUserReviewsAsync(userId);
        var movies = await _postgres.GetUserMoviesAllAsync(userId);

        return Ok(new
        {
            user.Id,
            user.Login,
            user.Username,
            user.Bio,
            user.AvatarUrl,
            Reviews = reviews.Select(r => new
            {
                r.Id, r.MovieId, r.Content, r.AuthorRating, r.CreatedAt, r.UpdatedAt
            }),
            MovieCount = movies.Count,
            WatchedCount = movies.Count(m => m.Status == "watched"),
            FavoriteCount = movies.Count(m => m.Status == "favorite"),
            PlannedCount = movies.Count(m => m.Status == "planned"),
            WatchingCount = movies.Count(m => m.Status == "watching"),
            DroppedCount = movies.Count(m => m.Status == "dropped")
        });
    }

    [HttpGet("{userId:guid}/stats")]
    public async Task<IActionResult> GetUserStats(Guid userId)
    {
        var movies = await _postgres.GetUserMoviesAllAsync(userId);
        var watched = movies.Where(m => m.Status == "watched" || m.Rating.HasValue).ToList();
        var totalMinutes = watched.Sum(m => m.Movie?.Runtime ?? 0);
        var genreCounts = watched
            .SelectMany(m => m.Movie?.Genres ?? [])
            .GroupBy(g => g.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(5)
            .ToList();

        return Ok(new
        {
            TotalMovies = movies.Count,
            WatchedMovies = watched.Count,
            TotalHours = Math.Round(totalMinutes / 60.0, 1),
            AverageRating = watched.Where(m => m.Rating.HasValue).Select(m => m.Rating.Value).DefaultIfEmpty().Average(),
            TopGenres = genreCounts
        });
    }

    [HttpGet("{userId:guid}/compare/{otherUserId:guid}")]
    public async Task<IActionResult> Compare(Guid userId, Guid otherUserId)
    {
        var userMovies = await _postgres.GetUserMoviesAllAsync(userId);
        var otherMovies = await _postgres.GetUserMoviesAllAsync(otherUserId);

        var userMovieIds = userMovies.Select(m => m.MovieId).ToHashSet();
        var otherMovieIds = otherMovies.Select(m => m.MovieId).ToHashSet();

        var common = userMovieIds.Intersect(otherMovieIds).ToList();
        var commonWithRatings = common
            .Select(id => (
                MovieId: id,
                UserRating: userMovies.First(m => m.MovieId == id).Rating,
                OtherRating: otherMovies.First(m => m.MovieId == id).Rating
            ))
            .Where(x => x.UserRating.HasValue && x.OtherRating.HasValue)
            .ToList();

        var totalCommon = common.Count;
        var bothRatedCount = commonWithRatings.Count;
        var avgDiff = bothRatedCount > 0
            ? Math.Round(commonWithRatings.Average(x => Math.Abs((double)(x.UserRating!.Value - x.OtherRating!.Value))), 1)
            : 0;

        return Ok(new
        {
            TotalMoviesUser = userMovies.Count,
            TotalMoviesOther = otherMovies.Count,
            CommonMovies = totalCommon,
            BothRated = bothRatedCount,
            AverageRatingDifference = avgDiff,
            OverlapPercent = userMovies.Count > 0
                ? Math.Round((double)totalCommon / userMovies.Count * 100, 1) : 0
        });
    }
}
