using System.Text.Json.Serialization;

namespace BlazorServerRenderKinopoisk.Models;

public class UserMovieDto
{
    [JsonPropertyName("movieId")] public int MovieId { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("rating")] public int? Rating { get; set; }
    [JsonPropertyName("movie")] public MovieDto? Movie { get; set; }
}
