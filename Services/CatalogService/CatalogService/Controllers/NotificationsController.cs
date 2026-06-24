using CatalogService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

[Route("api/catalog/notifications")]
[ApiController]
public class NotificationsController : ControllerBase
{
    private readonly IPostgresService _postgres;

    public NotificationsController(IPostgresService postgres)
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
    public async Task<IActionResult> GetNotifications([FromQuery] bool? unreadOnly, [FromQuery] int page = 1)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var notifications = await _postgres.GetNotificationsAsync(userId, unreadOnly, page);
        var unreadCount = await _postgres.GetUnreadNotificationCountAsync(userId);

        return Ok(new
        {
            Items = notifications.Select(n => new
            {
                n.Id,
                n.EventType,
                n.ActorId,
                n.MovieId,
                n.ReviewId,
                n.PlaylistId,
                n.IsRead,
                n.CreatedAt,
                Movie = n.Movie is null ? null : new
                {
                    n.Movie.Id,
                    n.Movie.Title,
                    n.Movie.PosterPath
                }
            }),
            UnreadCount = unreadCount
        });
    }

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId)
    {
        await _postgres.MarkNotificationReadAsync(notificationId);
        return Ok();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();
        await _postgres.MarkAllNotificationsReadAsync(userId);
        return Ok();
    }

    [HttpPost("check-new-content")]
    public async Task<IActionResult> CheckNewContent()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _postgres.CheckNewCollectionMoviesAsync();
        await _postgres.CheckNewVideosAsync();
        await _postgres.CheckNewFilesAsync();
        return Ok();
    }
}
