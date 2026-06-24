using CatalogService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

[Route("api/catalog/friends")]
[ApiController]
public class FriendsController : ControllerBase
{
    private readonly IPostgresService _postgres;

    public FriendsController(IPostgresService postgres)
    {
        _postgres = postgres;
    }

    private Guid GetUserId()
    {
        if (Request.Headers.TryGetValue("X-User-Id", out var value) && Guid.TryParse(value, out var userId))
            return userId;
        return Guid.Empty;
    }

    [HttpPost("{targetUserId:guid}/follow")]
    public async Task<IActionResult> Follow(Guid targetUserId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();
        var targetUser = await _postgres.GetUserByIdAsync(targetUserId);
        if (targetUser is null) return NotFound(new { error = "Пользователь не найден" });
        if (targetUser.Role == 0) return BadRequest(new { error = "Нельзя подписаться на администратора" });
        await _postgres.FollowUserAsync(userId, targetUserId);
        await _postgres.RecordActivityAsync(userId, "followed_user");
        await _postgres.CreateNotificationAsync(targetUserId, userId, "followed_user");
        return Ok();
    }

    [HttpDelete("{targetUserId:guid}/unfollow")]
    public async Task<IActionResult> Unfollow(Guid targetUserId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();
        await _postgres.UnfollowUserAsync(userId, targetUserId);
        return Ok();
    }

    [HttpGet("following")]
    public async Task<IActionResult> GetFollowing()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();
        var ids = await _postgres.GetFollowingIdsAsync(userId);
        var users = await _postgres.GetUserBatchAsync(ids);
        return Ok(users.Select(u => new
        {
            u.Id, u.Login, u.Username, u.AvatarUrl
        }));
    }

    [HttpGet("followers")]
    public async Task<IActionResult> GetFollowers()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();
        var ids = await _postgres.GetFollowerIdsAsync(userId);
        var users = await _postgres.GetUserBatchAsync(ids);
        return Ok(users.Select(u => new
        {
            u.Id, u.Login, u.Username, u.AvatarUrl
        }));
    }

    [HttpGet("{targetUserId:guid}/status")]
    public async Task<IActionResult> GetFollowStatus(Guid targetUserId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();
        var isFollowing = await _postgres.IsFollowingAsync(userId, targetUserId);
        var followingCount = await _postgres.GetFollowingCountAsync(targetUserId);
        var followerCount = await _postgres.GetFollowerCountAsync(targetUserId);
        return Ok(new { isFollowing, followingCount, followerCount });
    }
}
