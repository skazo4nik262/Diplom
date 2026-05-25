using System.Text.Json.Serialization;

namespace CatalogService.Data.Entities;

public class CollectionEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }

    [JsonIgnore]
    public ICollection<MovieEntity> Movies { get; set; } = new List<MovieEntity>();
}
