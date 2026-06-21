using IdentityService.Data;
using IdentityService.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Models
{
    public class Identity : IIdentity
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher _hasher;
        private readonly ITokenService _tokenService;

        public Identity(AppDbContext db, IPasswordHasher hasher, ITokenService tokenService)
        {
            _db = db;
            _hasher = hasher;
            _tokenService = tokenService;
        }

        public async Task<User?> GetByIdAsync(Guid id) =>
            await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

        public async Task<User?> GetByLoginAsync(string login) =>
            await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Login == login && u.IsActive);

        public async Task<User> CreateAsync(string login, string password, int role = 1, string? username = null)
        {
            if (await _db.Users.AnyAsync(u => u.Login == login))
                throw new InvalidOperationException("Login already exists");

            var user = new User
            {
                Login = login,
                Username = string.IsNullOrWhiteSpace(username) ? login : username,
                PasswordHash = _hasher.Hash(password),
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        public async Task<User?> UpdateProfileAsync(Guid id, string? username, string? bio, DateTime? birthday, string? avatarUrl)
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null || !user.IsActive) return null;

            if (username is not null) user.Username = username;
            if (bio is not null) user.Bio = bio;
            if (birthday is not null) user.Birthday = birthday;
            if (avatarUrl is not null) user.AvatarUrl = avatarUrl;

            await _db.SaveChangesAsync();
            return user;
        }

        public async Task<bool> AuthenticateAsync(string login, string password)
        {
            var user = await GetByLoginAsync(login);

            if (user is null)
            {
                await Task.Delay(Random.Shared.Next(150, 300));
                return false;
            }

            return _hasher.Verify(password, user.PasswordHash);
        }

        public async Task<bool> DeactivateAsync(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null || !user.IsActive) return false;

            user.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }
        public async Task<string?> GenerateTokenAsync(string login, string password)
        {
            var user = await GetByLoginAsync(login);

            if (user is null || !_hasher.Verify(password, user.PasswordHash))
            {
                await Task.Delay(Random.Shared.Next(150, 300));
                return null;
            }

            return _tokenService.GenerateToken(user);
        }

        public async Task<List<User>> SearchUsersAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return [];
            var lower = query.ToLower();
            return await _db.Users
                .AsNoTracking()
                .Where(u => u.IsActive && (u.Login.ToLower().Contains(lower) || u.Username!.ToLower().Contains(lower)))
                .Take(20)
                .ToListAsync();
        }
    }
}
