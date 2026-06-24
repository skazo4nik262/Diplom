using BlazorServerRenderKinopoisk.Models;
using Flurl;
using Flurl.Http;

namespace BlazorServerRenderKinopoisk.Services;

public class CatalogService
{
    private readonly IFlurlClient _flurl;
    private readonly TokenStore _tokenStore;

    public CatalogService(IFlurlClient flurl, TokenStore tokenStore)
    {
        _flurl = flurl;
        _tokenStore = tokenStore;
    }

    public async Task<List<MovieDto>> GetPopularAsync(int page = 1)
    {
        try
        {
            return await _flurl.Request("api/catalog/movies/popular")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .SetQueryParam("page", page)
                .GetJsonAsync<List<MovieDto>>();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
        catch { return []; }
    }

    public async Task<List<MovieDto>> GetTopRatedAsync(int page = 1)
    {
        try
        {
            return await _flurl.Request("api/catalog/movies/top-rated")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .SetQueryParam("page", page)
                .GetJsonAsync<List<MovieDto>>();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
        catch { return []; }
    }

    public async Task<List<MovieDto>> SearchAsync(string query, int page = 1, string? genreIds = null,
        int? yearFrom = null, int? yearTo = null, double? ratingFrom = null, double? ratingTo = null,
        int? runtimeFrom = null, int? runtimeTo = null, string? sortBy = null, string? sortOrder = null)
    {
        try
        {
            var request = _flurl.Request("api/catalog/movies/search")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .SetQueryParam("query", query)
                .SetQueryParam("page", page);
            if (!string.IsNullOrEmpty(genreIds))
                request = request.SetQueryParam("genreIds", genreIds);
            if (yearFrom.HasValue) request = request.SetQueryParam("yearFrom", yearFrom.Value);
            if (yearTo.HasValue) request = request.SetQueryParam("yearTo", yearTo.Value);
            if (ratingFrom.HasValue) request = request.SetQueryParam("ratingFrom", ratingFrom.Value);
            if (ratingTo.HasValue) request = request.SetQueryParam("ratingTo", ratingTo.Value);
            if (runtimeFrom.HasValue) request = request.SetQueryParam("runtimeFrom", runtimeFrom.Value);
            if (runtimeTo.HasValue) request = request.SetQueryParam("runtimeTo", runtimeTo.Value);
            if (!string.IsNullOrEmpty(sortBy)) request = request.SetQueryParam("sortBy", sortBy);
            if (!string.IsNullOrEmpty(sortOrder)) request = request.SetQueryParam("sortOrder", sortOrder);
            return await request.GetJsonAsync<List<MovieDto>>();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 503)
        {
            throw new InvalidOperationException("TMDB недоступен");
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
        catch { return []; }
    }

    public async Task<List<MovieDto>> SearchByMoodAsync(string mood, int page = 1)
    {
        try
        {
            return await _flurl.Request($"api/catalog/movies/mood/{mood}")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .SetQueryParam("page", page)
                .GetJsonAsync<List<MovieDto>>();
        }
        catch { return []; }
    }

    public async Task<List<MovieDto>> SearchByImageAsync(string query, int page = 1)
    {
        try
        {
            return await _flurl.Request("api/catalog/movies/search-by-image")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .SetQueryParam("query", query)
                .SetQueryParam("page", page)
                .GetJsonAsync<List<MovieDto>>();
        }
        catch { return []; }
    }

    public async Task<MovieDto?> GetByIdAsync(int id)
    {
        try
        {
            return await _flurl.Request($"api/catalog/movies/{id}")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<MovieDto>();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
        catch { return null; }
    }

    public async Task<List<MovieDto>> GetSimilarAsync(int movieId, int page = 1)
    {
        try
        {
            return await _flurl.Request($"api/catalog/movies/{movieId}/similar")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .SetQueryParam("page", page)
                .GetJsonAsync<List<MovieDto>>();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
        catch { return []; }
    }

    public async Task<List<CastDto>> GetCreditsAsync(int movieId)
    {
        try
        {
            return await _flurl.Request($"api/catalog/movies/{movieId}/cast")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<List<CastDto>>();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
        catch { return []; }
    }

    public async Task<List<CrewDto>> GetCrewAsync(int movieId)
    {
        try
        {
            return await _flurl.Request($"api/catalog/movies/{movieId}/crew")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<List<CrewDto>>();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
        catch { return []; }
    }

    public async Task<CollectionDto?> GetCollectionAsync(int movieId)
    {
        try
        {
            return await _flurl.Request($"api/catalog/movies/{movieId}/collection")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<CollectionDto>();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
        catch { return null; }
    }

    public async Task<List<MovieDto>> GetRecommendationsAsync(int page = 1)
    {
        try
        {
            return await _flurl.Request("api/catalog/movies/recommendations")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .SetQueryParam("page", page)
                .GetJsonAsync<List<MovieDto>>();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
        catch { return []; }
    }

    public async Task<(int? Rating, string? Status, bool IsFavorite, double? LastPosition, double? Duration)> GetUserMovieStatusAsync(int movieId)
    {
        try
        {
            var response = await _flurl.Request($"api/catalog/user-movies/{movieId}/status")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<UserMovieStatusResponse>();
            return (response.Rating, response.Status, response.IsFavorite, response.LastPositionSeconds, response.DurationSeconds);
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
        catch { return (null, null, false, null, null); }
    }

    public async Task SaveProgressAsync(int movieId, double position, double duration)
    {
        try
        {
            await _flurl.Request($"api/catalog/user-movies/{movieId}/progress")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .PutJsonAsync(new { position, duration });
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
        catch { }
    }

    public async Task<List<UserMovieDto>> GetContinueWatchingAsync(int page = 1)
    {
        try
        {
            return await _flurl.Request("api/catalog/user-movies/continue-watching")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .SetQueryParam("page", page)
                .GetJsonAsync<List<UserMovieDto>>();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
        catch { return []; }
    }

    public async Task RateMovieAsync(int movieId, int rating)
    {
        try
        {
            await _flurl.Request($"api/catalog/user-movies/{movieId}/rate")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .PostJsonAsync(new { rating });
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
    }

    public async Task SetMovieStatusAsync(int movieId, string? status)
    {
        try
        {
            await _flurl.Request($"api/catalog/user-movies/{movieId}/status")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .PostJsonAsync(new { status });
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
    }

    public async Task<List<UserMovieDto>> GetUserMoviesAsync(string? status = null, int page = 1)
    {
        try
        {
            var request = _flurl.Request("api/catalog/user-movies")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .SetQueryParam("page", page);
            if (!string.IsNullOrEmpty(status))
                request = request.SetQueryParam("status", status);
            return await request.GetJsonAsync<List<UserMovieDto>>();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
        catch { return []; }
    }

    public async Task<List<MovieDto>> GetTrendingAsync(int page = 1)
    {
        try
        {
            return await _flurl.Request("api/catalog/movies/trending")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .SetQueryParam("page", page)
                .GetJsonAsync<List<MovieDto>>();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
        catch { return []; }
    }

    public async Task<CollectionDto?> GetCollectionByIdAsync(int collectionId)
    {
        try
        {
            return await _flurl.Request($"api/catalog/collections/{collectionId}")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<CollectionDto>();
        }
        catch { return null; }
    }

    public async Task<Dictionary<int, UserMovieStatusDto>> GetUserMoviesStatusBatchAsync(List<int> movieIds)
    {
        try
        {
            var ids = string.Join(",", movieIds);
            return await _flurl.Request("api/catalog/user-movies/batch-status")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .SetQueryParam("movieIds", ids)
                .GetJsonAsync<Dictionary<int, UserMovieStatusDto>>();
        }
        catch { return []; }
    }

    public async Task RemoveUserMovieAsync(int movieId)
    {
        try
        {
            await _flurl.Request($"api/catalog/user-movies/{movieId}")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .DeleteAsync();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
    }

    public async Task<PersonDto?> GetPersonAsync(int personId)
    {
        try
        {
            return await _flurl.Request($"api/catalog/people/{personId}")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<PersonDto>();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
        catch { return null; }
    }

    public async Task<List<MovieDto>> GetPersonMoviesAsync(int personId, int page = 1)
    {
        try
        {
            return await _flurl.Request($"api/catalog/people/{personId}/movies")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .SetQueryParam("page", page)
                .GetJsonAsync<List<MovieDto>>();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
        catch { return []; }
    }

    public async Task<PersonDto?> SearchPersonAsync(string query)
    {
        try
        {
            return await _flurl.Request("api/catalog/people/search")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .SetQueryParam("query", query)
                .GetJsonAsync<PersonDto>();
        }
        catch { return null; }
    }

    // TMDB genres — fetched from backend
    public async Task<List<GenreDto>> GetGenresAsync()
    {
        try
        {
            return await _flurl.Request("api/catalog/genres")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<List<GenreDto>>();
        }
        catch { return []; }
    }

    public async Task<List<GenreDto>> GetPopularGenresAsync(int count = 10)
    {
        try
        {
            return await _flurl.Request("api/catalog/genres/popular")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .SetQueryParam("count", count)
                .GetJsonAsync<List<GenreDto>>();
        }
        catch { return []; }
    }

    public async Task<List<PlaylistDto>> GetPlaylistsAsync()
    {
        try
        {
            return await _flurl.Request("api/catalog/playlists")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<List<PlaylistDto>>();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
        catch { return []; }
    }

    public async Task<PlaylistDto?> GetPlaylistAsync(int playlistId)
    {
        try
        {
            return await _flurl.Request($"api/catalog/playlists/{playlistId}")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<PlaylistDto>();
        }
        catch { return null; }
    }

    public async Task CreatePlaylistAsync(string name, string? description, List<int>? tmdbIds = null)
    {
        try
        {
            await _flurl.Request("api/catalog/playlists")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .PostJsonAsync(new { name, description, tmdbIds = tmdbIds ?? [] });
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
    }

    public async Task AddMovieToPlaylistAsync(int playlistId, int movieId)
    {
        try
        {
            await _flurl.Request($"api/catalog/playlists/{playlistId}/items")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .PostJsonAsync(new { tmdbId = movieId });
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
    }

    public async Task<List<MovieDto>> GetPlaylistSuggestionsAsync(int playlistId, int count = 5)
    {
        try
        {
            return await _flurl.Request($"api/catalog/playlists/{playlistId}/suggestions")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .SetQueryParam("count", count)
                .GetJsonAsync<List<MovieDto>>();
        }
        catch { return []; }
    }

    public async Task RemoveMovieFromPlaylistAsync(int playlistId, int movieId)
    {
        try
        {
            await _flurl.Request($"api/catalog/playlists/{playlistId}/items/{movieId}")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .DeleteAsync();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
    }

    public async Task<List<ReviewDto>> GetMovieReviewsAsync(int movieId)
    {
        try
        {
            return await _flurl.Request($"api/catalog/movies/{movieId}/reviews")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<List<ReviewDto>>();
        }
        catch { return []; }
    }

    public async Task<ReviewDto?> GetMyReviewAsync(int movieId)
    {
        try
        {
            return await _flurl.Request($"api/catalog/movies/{movieId}/reviews/my")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<ReviewDto>();
        }
        catch { return null; }
    }

    public async Task AddMovieReviewAsync(int movieId, string content, int? rating)
    {
        try
        {
            await _flurl.Request($"api/catalog/movies/{movieId}/reviews")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .PostJsonAsync(new { content, rating });
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
    }

    public async Task DeleteMovieReviewAsync(int movieId, string reviewId)
    {
        try
        {
            await _flurl.Request($"api/catalog/movies/{movieId}/reviews/{reviewId}")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .DeleteAsync();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
    }

    public async Task<Dictionary<int, int>> GetMovieRatingDistributionAsync(int movieId)
    {
        try
        {
            return await _flurl.Request($"api/catalog/movies/{movieId}/rating-distribution")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<Dictionary<int, int>>();
        }
        catch { return []; }
    }

    public async Task<List<UserMovieDto>> GetFavoritesAsync(int page = 1)
    {
        try
        {
            return await _flurl.Request("api/catalog/user-movies/favorites")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .SetQueryParam("page", page)
                .GetJsonAsync<List<UserMovieDto>>();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
        catch { return []; }
    }

    public async Task SetFavoriteAsync(int movieId)
    {
        try
        {
            await _flurl.Request($"api/catalog/user-movies/{movieId}/favorite")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .PostJsonAsync(new { });
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
    }

    public async Task RemoveFavoriteAsync(int movieId)
    {
        try
        {
            await _flurl.Request($"api/catalog/user-movies/{movieId}/favorite")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .DeleteAsync();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
    }

    public async Task<List<MovieDto>> GetKnownForMoviesAsync(int personId)
    {
        try
        {
            return await _flurl.Request($"api/catalog/people/{personId}/known-for")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<List<MovieDto>>();
        }
        catch { return []; }
    }

    private record UserMovieStatusResponse(int? Rating, string? Status, bool IsFavorite, double? LastPositionSeconds = null, double? DurationSeconds = null);

    public async Task<bool> DeletePlaylistAsync(int playlistId)
    {
        try
        {
            await _flurl.Request($"api/catalog/playlists/{playlistId}")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .DeleteAsync();
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> UpdatePlaylistAsync(int playlistId, string name, string? description)
    {
        try
        {
            await _flurl.Request($"api/catalog/playlists/{playlistId}")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .PutJsonAsync(new { name, description });
            return true;
        }
        catch { return false; }
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId)
    {
        try
        {
            return await _flurl.Request($"api/catalog/users/{userId}/profile")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<UserProfileDto>();
        }
        catch { return null; }
    }

    public async Task<UserStatsDto?> GetUserStatsAsync(Guid userId)
    {
        try
        {
            return await _flurl.Request($"api/catalog/users/{userId}/stats")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<UserStatsDto>();
        }
        catch { return null; }
    }

    public async Task<TasteDnaDto?> GetUserTasteDnaAsync(Guid userId)
    {
        try
        {
            return await _flurl.Request($"api/catalog/users/{userId}/taste-dna")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<TasteDnaDto>();
        }
        catch { return null; }
    }

    public async Task<List<ReviewCommentDto>> GetReviewCommentsAsync(int movieId, string reviewId)
    {
        try
        {
            return await _flurl.Request($"api/catalog/movies/{movieId}/reviews/{reviewId}/comments")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<List<ReviewCommentDto>>();
        }
        catch { return []; }
    }

    public async Task<List<ReviewCommentDto>> AddReviewCommentAsync(int movieId, string reviewId, string content)
    {
        try
        {
            return await _flurl.Request($"api/catalog/movies/{movieId}/reviews/{reviewId}/comments")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .PostJsonAsync(new { content })
                .ReceiveJson<List<ReviewCommentDto>>();
        }
        catch { return []; }
    }

    public async Task<ReviewLikesDto?> GetReviewLikesAsync(int movieId, string reviewId)
    {
        try
        {
            return await _flurl.Request($"api/catalog/movies/{movieId}/reviews/{reviewId}/likes")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<ReviewLikesDto>();
        }
        catch { return null; }
    }

    public async Task<bool> AddReviewLikeAsync(int movieId, string reviewId, bool isPositive)
    {
        try
        {
            await _flurl.Request($"api/catalog/movies/{movieId}/reviews/{reviewId}/likes")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .PostJsonAsync(new { isPositive });
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> RemoveReviewLikeAsync(int movieId, string reviewId)
    {
        try
        {
            await _flurl.Request($"api/catalog/movies/{movieId}/reviews/{reviewId}/likes")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .DeleteAsync();
            return true;
        }
        catch { return false; }
    }

    public async Task<List<VideoDto>> GetMovieVideosAsync(int movieId)
    {
        try
        {
            return await _flurl.Request($"api/catalog/movies/{movieId}/videos")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<List<VideoDto>>();
        }
        catch { return []; }
    }

    #region Friends
    public async Task<bool> FollowUserAsync(Guid targetUserId)
    {
        try
        {
            await _flurl.Request($"api/catalog/friends/{targetUserId}/follow")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .PostAsync();
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> UnfollowUserAsync(Guid targetUserId)
    {
        try
        {
            await _flurl.Request($"api/catalog/friends/{targetUserId}/unfollow")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .DeleteAsync();
            return true;
        }
        catch { return false; }
    }

    public async Task<List<UserBriefDto>> GetFollowingAsync()
    {
        try
        {
            return await _flurl.Request("api/catalog/friends/following")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<List<UserBriefDto>>();
        }
        catch { return []; }
    }

    public async Task<List<UserBriefDto>> GetFollowersAsync()
    {
        try
        {
            return await _flurl.Request("api/catalog/friends/followers")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<List<UserBriefDto>>();
        }
        catch { return []; }
    }

    public async Task<FollowStatusDto?> GetFollowStatusAsync(Guid targetUserId)
    {
        try
        {
            return await _flurl.Request($"api/catalog/friends/{targetUserId}/status")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<FollowStatusDto>();
        }
        catch { return null; }
    }
    #endregion

    #region Activity
    public async Task<List<ActivityEventDto>> GetFeedAsync(int page = 1)
    {
        try
        {
            return await _flurl.Request("api/catalog/activity")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .SetQueryParam("page", page)
                .GetJsonAsync<List<ActivityEventDto>>();
        }
        catch { return []; }
    }
    #endregion

    #region Notifications
    public async Task<NotificationListDto> GetNotificationsAsync(bool? unreadOnly = null, int page = 1)
    {
        try
        {
            return await _flurl.Request("api/catalog/notifications")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .SetQueryParam("unreadOnly", unreadOnly)
                .SetQueryParam("page", page)
                .GetJsonAsync<NotificationListDto>();
        }
        catch { return new NotificationListDto(); }
    }

    public async Task<bool> MarkNotificationReadAsync(Guid notificationId)
    {
        try
        {
            await _flurl.Request($"api/catalog/notifications/{notificationId}/read")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .PostAsync();
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> MarkAllNotificationsReadAsync()
    {
        try
        {
            await _flurl.Request("api/catalog/notifications/read-all")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .PostAsync();
            return true;
        }
        catch { return false; }
    }
    #endregion

    #region Diary
    public async Task<List<DiaryEntryDto>> GetDiaryAsync(int? year = null, int? month = null, int page = 1)
    {
        try
        {
            return await _flurl.Request("api/catalog/diary")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .SetQueryParam("year", year)
                .SetQueryParam("month", month)
                .SetQueryParam("page", page)
                .GetJsonAsync<List<DiaryEntryDto>>();
        }
        catch { return []; }
    }

    public async Task<bool> AddDiaryEntryAsync(int movieId, DateTime? watchedAt, int rating)
    {
        try
        {
            await _flurl.Request("api/catalog/diary")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .PostJsonAsync(new { movieId, watchedAt, rating });
            return true;
        }
        catch { return false; }
    }
    #endregion

    #region Taste comparison
    public async Task<TasteComparisonDto?> CompareWithUserAsync(Guid userId, Guid otherUserId)
    {
        try
        {
            return await _flurl.Request($"api/catalog/users/{userId}/compare/{otherUserId}")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<TasteComparisonDto>();
        }
        catch { return null; }
    }
    #endregion

    #region Notifications
    public async Task<NotificationSettingsDto> GetNotificationSettingsAsync()
    {
        try
        {
            return await _flurl.Request("api/catalog/users/notification-settings")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<NotificationSettingsDto>();
        }
        catch { return new NotificationSettingsDto(); }
    }

    public async Task SetNotificationSettingsAsync(bool? notifyNewInCollection, bool? notifyVideoAdded, bool? notifyFileAdded)
    {
        try
        {
            await _flurl.Request("api/catalog/users/notification-settings")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .PutJsonAsync(new { notifyNewInCollection, notifyVideoAdded, notifyFileAdded });
        }
        catch { }
    }
    #endregion

    #region Library Scan
    public async Task<string?> StartScanAsync()
    {
        try
        {
            var response = await _flurl.Request("api/admin/movies/scan")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .PostAsync();

            var json = await response.GetStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("message").GetString();
        }
        catch { return null; }
    }

    public async Task<ScanStatusDto?> GetScanStatusAsync()
    {
        try
        {
            return await _flurl.Request("api/admin/movies/scan/status")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<ScanStatusDto>();
        }
        catch { return null; }
    }
    #endregion

    #region Embeddings
    public async Task<string?> StartEmbeddingGenerationAsync(bool regenerateAll)
    {
        try
        {
            var url = regenerateAll
                ? "api/catalog/movies/embeddings/regenerate-all"
                : "api/catalog/movies/embeddings/generate-missing";

            var response = await _flurl.Request(url)
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .PostAsync();

            var json = await response.GetStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("message").GetString();
        }
        catch { return null; }
    }

    public async Task<EmbeddingGeneratorStatusDto?> GetEmbeddingStatusAsync()
    {
        try
        {
            return await _flurl.Request("api/catalog/movies/embeddings/status")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<EmbeddingGeneratorStatusDto>();
        }
        catch { return null; }
    }
    #endregion

    #region Admin
    public async Task<MovieFileDto?> GetMovieFileAsync(int tmdbId)
    {
        try
        {
            return await _flurl.Request($"api/admin/movies/{tmdbId}/file")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<MovieFileDto>();
        }
        catch { return null; }
    }

    public async Task<bool> DownloadViaTorrentAsync(int tmdbId, string magnetUrl)
    {
        try
        {
            await _flurl.Request("api/admin/movies/download")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .PostJsonAsync(new { tmdbId, magnetUrl });
            return true;
        }
        catch { return false; }
    }
    #endregion
}
