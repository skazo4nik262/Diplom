namespace DataParserToDB.Data.Entities;

public class AlternativeTitleEntity
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public string? Iso3166_1 { get; set; }
    public string? Title { get; set; }
    public string? Type { get; set; }

    public MovieEntity Movie { get; set; } = null!;
}
