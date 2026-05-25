using IdentityService.Entities;

namespace IdentityService.Models
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}
