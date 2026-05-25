namespace DataParserToDB.Data.Entities;

public class VideoEntity
{
    public string Id { get; set; } = null!;
    public int MovieId { get; set; }
    public string? Iso3166_1 { get; set; }
    public string? Iso639_1 { get; set; }
    public string? Key { get; set; }
    public string? Name { get; set; }
    public bool Official { get; set; }
    public DateTime PublishedAt { get; set; }
    public string? Site { get; set; }
    public int Size { get; set; }
    public string? Type { get; set; }

    public MovieEntity Movie { get; set; } = null!;
}
