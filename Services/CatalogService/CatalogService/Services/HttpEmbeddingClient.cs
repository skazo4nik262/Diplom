using Flurl.Http;

namespace CatalogService.Services;

public class HttpEmbeddingClient(string baseUrl) : IEmbeddingClient
{
    private static readonly FlurlClient NoProxyClient = new(
        new HttpClient(new SocketsHttpHandler
        {
            UseProxy = false,
            Proxy = null
        })
    );

    public async Task<float[]> GenerateTextEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var response = await NoProxyClient
            .Request($"{baseUrl}/embed/text")
            .PostJsonAsync(new { text })
            .ReceiveJson<EmbedResponse>();
        return response.Embedding;
    }

    public async Task<float[]> GenerateImageEmbeddingAsync(byte[] imageBytes, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent
        {
            { new ByteArrayContent(imageBytes), "image", "poster.jpg" }
        };

        var response = await NoProxyClient
            .Request($"{baseUrl}/embed/image")
            .PostAsync(form)
            .ReceiveJson<EmbedResponse>();
        return response.Embedding;
    }

    public async Task<float[]> GenerateClipTextEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var response = await NoProxyClient
            .Request($"{baseUrl}/embed/clip-text")
            .PostJsonAsync(new { text })
            .ReceiveJson<EmbedResponse>();
        return response.Embedding;
    }

    private record EmbedResponse(float[] Embedding);
}
