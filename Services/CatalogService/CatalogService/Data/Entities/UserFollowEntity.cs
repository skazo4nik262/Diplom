namespace CatalogService.Data.Entities;

public class UserFollowEntity
{
    public Guid UserId { get; set; }
    public Guid FollowedUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserEntity User { get; set; } = null!;
    public UserEntity FollowedUser { get; set; } = null!;
}
