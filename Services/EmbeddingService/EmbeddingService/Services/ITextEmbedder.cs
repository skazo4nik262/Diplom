namespace EmbeddingService.Services;

public interface ITextEmbedder
{
    int Dimension { get; }
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
}
