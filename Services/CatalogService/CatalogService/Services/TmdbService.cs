using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.People;
using TMDbLib.Objects.Search;

namespace CatalogService.Services
{
    public class TmdbService : ITmdbService
    {
        #region readonly поля
        private readonly TMDbClient _client;
        #endregion
        public TmdbService(IConfiguration config)
        {
            var apiKey = config["Tmdb:ApiKey"]
            ?? throw new InvalidOperationException("TMDB API Key is missing in configuration.");

            _client = new TMDbClient(apiKey);
            _client.DefaultLanguage = "ru-RU";
        }
        #region поиск
        public async Task<SearchContainer<SearchMovie>> SearchMoviesAsync(string query, string? lang, int page = 1)
        {
            return await _client.SearchMovieAsync(query, language: lang ?? "ru-RU", page: page);
        }

        public async Task<Movie?> GetMovieDetailsAsync(int tmdbId)
        {
            var methods = MovieMethods.Credits | MovieMethods.Images;
            return await _client.GetMovieAsync(tmdbId, methods);
        }

        public async Task<Movie?> GetMovieFullDetailsAsync(int tmdbId)
        {
            var methods = MovieMethods.Credits | MovieMethods.Images | MovieMethods.Videos |
                          MovieMethods.AlternativeTitles | MovieMethods.Keywords | MovieMethods.ReleaseDates;
            return await _client.GetMovieAsync(tmdbId, methods);
        }
        #endregion
        #region топ листы
        public async Task<SearchContainer<SearchMovie>> GetMoviePopulatListAsync(int page, string lang)
        {
            return await _client.GetMoviePopularListAsync(language: lang, page: page);
        }

        public async Task<SearchContainer<SearchMovie>> GetMovieTopRatedListAsync(int page, string lang)
        {
            return await _client.GetMovieTopRatedListAsync(language: lang, page: page);
        }

        public async Task<SearchContainer<SearchMovie>> GetMovieNowPlayingListAsync(int page, string lang)
        {
            return await _client.GetMovieNowPlayingListAsync(language: lang, page: page);
        }
        #endregion
        #region рекомендации
        public async Task<SearchContainer<SearchMovie>> GetMovieRecommendationsAsync(int tmdbId, int page, string lang) //более сложная
        {
            return await _client.GetMovieRecommendationsAsync(tmdbId, language: lang, page: page);
        }

        public async Task<SearchContainer<SearchMovie>> GetMovieSimilarAsync(int tmdbId, int page, string lang)
        {
            return await _client.GetMovieSimilarAsync(tmdbId, language: lang, page: page);
        }

        public async Task<Person?> GetPersonFullDetailsAsync(int personId)
        {
            var methods = PersonMethods.ExternalIds | PersonMethods.Images;
            return await _client.GetPersonAsync(personId, methods);
        }
        #endregion
    }
}