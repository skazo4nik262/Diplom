namespace BlazorServerRenderKinopoisk.Models
{
    public record AuthResponse(string Token);
    public partial record RegisterResponse(Guid Id, string Login, int Role);
}
