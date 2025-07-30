using Microsoft.Extensions.Logging;
using FlockForge.Services.Database;
using FlockForge.Services.Firebase;
using System.Timers;

namespace FlockForge.Services.Sync;

public class BackgroundSyncService : IBackgroundSyncService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundSyncService> _logger;
    private readonly System.Timers.Timer _syncTimer;
    private readonly SemaphoreSlim _syncSemaphore;
    private bool _isRunning;
    private bool _disposed;

    public bool IsRunning => _isRunning;
    public event EventHandler<SyncCompletedEventArgs>? SyncCompleted;

    public BackgroundSyncService(IServiceProvider serviceProvider, ILogger<BackgroundSyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _syncSemaphore = new SemaphoreSlim(1, 1);
        
        // Configure timer for every 5 minutes
        _syncTimer = new System.Timers.Timer(TimeSpan.FromMinutes(5));
        _syncTimer.Elapsed += OnTimerElapsed;
        _syncTimer.AutoReset = true;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BackgroundSyncService));

        if (_isRunning)
        {
            _logger.LogWarning("Background sync service is already running");
            return;
        }

        try
        {
            _isRunning = true;
            _syncTimer.Start();
            
            // Perform initial sync
            await SyncNowAsync(cancellationToken);
            
            _logger.LogInformation("Background sync service started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start background sync service");
            _isRunning = false;
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            _logger.LogWarning("Background sync service is not running");
            return;
        }

        try
        {
            _syncTimer.Stop();
            
            // Wait for any ongoing sync to complete
            await _syncSemaphore.WaitAsync(cancellationToken);
            try
            {
                _isRunning = false;
                _logger.LogInformation("Background sync service stopped");
            }
            finally
            {
                _syncSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping background sync service");
            throw;
        }
    }

    public async Task SyncNowAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BackgroundSyncService));

        await _syncSemaphore.WaitAsync(cancellationToken);
        try
        {
            await PerformSyncAsync(cancellationToken);
        }
        finally
        {
            _syncSemaphore.Release();
        }
    }

    private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            await SyncNowAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Timer-triggered sync failed");
        }
    }

    private async Task PerformSyncAsync(CancellationToken cancellationToken = default)
    {
        var syncStartTime = DateTimeOffset.UtcNow;
        var itemsSynced = 0;
        var success = false;
        string? errorMessage = null;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
            var firebaseService = scope.ServiceProvider.GetRequiredService<IFirebaseService>();

            // Check if we're online
            if (!await firebaseService.IsOnlineAsync())
            {
                _logger.LogDebug("Skipping sync - device is offline");
                return;
            }

            // Check if user is authenticated
            if (!await firebaseService.IsAuthenticatedAsync())
            {
                _logger.LogDebug("Skipping sync - user not authenticated");
                return;
            }

            _logger.LogDebug("Starting background sync");

            // Sync local changes to Firebase
            await databaseService.SyncPendingChangesAsync();

            // Sync remote changes from Firebase
            await firebaseService.SyncAllDataAsync();

            success = true;
            _logger.LogInformation("Background sync completed successfully. Items synced: {ItemsSynced}", itemsSynced);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Background sync was cancelled");
            errorMessage = "Sync was cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background sync failed");
            errorMessage = ex.Message;
        }
        finally
        {
            // Notify subscribers
            SyncCompleted?.Invoke(this, new SyncCompletedEventArgs
            {
                Success = success,
                ErrorMessage = errorMessage,
                ItemsSynced = itemsSynced,
                Timestamp = syncStartTime
            });
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _syncTimer?.Stop();
                _syncTimer?.Dispose();
                _syncSemaphore?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}