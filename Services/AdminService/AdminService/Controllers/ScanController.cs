using Microsoft.AspNetCore.Mvc;
using AdminService.Services;

namespace AdminService.Controllers;

[ApiController]
[Route("api/admin/movies/scan")]
public class ScanController : ControllerBase
{
    private readonly LibraryScanner _scanner;

    public ScanController(LibraryScanner scanner)
    {
        _scanner = scanner;
    }

    [HttpPost]
    public IActionResult StartScan()
    {
        var message = _scanner.StartScan();
        var status = _scanner.GetStatus();
        return Ok(new { message, status });
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var status = _scanner.GetStatus();
        return Ok(new
        {
            status.IsRunning,
            status.IsComplete,
            status.FilesFound,
            status.Processed,
            status.NewRecords,
            status.Errors,
            status.StartedAt,
            status.CompletedAt
        });
    }
}
