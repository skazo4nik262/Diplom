namespace EmbeddingService.Services;

public interface IImageEmbedder
{
    int Dimension { get; }
    Task<float[]> EmbedAsync(byte[] imageBytes, CancellationToken ct = default);
    Task<float[]> EmbedTextAsync(string text, CancellationToken ct = default);
}
