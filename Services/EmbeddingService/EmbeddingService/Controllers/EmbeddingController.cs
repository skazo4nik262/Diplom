using EmbeddingService.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmbeddingService.Controllers;

[Route("embed")]
[ApiController]
public class EmbeddingController : ControllerBase
{
    private readonly ITextEmbedder _textEmbedder;
    private readonly IImageEmbedder _imageEmbedder;

    public EmbeddingController(ITextEmbedder textEmbedder, IImageEmbedder imageEmbedder)
    {
        _textEmbedder = textEmbedder;
        _imageEmbedder = imageEmbedder;
    }

    [HttpPost("text")]
    public async Task<IActionResult> EmbedText([FromBody] EmbedTextRequest request, CancellationToken ct)
    {
        var embedding = await _textEmbedder.EmbedAsync(request.Text, ct);
        return Ok(new EmbedResponse(embedding));
    }

    [HttpPost("image")]
    public async Task<IActionResult> EmbedImage([FromBody] EmbedImageRequest request, CancellationToken ct)
    {
        var embedding = await _imageEmbedder.EmbedAsync(request.Url, ct);
        return Ok(new EmbedResponse(embedding));
    }
}

public record EmbedTextRequest(string Text);
public record EmbedImageRequest(string Url);
public record EmbedResponse(float[] Embedding);
