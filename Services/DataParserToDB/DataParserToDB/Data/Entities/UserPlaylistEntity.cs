namespace DataParserToDB.Data.Entities;

public class UserPlaylistEntity
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserEntity User { get; set; } = null!;
    public ICollection<UserPlaylistItemEntity> Items { get; set; } = new List<UserPlaylistItemEntity>();
}
