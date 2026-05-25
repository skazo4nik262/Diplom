using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;

namespace JellyfinService.Controllers
{


    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpFactory;
        private readonly HttpClient _httpClient;
        private readonly ILogger<MediaController> _logger;

        public MediaController(
            IConfiguration configuration,
            IHttpClientFactory httpFactory,
            ILogger<MediaController> logger)
        {
            _configuration = configuration;
            _httpFactory = httpFactory;
            _httpClient = _httpFactory.CreateClient("JellyfinClient");
            _logger = logger;

        }


        [HttpGet("stream/{mediaId}")]
        public async Task<IActionResult> StreamVideo(Guid mediaId)
        {
            // 1. Получаем URL стрима от Jellyfin
            // Важно: запрашиваем прямой стрим (direct stream), чтобы Jellyfin не транскодил лишний раз, если возможно
            var jellyfinUrl = $"{_configuration["Jellyfin:Url"]}/Videos/{mediaId}/stream.mkv";

            // Создаем запрос к Jellyfin
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, jellyfinUrl);

            // Добавляем API ключ Jellyfin
            request.Headers.Add("X-Emby-Token", _configuration["Jellyfin:ApiKey"]);

            // ВАЖНО: Пробрасываем заголовок Range от клиента к Jellyfin.
            // Это позволяет делать перемотку (seek) без перезагрузки всего файла.
            if (Request.Headers.ContainsKey("Range"))
            {
                // Получаем значение заголовка (например, "bytes=0-1023")
                var rangeHeader = Request.Headers["Range"].FirstOrDefault();

                if (!string.IsNullOrEmpty(rangeHeader))
                {
                    // Парсим строку в объект RangeHeaderValue
                    if (System.Net.Http.Headers.RangeHeaderValue.TryParse(rangeHeader, out var parsedRange))
                    {
                        request.Headers.Range = parsedRange;
                    }
                }
            }

            try
            {
                // 2. Получаем ответ от Jellyfin
                var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, "Failed to connect to Jellyfin");

                // 3. Возвращаем поток клиенту
                // Мы не читаем весь файл в память! Мы передаем поток напрямую.
                var stream = await response.Content.ReadAsStreamAsync();

                return new FileStreamResult(stream, "video/x-matroska") // Или application/octet-stream
                {
                    FileDownloadName = $"movie_{mediaId}.mkv" // Опционально
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming video {MediaId}", mediaId);
                return StatusCode(500, "Internal streaming error");
            }
        }
    }
}
