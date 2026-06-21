namespace DataParserToDB.Data.Entities;

public class ReviewCommentEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ReviewId { get; set; } = null!;
    public Guid UserId { get; set; }
    public string? AuthorName { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ReviewEntity Review { get; set; } = null!;
}
