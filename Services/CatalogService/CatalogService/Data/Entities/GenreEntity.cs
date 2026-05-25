using System.Text.Json.Serialization;

namespace CatalogService.Data.Entities;

public class GenreEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }

    [JsonIgnore]
    public ICollection<MovieEntity> Movies { get; set; } = new List<MovieEntity>();
}
