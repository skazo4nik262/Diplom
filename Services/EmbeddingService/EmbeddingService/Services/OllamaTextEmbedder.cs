using Flurl.Http;

namespace EmbeddingService.Services;

public class OllamaTextEmbedder : ITextEmbedder
{
    private readonly string _baseUrl;
    private readonly string _model;

    public int Dimension => 768;

    public OllamaTextEmbedder(string baseUrl = "http://ollama:11434", string model = "nomic-embed-text")
    {
        _baseUrl = baseUrl;
        _model = model;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var delay = TimeSpan.FromSeconds(1);
        const int maxAttempts = 10;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var response = await new FlurlRequest($"{_baseUrl}/api/embed")
                    .PostJsonAsync(new { model = _model, input = new[] { text } })
                    .ReceiveJson<OllamaEmbedResponse>();

                return response.Embeddings[0];
            }
            catch (FlurlHttpException) when (attempt < maxAttempts)
            {
                await Task.Delay(delay, ct);
                delay *= 2;
            }
        }

        throw new InvalidOperationException($"Ollama at {_baseUrl} is unavailable after {maxAttempts} attempts.");
    }

    private record OllamaEmbedResponse(float[][] Embeddings);
}
