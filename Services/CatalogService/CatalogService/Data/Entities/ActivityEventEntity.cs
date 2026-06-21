namespace CatalogService.Data.Entities;

public class ActivityEventEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string EventType { get; set; } = null!;
    public int? MovieId { get; set; }
    public string? ReviewId { get; set; }
    public int? PlaylistId { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserEntity User { get; set; } = null!;
    public MovieEntity? Movie { get; set; }
}
