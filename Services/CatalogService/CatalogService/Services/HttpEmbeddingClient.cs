using Flurl.Http;

namespace CatalogService.Services;

public class HttpEmbeddingClient(string baseUrl) : IEmbeddingClient
{
    public async Task<float[]> GenerateTextEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var response = await new FlurlRequest($"{baseUrl}/embed/text")
            .PostJsonAsync(new { text })
            .ReceiveJson<EmbedResponse>();
        return response.Embedding;
    }

    public async Task<float[]> GenerateImageEmbeddingAsync(string imageUrl, CancellationToken ct = default)
    {
        var response = await new FlurlRequest($"{baseUrl}/embed/image")
            .PostJsonAsync(new { url = imageUrl })
            .ReceiveJson<EmbedResponse>();
        return response.Embedding;
    }

    private record EmbedResponse(float[] Embedding);
}
