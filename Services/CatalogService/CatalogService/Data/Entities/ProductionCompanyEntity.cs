namespace CatalogService.Data.Entities;

public class ProductionCompanyEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? LogoPath { get; set; }
    public string? OriginCountry { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<MovieEntity> Movies { get; set; } = new List<MovieEntity>();
}
