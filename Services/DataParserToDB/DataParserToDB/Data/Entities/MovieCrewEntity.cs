namespace DataParserToDB.Data.Entities;

public class MovieCrewEntity
{
    public int MovieId { get; set; }
    public int PersonId { get; set; }
    public string CreditId { get; set; } = null!;
    public string? Department { get; set; }
    public string? Job { get; set; }
    public bool Adult { get; set; }
    public string? Gender { get; set; }
    public string? KnownForDepartment { get; set; }
    public string? OriginalName { get; set; }
    public float Popularity { get; set; }
    public string? ProfilePath { get; set; }

    public MovieEntity Movie { get; set; } = null!;
    public PersonEntity Person { get; set; } = null!;
}
