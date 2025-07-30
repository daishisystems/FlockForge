namespace FlockForge.Services.Platform;

public interface IPlatformMemoryService
{
    Task HandleMemoryPressureAsync(MemoryPressureLevel level);
    Task OptimizeMemoryUsageAsync();
    long GetAvailableMemory();
    long GetUsedMemory();
    void RegisterMemoryPressureCallback(Action<MemoryPressureLevel> callback);
    void UnregisterMemoryPressureCallback(Action<MemoryPressureLevel> callback);
}

public enum MemoryPressureLevel
{
    Normal = 0,
    Low = 1,
    Medium = 2,
    Critical = 3
}