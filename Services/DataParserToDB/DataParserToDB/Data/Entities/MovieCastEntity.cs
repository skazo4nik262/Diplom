namespace DataParserToDB.Data.Entities;

public class MovieCastEntity
{
    public int MovieId { get; set; }
    public int PersonId { get; set; }
    public string CreditId { get; set; } = null!;
    public int? CastId { get; set; }
    public string? Character { get; set; }
    public int Order { get; set; }
    public bool Adult { get; set; }
    public string? Gender { get; set; }
    public string? KnownForDepartment { get; set; }
    public string? OriginalName { get; set; }
    public float Popularity { get; set; }
    public string? ProfilePath { get; set; }

    public MovieEntity Movie { get; set; } = null!;
    public PersonEntity Person { get; set; } = null!;
}
