namespace DataParserToDB.Data.Entities;

public class KeywordEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }

    public ICollection<MovieEntity> Movies { get; set; } = new List<MovieEntity>();
}
