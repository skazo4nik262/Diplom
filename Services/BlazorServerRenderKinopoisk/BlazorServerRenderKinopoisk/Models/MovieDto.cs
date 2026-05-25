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

        public string PosterUrl => PosterPath is not null
            ? $"http://screeny.ddns.net/api/catalog/poster/w500{PosterPath}" : "";
        public string RatingText => $"{VoteAverage:F1}";
        public string ReleaseYear => ReleaseDate?.Year.ToString() ?? "";
        public string AgeRating => ReleaseDates?
            .FirstOrDefault(r => r.Iso3166_1 == "US" && r.Type == 3 && !string.IsNullOrEmpty(r.Certification))?
            .Certification is { } cert && CertificationMap.TryGetValue(cert, out var age)
            ? age : Adult ? "18+" : "";
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
}
