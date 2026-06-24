using IdentityService.Data;
using IdentityService.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace IdentityService.Models;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly AppDbContext _db;

    public RefreshTokenService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RefreshTokenEntity> GenerateRefreshTokenAsync(User user)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var token = Convert.ToBase64String(randomBytes);

        var entity = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<RefreshTokenEntity?> ValidateRefreshTokenAsync(string token)
    {
        var now = DateTime.UtcNow;
        return await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token
                && rt.RevokedAt == null
                && rt.ExpiresAt > now);
    }

    public async Task RevokeRefreshTokenAsync(string token)
    {
        var rt = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
        if (rt != null)
        {
            rt.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId)
    {
        var activeTokens = await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var rt in activeTokens)
            rt.RevokedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }
}
