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

    public async Task<(int? Rating, string? Status)> GetUserMovieStatusAsync(int movieId)
    {
        try
        {
            var response = await _flurl.Request($"api/catalog/user-movies/{movieId}/status")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<UserMovieStatusResponse>();
            return (response.Rating, response.Status);
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
        catch { return (null, null); }
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

    // Hardcoded TMDB genres — no backend endpoint needed and they rarely change
    public static List<GenreDto> Genres =>
    [
        new() { Id = 28, Name = "Боевик" }, new() { Id = 12, Name = "Приключения" },
        new() { Id = 16, Name = "Мультфильм" }, new() { Id = 35, Name = "Комедия" },
        new() { Id = 80, Name = "Криминал" }, new() { Id = 99, Name = "Документальный" },
        new() { Id = 18, Name = "Драма" }, new() { Id = 10751, Name = "Семейный" },
        new() { Id = 14, Name = "Фэнтези" }, new() { Id = 36, Name = "История" },
        new() { Id = 27, Name = "Ужасы" }, new() { Id = 10402, Name = "Музыка" },
        new() { Id = 9648, Name = "Детектив" }, new() { Id = 10749, Name = "Мелодрама" },
        new() { Id = 878, Name = "Фантастика" }, new() { Id = 10770, Name = "ТВ-фильм" },
        new() { Id = 53, Name = "Триллер" }, new() { Id = 10752, Name = "Военный" },
        new() { Id = 37, Name = "Вестерн" }
    ];

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

    private record UserMovieStatusResponse(int? Rating, string? Status);

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
}
