using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.People;
using TMDbLib.Objects.Search;

namespace CatalogService.Services
{
    public interface ITmdbService
    {
        Task<SearchContainer<SearchMovie>> SearchMoviesAsync(string query, string? lang, int page = 1);
        Task<Movie?> GetMovieDetailsAsync(int tmdbId);
        Task<Movie?> GetMovieFullDetailsAsync(int tmdbId);
        Task<Person?> GetPersonFullDetailsAsync(int personId);
        Task<SearchContainer<SearchMovie>> GetMoviePopulatListAsync(int page, string lang);
        Task<SearchContainer<SearchMovie>> GetMovieTopRatedListAsync(int page, string lang);
        Task<SearchContainer<SearchMovie>> GetMovieNowPlayingListAsync(int page, string lang);
        Task<SearchContainer<SearchMovie>> GetMovieRecommendationsAsync(int tmdbId, int page, string lang);
        Task<SearchContainer<SearchMovie>> GetMovieSimilarAsync(int tmdbId, int page, string lang);
        Task<Keyword?> GetKeywordAsync(int keywordId);
    }
}
