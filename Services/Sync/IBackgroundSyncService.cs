namespace FlockForge.Services.Sync;

public interface IBackgroundSyncService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task SyncNowAsync(CancellationToken cancellationToken = default);
    bool IsRunning { get; }
    event EventHandler<SyncCompletedEventArgs>? SyncCompleted;
}

public class SyncCompletedEventArgs : EventArgs
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int ItemsSynced { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}