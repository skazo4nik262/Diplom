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

        public async Task<List<MovieEntity>> SearchMoviesAsync(string query, int page)
        {
            return await _db.Movies
                .Include(m => m.Collection)
                .Include(m => m.Genres)
                .Where(m => m.Title != null && EF.Functions.ILike(m.Title, $"%{query}%")
                         || m.OriginalTitle != null && EF.Functions.ILike(m.OriginalTitle, $"%{query}%"))
                .OrderByDescending(m => m.Popularity)
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
            return await _db.Movies
                .Where(m => m.Id == tmdbId)
                .Select(m => m.Collection)
                .FirstOrDefaultAsync();
        }

        public async Task<List<MovieCrewEntity>> GetMovieCrewAsync(int tmdbId)
        {
            return await _db.MovieCrew
                .Include(c => c.Person)
                .Where(c => c.MovieId == tmdbId)
                .ToListAsync();
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
            return await _db.Movies
                .Include(m => m.Collection)
                .Include(m => m.Genres)
                .OrderByDescending(m => m.VoteAverage)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
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
        public async Task RebuildAllEmbeddingsAsync()
        {
            var movieIds = await _db.Movies
                .Where(m => !_db.MovieEmbeddings.Any(e => e.MovieId == m.Id))
                .Select(m => m.Id)
                .ToListAsync();

            foreach (var id in movieIds)
                await EnsureEmbeddingsAsync(id);
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
            }
            else
            {
                _db.UserMovies.Add(new UserMovieEntity
                {
                    UserId = userId,
                    MovieId = tmdbId,
                    Status = status,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
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
            var embedParam = new Pgvector.Vector(target);
            var offset = (page - 1) * PageSize;

            var ids = await _db.Database.SqlQueryRaw<int>(
                "SELECT e.\"MovieId\" FROM \"MovieEmbeddings\" e ORDER BY e.\"Embedding\" <=> {0} LIMIT {1} OFFSET {2}",
                embedParam, PageSize + exclude.Count, 0).ToListAsync();

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
    }
}
