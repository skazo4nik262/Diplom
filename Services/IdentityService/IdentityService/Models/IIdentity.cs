using IdentityService.Entities;

namespace IdentityService.Models
{
    public interface IIdentity
    {
        Task<User?> GetByLoginAsync(string login);
        Task<User?> GetByIdAsync(Guid id);
        Task<User> CreateAsync(string login, string password, int role = 1, string? username = null);
        Task<bool> AuthenticateAsync(string login, string password);
        Task<string?> GenerateTokenAsync(string login, string password);
        Task<bool> DeactivateAsync(Guid id);
        Task<bool> ActivateAsync(Guid id);
        Task<User?> SetRoleAsync(Guid id, int role);
        Task<int> GetTokenVersionAsync(Guid id);
        Task<User?> UpdateProfileAsync(Guid id, string? username, string? bio, DateTime? birthday, string? avatarUrl);
        Task<List<User>> SearchUsersAsync(string query);
        Task<List<User>> SearchAllUsersAsync(string query);
    }
}
