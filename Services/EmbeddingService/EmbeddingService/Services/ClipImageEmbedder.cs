using Flurl.Http;

namespace EmbeddingService.Services;

public class ClipImageEmbedder : IImageEmbedder
{
    private readonly string _baseUrl;
    private readonly string _model;

    public int Dimension => 512;

    public ClipImageEmbedder(string baseUrl = "http://oclip:11435", string model = "hf-hub:laion/CLIP-ViT-B-32-laion2B-s34B-b79K")
    {
        _baseUrl = baseUrl;
        _model = model;
    }

    public async Task<float[]> EmbedAsync(byte[] imageBytes, CancellationToken ct = default)
    {
        var dataJson = System.Text.Json.JsonSerializer.Serialize(new[] { new { model = _model } });
        var dataPart = new StringContent(dataJson, System.Text.Encoding.UTF8, "application/json");
        var imagePart = new ByteArrayContent(imageBytes);
        imagePart.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        using var form = new MultipartFormDataContent
        {
            { dataPart, "data", "data" },
            { imagePart, "image", "poster.jpg" }
        };

        var response = await new FlurlRequest($"{_baseUrl}/api/embed")
            .WithTimeout(TimeSpan.FromSeconds(30))
            .PostAsync(form)
            .ReceiveJson<OclipResponse>();

        return response.Embeddings[0];
    }

    public async Task<float[]> EmbedTextAsync(string text, CancellationToken ct = default)
    {
        var response = await new FlurlRequest($"{_baseUrl}/api/embed")
            .WithTimeout(TimeSpan.FromSeconds(15))
            .PostJsonAsync(new { model = _model, input = text })
            .ReceiveJson<OclipResponse>();

        return response.Embeddings[0];
    }

    private record OclipResponse(float[][] Embeddings);
}
