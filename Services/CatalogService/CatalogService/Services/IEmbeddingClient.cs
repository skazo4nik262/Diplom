namespace CatalogService.Services;

public interface IEmbeddingClient
{
    Task<float[]> GenerateTextEmbeddingAsync(string text, CancellationToken ct = default);
    Task<float[]> GenerateImageEmbeddingAsync(string imageUrl, CancellationToken ct = default);
}
