namespace DataParserToDB.Data.Entities;

public class ReviewEntity
{
    public string Id { get; set; } = null!;
    public int MovieId { get; set; }
    public Guid? UserId { get; set; }
    public string? Author { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorUsername { get; set; }
    public string? AuthorAvatarPath { get; set; }
    public double? AuthorRating { get; set; }
    public string? Content { get; set; }
    public string? Url { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Iso639_1 { get; set; }
    public int MediaId { get; set; }
    public string? MediaTitle { get; set; }
    public string? MediaType { get; set; }

    public MovieEntity Movie { get; set; } = null!;
    public UserEntity? User { get; set; }
}
