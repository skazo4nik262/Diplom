namespace BlazorServerRenderKinopoisk.Services;

public static class AvatarHelper
{
    public static string? Resolve(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (path.StartsWith("/api/")) return path;
        if (path.StartsWith("http")) return path;
        if (path.StartsWith("/https://") || path.StartsWith("/http://")) return path[1..];
        return null;
    }
}
