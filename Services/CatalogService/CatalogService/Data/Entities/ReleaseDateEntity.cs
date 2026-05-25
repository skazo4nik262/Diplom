namespace CatalogService.Data.Entities;

public class ReleaseDateEntity
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public string? Iso3166_1 { get; set; }
    public string? Certification { get; set; }
    public string? Iso639_1 { get; set; }
    public string? Note { get; set; }
    public DateTime ReleaseDate { get; set; }
    public int Type { get; set; }

    public MovieEntity Movie { get; set; } = null!;
}
