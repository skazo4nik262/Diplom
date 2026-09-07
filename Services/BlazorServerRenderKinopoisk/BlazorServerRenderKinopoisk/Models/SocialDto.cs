using System.Text.Json.Serialization;

namespace BlazorServerRenderKinopoisk.Models;

public class UserBriefDto
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("login")] public string? Login { get; set; }
    [JsonPropertyName("username")] public string? Username { get; set; }
    [JsonPropertyName("avatarUrl")] public string? AvatarUrl { get; set; }
    [JsonPropertyName("role")] public int Role { get; set; }
    [JsonPropertyName("isActive")] public bool IsActive { get; set; } = true;
    public string DisplayName => Username ?? Login ?? "User";
    public bool IsAdmin => Role == 0;
    public bool IsBlocked => !IsActive;
}

public class FollowStatusDto
{
    [JsonPropertyName("isFollowing")] public bool IsFollowing { get; set; }
    [JsonPropertyName("followingCount")] public int FollowingCount { get; set; }
    [JsonPropertyName("followerCount")] public int FollowerCount { get; set; }
}

public class ActivityEventDto
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("eventType")] public string? EventType { get; set; }
    [JsonPropertyName("movieId")] public int? MovieId { get; set; }
    [JsonPropertyName("reviewId")] public string? ReviewId { get; set; }
    [JsonPropertyName("playlistId")] public int? PlaylistId { get; set; }
    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
    [JsonPropertyName("user")] public UserBriefDto? User { get; set; }
    [JsonPropertyName("movie")] public MovieBriefDto? Movie { get; set; }

    public string EventText => EventType switch
    {
        "movie_watched" => "посмотрел(а) фильм",
        "movie_watching" => "начал(а) смотреть",
        "movie_planned" => "запланировал(а)",
        "movie_dropped" => "бросил(а)",
        "movie_rated" => "оценил(а)",
        "review_written" => "написал(а) рецензию",
        "review_liked" => "оценил(а) рецензию",
        "comment_added" => "оставил(а) комментарий",
        "favorite_added" => "добавил(а) в избранное",
        "followed_user" => "подписался(ась)",
        _ => EventType ?? ""
    };
}

public class MovieBriefDto
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("posterPath")] public string? PosterPath { get; set; }
    [JsonPropertyName("releaseDate")] public DateTime? ReleaseDate { get; set; }
    [JsonPropertyName("voteAverage")] public double VoteAverage { get; set; }
    public string PosterUrl => PosterPath is not null
        ? $"/api/catalog/poster/w500{PosterPath}" : "";
    public string? ReleaseYear => ReleaseDate?.Year.ToString();
}

public class NotificationItemDto
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("eventType")] public string? EventType { get; set; }
    [JsonPropertyName("actorId")] public Guid? ActorId { get; set; }
    [JsonPropertyName("movieId")] public int? MovieId { get; set; }
    [JsonPropertyName("reviewId")] public string? ReviewId { get; set; }
    [JsonPropertyName("playlistId")] public int? PlaylistId { get; set; }
    [JsonPropertyName("isRead")] public bool IsRead { get; set; }
    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
    [JsonPropertyName("movie")] public MovieBriefDto? Movie { get; set; }

    public string EventText => EventType switch
    {
        "review_liked" => "оценил вашу рецензию",
        "comment_added" => "прокомментировал вашу рецензию",
        "followed_user" => "подписался на вас",
        "new_in_collection" => "Вышел новый фильм в коллекции",
        "video_added" => "Добавлен новый трейлер",
        "file_added" => "Фильм доступен для просмотра",
        _ => EventType ?? ""
    };
}

public class NotificationListDto
{
    [JsonPropertyName("items")] public List<NotificationItemDto> Items { get; set; } = [];
    [JsonPropertyName("unreadCount")] public int UnreadCount { get; set; }
}

public class NotificationSettingsDto
{
    [JsonPropertyName("notifyNewInCollection")] public bool NotifyNewInCollection { get; set; } = true;
    [JsonPropertyName("notifyVideoAdded")] public bool NotifyVideoAdded { get; set; } = true;
    [JsonPropertyName("notifyFileAdded")] public bool NotifyFileAdded { get; set; } = true;
}

public class DiaryEntryDto
{
    [JsonPropertyName("movieId")] public int MovieId { get; set; }
    [JsonPropertyName("rating")] public int? Rating { get; set; }
    [JsonPropertyName("watchedAt")] public DateTime? WatchedAt { get; set; }
    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
    [JsonPropertyName("movie")] public MovieBriefDto? Movie { get; set; }
}

public class TasteComparisonDto
{
    [JsonPropertyName("totalMoviesUser")] public int TotalMoviesUser { get; set; }
    [JsonPropertyName("totalMoviesOther")] public int TotalMoviesOther { get; set; }
    [JsonPropertyName("commonMovies")] public int CommonMovies { get; set; }
    [JsonPropertyName("bothRated")] public int BothRated { get; set; }
    [JsonPropertyName("averageRatingDifference")] public double AverageRatingDifference { get; set; }
    [JsonPropertyName("overlapPercent")] public double OverlapPercent { get; set; }
}
