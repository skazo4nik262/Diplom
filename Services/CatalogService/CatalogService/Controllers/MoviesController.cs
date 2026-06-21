using CatalogService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

[Route("api/catalog/movies")]
[ApiController]
public class MoviesController : ControllerBase
{
    private readonly IPostgresService _postgres;
    private readonly ITmdbService _tmdb;

    public MoviesController(IPostgresService postgres, ITmdbService tmdb)
    {
        _postgres = postgres;
        _tmdb = tmdb;
    }

    private Guid GetUserId()
    {
        if (Request.Headers.TryGetValue("X-User-Id", out var value) && Guid.TryParse(value, out var userId))
            return userId;
        return Guid.Empty;
    }

    [HttpGet("{tmdbId:int}")]
    public async Task<IActionResult> GetMovie(int tmdbId)
    {
        var movie = await _postgres.GetMovieWithDetailsAsync(tmdbId);
        if (movie is not null) return Ok(movie);

        var tmdbMovie = await _tmdb.GetMovieFullDetailsAsync(tmdbId);
        if (tmdbMovie is null) return NotFound();

        await _postgres.AddMovie(tmdbMovie);
        movie = await _postgres.GetMovieWithDetailsAsync(tmdbId);
        return Ok(movie);
    }

    [HttpGet("{tmdbId:int}/cast")]
    public async Task<IActionResult> GetCast(int tmdbId)
    {
        var cast = await _postgres.GetMovieCastAsync(tmdbId);
        if (cast.Count != 0) return Ok(cast);

        var movie = await _postgres.GetMovieAsync(tmdbId);
        if (movie is null)
        {
            var tmdbMovie = await _tmdb.GetMovieFullDetailsAsync(tmdbId);
            if (tmdbMovie is null) return NotFound();
            await _postgres.AddMovie(tmdbMovie);
        }

        cast = await _postgres.GetMovieCastAsync(tmdbId);
        return Ok(cast);
    }

    [HttpGet("{tmdbId:int}/crew")]
    public async Task<IActionResult> GetCrew(int tmdbId)
    {
        var crew = await _postgres.GetMovieCrewAsync(tmdbId);
        if (crew.Count != 0) return Ok(crew);

        var movie = await _postgres.GetMovieAsync(tmdbId);
        if (movie is null)
        {
            var tmdbMovie = await _tmdb.GetMovieFullDetailsAsync(tmdbId);
            if (tmdbMovie is null) return NotFound();
            await _postgres.AddMovie(tmdbMovie);
        }

        crew = await _postgres.GetMovieCrewAsync(tmdbId);
        return Ok(crew);
    }

    [HttpGet("{tmdbId:int}/collection")]
    public async Task<IActionResult> GetCollection(int tmdbId)
    {
        var collection = await _postgres.GetMovieCollectionAsync(tmdbId);
        if (collection is not null) return Ok(collection);

        var movie = await _postgres.GetMovieAsync(tmdbId);
        if (movie is null)
        {
            var tmdbMovie = await _tmdb.GetMovieFullDetailsAsync(tmdbId);
            if (tmdbMovie is null) return NotFound();
            await _postgres.AddMovie(tmdbMovie);
        }

        collection = await _postgres.GetMovieCollectionAsync(tmdbId);
        return Ok(collection);
    }

    [HttpGet("{tmdbId:int}/similar")]
    public async Task<IActionResult> GetSimilar(int tmdbId, [FromQuery] int page = 1)
    {
        var similar = await _postgres.GetSimilarMoviesAsync(tmdbId, page);
        return Ok(similar);
    }

    [HttpPost("embeddings/generate-missing")]
    public async Task<IActionResult> GenerateMissingEmbeddings()
    {
        var result = await _postgres.RebuildAllEmbeddingsAsync();
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string query,
        [FromQuery] int page = 1,
        [FromQuery] string? genreIds = null,
        [FromQuery] int? yearFrom = null,
        [FromQuery] int? yearTo = null,
        [FromQuery] double? ratingFrom = null,
        [FromQuery] double? ratingTo = null,
        [FromQuery] int? runtimeFrom = null,
        [FromQuery] int? runtimeTo = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null)
    {
        var parsedGenreIds = genreIds?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(id => int.TryParse(id, out var g) ? g : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        if (string.IsNullOrWhiteSpace(query) && (parsedGenreIds is null || parsedGenreIds.Count == 0)
            && !yearFrom.HasValue && !yearTo.HasValue && !ratingFrom.HasValue && !ratingTo.HasValue)
            return BadRequest("At least one filter parameter is required.");

        var results = await _postgres.SearchMoviesAsync(query, page, parsedGenreIds,
            yearFrom, yearTo, ratingFrom, ratingTo, runtimeFrom, runtimeTo, sortBy, sortOrder);
        if (results.Count != 0 || parsedGenreIds?.Count > 0) return Ok(results);

        try
        {
            var tmdbResults = await _tmdb.SearchMoviesAsync(query, "ru-RU", page);
            if (tmdbResults.Results.Count == 0) return Ok(results);

            var tmdbIds = tmdbResults.Results.Select(m => m.Id).ToList();

            foreach (var searchMovie in tmdbResults.Results)
            {
                var movie = await _tmdb.GetMovieFullDetailsAsync(searchMovie.Id);
                if (movie is not null)
                    await _postgres.AddMovie(movie);
            }

            results = await _postgres.GetMoviesBatchAsync(tmdbIds);
        }
        catch
        {
            return StatusCode(503);
        }

        return Ok(results);
    }

    [HttpGet("popular")]
    public async Task<IActionResult> GetPopular([FromQuery] int page = 1)
    {
        var movies = await _postgres.GetPopularMoviesAsync(page);
        return Ok(movies);
    }

    [HttpGet("top-rated")]
    public async Task<IActionResult> GetTopRated([FromQuery] int page = 1)
    {
        var movies = await _postgres.GetTopRatedMoviesAsync(page);
        return Ok(movies);
    }

    [HttpGet("trending")]
    public async Task<IActionResult> GetTrending([FromQuery] int page = 1)
    {
        var movies = await _postgres.GetTrendingMoviesAsync(page);
        return Ok(movies);
    }

    [HttpGet("recommendations")]
    public async Task<IActionResult> GetRecommendations([FromQuery] int page = 1)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var recommendations = await _postgres.GetRecommendationsAsync(userId, page);
        return Ok(recommendations);
    }

    [HttpGet("{tmdbId:int}/rating-distribution")]
    public async Task<IActionResult> GetRatingDistribution(int tmdbId)
    {
        var distribution = await _postgres.GetMovieRatingDistributionAsync(tmdbId);
        return Ok(distribution);
    }

    [HttpGet("{tmdbId:int}/videos")]
    public async Task<IActionResult> GetVideos(int tmdbId)
    {
        var videos = await _postgres.GetMovieVideosAsync(tmdbId);
        return Ok(videos);
    }

    [HttpGet("{tmdbId:int}/images")]
    public async Task<IActionResult> GetImages(int tmdbId, [FromQuery] string? type = null)
    {
        var images = await _postgres.GetMovieImagesAsync(tmdbId, type);
        return Ok(images);
    }
}
