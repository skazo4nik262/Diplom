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

    public async Task<List<MovieDto>> SearchAsync(string query, int page = 1)
    {
        try
        {
            return await _flurl.Request("api/catalog/movies/search")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .SetQueryParam("query", query)
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

    public async Task<int?> GetUserRatingAsync(int movieId)
    {
        try
        {
            var response = await _flurl.Request($"api/catalog/user-movies/{movieId}/rating")
                .WithOAuthBearerToken(_tokenStore.Token ?? "")
                .GetJsonAsync<UserRatingResponse>();
            return response.Rating;
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            await _tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Token expired or invalid");
        }
        catch { return null; }
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

    private record UserRatingResponse(int? Rating);
}
