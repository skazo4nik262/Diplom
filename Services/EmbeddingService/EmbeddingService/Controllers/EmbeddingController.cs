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
    public async Task<IActionResult> EmbedImage(IFormFile image, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await image.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        var embedding = await _imageEmbedder.EmbedAsync(bytes, ct);
        return Ok(new EmbedResponse(embedding));
    }

    [HttpPost("clip-text")]
    public async Task<IActionResult> EmbedClipText([FromBody] EmbedTextRequest request, CancellationToken ct)
    {
        var embedding = await _imageEmbedder.EmbedTextAsync(request.Text, ct);
        return Ok(new EmbedResponse(embedding));
    }
}

public record EmbedTextRequest(string Text);
public record EmbedResponse(float[] Embedding);
