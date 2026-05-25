namespace CatalogService.Data.Entities;

public class KeywordEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<MovieEntity> Movies { get; set; } = new List<MovieEntity>();
}
