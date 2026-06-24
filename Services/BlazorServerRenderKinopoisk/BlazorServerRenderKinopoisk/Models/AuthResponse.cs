namespace BlazorServerRenderKinopoisk.Models
{
    public record AuthResponse(string Token, Guid UserId, string Login, string? Username, int Role, string RefreshToken, DateTime RefreshTokenExpiresAt);
    public partial record RegisterResponse(Guid Id, string Login, string? Username, int Role);
    public record RefreshResponse(string Token, string RefreshToken, DateTime RefreshTokenExpiresAt);
    public record RefreshRequest(string RefreshToken);
}
