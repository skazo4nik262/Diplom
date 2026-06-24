namespace EmbeddingService.Services;

public class MockImageEmbedder : IImageEmbedder
{
    public int Dimension => 768;

    public Task<float[]> EmbedAsync(byte[] imageBytes, CancellationToken ct = default)
    {
        var rng = new Random(imageBytes.Length);
        var embedding = new float[768];
        for (var i = 0; i < 768; i++)
            embedding[i] = (float)(rng.NextDouble() * 2 - 1);
        return Task.FromResult(embedding);
    }

    public Task<float[]> EmbedTextAsync(string text, CancellationToken ct = default)
    {
        return MockEmbedAsync(text, ct);
    }

    private static Task<float[]> MockEmbedAsync(string input, CancellationToken ct)
    {
        var embedding = new float[768];
        var hash = input.GetHashCode();
        var rng = new Random(hash);
        for (var i = 0; i < 768; i++)
            embedding[i] = (float)(rng.NextDouble() * 2 - 1);
        return Task.FromResult(embedding);
    }
}
