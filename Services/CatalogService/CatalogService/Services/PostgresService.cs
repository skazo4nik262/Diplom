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
        private readonly ICacheImageClient _cacheImage;
        private readonly ITmdbService _tmdb;
        private const int PageSize = 20;
        private const double TextSimilarityThreshold = 0.35;
        private const double ImageSimilarityThreshold = 0.55;

        public PostgresService(TmdbDbContext db, IEmbeddingClient embedding, ICacheImageClient cacheImage, ITmdbService tmdb)
        {
            _db = db;
            _embedding = embedding;
            _cacheImage = cacheImage;
            _tmdb = tmdb;
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
            int? runtimeFrom = null, int? runtimeTo = null, string? sortBy = null, string? sortOrder = null,
            int? personId = null, string? country = null)
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

            if (personId.HasValue)
                baseQuery = baseQuery.Where(m => m.Cast.Any(c => c.PersonId == personId.Value));

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

            if (!string.IsNullOrEmpty(country))
            {
                var countryCodes = country.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                baseQuery = baseQuery.Where(m => m.ProductionCountries.Any(pc => countryCodes.Contains(pc.Iso3166_1)));
            }

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

        public async Task<List<MovieEntity>> SearchByImageAsync(string query, int page, CancellationToken ct = default)
        {
            var pageSize = 40;

            float[]? clipEmb = null;
            float[]? textEmb = null;

            var searchQuery = "search_query: " + query;

            try
            {
                using var clipCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                clipCts.CancelAfter(TimeSpan.FromSeconds(10));
                var clipTask = _embedding.GenerateClipTextEmbeddingAsync(query, clipCts.Token);
                var textTask = _embedding.GenerateTextEmbeddingAsync(searchQuery, ct);
                await Task.WhenAll(clipTask, textTask);
                clipEmb = clipTask.Result;
                textEmb = textTask.Result;
            }
            catch
            {
                // CLIP may be unavailable; attempt text-only fallback
            }

            if (textEmb is null)
            {
                try
                {
                    textEmb = await _embedding.GenerateTextEmbeddingAsync(searchQuery, ct);
                }
                catch
                {
                    return [];
                }
            }

            List<MovieEntity> imageMovies = [];
            if (clipEmb is not null)
            {
                try
                {
                    imageMovies = await FindNearestMoviesByImageAsync(clipEmb, null, 1, pageSize);
                }
                catch { }
            }

            List<MovieEntity> textMovies;
            try
            {
                textMovies = await FindNearestMoviesAsync(textEmb, null, 1, pageSize);
            }
            catch
            {
                return [];
            }

            var seen = new HashSet<int>();
            var merged = new List<MovieEntity>();

            for (var i = 0; i < Math.Max(imageMovies.Count, textMovies.Count); i++)
            {
                if (i < imageMovies.Count && seen.Add(imageMovies[i].Id))
                    merged.Add(imageMovies[i]);
                if (i < textMovies.Count && seen.Add(textMovies[i].Id))
                    merged.Add(textMovies[i]);
            }

            var offset = (page - 1) * 20;
            return merged.Skip(offset).Take(20).ToList();
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
                if (k is null)
                {
                    k = keyword.ToEntity();
                    _db.Keywords.Add(k);
                    try
                    {
                        var ru = await _tmdb.GetKeywordAsync(keyword.Id);
                        if (ru?.Name is not null && ru.Name != keyword.Name)
                            k.NameRu = ru.Name;
                    }
                    catch { }
                    entity.Keywords.Add(k);
                }
                else
                {
                    entity.Keywords.Add(k);
                }
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

            _ = CacheMovieImagesAsync(entity);
            await CheckNewCollectionMovieForMovieAsync(movie.Id);
        }

        public async Task AttachKeywordsAsync(int tmdbId, List<Keyword> keywords)
        {
            var movie = await _db.Movies
                .Include(m => m.Keywords)
                .FirstOrDefaultAsync(m => m.Id == tmdbId);
            if (movie is null) return;

            foreach (var keyword in keywords)
            {
                if (movie.Keywords.Any(k => k.Id == keyword.Id)) continue;
                var k = await _db.Keywords.FindAsync(keyword.Id);
                if (k is null)
                {
                    k = keyword.ToEntity();
                    _db.Keywords.Add(k);
                    try
                    {
                        var ru = await _tmdb.GetKeywordAsync(keyword.Id);
                        if (ru?.Name is not null && ru.Name != keyword.Name)
                            k.NameRu = ru.Name;
                    }
                    catch { }
                }
                movie.Keywords.Add(k);
            }
            await _db.SaveChangesAsync();
        }

        private async Task CacheMovieImagesAsync(MovieEntity movie)
        {
            try
            {
                if (movie.PosterPath is not null)
                    await _cacheImage.GetImageAsync(movie.PosterPath.TrimStart('/'), "w500");
            }
            catch { }

            var personIds = new HashSet<int>();
            if (movie.Cast is not null)
                foreach (var c in movie.Cast.Where(c => c.Person?.ProfilePath is not null))
                    personIds.Add(c.PersonId);
            if (movie.Crew is not null)
                foreach (var c in movie.Crew.Where(c => c.Person?.ProfilePath is not null))
                    personIds.Add(c.PersonId);

            foreach (var pid in personIds)
            {
                try
                {
                    var person = await _db.People.FindAsync(pid);
                    if (person?.ProfilePath is not null)
                        await _cacheImage.GetImageAsync(person.ProfilePath.TrimStart('/'), "w500");
                }
                catch { }
            }
        }

        public async Task CheckNewCollectionMovieForMovieAsync(int tmdbId)
        {
            var movie = await _db.Movies
                .Where(m => m.Id == tmdbId && m.BelongsToCollectionId != null)
                .Select(m => new { m.Id, m.BelongsToCollectionId, m.Title, m.ReleaseDate })
                .FirstOrDefaultAsync();

            if (movie?.BelongsToCollectionId == null) return;

            var collectionId = movie.BelongsToCollectionId.Value;

            var usersWithMoviesInCollection = await _db.UserMovies
                .Where(um => um.Status == "watched"
                    && um.MovieId != tmdbId
                    && _db.Movies.Any(m => m.Id == um.MovieId && m.BelongsToCollectionId == collectionId))
                .Select(um => um.UserId)
                .Distinct()
                .Join(_db.Users.Where(u => u.NotifyNewInCollection),
                    uid => uid, u => u.Id, (uid, _) => uid)
                .ToListAsync();

            if (usersWithMoviesInCollection.Count == 0) return;

            var existingNotifs = await _db.Notifications
                .Where(n => usersWithMoviesInCollection.Contains(n.UserId)
                    && n.EventType == "new_in_collection"
                    && n.MovieId == tmdbId)
                .Select(n => n.UserId)
                .ToListAsync();

            foreach (var userId in usersWithMoviesInCollection)
            {
                if (existingNotifs.Contains(userId)) continue;

                var alreadyWatched = await _db.UserMovies.AnyAsync(um =>
                    um.UserId == userId && um.MovieId == tmdbId);
                if (alreadyWatched) continue;

                _db.Notifications.Add(new NotificationEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ActorId = null,
                    EventType = "new_in_collection",
                    MovieId = tmdbId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

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

            _ = CachePersonImageAsync(entity);
        }

        private async Task CachePersonImageAsync(PersonEntity person)
        {
            try
            {
                if (person.ProfilePath is not null)
                    await _cacheImage.GetImageAsync(person.ProfilePath.TrimStart('/'), "w500");
            }
            catch { }
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
            var existing = await _db.MovieEmbeddings.FirstOrDefaultAsync(e => e.MovieId == tmdbId);

            var movie = await _db.Movies
                .Include(m => m.Collection)
                .Include(m => m.Genres)
                .Include(m => m.Keywords)
                .Include(m => m.Cast.OrderBy(c => c.Order).Take(5))
                    .ThenInclude(c => c.Person)
                .Include(m => m.Crew.Where(c => c.Job == "Director"))
                    .ThenInclude(c => c.Person)
                .FirstOrDefaultAsync(m => m.Id == tmdbId);

            if (movie is null) return;

            var hasText = existing?.Embedding is { Length: > 0 };
            var hasImage = existing?.ImageEmbedding is { Length: > 0 };

            var textTask = hasText
                ? Task.FromResult(existing!.Embedding)
                : _embedding.GenerateTextEmbeddingAsync(BuildEmbeddingPrompt(movie));

            Task<float[]?>? imageTask = null;
            if (!hasImage && movie.PosterPath is not null)
            {
                var posterPath = movie.PosterPath.TrimStart('/');
                imageTask = DownloadAndEmbedImageAsync(posterPath);
            }

            var textEmbedding = await textTask;
            float[]? imageEmbedding = imageTask is not null ? await imageTask : existing?.ImageEmbedding;

            if (existing is not null)
            {
                existing.Embedding = textEmbedding;
                existing.ImageEmbedding = imageEmbedding ?? existing.ImageEmbedding;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.MovieEmbeddings.Add(new MovieEmbeddingEntity
                {
                    MovieId = tmdbId,
                    Embedding = textEmbedding,
                    ImageEmbedding = imageEmbedding,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
        }

        private static string BuildEmbeddingPrompt(MovieEntity movie)
        {
            var parts = new List<string>();

            parts.Add($"Название: {movie.Title}");
            if (movie.ReleaseDate.HasValue)
                parts.Add($"Год: {movie.ReleaseDate.Value.Year}");

            if (movie.Genres?.Count > 0)
                parts.Add($"Жанры: {string.Join(", ", movie.Genres.Select(g => g.Name))}");

            var creators = new List<string>();
            var directors = movie.Crew?.Where(c => c.Job == "Director")
                .Select(c => c.Person?.Name?.Replace(" ", "_"))
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();
            if (directors?.Count > 0)
                creators.AddRange(directors!);

            var topActors = movie.Cast?.OrderBy(c => c.Order).Take(5)
                .Select(c => c.Person?.Name?.Replace(" ", "_"))
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();
            if (topActors?.Count > 0)
                creators.AddRange(topActors!);

            if (creators.Count > 0)
                parts.Add($"Создатели: {string.Join(", ", creators)}");

            var plot = movie.Overview ?? movie.Tagline;
            if (!string.IsNullOrWhiteSpace(plot))
            {
                var snippet = plot.Length > 300 ? plot[..300] : plot;
                parts.Add($"Сюжет: {snippet}");
            }

            var atmos = new List<string>();
            if (!string.IsNullOrWhiteSpace(movie.Tagline))
                atmos.Add(movie.Tagline);
            if (movie.Keywords?.Count > 0)
                atmos.AddRange(movie.Keywords.Select(k => k.Name!));
            if (atmos.Count > 0)
                parts.Add($"Ключевые слова: {string.Join(", ", atmos)}");

            return "search_document: " + string.Join(". ", parts) + ".";
        }

        private async Task<float[]?> DownloadAndEmbedImageAsync(string posterPath)
        {
            byte[] imageBytes;
            try
            {
                imageBytes = await _cacheImage.GetImageAsync(posterPath, "w500");
            }
            catch
            {
                try
                {
                    using var http = new HttpClient();
                    imageBytes = await http.GetByteArrayAsync($"https://image.tmdb.org/t/p/w500/{posterPath}");
                }
                catch
                {
                    return null;
                }
            }

            try
            {
                return await _embedding.GenerateImageEmbeddingAsync(imageBytes);
            }
            catch
            {
                return null;
            }
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

        public async Task<List<ProductionCountryEntity>> GetCountriesAsync()
        {
            return await _db.ProductionCountries
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<List<GenreEntity>> GetPopularGenresAsync(int count = 10)
        {
            return await _db.Genres
                .OrderByDescending(g => g.Movies.Count)
                .Take(count)
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
                .ThenInclude(m => m.Genres)
                .FirstOrDefaultAsync(p => p.Id == playlistId);
        }

        public async Task<List<MovieEntity>> GetPlaylistSuggestionsAsync(int playlistId, int count = 5)
        {
            var playlist = await _db.UserPlaylists
                .Include(p => p.Items)
                .ThenInclude(i => i.Movie)
                .Where(p => p.Id == playlistId)
                .FirstOrDefaultAsync();

            if (playlist is null || playlist.Items.Count == 0)
                return [];

            var movieIds = playlist.Items.Select(i => i.MovieId).ToList();
            var excludeIds = movieIds.ToList();

            var embeddings = await _db.MovieEmbeddings
                .Where(e => movieIds.Contains(e.MovieId) && e.Embedding != null)
                .Select(e => new { e.MovieId, e.Embedding })
                .ToListAsync();

            if (embeddings.Count == 0)
                return [];

            var dim = embeddings[0].Embedding!.Length;
            var avg = new float[dim];

            foreach (var e in embeddings)
                for (var i = 0; i < dim; i++)
                    avg[i] += e.Embedding![i];

            float[]? nameEmb = null;
            if (!string.IsNullOrWhiteSpace(playlist.Name))
            {
                try
                {
                    nameEmb = await _embedding.GenerateTextEmbeddingAsync(playlist.Name);
                }
                catch { }
            }

            if (nameEmb is not null && nameEmb.Length == dim)
            {
                for (var i = 0; i < dim; i++)
                    avg[i] = avg[i] / embeddings.Count * 0.6f + nameEmb[i] * 0.4f;
            }
            else
            {
                for (var i = 0; i < dim; i++)
                    avg[i] /= embeddings.Count;
            }

            return await FindNearestMoviesAsync(avg, excludeIds, 1, count);
        }

        public async Task<List<MovieEntity>> GetRecommendationsAsync(Guid userId, int page, bool useImage = false)
        {
            var avgEmbedding = await ComputeUserAverageEmbeddingAsync(userId, useImage);
            if (avgEmbedding is null) return [];

            var excludeIds = await _db.UserMovies
                .Where(u => u.UserId == userId
                    && (u.Status == "watched" || u.Status == "watching" || u.Status == "planned"))
                .Select(u => u.MovieId)
                .ToListAsync();

            return useImage
                ? await FindNearestMoviesByImageAsync(avgEmbedding, excludeIds, page)
                : await FindNearestMoviesAsync(avgEmbedding, excludeIds, page);
        }

        private static float CalculateWeight(bool isFavorite, string? status, int? rating)
        {
            var w = 0f;

            if (isFavorite)
                w += 2f;

            if (rating >= 9)
                w += 1.5f;
            else if (rating >= 7)
                w += 1f;
            else if (rating >= 5)
                w += 0.5f;
            else if (rating >= 1)
                w += -1.5f;

            if (status == "watching")
                w += 0.7f;
            else if (status == "watched")
                w += 0.7f;
            else if (status == "planned")
                w += 0.5f;
            else if (status == "dropped")
                w += -1.5f;

            return w;
        }

        public async Task<List<MovieEntity>> GetSimilarMoviesAsync(int tmdbId, int page, bool useImage = false)
        {
            var emb = await _db.MovieEmbeddings.FirstOrDefaultAsync(e => e.MovieId == tmdbId);

            if (emb is null)
            {
                await EnsureEmbeddingsAsync(tmdbId);
                emb = await _db.MovieEmbeddings.FirstOrDefaultAsync(e => e.MovieId == tmdbId);
                if (emb is null) return [];
            }

            if (useImage)
            {
                if (emb.ImageEmbedding is not { Length: > 0 }) return [];
                return await FindNearestMoviesByImageAsync(emb.ImageEmbedding, [tmdbId], page);
            }

            return await FindNearestMoviesAsync(emb.Embedding, [tmdbId], page);
        }

        private static readonly Dictionary<string, (string Label, List<int> GenreIds, string Prompt)> Moods = new()
        {
            ["sad"] = ("Грустное", [18], "грустный эмоциональный драматический фильм, который заставляет плакать"),
            ["happy"] = ("Веселое", [35], "веселый смешной комедийный фильм, поднимающий настроение"),
            ["romantic"] = ("Романтичное", [10749], "романтичный фильм о любви и отношениях"),
            ["scary"] = ("Страшное", [27, 53], "страшный пугающий фильм ужасов, триллер, держит в напряжении"),
            ["inspiring"] = ("Мотивирующее", [36, 12], "мотивирующий вдохновляющий фильм о силе духа и достижениях"),
            ["mysterious"] = ("Загадочное", [9648, 53], "загадочный мистический фильм, детектив с неожиданной развязкой"),
            ["epic"] = ("Эпическое", [12, 28, 14], "эпический масштабный фильм, блокбастер с грандиозными сценами"),
            ["touching"] = ("Трогательное", [18, 10751], "трогательный душевный фильм, который согревает сердце"),
            ["atmospheric"] = ("Атмосферное", [14, 18], "атмосферный красивый фильм с уникальным визуальным стилем"),
            ["crazy"] = ("Безумное", [35, 80], "безумный абсурдный фильм, комедия с сумасшедшим сюжетом")
        };

        public async Task<List<MovieEntity>> GetMoodMoviesAsync(string mood, int page)
        {
            if (!Moods.TryGetValue(mood, out var moodDef))
                return [];

            try
            {
                var prompt = "search_query: " + moodDef.Prompt;
                var emb = await _embedding.GenerateTextEmbeddingAsync(prompt);
                if (emb is { Length: > 0 })
                    return await FindNearestMoviesAsync(emb, null, page);
            }
            catch
            {
            }

            return await _db.Movies
                .Include(m => m.Collection)
                .Include(m => m.Genres)
                .Where(m => m.Genres.Any(g => moodDef.GenreIds.Contains(g.Id)))
                .OrderByDescending(m => m.VoteAverage)
                .ThenByDescending(m => m.VoteCount)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
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

        public async Task<Dictionary<int, (int? Rating, string? Status, bool IsFavorite, double? LastPosition, double? Duration)>> GetUserMoviesStatusBatchAsync(Guid userId, List<int> movieIds)
        {
            if (movieIds.Count == 0) return [];

            var entries = await _db.UserMovies
                .Where(u => u.UserId == userId && movieIds.Contains(u.MovieId))
                .Select(u => new { u.MovieId, u.Rating, u.Status, u.IsFavorite, u.LastPositionSeconds, u.DurationSeconds })
                .ToListAsync();

            return entries.ToDictionary(e => e.MovieId, e => (e.Rating, e.Status, e.IsFavorite, e.LastPositionSeconds, e.DurationSeconds));
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
            {
                if (status == "favorite")
                    query = query.Where(u => u.IsFavorite);
                else
                    query = query.Where(u => u.Status == status);
            }

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

        public async Task<(int? Rating, string? Status, bool IsFavorite)> GetUserMovieStatusAsync(Guid userId, int tmdbId)
        {
            var entity = await _db.UserMovies
                .Where(u => u.UserId == userId && u.MovieId == tmdbId)
                .Select(u => new { u.Rating, u.Status, u.IsFavorite })
                .FirstOrDefaultAsync();
            if (entity is null)
                return (null, null, false);
            return (entity.Rating, entity.Status, entity.IsFavorite);
        }

        public async Task<(int? Rating, string? Status, bool IsFavorite, double? LastPosition, double? Duration)> GetUserMovieExtendedStatusAsync(Guid userId, int tmdbId)
        {
            var entity = await _db.UserMovies
                .Where(u => u.UserId == userId && u.MovieId == tmdbId)
                .Select(u => new { u.Rating, u.Status, u.IsFavorite, u.LastPositionSeconds, u.DurationSeconds })
                .FirstOrDefaultAsync();
            if (entity is null)
                return (null, null, false, null, null);
            return (entity.Rating, entity.Status, entity.IsFavorite, entity.LastPositionSeconds, entity.DurationSeconds);
        }

        public async Task SetFavoriteAsync(Guid userId, int tmdbId)
        {
            var entry = await _db.UserMovies
                .FirstOrDefaultAsync(u => u.UserId == userId && u.MovieId == tmdbId);

            if (entry is not null)
            {
                entry.IsFavorite = true;
            }
            else
            {
                _db.UserMovies.Add(new UserMovieEntity
                {
                    UserId = userId,
                    MovieId = tmdbId,
                    IsFavorite = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
        }

        public async Task RemoveFavoriteAsync(Guid userId, int tmdbId)
        {
            var entry = await _db.UserMovies
                .FirstOrDefaultAsync(u => u.UserId == userId && u.MovieId == tmdbId);
            if (entry is not null)
            {
                entry.IsFavorite = false;
                await _db.SaveChangesAsync();
            }
        }

        public async Task SetMovieProgressAsync(Guid userId, int tmdbId, double position, double duration)
        {
            var entry = await _db.UserMovies
                .FirstOrDefaultAsync(u => u.UserId == userId && u.MovieId == tmdbId);

            if (entry is not null)
            {
                entry.LastPositionSeconds = position;
                entry.DurationSeconds = duration;
            }
            else
            {
                _db.UserMovies.Add(new UserMovieEntity
                {
                    UserId = userId,
                    MovieId = tmdbId,
                    Status = "watching",
                    LastPositionSeconds = position,
                    DurationSeconds = duration,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
        }

        public async Task<List<UserMovieEntity>> GetContinueWatchingAsync(Guid userId, int page = 1)
        {
            return await _db.UserMovies
                .Include(u => u.Movie)
                .ThenInclude(m => m.Genres)
                .Where(u => u.UserId == userId && u.LastPositionSeconds != null && u.LastPositionSeconds > 0)
                .OrderByDescending(u => u.LastPositionSeconds)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
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
        public async Task<List<int>> GetAllMovieIdsAsync()
        {
            return await _db.Movies.Select(m => m.Id).ToListAsync();
        }

        public async Task<List<int>> GetMovieIdsWithoutEmbeddingsAsync()
        {
            return await _db.Movies
                .Where(m => !_db.MovieEmbeddings.Any(e => e.MovieId == m.Id
                    && e.Embedding != null && e.Embedding.Length > 0
                    && e.ImageEmbedding != null && e.ImageEmbedding.Length > 0))
                .Select(m => m.Id)
                .ToListAsync();
        }

        public async Task ClearAllEmbeddingsAsync()
        {
            _db.MovieEmbeddings.RemoveRange(_db.MovieEmbeddings);
            await _db.SaveChangesAsync();
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

        public async Task<List<PersonEntity>> SearchPeopleMultipleAsync(string query, int limit = 10)
        {
            return await _db.People
                .Include(p => p.ExternalIds)
                .Where(p => p.Name != null && EF.Functions.ILike(p.Name, $"%{query}%"))
                .OrderByDescending(p => p.Popularity)
                .Take(limit)
                .ToListAsync();
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

        private async Task<float[]?> ComputeUserAverageEmbeddingAsync(Guid userId, bool useImage = false)
        {
            var userMovies = await _db.UserMovies
                .Where(u => u.UserId == userId)
                .Select(u => new { u.MovieId, u.Status, u.Rating, u.IsFavorite })
                .ToListAsync();

            if (userMovies.Count == 0) return null;

            var movieIds = userMovies.Select(u => u.MovieId).ToList();

            Dictionary<int, float[]> embeddings;
            if (useImage)
            {
                embeddings = await _db.MovieEmbeddings
                    .Where(e => movieIds.Contains(e.MovieId) && e.ImageEmbedding != null)
                    .Select(e => new { e.MovieId, Embedding = e.ImageEmbedding! })
                    .ToDictionaryAsync(e => e.MovieId, e => e.Embedding);
            }
            else
            {
                embeddings = await _db.MovieEmbeddings
                    .Where(e => movieIds.Contains(e.MovieId))
                    .Select(e => new { e.MovieId, Embedding = e.Embedding })
                    .ToDictionaryAsync(e => e.MovieId, e => e.Embedding);
            }

            if (embeddings.Count == 0) return null;

            var dim = embeddings.First().Value.Length;
            var weightedSum = new float[dim];
            var hasSignal = false;

            foreach (var um in userMovies)
            {
                if (!embeddings.TryGetValue(um.MovieId, out var emb)) continue;

                var weight = CalculateWeight(um.IsFavorite, um.Status, um.Rating);
                if (weight == 0) continue;
                hasSignal = true;

                for (var i = 0; i < dim; i++)
                    weightedSum[i] += weight * emb[i];
            }

            if (!hasSignal) return null;
            return weightedSum;
        }

        private async Task<List<MovieEntity>> FindNearestMoviesAsync(float[] target, List<int>? excludeIds, int page, int limit = 0)
        {
            var exclude = excludeIds ?? [];
            var vectorStr = "[" + string.Join(",", target.Select(f => f.ToString("G", CultureInfo.InvariantCulture))) + "]";
            var offset = (page - 1) * PageSize;
            var fetch = limit > 0 ? limit : PageSize;

            var rows = await _db.Database.SqlQueryRaw<MovieIdDistance>(
                "SELECT e.\"MovieId\", e.\"Embedding\"::vector <=> {0}::vector AS \"Distance\" FROM \"MovieEmbeddings\" e ORDER BY \"Distance\" LIMIT {1} OFFSET {2}",
                vectorStr, fetch + exclude.Count, 0).ToListAsync();

            rows = rows.Where(r => r.Distance <= TextSimilarityThreshold).ToList();
            var filtered = exclude.Count > 0 ? rows.Where(r => !exclude.Contains(r.MovieId)).ToList() : rows;
            var paged = filtered.Skip(offset).Take(PageSize).ToList();

            if (paged.Count == 0) return [];

            var movies = await _db.Movies
                .Include(m => m.Collection)
                .Include(m => m.Genres)
                .Where(m => paged.Select(r => r.MovieId).Contains(m.Id))
                .ToListAsync();

            var ordered = paged.Select(r =>
            {
                var m = movies.First(m => m.Id == r.MovieId);
                m.MatchPercentage = Math.Round((1.0 - Math.Min(r.Distance, 1.0)) * 100, 1);
                return m;
            }).ToList();

            return ordered;
        }

        private record MovieIdDistance(int MovieId, double Distance);

        private async Task<List<MovieEntity>> FindNearestMoviesByImageAsync(float[] target, List<int>? excludeIds, int page, int limit = 0)
        {
            var exclude = excludeIds ?? [];
            var vectorStr = "[" + string.Join(",", target.Select(f => f.ToString("G", CultureInfo.InvariantCulture))) + "]";
            var offset = (page - 1) * PageSize;
            var fetch = limit > 0 ? limit : PageSize;

            var rows = await _db.Database.SqlQueryRaw<MovieIdDistance>(
                "SELECT e.\"MovieId\", e.\"ImageEmbedding\"::vector <=> {0}::vector AS \"Distance\" FROM \"MovieEmbeddings\" e WHERE e.\"ImageEmbedding\" IS NOT NULL ORDER BY \"Distance\" LIMIT {1} OFFSET {2}",
                vectorStr, fetch + exclude.Count, 0).ToListAsync();

            rows = rows.Where(r => r.Distance <= ImageSimilarityThreshold).ToList();
            var filtered = exclude.Count > 0 ? rows.Where(r => !exclude.Contains(r.MovieId)).ToList() : rows;
            var paged = filtered.Skip(offset).Take(PageSize).ToList();

            if (paged.Count == 0) return [];

            var movies = await _db.Movies
                .Include(m => m.Collection)
                .Include(m => m.Genres)
                .Where(m => paged.Select(r => r.MovieId).Contains(m.Id))
                .ToListAsync();

            var ordered = paged.Select(r =>
            {
                var m = movies.First(m => m.Id == r.MovieId);
                m.MatchPercentage = Math.Round((1.0 - Math.Min(r.Distance, 1.0)) * 100, 1);
                return m;
            }).ToList();

            return ordered;
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

        public async Task<TasteDnaResult> GetUserTasteDnaAsync(Guid userId)
        {
            var userMovies = await _db.UserMovies
                .Include(u => u.Movie)
                    .ThenInclude(m => m.Genres)
                .Include(u => u.Movie)
                    .ThenInclude(m => m.Keywords)
                .Where(u => u.UserId == userId)
                .ToListAsync();

            var genreScores = new Dictionary<int, (string Name, double Score)>();
            var keywordScores = new Dictionary<int, (string Name, double Score)>();

            foreach (var um in userMovies)
            {
                var weight = CalculateWeight(um.IsFavorite, um.Status, um.Rating);
                if (weight == 0) continue;

                if (um.Movie?.Genres is not null)
                {
                    foreach (var g in um.Movie.Genres)
                    {
                        if (g.Name is null) continue;
                        if (genreScores.TryGetValue(g.Id, out var existing))
                            genreScores[g.Id] = (g.Name, existing.Score + weight);
                        else
                            genreScores[g.Id] = (g.Name, weight);
                    }
                }

                if (um.Movie?.Keywords is not null)
                {
                    foreach (var kw in um.Movie.Keywords)
                    {
                        var name = kw.NameRu ?? kw.Name;
                        if (name is null) continue;
                        if (keywordScores.TryGetValue(kw.Id, out var existing))
                            keywordScores[kw.Id] = (name, existing.Score + weight);
                        else
                            keywordScores[kw.Id] = (name, weight);
                    }
                }
            }

            // lazily fetch Russian names for top keywords missing NameRu
            var dbKwMap = await _db.Keywords
                .Where(k => keywordScores.Keys.Contains(k.Id))
                .ToDictionaryAsync(k => k.Id);

            var resultKeywords = new List<TasteDnaItem>();
            foreach (var kv in keywordScores.OrderByDescending(kv => kv.Value.Score).Take(20))
            {
                var name = kv.Value.Name;
                if (dbKwMap.TryGetValue(kv.Key, out var kwEntity) && kwEntity.NameRu is not null)
                    name = kwEntity.NameRu;
                else if (dbKwMap.TryGetValue(kv.Key, out kwEntity) && kwEntity.NameRu is null)
                {
                    try
                    {
                        var tmdb = await _tmdb.GetKeywordAsync(kv.Key);
                        if (tmdb?.Name is not null)
                        {
                            kwEntity.NameRu = tmdb.Name;
                            name = tmdb.Name;
                        }
                    }
                    catch { }
                }
                resultKeywords.Add(new TasteDnaItem { Name = name, Score = Math.Round(kv.Value.Score, 1) });
            }
            await _db.SaveChangesAsync();

            return new TasteDnaResult
            {
                Genres = genreScores.Values
                    .OrderByDescending(g => g.Score)
                    .Take(10)
                    .Select(g => new TasteDnaItem { Name = g.Name, Score = Math.Round(g.Score, 1) })
                    .ToList(),
                Keywords = resultKeywords
            };
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

            return await _db.ActivityEvents
                .Include(e => e.User)
                .Include(e => e.Movie)
                .Where(e => followingIds.Contains(e.UserId))
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

        public async Task CreateNotificationAsync(Guid userId, Guid? actorId, string eventType, int? movieId = null, string? reviewId = null, int? playlistId = null)
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

        public async Task<(bool NotifyNewInCollection, bool NotifyVideoAdded, bool NotifyFileAdded)> GetNotificationSettingsAsync(Guid userId)
        {
            var user = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.NotifyNewInCollection, u.NotifyVideoAdded, u.NotifyFileAdded })
                .FirstOrDefaultAsync();
            return (user?.NotifyNewInCollection ?? true, user?.NotifyVideoAdded ?? true, user?.NotifyFileAdded ?? true);
        }

        public async Task SetNotificationSettingsAsync(Guid userId, bool? notifyNewInCollection, bool? notifyVideoAdded, bool? notifyFileAdded)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user is null) return;
            if (notifyNewInCollection.HasValue)
                user.NotifyNewInCollection = notifyNewInCollection.Value;
            if (notifyVideoAdded.HasValue)
                user.NotifyVideoAdded = notifyVideoAdded.Value;
            if (notifyFileAdded.HasValue)
                user.NotifyFileAdded = notifyFileAdded.Value;
            await _db.SaveChangesAsync();
        }

        public async Task CheckNewCollectionMoviesAsync()
        {
            var users = await _db.Users
                .Where(u => u.NotifyNewInCollection)
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var userId in users)
            {
                var userMovieIds = await _db.UserMovies
                    .Where(um => um.UserId == userId)
                    .Select(um => um.MovieId)
                    .ToListAsync();

                var collectionIds = await _db.Movies
                    .Where(m => userMovieIds.Contains(m.Id) && m.BelongsToCollectionId != null)
                    .Select(m => m.BelongsToCollectionId!.Value)
                    .Distinct()
                    .ToListAsync();

                if (collectionIds.Count == 0) continue;

                var collectionMovies = await _db.Movies
                    .Where(m => collectionIds.Contains(m.BelongsToCollectionId!.Value))
                    .Select(m => new { m.Id, m.BelongsToCollectionId, m.Title, m.ReleaseDate })
                    .ToListAsync();

                var existingNotifs = await _db.Notifications
                    .Where(n => n.UserId == userId && n.EventType == "new_in_collection" && n.MovieId != null)
                    .Select(n => n.MovieId!.Value)
                    .ToListAsync();

                foreach (var cid in collectionIds)
                {
                    var userMoviesInCollection = await _db.UserMovies
                        .Where(um => um.UserId == userId && collectionMovies.Any(cm => cm.Id == um.MovieId && cm.BelongsToCollectionId == cid))
                        .Select(um => um.MovieId)
                        .ToListAsync();

                    var newMovies = collectionMovies
                        .Where(cm => cm.BelongsToCollectionId == cid
                            && !userMoviesInCollection.Contains(cm.Id)
                            && !existingNotifs.Contains(cm.Id)
                            && cm.ReleaseDate.HasValue)
                        .ToList();

                    foreach (var movie in newMovies)
                    {
                        _db.Notifications.Add(new NotificationEntity
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            ActorId = null,
                            EventType = "new_in_collection",
                            MovieId = movie.Id,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
            await _db.SaveChangesAsync();
        }

        public async Task CheckNewVideosAsync()
        {
            var users = await _db.Users
                .Where(u => u.NotifyVideoAdded)
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var userId in users)
            {
                var userMovieEntries = await _db.UserMovies
                    .Where(um => um.UserId == userId && um.Status != null)
                    .Select(um => new { um.MovieId, um.CreatedAt })
                    .ToListAsync();

                if (userMovieEntries.Count == 0) continue;

                var movieIds = userMovieEntries.Select(e => e.MovieId).ToList();
                var movieAddedAt = userMovieEntries.ToDictionary(e => e.MovieId, e => e.CreatedAt);

                var existingNotifs = await _db.Notifications
                    .Where(n => n.UserId == userId && n.EventType == "video_added" && n.MovieId != null)
                    .Select(n => n.MovieId!.Value)
                    .ToListAsync();

                var moviesWithVideos = await _db.Movies
                    .Where(m => movieIds.Contains(m.Id) && m.Videos.Any())
                    .Select(m => new
                    {
                        m.Id,
                        m.Title,
                        LatestVideo = m.Videos.Max(v => (DateTime?)v.PublishedAt)
                    })
                    .ToListAsync();

                foreach (var movie in moviesWithVideos)
                {
                    if (existingNotifs.Contains(movie.Id)) continue;
                    if (!movie.LatestVideo.HasValue) continue;
                    if (!movieAddedAt.TryGetValue(movie.Id, out var addedAt)) continue;
                    if (movie.LatestVideo.Value <= addedAt) continue;

                    _db.Notifications.Add(new NotificationEntity
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        ActorId = null,
                        EventType = "video_added",
                        MovieId = movie.Id,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            await _db.SaveChangesAsync();
        }

        public async Task CheckNewFilesAsync()
        {
            var newlyReady = await _db.MovieFiles
                .Where(f => f.IsReady)
                .Select(f => f.TmdbId)
                .ToListAsync();

            var alreadyNotified = await _db.Notifications
                .Where(n => n.EventType == "file_added" && n.MovieId != null)
                .Select(n => n.MovieId!.Value)
                .ToListAsync();

            var toNotify = newlyReady.Except(alreadyNotified).ToList();
            if (toNotify.Count == 0) return;

            var users = await _db.Users
                .Where(u => u.NotifyFileAdded)
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var userId in users)
            {
                var userMovieIds = await _db.UserMovies
                    .Where(um => um.UserId == userId)
                    .Select(um => um.MovieId)
                    .ToListAsync();

                foreach (var tmdbId in toNotify.Intersect(userMovieIds))
                {
                    _db.Notifications.Add(new NotificationEntity
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        ActorId = null,
                        EventType = "file_added",
                        MovieId = tmdbId,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
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

}

