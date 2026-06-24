using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminService.Data;

namespace AdminService.Controllers;

[ApiController]
[Route("api/admin/movies")]
public class MovieFilesController : ControllerBase
{
    private readonly AdminDbContext _db;

    public MovieFilesController(AdminDbContext db)
    {
        _db = db;
    }

    [HttpGet("{tmdbId}/file")]
    public async Task<IActionResult> GetMovieFile(int tmdbId)
    {
        var file = await _db.MovieFiles
            .Where(f => f.TmdbId == tmdbId)
            .OrderByDescending(f => f.CreatedAt)
            .FirstOrDefaultAsync();

        if (file == null)
            return NotFound(new { error = "No file uploaded" });

        return Ok(new
        {
            file.JellyfinItemId,
            file.IsReady,
            file.FileName,
            file.SizeBytes
        });
    }
}
