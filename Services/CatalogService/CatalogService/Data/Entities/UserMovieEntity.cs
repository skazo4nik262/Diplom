namespace CatalogService.Data.Entities;

public class UserMovieEntity
{
    public Guid UserId { get; set; }
    public int MovieId { get; set; }
    public string? Status { get; set; }
    public int? Rating { get; set; }
    public DateTime? WatchedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserEntity User { get; set; } = null!;
    public MovieEntity Movie { get; set; } = null!;
}
