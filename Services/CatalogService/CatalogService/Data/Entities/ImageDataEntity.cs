namespace CatalogService.Data.Entities;

public class ImageDataEntity
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public string? FilePath { get; set; }
    public double AspectRatio { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
    public string? Iso639_1 { get; set; }
    public string? Iso3166_1 { get; set; }
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
    public string Type { get; set; } = null!;

    public MovieEntity Movie { get; set; } = null!;
}
