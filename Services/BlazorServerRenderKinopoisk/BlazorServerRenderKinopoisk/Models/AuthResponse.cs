namespace BlazorServerRenderKinopoisk.Models
{
    public record AuthResponse(string Token, Guid UserId, string Login, string? Username, int Role);
    public partial record RegisterResponse(Guid Id, string Login, string? Username, int Role);
}
