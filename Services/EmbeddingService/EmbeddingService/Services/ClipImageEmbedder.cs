using Flurl.Http;

namespace EmbeddingService.Services;

public class ClipImageEmbedder : IImageEmbedder
{
    private readonly string _baseUrl;

    public int Dimension => 512;

    public ClipImageEmbedder(string baseUrl = "http://clip:51000")
    {
        _baseUrl = baseUrl;
    }

    public async Task<float[]> EmbedAsync(string imageUrl, CancellationToken ct = default)
    {
        var delay = TimeSpan.FromSeconds(1);
        const int maxAttempts = 10;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var response = await new FlurlRequest($"{_baseUrl}/post")
                    .PostJsonAsync(new { data = new[] { new { uri = imageUrl } } })
                    .ReceiveJson<ClipResponse>();

                return response.Data[0].Embedding;
            }
            catch (FlurlHttpException) when (attempt < maxAttempts)
            {
                await Task.Delay(delay, ct);
                delay *= 2;
            }
        }

        throw new InvalidOperationException($"CLIP at {_baseUrl} is unavailable after {maxAttempts} attempts.");
    }

    private record ClipResponse(ClipDataItem[] Data);
    private record ClipDataItem(float[] Embedding);
}
