using CatalogService.Data.Entities;
using CatalogService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

[Route("api/catalog/movies/{tmdbId:int}/reviews")]
[ApiController]
public class ReviewsController : ControllerBase
{
    private readonly IPostgresService _postgres;

    public ReviewsController(IPostgresService postgres)
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
    public async Task<IActionResult> GetReviews(int tmdbId)
    {
        var reviews = await _postgres.GetMovieReviewsAsync(tmdbId);
        return Ok(reviews);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyReview(int tmdbId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var review = await _postgres.GetUserMovieReviewAsync(userId, tmdbId);
        return Ok(review);
    }

    [HttpPost]
    public async Task<IActionResult> UpsertReview(int tmdbId, [FromBody] AddReviewRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var existing = await _postgres.GetUserMovieReviewAsync(userId, tmdbId);

        if (existing is not null)
        {
            existing.Content = request.Content;
            existing.AuthorRating = request.Rating;
            existing.UpdatedAt = DateTime.UtcNow;
            await _postgres.UpdateReviewAsync(existing);
            return Ok(existing);
        }

        var login = await _postgres.GetUserUsernameAsync(userId);
        var review = new ReviewEntity
        {
            Id = Guid.NewGuid().ToString(),
            MovieId = tmdbId,
            Author = login,
            AuthorRating = request.Rating,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            Iso639_1 = "ru"
        };

        await _postgres.AddReviewAsync(userId, tmdbId, review);
        await _postgres.RecordActivityAsync(userId, "review_written", tmdbId, review.Id);
        return Ok(review);
    }

    [HttpDelete("{reviewId}")]
    public async Task<IActionResult> DeleteReview(int tmdbId, string reviewId)
    {
        await _postgres.RemoveReviewAsync(reviewId);
        return Ok();
    }

    [HttpGet("{reviewId}/comments")]
    public async Task<IActionResult> GetComments(int tmdbId, string reviewId)
    {
        var comments = await _postgres.GetReviewCommentsAsync(reviewId);
        return Ok(comments);
    }

    [HttpPost("{reviewId}/comments")]
    public async Task<IActionResult> AddComment(int tmdbId, string reviewId, [FromBody] AddCommentRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var user = await _postgres.GetUserByIdAsync(userId);
        var authorName = (user?.Username ?? user?.Login) ?? "User";
        await _postgres.AddReviewCommentAsync(reviewId, userId, authorName, request.Content);
        await _postgres.RecordActivityAsync(userId, "comment_added", tmdbId, reviewId);
        // notify review author
        var reviews = await _postgres.GetMovieReviewsAsync(tmdbId);
        var review = reviews.FirstOrDefault(r => r.Id == reviewId);
        if (review?.UserId.HasValue == true && review.UserId.Value != userId)
            await _postgres.CreateNotificationAsync(review.UserId.Value, userId, "comment_added", tmdbId, reviewId);
        var comments = await _postgres.GetReviewCommentsAsync(reviewId);
        return Ok(comments);
    }

    [HttpGet("{reviewId}/likes")]
    public async Task<IActionResult> GetLikes(int tmdbId, string reviewId)
    {
        var counts = await _postgres.GetReviewLikesCountAsync(reviewId);
        var userId = GetUserId();
        bool? userLike = userId != Guid.Empty ? await _postgres.GetUserReviewLikeAsync(reviewId, userId) : null;
        return Ok(new { counts.Likes, counts.Dislikes, UserLike = userLike });
    }

    [HttpPost("{reviewId}/likes")]
    public async Task<IActionResult> AddLike(int tmdbId, string reviewId, [FromBody] AddLikeRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _postgres.AddOrUpdateReviewLikeAsync(reviewId, userId, request.IsPositive);
        await _postgres.RecordActivityAsync(userId, "review_liked", tmdbId, reviewId);
        // notify review author
        var reviews = await _postgres.GetMovieReviewsAsync(tmdbId);
        var review = reviews.FirstOrDefault(r => r.Id == reviewId);
        if (review?.UserId.HasValue == true && review.UserId.Value != userId)
            await _postgres.CreateNotificationAsync(review.UserId.Value, userId, "review_liked", tmdbId, reviewId);
        return Ok();
    }

    [HttpDelete("{reviewId}/likes")]
    public async Task<IActionResult> RemoveLike(int tmdbId, string reviewId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _postgres.RemoveReviewLikeAsync(reviewId, userId);
        return Ok();
    }
}

public record AddReviewRequest(string Content, int? Rating);
public record AddCommentRequest(string Content);
public record AddLikeRequest(bool IsPositive);
