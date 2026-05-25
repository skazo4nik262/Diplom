namespace DataParserToDB.Data.Entities;

public class UserEntity
{
    public Guid Id { get; set; }
    public string Login { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public int Role { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<UserMovieEntity> UserMovies { get; set; } = new List<UserMovieEntity>();
    public ICollection<UserPlaylistEntity> Playlists { get; set; } = new List<UserPlaylistEntity>();
}
