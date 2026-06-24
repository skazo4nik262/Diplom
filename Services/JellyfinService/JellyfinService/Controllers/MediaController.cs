using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace JellyfinService.Controllers;

[Route("api/jellyfin/[controller]")]
[ApiController]
public class MediaController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MediaController> _logger;

    public MediaController(
        IConfiguration configuration,
        ILogger<MediaController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("hls/{mediaId}/{**rest}")]
    public async Task HlsProxy(Guid mediaId, string? rest)
    {
        var path = string.IsNullOrEmpty(rest) ? "" : $"/{rest}";
        var query = Request.Query.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
        query.TryAdd("MediaSourceId", mediaId.ToString());
        query.TryAdd("VideoCodec", "h264");
        query.TryAdd("AudioCodec", "aac");
        var queryString = "?" + string.Join("&", query.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
        var jellyfinUrl = $"{_configuration["Jellyfin:Url"]}/Videos/{mediaId}{path}{queryString}";

        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, jellyfinUrl);
        request.Headers.Add("X-Emby-Token", _configuration["Jellyfin:ApiKey"]);

        try
        {
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            Response.StatusCode = (int)response.StatusCode;
            foreach (var header in response.Headers)
                Response.Headers[header.Key] = header.Value.ToArray();
            foreach (var header in response.Content.Headers)
                Response.Headers[header.Key] = header.Value.ToArray();

            Response.Headers.Remove("Transfer-Encoding");

            await response.Content.CopyToAsync(Response.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying HLS for {MediaId}", mediaId);
            Response.StatusCode = 500;
        }
    }

    [HttpGet("stream/{mediaId}")]
    public async Task StreamVideo(Guid mediaId)
    {
        var jellyfinUrl = $"{_configuration["Jellyfin:Url"]}/Videos/{mediaId}/stream?static=true";

        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, jellyfinUrl);
        request.Headers.Add("X-Emby-Token", _configuration["Jellyfin:ApiKey"]);

        if (Request.Headers.TryGetValue("Range", out var rangeValues))
        {
            var rangeHeader = rangeValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(rangeHeader) && RangeHeaderValue.TryParse(rangeHeader, out var parsedRange))
                request.Headers.Range = parsedRange;
        }

        try
        {
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            Response.StatusCode = (int)response.StatusCode;
            foreach (var header in response.Headers)
                Response.Headers[header.Key] = header.Value.ToArray();
            foreach (var header in response.Content.Headers)
                Response.Headers[header.Key] = header.Value.ToArray();

            Response.Headers.Remove("Transfer-Encoding");

            await response.Content.CopyToAsync(Response.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming video {MediaId}", mediaId);
            Response.StatusCode = 500;
        }
    }
}
