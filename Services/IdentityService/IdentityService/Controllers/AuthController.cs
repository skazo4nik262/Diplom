using IdentityService.Models;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers
{
    public record RegisterRequest(string Login, string Password, string? Username = null, int Role = 1);
    public record LoginRequest(string Login, string Password);
    public record AuthResponse(string Token, Guid UserId, string Login, string? Username, int Role);
    public record UserResponse(Guid Id, string Login, string? Username, int Role);
    public record ProfileResponse(Guid Id, string Login, string? Username, string? Bio, DateTime? Birthday, string? AvatarUrl, int Role);
    public record UpdateProfileRequest(string? Username, string? Bio, DateTime? Birthday, string? AvatarUrl);

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IIdentity _identityService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IIdentity identityService, ILogger<AuthController> logger)
        {
            _identityService = identityService;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            if (Request.Headers.TryGetValue("X-User-Id", out var value) && Guid.TryParse(value, out var userId))
                return userId;
            return Guid.Empty;
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
                return Ok(new UserResponse(user.Id, user.Login, user.Username, user.Role));
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

            return Ok(new AuthResponse(
                Token: token,
                UserId: user!.Id,
                Login: user.Login,
                Username: user.Username,
                Role: user.Role
            ));
        }

        [HttpGet("users/{login}")]
        public async Task<ActionResult<UserResponse>> GetUser(string login)
        {
            var user = await _identityService.GetByLoginAsync(login);
            if (user is null) return NotFound();

            return Ok(new UserResponse(user.Id, user.Login, user.Username, user.Role));
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
    }
}
