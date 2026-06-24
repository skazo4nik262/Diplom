using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminService.Data;
using AdminService.Data.Entities;
using AdminService.Services;

namespace AdminService.Controllers;

[ApiController]
[Route("api/admin/movies")]
public class DownloadController : ControllerBase
{
    private readonly AdminDbContext _db;
    private readonly TransmissionClient _transmission;

    public DownloadController(AdminDbContext db, TransmissionClient transmission)
    {
        _db = db;
        _transmission = transmission;
    }

    [HttpPost("download")]
    public async Task<IActionResult> DownloadViaTorrent([FromBody] DownloadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MagnetUrl) || !request.MagnetUrl.StartsWith("magnet:"))
            return BadRequest(new { error = "Invalid magnet URL" });

        var existingJob = await _db.DownloadJobs
            .Where(j => j.TmdbId == request.TmdbId && j.Status == "downloading")
            .FirstOrDefaultAsync();

        if (existingJob != null)
            return Conflict(new { error = "Already downloading", jobId = existingJob.Id });

        var transmissionId = await _transmission.AddTorrentAsync(request.MagnetUrl, "/downloads");

        var job = new DownloadJobEntity
        {
            TmdbId = request.TmdbId,
            MagnetUrl = request.MagnetUrl,
            Status = "downloading",
            TransmissionId = transmissionId,
            CreatedAt = DateTime.UtcNow
        };

        _db.DownloadJobs.Add(job);
        await _db.SaveChangesAsync();

        return Ok(new { jobId = job.Id, status = "downloading" });
    }
}

public record DownloadRequest(int TmdbId, string MagnetUrl);
