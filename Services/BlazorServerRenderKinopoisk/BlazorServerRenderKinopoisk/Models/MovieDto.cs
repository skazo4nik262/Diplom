using System.Text.Json.Serialization;

namespace BlazorServerRenderKinopoisk.Models
{
    public class MovieDto
    {
        private static readonly Dictionary<string, string> CertificationMap = new()
        {
            ["G"] = "0+",
            ["PG"] = "12+",
            ["PG-13"] = "13+",
            ["R"] = "17+",
            ["NC-17"] = "18+",
        };

        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("posterPath")] public string? PosterPath { get; set; }
        [JsonPropertyName("releaseDate")] public DateTime? ReleaseDate { get; set; }
        [JsonPropertyName("voteAverage")] public double VoteAverage { get; set; }
        [JsonPropertyName("overview")] public string? Overview { get; set; }
        [JsonPropertyName("runtime")] public int? Runtime { get; set; }
        [JsonPropertyName("genres")] public List<GenreDto>? Genres { get; set; }
        [JsonPropertyName("adult")] public bool Adult { get; set; }
        [JsonPropertyName("productionCountries")] public List<ProductionCountryDto>? ProductionCountries { get; set; }
        [JsonPropertyName("releaseDates")] public List<ReleaseDateDto>? ReleaseDates { get; set; }
        [JsonPropertyName("budget")] public long Budget { get; set; }
        [JsonPropertyName("revenue")] public long Revenue { get; set; }
        [JsonPropertyName("videos")] public List<VideoDto>? Videos { get; set; }
        [JsonPropertyName("images")] public List<ImageDto>? Images { get; set; }

