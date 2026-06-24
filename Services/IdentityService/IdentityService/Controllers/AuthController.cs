using IdentityService.Entities;
using IdentityService.Models;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers
{
    public record RegisterRequest(string Login, string Password, string? Username = null, int Role = 1);
    public record LoginRequest(string Login, string Password);
    public record AuthResponse(string Token, Guid UserId, string Login, string? Username, int Role, string RefreshToken, DateTime RefreshTokenExpiresAt);
    public record RefreshRequest(string RefreshToken);
    public record RefreshResponse(string Token, string RefreshToken, DateTime RefreshTokenExpiresAt);
    public record UserResponse(Guid Id, string Login, string? Username, int Role, bool IsActive = true, string? AvatarUrl = null);
    public record ProfileResponse(Guid Id, string Login, string? Username, string? Bio, DateTime? Birthday, string? AvatarUrl, int Role);
    public record UpdateProfileRequest(string? Username, string? Bio, DateTime? Birthday, string? AvatarUrl);

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IIdentity _identityService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IIdentity identityService,
            IRefreshTokenService refreshTokenService,
            ITokenService tokenService,
            ILogger<AuthController> logger)
        {
            _identityService = identityService;
            _refreshTokenService = refreshTokenService;
            _tokenService = tokenService;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            if (Request.Headers.TryGetValue("X-User-Id", out var value) && Guid.TryParse(value, out var userId))
                return userId;
            return Guid.Empty;
        }

        private int GetUserRole()
        {
            if (Request.Headers.TryGetValue("X-User-Role", out var value) && int.TryParse(value, out var role))
                return role;
            return 1;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserResponse>> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Login) || request.Login.Length < 3)
                return BadRequest(new { error = "Login must be at least 3 characters" });

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
                return BadRequest(new { error = "Пароль должен содержать минимум 8 символов" });
            if (!request.Password.Any(char.IsUpper))
                return BadRequest(new { error = "Пароль должен содержать хотя бы одну заглавную букву" });
            if (!request.Password.Any(char.IsLower))
                return BadRequest(new { error = "Пароль должен содержать хотя бы одну строчную букву" });
            if (!request.Password.Any(char.IsDigit))
                return BadRequest(new { error = "Пароль должен содержать хотя бы одну цифру" });
            if (!request.Password.Any(c => !char.IsLetterOrDigit(c)))
                return BadRequest(new { error = "Пароль должен содержать хотя бы один спецсимвол" });

            try
            {
                var user = await _identityService.CreateAsync(request.Login, request.Password, request.Role, request.Username);
                _logger.LogInformation("User registered: {Login}", user.Login);
                return Ok(new UserResponse(user.Id, user.Login, user.Username, user.Role, user.IsActive, user.AvatarUrl));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var token = await _identityService.GenerateTokenAsync(request.Login, request.Password);

            if (token is null)
                return Unauthorized(new { error = "Invalid credentials" });

            var user = await _identityService.GetByLoginAsync(request.Login);
            var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(user!);

            return Ok(new AuthResponse(
                Token: token,
                UserId: user!.Id,
                Login: user.Login,
                Username: user.Username,
                Role: user.Role,
                RefreshToken: refreshToken.Token,
                RefreshTokenExpiresAt: refreshToken.ExpiresAt
            ));
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<RefreshResponse>> Refresh([FromBody] RefreshRequest request)
        {
            var refreshTokenEntity = await _refreshTokenService.ValidateRefreshTokenAsync(request.RefreshToken);
            if (refreshTokenEntity is null)
                return Unauthorized(new { error = "Invalid or expired refresh token" });

            var user = refreshTokenEntity.User;
            if (!user.IsActive)
                return Unauthorized(new { error = "User is deactivated" });

            await _refreshTokenService.RevokeRefreshTokenAsync(request.RefreshToken);

            var newAccessToken = _tokenService.GenerateToken(user);
            var newRefreshTokenEntity = await _refreshTokenService.GenerateRefreshTokenAsync(user);

            return Ok(new RefreshResponse(
                Token: newAccessToken,
                RefreshToken: newRefreshTokenEntity.Token,
                RefreshTokenExpiresAt: newRefreshTokenEntity.ExpiresAt
            ));
        }

        [HttpGet("users/search")]
        public async Task<ActionResult<List<UserResponse>>> SearchUsers([FromQuery] string query, [FromQuery] bool includeInactive = false)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Ok(new List<UserResponse>());

            List<User> users;
            if (includeInactive && GetUserRole() == 0)
                users = await _identityService.SearchAllUsersAsync(query);
            else
                users = await _identityService.SearchUsersAsync(query);

            return Ok(users.Select(u => new UserResponse(u.Id, u.Login, u.Username, u.Role, u.IsActive, u.AvatarUrl)).ToList());
        }

        [HttpGet("users/{login}")]
        public async Task<ActionResult<UserResponse>> GetUser(string login)
        {
            var user = await _identityService.GetByLoginAsync(login);
            if (user is null) return NotFound();

            return Ok(new UserResponse(user.Id, user.Login, user.Username, user.Role, user.IsActive, user.AvatarUrl));
        }

        [HttpGet("users/id/{id:guid}")]
        public async Task<ActionResult<ProfileResponse>> GetUserById(Guid id)
        {
            var user = await _identityService.GetByIdAsync(id);
            if (user is null) return NotFound();

            return Ok(new ProfileResponse(user.Id, user.Login, user.Username, user.Bio, user.Birthday, user.AvatarUrl, user.Role));
        }

        [HttpPost("avatar")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized(new { error = "Invalid token" });

            if (file is null || file.Length == 0)
                return BadRequest(new { error = "Файл не выбран" });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { error = "Файл слишком большой. Максимальный размер — 5 МБ" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new HashSet<string> { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowedExtensions.Contains(ext))
                return BadRequest(new { error = "Недопустимый формат файла. Разрешены: jpg, png, gif, webp" });

            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.FileName);
            if (fileNameWithoutExt.Contains('.') || fileNameWithoutExt.Contains(".."))
                return BadRequest(new { error = "Недопустимое имя файла" });

            var allowedMimeTypes = new HashSet<string>
            {
                "image/jpeg", "image/png", "image/gif", "image/webp"
            };
            if (!allowedMimeTypes.Contains(file.ContentType))
                return BadRequest(new { error = "Недопустимый тип файла" });

            using (var stream = file.OpenReadStream())
            {
                var header = new byte[12];
                await stream.ReadExactlyAsync(header, 0, Math.Min(header.Length, (int)stream.Length));

                var validMagic = (header.Take(4).SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }) ||
                                  header.Take(4).SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 }) ||
                                  header.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }) ||
                                  header.Take(6).SequenceEqual(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }) ||
                                  header.Take(6).SequenceEqual(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }) ||
                                  (header.Take(4).SequenceEqual(new byte[] { 0x52, 0x49, 0x46, 0x46 }) &&
                                   header.Skip(8).Take(4).SequenceEqual(new byte[] { 0x57, 0x45, 0x42, 0x50 })));

                if (!validMagic)
                    return BadRequest(new { error = "Файл не является изображением" });
            }

            var avatarsDir = Path.Combine(Directory.GetCurrentDirectory(), "avatars");
            Directory.CreateDirectory(avatarsDir);

            var existingFiles = Directory.GetFiles(avatarsDir, $"{userId}.*");
            foreach (var f in existingFiles)
            {
                try { System.IO.File.Delete(f); } catch { }
            }

            var filePath = Path.Combine(avatarsDir, $"{userId}{ext}");
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var avatarUrl = $"/api/catalog/avatar/{userId}";
            return Ok(new { url = avatarUrl });
        }

        [HttpPost("profile")]
        public async Task<ActionResult<ProfileResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized(new { error = "Invalid token" });

            var user = await _identityService.UpdateProfileAsync(userId, request.Username, request.Bio, request.Birthday, request.AvatarUrl);
            if (user is null) return NotFound();

            return Ok(new ProfileResponse(user.Id, user.Login, user.Username, user.Bio, user.Birthday, user.AvatarUrl, user.Role));
        }

        [HttpDelete("users/{id:guid}")]
        public async Task<IActionResult> DeactivateUser(Guid id)
        {
            var result = await _identityService.DeactivateAsync(id);
            if (!result) return NotFound();

            return NoContent();
        }

        [HttpPatch("users/{id:guid}/reactivate")]
        public async Task<IActionResult> ReactivateUser(Guid id)
        {
            var result = await _identityService.ActivateAsync(id);
            if (!result) return NotFound();

            return NoContent();
        }

        [HttpPatch("users/{id:guid}/role")]
        public async Task<ActionResult<UserResponse>> SetUserRole(Guid id, [FromBody] SetRoleRequest request)
        {
            var user = await _identityService.SetRoleAsync(id, request.Role);
            if (user is null) return NotFound();

            _logger.LogInformation("User {Login} role changed to {Role}", user.Login, user.Role);
            return Ok(new UserResponse(user.Id, user.Login, user.Username, user.Role, user.IsActive, user.AvatarUrl));
        }

        [HttpGet("users/{id:guid}/token-version")]
        public async Task<ActionResult<TokenVersionResponse>> GetTokenVersion(Guid id)
        {
            var version = await _identityService.GetTokenVersionAsync(id);
            if (version == -1) return NotFound();
            return Ok(new TokenVersionResponse(version));
        }
    }

    public record SetRoleRequest(int Role);
    public record TokenVersionResponse(int TokenVersion);
}
