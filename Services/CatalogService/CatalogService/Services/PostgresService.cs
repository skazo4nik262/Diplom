using System.Globalization;
using CatalogService.Data;
using CatalogService.Data.Entities;
using CatalogService.Data.Mappers;
using Microsoft.EntityFrameworkCore;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.People;
using TMDbLib.Objects.Reviews;

namespace CatalogService.Services
{
    public class PostgresService : IPostgresService
    {
        private readonly TmdbDbContext _db;
        private readonly IEmbeddingClient _embedding;
        private const int PageSize = 20;

        public PostgresService(TmdbDbContext db, IEmbeddingClient embedding)
        {
            _db = db;
            _embedding = embedding;
        }

        public async Task<MovieEntity?> GetMovieAsync(int tmdbId)
        {
            return await _db.Movies
                .Include(m => m.Collection)
                .Include(m => m.ExternalIds)
                .Include(m => m.Genres)
                .FirstOrDefaultAsync(m => m.Id == tmdbId);
        }

        public async Task<MovieEntity?> GetMovieWithDetailsAsync(int tmdbId)
        {
            return await _db.Movies
                .Include(m => m.Collection)
                .Include(m => m.ExternalIds)
                .Include(m => m.Genres)
                .Include(m => m.ProductionCompanies)
                .Include(m => m.ProductionCountries)
                .Include(m => m.SpokenLanguages)
                .Include(m => m.Keywords)
                .Include(m => m.Cast)
                .Include(m => m.Crew)
                .Include(m => m.Videos)
                .Include(m => m.AlternativeTitles)
                .Include(m => m.ReleaseDates)
                .Include(m => m.Reviews)
                .Include(m => m.Images)
                .FirstOrDefaultAsync(m => m.Id == tmdbId);
        }

        public async Task<List<MovieEntity>> GetMoviesBatchAsync(IEnumerable<int> tmdbIds)
        {
            return await _db.Movies
                .Include(m => m.Collection)
                .Include(m => m.ExternalIds)
                .Include(m => m.Genres)
                .Where(m => tmdbIds.Contains(m.Id))
                .ToListAsync();
        }

        public async Task<List<MovieEntity>> SearchMoviesAsync(string query, int page, List<int>? genreIds = null,
            int? yearFrom = null, int? yearTo = null, double? ratingFrom = null, double? ratingTo = null,
            int? runtimeFrom = null, int? runtimeTo = null, string? sortBy = null, string? sortOrder = null)
        {
            var baseQuery = _db.Movies
                .Include(m => m.Collection)
                .Include(m => m.Genres)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                baseQuery = baseQuery.Where(m => m.Title != null && EF.Functions.ILike(m.Title, $"%{query}%")
                                             || m.OriginalTitle != null && EF.Functions.ILike(m.OriginalTitle, $"%{query}%"));
            }

            if (genreIds?.Count > 0)
                baseQuery = baseQuery.Where(m => m.Genres.Any(g => genreIds.Contains(g.Id)));

