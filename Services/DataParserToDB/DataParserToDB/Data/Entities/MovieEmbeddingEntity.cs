namespace DataParserToDB.Data.Entities;

public class MovieEmbeddingEntity
{
    public int MovieId { get; set; }
    public float[] Embedding { get; set; } = [];
    public DateTime UpdatedAt { get; set; }

    public MovieEntity Movie { get; set; } = null!;
}
