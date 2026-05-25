namespace EmbeddingService.Services;

public class MockImageEmbedder : IImageEmbedder
{
    public int Dimension => 768;

    public Task<float[]> EmbedAsync(string imageUrl, CancellationToken ct = default)
    {
        var embedding = new float[Dimension];
        var hash = imageUrl.GetHashCode();
        var rng = new Random(hash);
        for (var i = 0; i < Dimension; i++)
            embedding[i] = (float)(rng.NextDouble() * 2 - 1);
        return Task.FromResult(embedding);
    }
}