            if (yearFrom.HasValue)
            {
                var from = new DateTime(yearFrom.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                baseQuery = baseQuery.Where(m => m.ReleaseDate >= from);
            }
            if (yearTo.HasValue)
            {
                var to = new DateTime(yearTo.Value, 12, 31, 23, 59, 59, DateTimeKind.Utc);
                baseQuery = baseQuery.Where(m => m.ReleaseDate <= to);
            }
            if (ratingFrom.HasValue)
                baseQuery = baseQuery.Where(m => m.VoteAverage >= ratingFrom.Value);
            if (ratingTo.HasValue)
                baseQuery = baseQuery.Where(m => m.VoteAverage <= ratingTo.Value);
            if (runtimeFrom.HasValue)
                baseQuery = baseQuery.Where(m => m.Runtime >= runtimeFrom.Value);
            if (runtimeTo.HasValue)
                baseQuery = baseQuery.Where(m => m.Runtime <= runtimeTo.Value);

            baseQuery = (sortBy?.ToLower(), sortOrder?.ToLower()) switch
            {
                ("rating", "asc") => baseQuery.OrderBy(m => m.VoteAverage),
                ("rating", _) => baseQuery.OrderByDescending(m => m.VoteAverage),
                ("year", "asc") => baseQuery.OrderBy(m => m.ReleaseDate),
                ("year", _) => baseQuery.OrderByDescending(m => m.ReleaseDate),
                ("title", "asc") => baseQuery.OrderBy(m => m.Title),
                ("title", _) => baseQuery.OrderByDescending(m => m.Title),
                ("runtime", "asc") => baseQuery.OrderBy(m => m.Runtime),
                ("runtime", _) => baseQuery.OrderByDescending(m => m.Runtime),
                _ => baseQuery.OrderByDescending(m => m.Popularity)
            };

            return await baseQuery
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

        public async Task<List<MovieEntity>> GetPopularMoviesAsync(int page)
        {
            return await _db.Movies
                .Include(m => m.Collection)
                .Include(m => m.Genres)
                .OrderByDescending(m => m.Popularity)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

        public async Task AddCollection(CollectionEntity collection)
        {
            var existing = await _db.Collections.FindAsync(collection.Id);
            if (existing is not null) return;
            _db.Collections.Add(collection);
            await _db.SaveChangesAsync();
        }

        public async Task AddMediaToPlaylistAsync(int playlistId, int tmdbId)
        {
            var exists = await _db.UserPlaylistItems
                .AnyAsync(i => i.PlaylistId == playlistId && i.MovieId == tmdbId);
            if (exists) return;

            var maxOrder = await _db.UserPlaylistItems
                .Where(i => i.PlaylistId == playlistId)
                .MaxAsync(i => (int?)i.Order) ?? 0;

            _db.UserPlaylistItems.Add(new UserPlaylistItemEntity
            {
                PlaylistId = playlistId,
                MovieId = tmdbId,
                Order = maxOrder + 1,
                AddedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        public async Task AddMovie(Movie movie)
        {
            var existing = await _db.Movies.FindAsync(movie.Id);
            if (existing is not null) return;

            var entity = movie.ToEntity();
            entity.ReleaseDate = NormalizeUtc(entity.ReleaseDate);

            if (entity.Collection is not null)
            {
                var existingCollection = await _db.Collections.FindAsync(entity.Collection.Id);
                if (existingCollection is not null)
                    entity.Collection = null;
            }

            foreach (var genre in movie.Genres ?? new List<Genre>())
            {
                var g = await _db.Genres.FindAsync(genre.Id);
                entity.Genres.Add(g ?? genre.ToEntity());
            }

            foreach (var company in movie.ProductionCompanies ?? new List<ProductionCompany>())
            {
                var c = await _db.ProductionCompanies.FindAsync(company.Id);
                entity.ProductionCompanies.Add(c ?? company.ToEntity());
            }

            foreach (var country in movie.ProductionCountries ?? new List<ProductionCountry>())
            {
                var c = await _db.ProductionCountries.FindAsync(country.Iso_3166_1);
                entity.ProductionCountries.Add(c ?? country.ToEntity());
            }

            foreach (var lang in movie.SpokenLanguages ?? new List<SpokenLanguage>())
            {
                var l = await _db.SpokenLanguages.FindAsync(lang.Iso_639_1);
                entity.SpokenLanguages.Add(l ?? lang.ToEntity());
            }

            foreach (var keyword in movie.Keywords?.Keywords ?? new List<Keyword>())
            {
                var k = await _db.Keywords.FindAsync(keyword.Id);
                entity.Keywords.Add(k ?? keyword.ToEntity());
            }

            if (movie.Credits?.Cast is not null || movie.Credits?.Crew is not null)
                foreach (var p in movie.ToPersonEntities())
                {
                    var existingPerson = await _db.People.FindAsync(p.Id);
                    if (existingPerson is null) _db.People.Add(p);
                }

            entity.Cast = movie.ToCastEntities();
            entity.Crew = movie.ToCrewEntities();
            entity.Videos = movie.ToVideoEntities();
            entity.AlternativeTitles = movie.ToAlternativeTitleEntities();
            entity.ReleaseDates = movie.ToReleaseDateEntities();
            entity.Images = movie.ToImageEntities();

            _db.Movies.Add(entity);
            await _db.SaveChangesAsync();
        }

        public async Task AddPerson(Person person)
        {
            var existing = await _db.People.FindAsync(person.Id);
            if (existing is not null) return;

            var entity = new PersonEntity
            {
                Id = person.Id,
                Name = person.Name,
                Biography = person.Biography,
                Birthday = NormalizeUtc(person.Birthday),
                Deathday = NormalizeUtc(person.Deathday),
                PlaceOfBirth = person.PlaceOfBirth,
                Homepage = person.Homepage,
                ImdbId = person.ImdbId,
                ProfilePath = person.ProfilePath,
                Adult = person.Adult,
                Gender = person.Gender.ToString(),
                KnownForDepartment = person.KnownForDepartment,
                Popularity = person.Popularity
            };

            _db.People.Add(entity);
            await _db.SaveChangesAsync();
        }

        public async Task AddReviewAsync(Guid userId, int tmdbId, ReviewEntity review)
        {
            review.UserId = userId;
            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();
        }
        public async Task<ReviewEntity?> GetUserMovieReviewAsync(Guid userId, int tmdbId)
        {
            return await _db.Reviews
                .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == tmdbId);
        }
        public async Task UpdateReviewAsync(ReviewEntity review)
        {
            var existing = await _db.Reviews.FindAsync(review.Id);
            if (existing is null) return;
            existing.Content = review.Content;
            existing.AuthorRating = review.AuthorRating;
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        public async Task<string?> GetUserLoginAsync(Guid userId)
        {
            return await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Login)
                .FirstOrDefaultAsync();
        }
        public async Task<string?> GetUserUsernameAsync(Guid userId)
        {
            return await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync();
        }
        public async Task AddPlaylistAsync(Guid userId, string name, string? description, IEnumerable<int> tmdbIds)
        {
            var playlist = new UserPlaylistEntity
            {
                UserId = userId,
                Name = name,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                Items = new List<UserPlaylistItemEntity>()
            };

            int order = 0;
            foreach (var tmdbId in tmdbIds)
            {
                playlist.Items.Add(new UserPlaylistItemEntity
                {
                    MovieId = tmdbId,
                    Order = ++order,
                    AddedAt = DateTime.UtcNow
                });
            }

            _db.UserPlaylists.Add(playlist);
            await _db.SaveChangesAsync();
        }
        public async Task EnsureEmbeddingsAsync(int tmdbId)
        {
            if (await _db.MovieEmbeddings.AnyAsync(e => e.MovieId == tmdbId))
                return;

            var movie = await _db.Movies
                .Include(m => m.Genres)
                .FirstOrDefaultAsync(m => m.Id == tmdbId);

            if (movie is null) return;

            var text = $"{movie.Title} {movie.Overview} {string.Join(" ", movie.Genres.Select(g => g.Name))}";
            var embedding = await _embedding.GenerateTextEmbeddingAsync(text);

            _db.MovieEmbeddings.Add(new MovieEmbeddingEntity
            {
                MovieId = tmdbId,
                Embedding = embedding,
                UpdatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
        public async Task<List<ProductionCompanyEntity>> GetCompaniesAsync()
        {
            return await _db.ProductionCompanies
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<CollectionEntity?> GetCollectionAsync(int collectionId)
        {
            return await _db.Collections
                .Include(c => c.Movies)
                .ThenInclude(m => m.Genres)
                .FirstOrDefaultAsync(c => c.Id == collectionId);
        }

        public async Task<List<GenreEntity>> GetGenresAsync()
        {
            return await _db.Genres
                .OrderBy(g => g.Name)
                .ToListAsync();
        }
        public async Task<List<MovieCastEntity>> GetMovieCastAsync(int tmdbId)
        {
            return await _db.MovieCast
                .Include(c => c.Person)
                .Where(c => c.MovieId == tmdbId)
                .OrderBy(c => c.Order)
                .ToListAsync();
        }

        public async Task<CollectionEntity?> GetMovieCollectionAsync(int tmdbId)
        {
            var collectionId = await _db.Movies
                .Where(m => m.Id == tmdbId)
                .Select(m => m.BelongsToCollectionId)
                .FirstOrDefaultAsync();

            if (collectionId is null) return null;

            return await _db.Collections
                .Include(c => c.Movies)
                .ThenInclude(m => m.Genres)
                .FirstOrDefaultAsync(c => c.Id == collectionId.Value);
        }

        public async Task<List<MovieCrewEntity>> GetMovieCrewAsync(int tmdbId)
        {
            return await _db.MovieCrew
                .Include(c => c.Person)
                .Where(c => c.MovieId == tmdbId)
                .ToListAsync();
        }

        public async Task<List<MovieEntity>> GetPersonMoviesAsync(int personId, int page)
        {
            var movieIds = await _db.MovieCast
                .Where(c => c.PersonId == personId)
                .Select(c => c.MovieId)
                .Distinct()
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            if (movieIds.Count == 0) return [];

            var movies = await _db.Movies
                .Include(m => m.Collection)
                .Include(m => m.Genres)
                .Where(m => movieIds.Contains(m.Id))
                .ToListAsync();

            return movieIds.Select(id => movies.First(m => m.Id == id)).ToList();
        }

        public async Task<List<PersonEntity>> GetPeopleBatchAsync(IEnumerable<int> personIds)
        {
            return await _db.People
                .Include(p => p.ExternalIds)
                .Where(p => personIds.Contains(p.Id))
                .ToListAsync();
        }

        public async Task<PersonEntity?> GetPersonAsync(int personId)
        {
            return await _db.People
                .Include(p => p.ExternalIds)
                .FirstOrDefaultAsync(p => p.Id == personId);
        }
        public async Task<List<UserPlaylistEntity>> GetPlaylistsAsync(Guid userId)
        {
            return await _db.UserPlaylists
                .Include(p => p.Items)
                .ThenInclude(i => i.Movie)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        public async Task<UserPlaylistEntity?> GetPlaylistAsync(int playlistId)
        {
            return await _db.UserPlaylists
                .Include(p => p.Items)
                .ThenInclude(i => i.Movie)
                .FirstOrDefaultAsync(p => p.Id == playlistId);
        }

        public async Task<List<MovieEntity>> GetRecommendationsAsync(Guid userId, int page)
        {
            var avgEmbedding = await ComputeUserAverageEmbeddingAsync(userId);
            if (avgEmbedding is null) return [];

            var seenIds = await _db.UserMovies
                .Where(u => u.UserId == userId)
                .Select(u => u.MovieId)
                .ToListAsync();

            return await FindNearestMoviesAsync(avgEmbedding, seenIds, page);
        }

        public async Task<List<MovieEntity>> GetSimilarMoviesAsync(int tmdbId, int page)
        {
            var embedding = await _db.MovieEmbeddings
                .Where(e => e.MovieId == tmdbId)
                .Select(e => e.Embedding)
                .FirstOrDefaultAsync();

            if (embedding is null)
            {
                await EnsureEmbeddingsAsync(tmdbId);
                embedding = await _db.MovieEmbeddings
                    .Where(e => e.MovieId == tmdbId)
                    .Select(e => e.Embedding)
                    .FirstOrDefaultAsync();
                if (embedding is null) return [];
            }

            return await FindNearestMoviesAsync(embedding, [tmdbId], page);
        }
        public async Task<List<MovieEntity>> GetTopRatedMoviesAsync(int page)
        {
            const int minVotes = 50;
            return await _db.Movies
                .Include(m => m.Collection)
                .Include(m => m.Genres)
                .Where(m => m.VoteCount >= minVotes)
                .OrderByDescending(m => m.VoteAverage)
                .ThenByDescending(m => m.VoteCount)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

        public async Task<List<MovieEntity>> GetTrendingMoviesAsync(int page)
        {
            var cutoff = DateTime.UtcNow.AddYears(-5);
            return await _db.Movies
                .Include(m => m.Collection)
                .Include(m => m.Genres)
                .Where(m => m.ReleaseDate >= cutoff)
                .OrderByDescending(m => m.Popularity)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

        public async Task<Dictionary<int, (int? Rating, string? Status)>> GetUserMoviesStatusBatchAsync(Guid userId, List<int> movieIds)
        {
            if (movieIds.Count == 0) return [];

            var entries = await _db.UserMovies
                .Where(u => u.UserId == userId && movieIds.Contains(u.MovieId))
                .Select(u => new { u.MovieId, u.Rating, u.Status })
                .ToListAsync();

            return entries.ToDictionary(e => e.MovieId, e => (e.Rating, e.Status));
        }
        public async Task<List<MovieEntity>> GetUserTasteAsync(Guid userId, int page)
        {
            var avgEmbedding = await ComputeUserAverageEmbeddingAsync(userId);
            if (avgEmbedding is null) return [];

            return await FindNearestMoviesAsync(avgEmbedding, null, page);
        }
        public async Task<List<UserMovieEntity>> GetUserMoviesAsync(Guid userId, string? status, int page)
        {
            var query = _db.UserMovies
                .Include(u => u.Movie)
                .ThenInclude(m => m.Genres)
                .Where(u => u.UserId == userId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(u => u.Status == status);

            return await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

        public async Task RateMovieAsync(Guid userId, int tmdbId, int rating)
        {
            var entry = await _db.UserMovies
                .FirstOrDefaultAsync(u => u.UserId == userId && u.MovieId == tmdbId);

            if (entry is not null)
            {
                entry.Rating = rating;
                entry.WatchedAt ??= DateTime.UtcNow;
            }
            else
            {
                _db.UserMovies.Add(new UserMovieEntity
                {
                    UserId = userId,
                    MovieId = tmdbId,
                    Rating = rating,
                    WatchedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
        }

        public async Task<(int? Rating, string? Status)> GetUserMovieStatusAsync(Guid userId, int tmdbId)
        {
            var entity = await _db.UserMovies
                .Where(u => u.UserId == userId && u.MovieId == tmdbId)
                .Select(u => new { u.Rating, u.Status })
                .FirstOrDefaultAsync();
            return (entity?.Rating, entity?.Status);
        }

        public async Task RemoveMovie(int tmdbId)
        {
            var movie = await _db.Movies.FindAsync(tmdbId);
            if (movie is not null)
            {
                _db.Movies.Remove(movie);
                await _db.SaveChangesAsync();
            }
        }

        public async Task RemovePerson(int personId)
        {
            var person = await _db.People.FindAsync(personId);
            if (person is not null)
            {
                _db.People.Remove(person);
                await _db.SaveChangesAsync();
            }
        }
        public async Task<EmbeddingGenerationResult> RebuildAllEmbeddingsAsync()
        {
            var movieIds = await _db.Movies
                .Where(m => !_db.MovieEmbeddings.Any(e => e.MovieId == m.Id))
                .Select(m => m.Id)
                .ToListAsync();

            var processed = 0;
            var errors = 0;

            foreach (var id in movieIds)
            {
                try
                {
                    await EnsureEmbeddingsAsync(id);
                    processed++;
                }
                catch
                {
                    errors++;
                }
            }

            return new EmbeddingGenerationResult(Total: movieIds.Count, Processed: processed, Errors: errors);
        }
        public async Task RemoveCollection(int collectionId)
        {
            var collection = await _db.Collections.FindAsync(collectionId);
            if (collection is not null)
            {
                _db.Collections.Remove(collection);
                await _db.SaveChangesAsync();
            }
        }

        public async Task RemoveMediaFromPlaylistAsync(int playlistId, int tmdbId)
        {
            var item = await _db.UserPlaylistItems
                .FirstOrDefaultAsync(i => i.PlaylistId == playlistId && i.MovieId == tmdbId);
            if (item is not null)
            {
                _db.UserPlaylistItems.Remove(item);
                await _db.SaveChangesAsync();
            }
        }
        public async Task RemoveUserMovieAsync(Guid userId, int tmdbId)
        {
            var entry = await _db.UserMovies
                .FirstOrDefaultAsync(u => u.UserId == userId && u.MovieId == tmdbId);
            if (entry is not null)
            {
                _db.UserMovies.Remove(entry);
                await _db.SaveChangesAsync();
            }
        }
        public async Task<PersonEntity?> SearchPersonAsync(string query)
        {
            return await _db.People
                .Include(p => p.ExternalIds)
                .Where(p => p.Name != null && EF.Functions.ILike(p.Name, $"%{query}%"))
                .OrderByDescending(p => p.Popularity)
                .FirstOrDefaultAsync();
        }
        public async Task SetMovieStatusAsync(Guid userId, int tmdbId, string status)
        {
            var entry = await _db.UserMovies
                .FirstOrDefaultAsync(u => u.UserId == userId && u.MovieId == tmdbId);

            if (entry is not null)
            {
                entry.Status = status;
                if (status == "watched")
                    entry.WatchedAt ??= DateTime.UtcNow;
                else if (status != "watched")
                    entry.WatchedAt = null;
            }
            else
            {
                _db.UserMovies.Add(new UserMovieEntity
                {
                    UserId = userId,
                    MovieId = tmdbId,
                    Status = status,
                    WatchedAt = status == "watched" ? DateTime.UtcNow : null,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
        }

        public async Task<List<ReviewEntity>> GetMovieReviewsAsync(int tmdbId)
        {
            return await _db.Reviews
                .Where(r => r.MovieId == tmdbId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Dictionary<int, int>> GetMovieRatingDistributionAsync(int tmdbId)
        {
            var distribution = await _db.UserMovies
                .Where(u => u.MovieId == tmdbId && u.Rating.HasValue)
                .GroupBy(u => u.Rating!.Value)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = new Dictionary<int, int>();
            for (int r = 1; r <= 10; r++)
                result[r] = 0;
            foreach (var entry in distribution)
                result[entry.Rating] = entry.Count;
            return result;
        }

        public async Task<List<MovieEntity>> GetKnownForMoviesAsync(int personId)
        {
            var movieIds = await _db.MovieCast
                .Where(c => c.PersonId == personId)
                .OrderBy(c => c.Order)
                .Select(c => c.MovieId)
                .Distinct()
                .Take(10)
                .ToListAsync();

            if (movieIds.Count == 0) return [];

            var movies = await _db.Movies
                .Include(m => m.Collection)
                .Include(m => m.Genres)
                .Where(m => movieIds.Contains(m.Id))
                .ToListAsync();

            return movieIds.Select(id => movies.First(m => m.Id == id)).ToList();
        }

        public async Task RemoveReviewAsync(string reviewId)
        {
            var review = await _db.Reviews.FindAsync(reviewId);
            if (review is not null)
            {
                _db.Reviews.Remove(review);
                await _db.SaveChangesAsync();
            }
        }
        public async Task UpdateCollection(CollectionEntity collection)
        {
            var existing = await _db.Collections.FindAsync(collection.Id);
            if (existing is null) return;
            _db.Entry(existing).CurrentValues.SetValues(collection);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateMovie(Movie movie, int tmdbId)
        {
            var existing = await _db.Movies.FindAsync(tmdbId);
            if (existing is null) return;

            existing.Title = movie.Title;
            existing.Overview = movie.Overview;
            existing.Tagline = movie.Tagline;
            existing.PosterPath = movie.PosterPath;
            existing.BackdropPath = movie.BackdropPath;
            existing.ReleaseDate = NormalizeUtc(movie.ReleaseDate);
            existing.Runtime = movie.Runtime;
            existing.VoteAverage = movie.VoteAverage;
            existing.VoteCount = movie.VoteCount;
            existing.Popularity = movie.Popularity;
            existing.Budget = movie.Budget;
            existing.Revenue = movie.Revenue;
            existing.Homepage = movie.Homepage;
            existing.ImdbId = movie.ImdbId;
            existing.Status = movie.Status;
            existing.Adult = movie.Adult;
            existing.Video = movie.Video;
            existing.OriginalLanguage = movie.OriginalLanguage;
            existing.OriginalTitle = movie.OriginalTitle;

            await _db.SaveChangesAsync();
        }

        public async Task UpdatePerson(Person person, int personId)
        {
            var existing = await _db.People.FindAsync(personId);
            if (existing is null) return;

            existing.Name = person.Name;
            existing.Biography = person.Biography;
            existing.Birthday = NormalizeUtc(person.Birthday);
            existing.Deathday = NormalizeUtc(person.Deathday);
            existing.PlaceOfBirth = person.PlaceOfBirth;
            existing.Homepage = person.Homepage;
            existing.ImdbId = person.ImdbId;
            existing.ProfilePath = person.ProfilePath;
            existing.Adult = person.Adult;
            existing.Gender = person.Gender.ToString();
            existing.KnownForDepartment = person.KnownForDepartment;
            existing.Popularity = person.Popularity;

            await _db.SaveChangesAsync();
        }

        private static DateTime? NormalizeUtc(DateTime? dt) =>
            dt.HasValue ? DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc) : null;

        private async Task<float[]?> ComputeUserAverageEmbeddingAsync(Guid userId)
        {
            var embeddings = await _db.UserMovies
                .Where(u => u.UserId == userId && u.Rating >= 4)
                .Join(_db.MovieEmbeddings, u => u.MovieId, e => e.MovieId, (_, e) => e.Embedding)
                .ToListAsync();

            if (embeddings.Count == 0) return null;

            var dim = embeddings[0].Length;
            var avg = new float[dim];
            foreach (var vec in embeddings)
                for (var i = 0; i < dim; i++)
                    avg[i] += vec[i];

            for (var i = 0; i < dim; i++)
                avg[i] /= embeddings.Count;

            return avg;
        }

        private async Task<List<MovieEntity>> FindNearestMoviesAsync(float[] target, List<int>? excludeIds, int page)
        {
            var exclude = excludeIds ?? [];
            var vectorStr = "[" + string.Join(",", target.Select(f => f.ToString("G", CultureInfo.InvariantCulture))) + "]";
            var offset = (page - 1) * PageSize;

            var ids = await _db.Database.SqlQueryRaw<int>(
                "SELECT e.\"MovieId\" FROM \"MovieEmbeddings\" e ORDER BY e.\"Embedding\"::vector <=> {0}::vector LIMIT {1} OFFSET {2}",
                vectorStr, PageSize + exclude.Count, 0).ToListAsync();

            var filtered = exclude.Count > 0 ? ids.Where(id => !exclude.Contains(id)).ToList() : ids;
            var paged = filtered.Skip(offset).Take(PageSize).ToList();

            if (paged.Count == 0) return [];

            var movies = await _db.Movies
                .Include(m => m.Collection)
                .Include(m => m.Genres)
                .Where(m => paged.Contains(m.Id))
                .ToListAsync();

            return paged.Select(id => movies.First(m => m.Id == id)).ToList();
        }

        public async Task<List<VideoEntity>> GetMovieVideosAsync(int tmdbId)
        {
            return await _db.Videos
                .Where(v => v.MovieId == tmdbId)
                .OrderByDescending(v => v.PublishedAt)
                .ToListAsync();
        }

        public async Task<List<ImageDataEntity>> GetMovieImagesAsync(int tmdbId, string? type = null)
        {
            var query = _db.Images.Where(i => i.MovieId == tmdbId);
            if (!string.IsNullOrEmpty(type))
                query = query.Where(i => i.Type == type);
            return await query
                .OrderByDescending(i => i.VoteCount)
                .ToListAsync();
        }

        public async Task DeletePlaylistAsync(int playlistId)
        {
            var playlist = await _db.UserPlaylists.FindAsync(playlistId);
            if (playlist is not null)
            {
                _db.UserPlaylists.Remove(playlist);
                await _db.SaveChangesAsync();
            }
        }

        public async Task UpdatePlaylistAsync(int playlistId, string name, string? description)
        {
            var playlist = await _db.UserPlaylists.FindAsync(playlistId);
            if (playlist is null) return;
            playlist.Name = name;
            playlist.Description = description;
            await _db.SaveChangesAsync();
        }

        public async Task<UserEntity?> GetUserByIdAsync(Guid userId)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<List<UserEntity>> GetUserBatchAsync(IEnumerable<Guid> userIds)
        {
            return await _db.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
        }

        public async Task<List<ReviewEntity>> GetUserReviewsAsync(Guid userId, int page = 1)
        {
            return await _db.Reviews
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

        public async Task<List<UserMovieEntity>> GetUserMoviesAllAsync(Guid userId)
        {
            return await _db.UserMovies
                .Include(u => u.Movie)
                .ThenInclude(m => m.Genres)
                .Where(u => u.UserId == userId)
                .ToListAsync();
        }

        public async Task AddReviewCommentAsync(string reviewId, Guid userId, string authorName, string content)
        {
            _db.ReviewComments.Add(new ReviewCommentEntity
            {
                ReviewId = reviewId,
                UserId = userId,
                AuthorName = authorName,
                Content = content,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        public async Task<List<ReviewCommentEntity>> GetReviewCommentsAsync(string reviewId)
        {
            return await _db.ReviewComments
                .Where(c => c.ReviewId == reviewId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task AddOrUpdateReviewLikeAsync(string reviewId, Guid userId, bool isPositive)
        {
            var existing = await _db.ReviewLikes
                .FirstOrDefaultAsync(l => l.ReviewId == reviewId && l.UserId == userId);
            if (existing is not null)
            {
                existing.IsPositive = isPositive;
            }
            else
            {
                _db.ReviewLikes.Add(new ReviewLikeEntity
                {
                    ReviewId = reviewId,
                    UserId = userId,
                    IsPositive = isPositive
                });
            }
            await _db.SaveChangesAsync();
        }

        public async Task RemoveReviewLikeAsync(string reviewId, Guid userId)
        {
            var like = await _db.ReviewLikes
                .FirstOrDefaultAsync(l => l.ReviewId == reviewId && l.UserId == userId);
            if (like is not null)
            {
                _db.ReviewLikes.Remove(like);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<(int Likes, int Dislikes)> GetReviewLikesCountAsync(string reviewId)
        {
            var likes = await _db.ReviewLikes
                .Where(l => l.ReviewId == reviewId)
                .GroupBy(l => l.IsPositive)
                .Select(g => new { IsPositive = g.Key, Count = g.Count() })
                .ToListAsync();
            return (likes.FirstOrDefault(l => l.IsPositive)?.Count ?? 0,
                    likes.FirstOrDefault(l => !l.IsPositive)?.Count ?? 0);
        }

        public async Task<bool?> GetUserReviewLikeAsync(string reviewId, Guid userId)
        {
            var like = await _db.ReviewLikes
                .FirstOrDefaultAsync(l => l.ReviewId == reviewId && l.UserId == userId);
            return like?.IsPositive;
        }

        #region Friends
        public async Task FollowUserAsync(Guid userId, Guid targetUserId)
        {
            if (userId == targetUserId) return;
            var exists = await _db.UserFollows.AnyAsync(f => f.UserId == userId && f.FollowedUserId == targetUserId);
            if (exists) return;
            _db.UserFollows.Add(new UserFollowEntity
            {
                UserId = userId,
                FollowedUserId = targetUserId,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        public async Task UnfollowUserAsync(Guid userId, Guid targetUserId)
        {
            var follow = await _db.UserFollows.FirstOrDefaultAsync(f => f.UserId == userId && f.FollowedUserId == targetUserId);
            if (follow is not null)
            {
                _db.UserFollows.Remove(follow);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<bool> IsFollowingAsync(Guid userId, Guid targetUserId)
        {
            return await _db.UserFollows.AnyAsync(f => f.UserId == userId && f.FollowedUserId == targetUserId);
        }

        public async Task<List<Guid>> GetFollowingIdsAsync(Guid userId)
        {
            return await _db.UserFollows
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => f.FollowedUserId)
                .ToListAsync();
        }

        public async Task<List<Guid>> GetFollowerIdsAsync(Guid userId)
        {
            return await _db.UserFollows
                .Where(f => f.FollowedUserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => f.UserId)
                .ToListAsync();
        }

        public async Task<int> GetFollowingCountAsync(Guid userId)
        {
            return await _db.UserFollows.CountAsync(f => f.UserId == userId);
        }

        public async Task<int> GetFollowerCountAsync(Guid userId)
        {
            return await _db.UserFollows.CountAsync(f => f.FollowedUserId == userId);
        }
        #endregion

        #region Activity
        public async Task<List<ActivityEventEntity>> GetFeedAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            var followingIds = await GetFollowingIdsAsync(userId);
            var userIds = followingIds.Append(userId).ToList();

            return await _db.ActivityEvents
                .Include(e => e.User)
                .Include(e => e.Movie)
                .Where(e => userIds.Contains(e.UserId))
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task RecordActivityAsync(Guid userId, string eventType, int? movieId = null, string? reviewId = null, int? playlistId = null)
        {
            _db.ActivityEvents.Add(new ActivityEventEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EventType = eventType,
                MovieId = movieId,
                ReviewId = reviewId,
                PlaylistId = playlistId,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
        #endregion

        #region Notifications
        public async Task<List<NotificationEntity>> GetNotificationsAsync(Guid userId, bool? unreadOnly = null, int page = 1, int pageSize = 20)
        {
            var query = _db.Notifications
                .Include(n => n.Movie)
                .Where(n => n.UserId == userId);

            if (unreadOnly == true)
                query = query.Where(n => !n.IsRead);

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetUnreadNotificationCountAsync(Guid userId)
        {
            return await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkNotificationReadAsync(Guid notificationId)
        {
            var notif = await _db.Notifications.FindAsync(notificationId);
            if (notif is not null)
            {
                notif.IsRead = true;
                await _db.SaveChangesAsync();
            }
        }

        public async Task MarkAllNotificationsReadAsync(Guid userId)
        {
            await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        }

        public async Task CreateNotificationAsync(Guid userId, Guid actorId, string eventType, int? movieId = null, string? reviewId = null, int? playlistId = null)
        {
            _db.Notifications.Add(new NotificationEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ActorId = actorId,
                EventType = eventType,
                MovieId = movieId,
                ReviewId = reviewId,
                PlaylistId = playlistId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
        #endregion

        #region Diary
        public async Task<List<UserMovieEntity>> GetDiaryAsync(Guid userId, int? year = null, int? month = null, int page = 1, int pageSize = 20)
        {
            var query = _db.UserMovies
                .Include(u => u.Movie).ThenInclude(m => m.Genres)
                .Where(u => u.UserId == userId && u.WatchedAt != null);

            if (year.HasValue)
                query = query.Where(u => u.WatchedAt!.Value.Year == year.Value);

            if (month.HasValue)
                query = query.Where(u => u.WatchedAt!.Value.Month == month.Value);

            return await query
                .OrderByDescending(u => u.WatchedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        #endregion
    }

    public record EmbeddingGenerationResult(int Total, int Processed, int Errors);
}

