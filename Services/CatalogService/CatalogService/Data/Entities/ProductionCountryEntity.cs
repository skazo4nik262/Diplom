namespace CatalogService.Data.Entities;

public class ProductionCountryEntity
{
    public string Iso3166_1 { get; set; } = null!;
    public string? Name { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<MovieEntity> Movies { get; set; } = new List<MovieEntity>();
}
