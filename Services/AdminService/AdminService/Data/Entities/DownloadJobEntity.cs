namespace AdminService.Data.Entities;

public class DownloadJobEntity
{
    public int Id { get; set; }
    public int TmdbId { get; set; }
    public string MagnetUrl { get; set; } = "";
    public string Status { get; set; } = "downloading"; // downloading / done / error
    public int? TransmissionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
