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

        public string PosterUrl => PosterPath is not null
            ? $"https://image.tmdb.org/t/p/w500{PosterPath}" : "";
        public string RatingText => $"{VoteAverage:F1}";
        public string ReleaseYear => ReleaseDate?.Year.ToString() ?? "";
    }
}
