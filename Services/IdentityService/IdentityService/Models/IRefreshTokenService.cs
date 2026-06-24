using IdentityService.Entities;

namespace IdentityService.Models;

public interface IRefreshTokenService
{
    Task<RefreshTokenEntity> GenerateRefreshTokenAsync(User user);
    Task<RefreshTokenEntity?> ValidateRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token);
    Task RevokeAllUserTokensAsync(Guid userId);
}
