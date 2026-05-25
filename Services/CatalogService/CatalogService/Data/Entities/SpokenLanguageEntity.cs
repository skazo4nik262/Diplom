namespace CatalogService.Data.Entities;

public class SpokenLanguageEntity
{
    public string Iso639_1 { get; set; } = null!;
    public string? Name { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<MovieEntity> Movies { get; set; } = new List<MovieEntity>();
}
