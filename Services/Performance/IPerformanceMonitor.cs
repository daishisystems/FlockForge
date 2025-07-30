namespace FlockForge.Services.Performance;

public interface IPerformanceMonitor
{
    Task<T> MeasureAsync<T>(string operationName, Func<Task<T>> operation);
    Task MeasureAsync(string operationName, Func<Task> operation);
    void RecordMetric(string metricName, double value, string? unit = null);
    void RecordEvent(string eventName, Dictionary<string, object>? properties = null);
    Task<PerformanceReport> GenerateReportAsync();
}

public class PerformanceReport
{
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
    public Dictionary<string, OperationMetrics> Operations { get; init; } = new();
    public Dictionary<string, double> CustomMetrics { get; init; } = new();
    public long TotalMemoryUsage { get; init; }
    public TimeSpan AppUptime { get; init; }
}

public class OperationMetrics
{
    public string OperationName { get; init; } = string.Empty;
    public int ExecutionCount { get; init; }
    public double AverageExecutionTime { get; init; }
    public double MinExecutionTime { get; init; }
    public double MaxExecutionTime { get; init; }
    public long AverageMemoryDelta { get; init; }
    public int ErrorCount { get; init; }
    public DateTimeOffset LastExecuted { get; init; }
}