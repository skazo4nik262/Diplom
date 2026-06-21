namespace DataParserToDB.Data.Entities;

public class ReviewLikeEntity
{
    public string ReviewId { get; set; } = null!;
    public Guid UserId { get; set; }
    public bool IsPositive { get; set; }

    public ReviewEntity Review { get; set; } = null!;
}
