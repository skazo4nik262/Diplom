using CatalogService.Data.Entities;
using CatalogService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

[Route("api/catalog/playlists")]
[ApiController]
public class PlaylistsController : ControllerBase
{
    private readonly IPostgresService _postgres;

    public PlaylistsController(IPostgresService postgres)
    {
        _postgres = postgres;
    }

    private Guid GetUserId()
    {
        if (Request.Headers.TryGetValue("X-User-Id", out var value) && Guid.TryParse(value, out var userId))
            return userId;
        return Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetPlaylists()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var playlists = await _postgres.GetPlaylistsAsync(userId);
        return Ok(playlists);
    }

    [HttpGet("{playlistId:int}")]
    public async Task<IActionResult> GetPlaylist(int playlistId)
    {
        var playlist = await _postgres.GetPlaylistAsync(playlistId);
        if (playlist is null) return NotFound();
        return Ok(playlist);
    }

    [HttpGet("{playlistId:int}/suggestions")]
    public async Task<IActionResult> GetSuggestions(int playlistId, [FromQuery] int count = 5)
    {
        var suggestions = await _postgres.GetPlaylistSuggestionsAsync(playlistId, count);
        return Ok(suggestions);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlaylist([FromBody] CreatePlaylistRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _postgres.AddPlaylistAsync(userId, request.Name, request.Description, request.TmdbIds ?? []);
        return Ok();
    }

    [HttpPost("{playlistId:int}/items")]
    public async Task<IActionResult> AddMovie(int playlistId, [FromBody] AddMovieRequest request)
    {
        await _postgres.AddMediaToPlaylistAsync(playlistId, request.TmdbId);
        return Ok();
    }

    [HttpDelete("{playlistId:int}/items/{tmdbId:int}")]
    public async Task<IActionResult> RemoveMovie(int playlistId, int tmdbId)
    {
        await _postgres.RemoveMediaFromPlaylistAsync(playlistId, tmdbId);
        return Ok();
    }

    [HttpDelete("{playlistId:int}")]
    public async Task<IActionResult> DeletePlaylist(int playlistId)
    {
        await _postgres.DeletePlaylistAsync(playlistId);
        return Ok();
    }

    [HttpPut("{playlistId:int}")]
    public async Task<IActionResult> UpdatePlaylist(int playlistId, [FromBody] UpdatePlaylistRequest request)
    {
        await _postgres.UpdatePlaylistAsync(playlistId, request.Name, request.Description);
        return Ok();
    }
}

public record CreatePlaylistRequest(string Name, string? Description, IEnumerable<int>? TmdbIds);
public record AddMovieRequest(int TmdbId);
public record UpdatePlaylistRequest(string Name, string? Description);
