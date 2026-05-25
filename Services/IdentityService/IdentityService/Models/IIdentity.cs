using IdentityService.Entities;

namespace IdentityService.Models
{
    public interface IIdentity
    {
        Task<User?> GetByLoginAsync(string login);
        Task<User> CreateAsync(string login, string password, int role = 1);
        Task<bool> AuthenticateAsync(string login, string password);
        Task<string?> GenerateTokenAsync(string login, string password); // ← Новый метод
        Task<bool> DeactivateAsync(Guid id);
    }
}
