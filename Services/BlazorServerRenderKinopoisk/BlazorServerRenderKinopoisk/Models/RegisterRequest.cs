namespace BlazorServerRenderKinopoisk.Models
{
    public record RegisterRequest(string Login, string Password, string? Username = null);
}
