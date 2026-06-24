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
    public record UserResponse(Guid Id, string Login, string? Username, int Role, bool IsActive = true);
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
                return BadRequest(new { error = "Password must be at least 8 characters" });

            try
            {
                var user = await _identityService.CreateAsync(request.Login, request.Password, request.Role, request.Username);
                _logger.LogInformation("User registered: {Login}", user.Login);
                return Ok(new UserResponse(user.Id, user.Login, user.Username, user.Role, user.IsActive));
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

            return Ok(users.Select(u => new UserResponse(u.Id, u.Login, u.Username, u.Role, u.IsActive)).ToList());
        }

        [HttpGet("users/{login}")]
        public async Task<ActionResult<UserResponse>> GetUser(string login)
        {
            var user = await _identityService.GetByLoginAsync(login);
            if (user is null) return NotFound();

            return Ok(new UserResponse(user.Id, user.Login, user.Username, user.Role, user.IsActive));
        }

        [HttpGet("users/id/{id:guid}")]
        public async Task<ActionResult<ProfileResponse>> GetUserById(Guid id)
        {
            var user = await _identityService.GetByIdAsync(id);
            if (user is null) return NotFound();

            return Ok(new ProfileResponse(user.Id, user.Login, user.Username, user.Bio, user.Birthday, user.AvatarUrl, user.Role));
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
            return Ok(new UserResponse(user.Id, user.Login, user.Username, user.Role, user.IsActive));
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
