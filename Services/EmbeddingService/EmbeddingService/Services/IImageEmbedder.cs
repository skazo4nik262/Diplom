namespace EmbeddingService.Services;

public interface IImageEmbedder
{
    int Dimension { get; }
    Task<float[]> EmbedAsync(string imageUrl, CancellationToken ct = default);
}
