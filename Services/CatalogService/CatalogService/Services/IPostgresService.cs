using CatalogService.Data.Entities;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.People;
using TMDbLib.Objects.Reviews;

namespace CatalogService.Services
{
    public interface IPostgresService
    {
        #region всякие Get-ы
        Task<MovieEntity?> GetMovieAsync(int tmdbId);
        Task<MovieEntity?> GetMovieWithDetailsAsync(int tmdbId);
        Task<List<MovieEntity>> GetMoviesBatchAsync(IEnumerable<int> tmdbIds);
        Task<List<MovieEntity>> SearchMoviesAsync(string query, int page);
        Task<List<MovieEntity>> GetPopularMoviesAsync(int page);
        Task<List<MovieEntity>> GetTopRatedMoviesAsync(int page);
        Task<List<MovieCastEntity>> GetMovieCastAsync(int tmdbId);
        Task<List<MovieCrewEntity>> GetMovieCrewAsync(int tmdbId);
        Task<PersonEntity?> GetPersonAsync(int personId);
        Task<List<GenreEntity>> GetGenresAsync();
        Task<List<ProductionCompanyEntity>> GetCompaniesAsync();
        Task<List<MovieEntity>> GetRecommendationsAsync(Guid userId, int page);
        Task<List<MovieEntity>> GetUserTasteAsync(Guid userId, int page);
        Task<List<MovieEntity>> GetSimilarMoviesAsync(int tmdbId, int page);
        Task<List<UserMovieEntity>> GetUserMoviesAsync(Guid userId, string? status, int page);
        Task<List<UserPlaylistEntity>> GetPlaylistsAsync(Guid userId);
        Task<UserPlaylistEntity?> GetPlaylistAsync(int playlistId);
        Task<List<PersonEntity>> GetPeopleBatchAsync(IEnumerable<int> personIds);
        Task<PersonEntity?> SearchPersonAsync(string query);
        Task<CollectionEntity?> GetCollectionAsync(int collectionId);
        Task<CollectionEntity?> GetMovieCollectionAsync(int tmdbId);
        #endregion

        #region Add
        Task AddMovie(Movie movie);
        Task AddPerson(Person person);
        Task AddCollection(CollectionEntity collection);
        Task SetMovieStatusAsync(Guid userId, int tmdbId,  string status);
        Task AddReviewAsync(Guid userId, int tmdbId, ReviewEntity review);
        Task AddPlaylistAsync(Guid userId, string name, string? description, IEnumerable<int> tmdbIds);
        Task AddMediaToPlaylistAsync(int playlistId, int tmdbId);
        #endregion

        #region Update
        Task UpdateMovie(Movie movie, int tmdbId);
        Task UpdatePerson(Person person, int personId);
        Task UpdateCollection(CollectionEntity collection);
        Task RateMovieAsync(Guid userId, int tmdbId, int rating);

        #endregion

        #region Delete
        Task RemoveMovie(int tmdbId);
        Task RemovePerson(int personId);
        Task RemoveCollection(int collectionId);
        Task RemoveUserMovieAsync(Guid userId, int tmdbId);
        Task RemoveMediaFromPlaylistAsync(int playlistId, int tmdbId);
        #endregion

        #region Embeddings
        Task EnsureEmbeddingsAsync(int tmdbId);
        Task RebuildAllEmbeddingsAsync();
        #endregion
    }
}
