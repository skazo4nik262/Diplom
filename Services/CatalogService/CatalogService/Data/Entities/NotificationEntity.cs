namespace CatalogService.Data.Entities;

public class NotificationEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ActorId { get; set; }
    public string EventType { get; set; } = null!;
    public int? MovieId { get; set; }
    public string? ReviewId { get; set; }
    public int? PlaylistId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserEntity User { get; set; } = null!;
    public MovieEntity? Movie { get; set; }
}
