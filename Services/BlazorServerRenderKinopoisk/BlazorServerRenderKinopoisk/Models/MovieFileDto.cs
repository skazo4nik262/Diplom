namespace BlazorServerRenderKinopoisk.Models;

public record MovieFileDto(Guid? JellyfinItemId, bool IsReady, string FileName, long SizeBytes);
