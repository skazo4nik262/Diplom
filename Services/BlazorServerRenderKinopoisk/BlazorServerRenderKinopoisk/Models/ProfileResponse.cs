namespace BlazorServerRenderKinopoisk.Models;

public record ProfileResponse(Guid Id, string Login, string? Username, string? Bio, DateTime? Birthday, string? AvatarUrl, int Role);
public record UpdateProfileRequest(string? Username, string? Bio, DateTime? Birthday, string? AvatarUrl);
