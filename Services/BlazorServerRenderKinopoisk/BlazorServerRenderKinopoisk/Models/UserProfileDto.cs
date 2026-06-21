using System.Text.Json.Serialization;

namespace BlazorServerRenderKinopoisk.Models;

public class UserProfileDto
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("login")] public string? Login { get; set; }
    [JsonPropertyName("username")] public string? Username { get; set; }
    [JsonPropertyName("bio")] public string? Bio { get; set; }
    [JsonPropertyName("avatarUrl")] public string? AvatarUrl { get; set; }
    [JsonPropertyName("reviews")] public List<UserReviewDto>? Reviews { get; set; }
    [JsonPropertyName("movieCount")] public int MovieCount { get; set; }
    [JsonPropertyName("watchedCount")] public int WatchedCount { get; set; }
    [JsonPropertyName("favoriteCount")] public int FavoriteCount { get; set; }
    [JsonPropertyName("plannedCount")] public int PlannedCount { get; set; }
    [JsonPropertyName("watchingCount")] public int WatchingCount { get; set; }
    [JsonPropertyName("droppedCount")] public int DroppedCount { get; set; }
}

public class UserReviewDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = null!;
    [JsonPropertyName("movieId")] public int MovieId { get; set; }
    [JsonPropertyName("content")] public string? Content { get; set; }
    [JsonPropertyName("authorRating")] public double? AuthorRating { get; set; }
    [JsonPropertyName("createdAt")] public DateTime? CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")] public DateTime? UpdatedAt { get; set; }
}

public class UserStatsDto
{
    [JsonPropertyName("totalMovies")] public int TotalMovies { get; set; }
    [JsonPropertyName("watchedMovies")] public int WatchedMovies { get; set; }
    [JsonPropertyName("totalHours")] public double TotalHours { get; set; }
    [JsonPropertyName("averageRating")] public double AverageRating { get; set; }
    [JsonPropertyName("topGenres")] public List<GenreCountDto>? TopGenres { get; set; }
}

public class GenreCountDto
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("count")] public int Count { get; set; }
}

public class ReviewCommentDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = null!;
    [JsonPropertyName("reviewId")] public string ReviewId { get; set; } = null!;
    [JsonPropertyName("authorName")] public string? AuthorName { get; set; }
    [JsonPropertyName("content")] public string? Content { get; set; }
    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
}

public class ReviewLikesDto
{
    [JsonPropertyName("likes")] public int Likes { get; set; }
    [JsonPropertyName("dislikes")] public int Dislikes { get; set; }
    [JsonPropertyName("userLike")] public bool? UserLike { get; set; }
}
