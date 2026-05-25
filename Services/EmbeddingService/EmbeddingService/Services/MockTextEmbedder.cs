namespace EmbeddingService.Services;

public class MockTextEmbedder : ITextEmbedder
{
    public int Dimension => 768;

    public Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var embedding = new float[Dimension];
        var hash = text.Aggregate(0, (h, c) => h ^= c);
        var rng = new Random(hash);
        for (var i = 0; i < Dimension; i++)
            embedding[i] = (float)(rng.NextDouble() * 2 - 1);
        return Task.FromResult(embedding);
    }
}
