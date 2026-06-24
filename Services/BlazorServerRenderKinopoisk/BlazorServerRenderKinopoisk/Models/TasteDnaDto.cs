using System.Text.Json.Serialization;

namespace BlazorServerRenderKinopoisk.Models;

public class TasteDnaDto
{
    [JsonPropertyName("genres")] public List<TasteDnaItemDto> Genres { get; set; } = [];
    [JsonPropertyName("keywords")] public List<TasteDnaItemDto> Keywords { get; set; } = [];
}

public class TasteDnaItemDto
{
    [JsonPropertyName("name")] public string Name { get; set; } = null!;
    [JsonPropertyName("score")] public double Score { get; set; }
}