        public string PosterUrl => PosterPath is not null
            ? $"http://screeny.ddns.net/api/catalog/poster/w500{PosterPath}" : "";
        public string RatingText => $"{VoteAverage:F1}";
        public string ReleaseYear => ReleaseDate?.Year.ToString() ?? "";
        public string AgeRating => ReleaseDates?
            .FirstOrDefault(r => r.Iso3166_1 == "US" && r.Type == 3 && !string.IsNullOrEmpty(r.Certification))?
            .Certification is { } cert && CertificationMap.TryGetValue(cert, out var age)
            ? age : Adult ? "18+" : "";
        public string BudgetText => Budget > 0 ? $"${Budget:N0}" : "";
        public string RevenueText => Revenue > 0 ? $"${Revenue:N0}" : "";
    }

    public class GenreDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    public class ProductionCountryDto
    {
        [JsonPropertyName("iso3166_1")] public string? Iso3166_1 { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    public class ReleaseDateDto
    {
        [JsonPropertyName("iso3166_1")] public string? Iso3166_1 { get; set; }
        [JsonPropertyName("certification")] public string? Certification { get; set; }
        [JsonPropertyName("type")] public int Type { get; set; }
    }

    public class CastDto
    {
        [JsonPropertyName("personId")] public int PersonId { get; set; }
        [JsonPropertyName("originalName")] public string? OriginalName { get; set; }
        [JsonPropertyName("character")] public string? Character { get; set; }
        [JsonPropertyName("profilePath")] public string? ProfilePath { get; set; }
        [JsonPropertyName("order")] public int Order { get; set; }

        public string ProfileUrl => ProfilePath is not null
            ? $"http://screeny.ddns.net/api/catalog/poster/w185{ProfilePath}" : "";
    }

    public class CrewDto
    {
        [JsonPropertyName("personId")] public int PersonId { get; set; }
        [JsonPropertyName("department")] public string? Department { get; set; }
        [JsonPropertyName("job")] public string? Job { get; set; }
        [JsonPropertyName("originalName")] public string? OriginalName { get; set; }
        [JsonPropertyName("profilePath")] public string? ProfilePath { get; set; }
    }

    public class CollectionDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("overview")] public string? Overview { get; set; }
        [JsonPropertyName("posterPath")] public string? PosterPath { get; set; }
        [JsonPropertyName("backdropPath")] public string? BackdropPath { get; set; }
        [JsonPropertyName("movies")] public List<MovieDto>? Movies { get; set; }

        public string PosterUrl => PosterPath is not null
            ? $"http://screeny.ddns.net/api/catalog/poster/w500{PosterPath}" : "";
    }

    public class PersonDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("biography")] public string? Biography { get; set; }
        [JsonPropertyName("birthday")] public DateTime? Birthday { get; set; }
        [JsonPropertyName("deathday")] public DateTime? Deathday { get; set; }
        [JsonPropertyName("placeOfBirth")] public string? PlaceOfBirth { get; set; }
        [JsonPropertyName("profilePath")] public string? ProfilePath { get; set; }
        [JsonPropertyName("knownForDepartment")] public string? KnownForDepartment { get; set; }
        [JsonPropertyName("homepage")] public string? Homepage { get; set; }
        [JsonPropertyName("imdbId")] public string? ImdbId { get; set; }
        [JsonPropertyName("externalIds")] public ExternalIdsDto? ExternalIds { get; set; }

        public string ProfileUrl => ProfilePath is not null
            ? $"http://screeny.ddns.net/api/catalog/poster/w500{ProfilePath}" : "";
        public string BirthYear => Birthday?.Year.ToString() ?? "";
        public bool IsDead => Deathday is not null;
    }

    public class ExternalIdsDto
    {
        [JsonPropertyName("facebookId")] public string? FacebookId { get; set; }
        [JsonPropertyName("twitterId")] public string? TwitterId { get; set; }
        [JsonPropertyName("instagramId")] public string? InstagramId { get; set; }
        [JsonPropertyName("wikidataId")] public string? WikidataId { get; set; }
    }

    public class ReviewDto
    {
        [JsonPropertyName("id")] public string Id { get; set; } = null!;
        [JsonPropertyName("movieId")] public int MovieId { get; set; }
        [JsonPropertyName("author")] public string? Author { get; set; }
        [JsonPropertyName("authorRating")] public double? AuthorRating { get; set; }
        [JsonPropertyName("content")] public string? Content { get; set; }
        [JsonPropertyName("createdAt")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updatedAt")] public DateTime? UpdatedAt { get; set; }
    }

    public class PlaylistDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
        [JsonPropertyName("items")] public List<PlaylistItemDto>? Items { get; set; }
        public int MovieCount => Items?.Count ?? 0;
    }

    public class PlaylistItemDto
    {
        [JsonPropertyName("movieId")] public int MovieId { get; set; }
        [JsonPropertyName("order")] public int Order { get; set; }
        [JsonPropertyName("addedAt")] public DateTime AddedAt { get; set; }
        [JsonPropertyName("movie")] public MovieDto? Movie { get; set; }
    }

    public class UserMovieStatusDto
    {
        [JsonPropertyName("rating")] public int? Rating { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
    }

    public class PlaylistCreateDto
    {
        [JsonPropertyName("name")] public string Name { get; set; } = null!;
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("tmdbIds")] public List<int>? TmdbIds { get; set; }
    }

    public class VideoDto
    {
        [JsonPropertyName("id")] public string Id { get; set; } = null!;
        [JsonPropertyName("movieId")] public int MovieId { get; set; }
        [JsonPropertyName("key")] public string? Key { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("site")] public string? Site { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("official")] public bool Official { get; set; }
        [JsonPropertyName("publishedAt")] public DateTime PublishedAt { get; set; }
    }

    public class ImageDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("movieId")] public int MovieId { get; set; }
        [JsonPropertyName("filePath")] public string? FilePath { get; set; }
        [JsonPropertyName("width")] public int Width { get; set; }
        [JsonPropertyName("height")] public int Height { get; set; }
        [JsonPropertyName("aspectRatio")] public double AspectRatio { get; set; }
        [JsonPropertyName("iso639_1")] public string? Iso639_1 { get; set; }
        [JsonPropertyName("voteAverage")] public double VoteAverage { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }

        public string ImageUrl => FilePath is not null
            ? $"http://screeny.ddns.net/api/catalog/poster/original{FilePath}" : "";
    }
}
