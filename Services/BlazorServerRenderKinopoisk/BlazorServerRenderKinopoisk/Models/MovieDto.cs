using System.Text.Json.Serialization;

namespace BlazorServerRenderKinopoisk.Models
{
    public class MovieDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("posterPath")] public string? PosterPath { get; set; }
        [JsonPropertyName("releaseDate")] public DateTime? ReleaseDate { get; set; }
        [JsonPropertyName("voteAverage")] public double VoteAverage { get; set; }
        [JsonPropertyName("overview")] public string? Overview { get; set; }
        [JsonPropertyName("runtime")] public int? Runtime { get; set; }
        [JsonPropertyName("genres")] public List<GenreDto>? Genres { get; set; }

        public string PosterUrl => PosterPath is not null
            ? $"http://screeny.ddns.net/api/catalog/poster/w500{PosterPath}" : "";
        public string RatingText => $"{VoteAverage:F1}";
        public string ReleaseYear => ReleaseDate?.Year.ToString() ?? "";
    }

    public class GenreDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }
}
