namespace CatalogService.Services;

public interface IEmbeddingClient
{
    Task<float[]> GenerateTextEmbeddingAsync(string text, CancellationToken ct = default);
    Task<float[]> GenerateImageEmbeddingAsync(byte[] imageBytes, CancellationToken ct = default);
    Task<float[]> GenerateClipTextEmbeddingAsync(string text, CancellationToken ct = default);
}
