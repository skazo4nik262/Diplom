namespace CatalogService.Data.Entities;

public class UserEntity
{
    public Guid Id { get; set; }
    public string Login { get; set; } = null!;
    public string? Username { get; set; }
    public string PasswordHash { get; set; } = null!;
    public int Role { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public string? Bio { get; set; }
    public DateTime? Birthday { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<UserMovieEntity> UserMovies { get; set; } = new List<UserMovieEntity>();
    public ICollection<UserPlaylistEntity> Playlists { get; set; } = new List<UserPlaylistEntity>();
    public ICollection<UserFollowEntity> Followers { get; set; } = new List<UserFollowEntity>();
    public ICollection<UserFollowEntity> Following { get; set; } = new List<UserFollowEntity>();
    public ICollection<NotificationEntity> Notifications { get; set; } = new List<NotificationEntity>();
    public ICollection<ActivityEventEntity> ActivityEvents { get; set; } = new List<ActivityEventEntity>();
}
