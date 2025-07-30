using Microsoft.Extensions.Logging;

namespace FlockForge.Services.Platform;

public class DefaultMemoryService : IPlatformMemoryService
{
    private readonly ILogger<DefaultMemoryService> _logger;
    private readonly List<Action<MemoryPressureLevel>> _callbacks;

    public DefaultMemoryService(ILogger<DefaultMemoryService> logger)
    {
        _logger = logger;
        _callbacks = new List<Action<MemoryPressureLevel>>();
    }

    public async Task HandleMemoryPressureAsync(MemoryPressureLevel level)
    {
        _logger.LogInformation("Handling memory pressure level: {Level}", level);

        switch (level)
        {
            case MemoryPressureLevel.Low:
                await OptimizeMemoryUsageAsync().ConfigureAwait(false);
                break;
            case MemoryPressureLevel.Medium:
                await OptimizeMemoryUsageAsync().ConfigureAwait(false);
                GC.Collect(0, GCCollectionMode.Optimized);
                break;
            case MemoryPressureLevel.Critical:
                await OptimizeMemoryUsageAsync().ConfigureAwait(false);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                break;
        }

        // Notify registered callbacks
        foreach (var callback in _callbacks.ToList())
        {
            try
            {
                callback(level);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in memory pressure callback");
            }
        }
    }

    public async Task OptimizeMemoryUsageAsync()
    {
        await Task.Run(() =>
        {
            _logger.LogDebug("Optimizing memory usage");
            
            // Force garbage collection
            GC.Collect(0, GCCollectionMode.Optimized);
            
            // Compact large object heap if available (.NET Framework only)
            // GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        });
    }

    public long GetAvailableMemory()
    {
        // This is a basic implementation - platform-specific versions should provide more accurate data
        try
        {
            var totalMemory = GC.GetTotalMemory(false);
            var workingSet = Environment.WorkingSet;
            
            // Rough estimate of available memory
            return Math.Max(0, workingSet - totalMemory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available memory");
            return 0;
        }
    }

    public long GetUsedMemory()
    {
        try
        {
            return GC.GetTotalMemory(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get used memory");
            return 0;
        }
    }

    public void RegisterMemoryPressureCallback(Action<MemoryPressureLevel> callback)
    {
        if (callback != null && !_callbacks.Contains(callback))
        {
            _callbacks.Add(callback);
            _logger.LogDebug("Registered memory pressure callback");
        }
    }

    public void UnregisterMemoryPressureCallback(Action<MemoryPressureLevel> callback)
    {
        if (callback != null && _callbacks.Contains(callback))
        {
            _callbacks.Remove(callback);
            _logger.LogDebug("Unregistered memory pressure callback");
        }
    }
}