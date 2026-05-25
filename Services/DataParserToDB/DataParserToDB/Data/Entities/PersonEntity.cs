namespace DataParserToDB.Data.Entities;

public class PersonEntity
{
    public int Id { get; set; }
    public bool Adult { get; set; }
    public string? Biography { get; set; }
    public DateTime? Birthday { get; set; }
    public DateTime? Deathday { get; set; }
    public string? Gender { get; set; }
    public string? Homepage { get; set; }
    public string? ImdbId { get; set; }
    public string? KnownForDepartment { get; set; }
    public string? Name { get; set; }
    public string? PlaceOfBirth { get; set; }
    public double Popularity { get; set; }
    public string? ProfilePath { get; set; }

    public ExternalIdsEntity? ExternalIds { get; set; }

    public ICollection<MovieCastEntity> CastMovies { get; set; } = new List<MovieCastEntity>();
    public ICollection<MovieCrewEntity> CrewMovies { get; set; } = new List<MovieCrewEntity>();
}
