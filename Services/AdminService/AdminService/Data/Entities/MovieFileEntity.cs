namespace AdminService.Data.Entities;

public class MovieFileEntity
{
    public int Id { get; set; }
    public int TmdbId { get; set; }
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public long SizeBytes { get; set; }
    public Guid? JellyfinItemId { get; set; }
    public bool IsReady { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
