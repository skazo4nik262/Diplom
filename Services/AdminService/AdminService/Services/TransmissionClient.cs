using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AdminService.Services;

public class TransmissionClient
{
    private readonly HttpClient _http;
    private readonly ILogger<TransmissionClient> _logger;
    private string? _sessionId;

    public TransmissionClient(HttpClient http, ILogger<TransmissionClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<JsonElement> SendAsync(string method, object? args, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new
        {
            method,
            arguments = args
        });

        var content = new StringContent(body, Encoding.UTF8, "application/json");
        if (_sessionId != null)
            content.Headers.Add("X-Transmission-Session-Id", _sessionId);

        var response = await _http.PostAsync("/transmission/rpc", content, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            _sessionId = response.Headers
                .Where(h => h.Key.Equals("X-Transmission-Session-Id", StringComparison.OrdinalIgnoreCase))
                .SelectMany(h => h.Value)
                .FirstOrDefault();

            if (_sessionId == null)
                throw new InvalidOperationException("No X-Transmission-Session-Id in response");

            content.Dispose();
            content = new StringContent(body, Encoding.UTF8, "application/json");
            content.Headers.Add("X-Transmission-Session-Id", _sessionId);
            response = await _http.PostAsync("/transmission/rpc", content, ct);
        }

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync(ct);
        return JsonDocument.Parse(result).RootElement;
    }

    public async Task<int> AddTorrentAsync(string magnetUrl, string downloadDir, CancellationToken ct = default)
    {
        var result = await SendAsync("torrent-add", new
        {
            filename = magnetUrl,
            download_dir = downloadDir
        }, ct);

        return result.GetProperty("arguments")
                     .GetProperty("torrent-added")
                     .GetProperty("id")
                     .GetInt32();
    }

    public async Task<List<TorrentInfo>> GetTorrentsAsync(CancellationToken ct = default)
    {
        var result = await SendAsync("torrent-get", new
        {
            fields = new[] { "id", "name", "percentDone", "status", "files", "downloadDir" }
        }, ct);

        var torrents = result.GetProperty("arguments").GetProperty("torrents");
        var list = new List<TorrentInfo>();

        foreach (var t in torrents.EnumerateArray())
        {
            var files = new List<TorrentFile>();
            if (t.TryGetProperty("files", out var f))
            {
                foreach (var file in f.EnumerateArray())
                {
                    files.Add(new TorrentFile
                    {
                        Name = file.GetProperty("name").GetString() ?? "",
                        Length = file.GetProperty("length").GetInt64(),
                        BytesCompleted = file.TryGetProperty("bytesCompleted", out var bc) ? bc.GetInt64() : 0
                    });
                }
            }

            list.Add(new TorrentInfo
            {
                Id = t.GetProperty("id").GetInt32(),
                Name = t.GetProperty("name").GetString() ?? "",
                PercentDone = t.GetProperty("percentDone").GetDouble(),
                Status = t.GetProperty("status").GetInt32(),
                Files = files,
                DownloadDir = t.TryGetProperty("downloadDir", out var dd) ? dd.GetString() ?? "" : ""
            });
        }

        return list;
    }

    public async Task RemoveTorrentAsync(int id, bool deleteData = false, CancellationToken ct = default)
    {
        await SendAsync("torrent-remove", new
        {
            ids = new[] { id },
            delete_local_data = deleteData
        }, ct);
    }
}

public class TorrentInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double PercentDone { get; set; }
    public int Status { get; set; }
    public List<TorrentFile> Files { get; set; } = [];
    public string DownloadDir { get; set; } = "";
    public bool IsComplete => PercentDone >= 1.0;
}

public class TorrentFile
{
    public string Name { get; set; } = "";
    public long Length { get; set; }
    public long BytesCompleted { get; set; }
}
