namespace CatalogService.Services;

public class EmbeddingGeneratorService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmbeddingGeneratorService> _logger;

    private EmbeddingState _state = new();
    private readonly object _lock = new();

    public EmbeddingGeneratorService(IServiceScopeFactory scopeFactory, ILogger<EmbeddingGeneratorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public string StartGeneration(bool regenerateAll)
    {
        lock (_lock)
        {
            if (_state.IsRunning)
                return "Генерация эмбеддингов уже выполняется";

            _state = new EmbeddingState { IsRunning = true, StartedAt = DateTime.UtcNow, RegenerateAll = regenerateAll };
        }

        _ = RunGenerationAsync(regenerateAll);
        return "Генерация эмбеддингов запущена";
    }

    public EmbeddingState GetStatus()
    {
        lock (_lock) return _state with { };
    }

    private async Task RunGenerationAsync(bool regenerateAll)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var postgres = scope.ServiceProvider.GetRequiredService<IPostgresService>();

            List<int> movieIds;

            if (regenerateAll)
            {
                movieIds = await postgres.GetAllMovieIdsAsync();
                await postgres.ClearAllEmbeddingsAsync();

                lock (_lock)
                {
                    _state.Total = movieIds.Count;
                }
            }
            else
            {
                movieIds = await postgres.GetMovieIdsWithoutEmbeddingsAsync();

                lock (_lock)
                {
                    _state.Total = movieIds.Count;
                }
            }

            foreach (var id in movieIds)
            {
                try
                {
                    await postgres.EnsureEmbeddingsAsync(id);

                    lock (_lock)
                    {
                        _state.Processed++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating embedding for movie {Id}", id);

                    lock (_lock)
                    {
                        _state.Errors++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in embedding generation");

            lock (_lock)
            {
                _state.Errors++;
            }
        }
        finally
        {
            lock (_lock)
            {
                _state.IsRunning = false;
                _state.IsComplete = true;
                _state.CompletedAt = DateTime.UtcNow;
            }
        }
    }
}

public record EmbeddingState
{
    public bool IsRunning { get; set; }
    public bool IsComplete { get; set; }
    public int Total { get; set; }
    public int Processed { get; set; }
    public int Errors { get; set; }
    public bool RegenerateAll { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
