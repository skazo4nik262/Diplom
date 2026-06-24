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
        Task<List<MovieEntity>> SearchMoviesAsync(string query, int page, List<int>? genreIds = null,
            int? yearFrom = null, int? yearTo = null, double? ratingFrom = null, double? ratingTo = null,
            int? runtimeFrom = null, int? runtimeTo = null, string? sortBy = null, string? sortOrder = null);
        Task<List<MovieEntity>> SearchByImageAsync(string query, int page, CancellationToken ct = default);
        Task<List<MovieEntity>> GetPersonMoviesAsync(int personId, int page);
        Task<List<MovieEntity>> GetPopularMoviesAsync(int page);
        Task<List<MovieEntity>> GetTopRatedMoviesAsync(int page);
        Task<List<MovieEntity>> GetTrendingMoviesAsync(int page);
        Task<Dictionary<int, (int? Rating, string? Status, bool IsFavorite, double? LastPosition, double? Duration)>> GetUserMoviesStatusBatchAsync(Guid userId, List<int> movieIds);
        Task<List<MovieCastEntity>> GetMovieCastAsync(int tmdbId);
        Task<List<MovieCrewEntity>> GetMovieCrewAsync(int tmdbId);
        Task<PersonEntity?> GetPersonAsync(int personId);
        Task<List<GenreEntity>> GetGenresAsync();
        Task<List<GenreEntity>> GetPopularGenresAsync(int count = 10);
        Task<List<ProductionCompanyEntity>> GetCompaniesAsync();
        Task<List<MovieEntity>> GetRecommendationsAsync(Guid userId, int page, bool useImage = false);
        Task<List<MovieEntity>> GetUserTasteAsync(Guid userId, int page);
        Task<List<MovieEntity>> GetSimilarMoviesAsync(int tmdbId, int page, bool useImage = false);
        Task<List<MovieEntity>> GetMoodMoviesAsync(string mood, int page);
        Task<List<UserMovieEntity>> GetUserMoviesAsync(Guid userId, string? status, int page);
        Task<List<UserPlaylistEntity>> GetPlaylistsAsync(Guid userId);
        Task<UserPlaylistEntity?> GetPlaylistAsync(int playlistId);
        Task<List<MovieEntity>> GetPlaylistSuggestionsAsync(int playlistId, int count = 5);
        Task<List<PersonEntity>> GetPeopleBatchAsync(IEnumerable<int> personIds);
        Task<PersonEntity?> SearchPersonAsync(string query);
        Task<CollectionEntity?> GetCollectionAsync(int collectionId);
        Task<CollectionEntity?> GetMovieCollectionAsync(int tmdbId);
        Task<List<ReviewEntity>> GetMovieReviewsAsync(int tmdbId);
        Task<Dictionary<int, int>> GetMovieRatingDistributionAsync(int tmdbId);
        Task<List<MovieEntity>> GetKnownForMoviesAsync(int personId);
        Task<List<VideoEntity>> GetMovieVideosAsync(int tmdbId);
        Task<List<ImageDataEntity>> GetMovieImagesAsync(int tmdbId, string? type = null);
        #endregion

        #region Add
        Task AddMovie(Movie movie);
        Task AddPerson(Person person);
        Task AddCollection(CollectionEntity collection);
        Task SetMovieStatusAsync(Guid userId, int tmdbId, string status);
        Task SetFavoriteAsync(Guid userId, int tmdbId);
        Task RemoveFavoriteAsync(Guid userId, int tmdbId);
        Task AddReviewAsync(Guid userId, int tmdbId, ReviewEntity review);
        Task AddPlaylistAsync(Guid userId, string name, string? description, IEnumerable<int> tmdbIds);
        Task AddMediaToPlaylistAsync(int playlistId, int tmdbId);
        Task<ReviewEntity?> GetUserMovieReviewAsync(Guid userId, int tmdbId);
        Task UpdateReviewAsync(ReviewEntity review);
        Task<string?> GetUserLoginAsync(Guid userId);
        Task<string?> GetUserUsernameAsync(Guid userId);
        Task AddReviewCommentAsync(string reviewId, Guid userId, string authorName, string content);
        Task AddOrUpdateReviewLikeAsync(string reviewId, Guid userId, bool isPositive);
        #endregion

        #region Notifications
        Task<(bool NotifyNewInCollection, bool NotifyVideoAdded, bool NotifyFileAdded)> GetNotificationSettingsAsync(Guid userId);
        Task SetNotificationSettingsAsync(Guid userId, bool? notifyNewInCollection, bool? notifyVideoAdded, bool? notifyFileAdded);
        Task CheckNewCollectionMoviesAsync();
        Task CheckNewCollectionMovieForMovieAsync(int tmdbId);
        Task CheckNewVideosAsync();
        Task CheckNewFilesAsync();
        #endregion

        #region Update
        Task UpdateMovie(Movie movie, int tmdbId);
        Task AttachKeywordsAsync(int tmdbId, List<TMDbLib.Objects.General.Keyword> keywords);
        Task UpdatePerson(Person person, int personId);
        Task UpdateCollection(CollectionEntity collection);
        Task UpdatePlaylistAsync(int playlistId, string name, string? description);
        Task RateMovieAsync(Guid userId, int tmdbId, int rating);
        Task<(int? Rating, string? Status, bool IsFavorite)> GetUserMovieStatusAsync(Guid userId, int tmdbId);
        Task SetMovieProgressAsync(Guid userId, int tmdbId, double position, double duration);
        Task<(int? Rating, string? Status, bool IsFavorite, double? LastPosition, double? Duration)> GetUserMovieExtendedStatusAsync(Guid userId, int tmdbId);
        Task<List<UserMovieEntity>> GetContinueWatchingAsync(Guid userId, int page = 1);

        #endregion

        #region Delete
        Task RemoveMovie(int tmdbId);
        Task RemovePerson(int personId);
        Task RemoveCollection(int collectionId);
        Task RemoveUserMovieAsync(Guid userId, int tmdbId);
        Task RemoveMediaFromPlaylistAsync(int playlistId, int tmdbId);
        Task RemoveReviewAsync(string reviewId);
        Task DeletePlaylistAsync(int playlistId);
        Task RemoveReviewLikeAsync(string reviewId, Guid userId);
        #endregion

        #region Queries
        Task<UserEntity?> GetUserByIdAsync(Guid userId);
        Task<List<UserEntity>> GetUserBatchAsync(IEnumerable<Guid> userIds);
        Task<List<ReviewEntity>> GetUserReviewsAsync(Guid userId, int page = 1);
        Task<List<UserMovieEntity>> GetUserMoviesAllAsync(Guid userId);
        Task<List<ReviewCommentEntity>> GetReviewCommentsAsync(string reviewId);
        Task<(int Likes, int Dislikes)> GetReviewLikesCountAsync(string reviewId);
        Task<bool?> GetUserReviewLikeAsync(string reviewId, Guid userId);
        Task<TasteDnaResult> GetUserTasteDnaAsync(Guid userId);
        #endregion

        #region Embeddings
        Task EnsureEmbeddingsAsync(int tmdbId);
        Task<List<int>> GetAllMovieIdsAsync();
        Task<List<int>> GetMovieIdsWithoutEmbeddingsAsync();
        Task ClearAllEmbeddingsAsync();
        #endregion

        #region Friends
        Task FollowUserAsync(Guid userId, Guid targetUserId);
        Task UnfollowUserAsync(Guid userId, Guid targetUserId);
        Task<bool> IsFollowingAsync(Guid userId, Guid targetUserId);
        Task<List<Guid>> GetFollowingIdsAsync(Guid userId);
        Task<List<Guid>> GetFollowerIdsAsync(Guid userId);
        Task<int> GetFollowingCountAsync(Guid userId);
        Task<int> GetFollowerCountAsync(Guid userId);
        #endregion

        #region Activity
        Task<List<ActivityEventEntity>> GetFeedAsync(Guid userId, int page = 1, int pageSize = 20);
        Task RecordActivityAsync(Guid userId, string eventType, int? movieId = null, string? reviewId = null, int? playlistId = null);
        #endregion

        #region Notifications
        Task<List<NotificationEntity>> GetNotificationsAsync(Guid userId, bool? unreadOnly = null, int page = 1, int pageSize = 20);
        Task<int> GetUnreadNotificationCountAsync(Guid userId);
        Task MarkNotificationReadAsync(Guid notificationId);
        Task MarkAllNotificationsReadAsync(Guid userId);
        Task CreateNotificationAsync(Guid userId, Guid? actorId, string eventType, int? movieId = null, string? reviewId = null, int? playlistId = null);
        #endregion

        #region Diary
        Task<List<UserMovieEntity>> GetDiaryAsync(Guid userId, int? year = null, int? month = null, int page = 1, int pageSize = 20);
        #endregion
    }

    public class TasteDnaItem
    {
        public string Name { get; set; } = null!;
        public double Score { get; set; }
    }

    public class TasteDnaResult
    {
        public List<TasteDnaItem> Genres { get; set; } = [];
        public List<TasteDnaItem> Keywords { get; set; } = [];
    }
}
