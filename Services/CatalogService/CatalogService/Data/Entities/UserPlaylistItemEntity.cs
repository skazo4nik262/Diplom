namespace CatalogService.Data.Entities;

public class UserPlaylistItemEntity
{
    public int PlaylistId { get; set; }
    public int MovieId { get; set; }
    public int Order { get; set; }
    public DateTime AddedAt { get; set; }

    public UserPlaylistEntity Playlist { get; set; } = null!;
    public MovieEntity Movie { get; set; } = null!;
}
