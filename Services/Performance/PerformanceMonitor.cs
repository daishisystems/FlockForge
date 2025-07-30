using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace FlockForge.Services.Performance;

public class PerformanceMonitor : IPerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly ConcurrentDictionary<string, List<OperationData>> _operationData;
    private readonly ConcurrentDictionary<string, double> _customMetrics;
    private readonly DateTimeOffset _startTime;

    public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
    {
        _logger = logger;
        _operationData = new ConcurrentDictionary<string, List<OperationData>>();
        _customMetrics = new ConcurrentDictionary<string, double>();
        _startTime = DateTimeOffset.UtcNow;
    }

    public async Task<T> MeasureAsync<T>(string operationName, Func<Task<T>> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        var memoryBefore = GC.GetTotalMemory(false);
        var success = false;

        try
        {
            var result = await operation();
            success = true;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation {OperationName} failed", operationName);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var memoryAfter = GC.GetTotalMemory(false);
            var memoryDelta = memoryAfter - memoryBefore;

            RecordOperationData(operationName, stopwatch.ElapsedMilliseconds, memoryDelta, success);

            _logger.LogDebug("Operation {OperationName} completed in {ElapsedMs}ms, " +
                "Memory delta: {MemoryDelta} bytes, Success: {Success}",
                operationName, stopwatch.ElapsedMilliseconds, memoryDelta, success);
        }
    }

    public async Task MeasureAsync(string operationName, Func<Task> operation)
    {
        await MeasureAsync(operationName, async () =>
        {
            await operation();
            return true; // Return dummy value for generic method
        });
    }

    public void RecordMetric(string metricName, double value, string? unit = null)
    {
        _customMetrics.AddOrUpdate(metricName, value, (key, oldValue) => value);
        
        _logger.LogDebug("Recorded metric {MetricName}: {Value} {Unit}", 
            metricName, value, unit ?? "");
    }

    public void RecordEvent(string eventName, Dictionary<string, object>? properties = null)
    {
        var logMessage = $"Event: {eventName}";
        if (properties != null && properties.Any())
        {
            var propertiesString = string.Join(", ", properties.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            logMessage += $" Properties: {propertiesString}";
        }

        _logger.LogInformation(logMessage);
    }

    public async Task<PerformanceReport> GenerateReportAsync()
    {
        return await Task.Run(() =>
        {
            var operations = new Dictionary<string, OperationMetrics>();

            foreach (var kvp in _operationData)
            {
                var operationName = kvp.Key;
                var dataPoints = kvp.Value.ToList(); // Create a snapshot

                if (dataPoints.Any())
                {
                    var executionTimes = dataPoints.Select(d => d.ExecutionTimeMs).ToList();
                    var memoryDeltas = dataPoints.Select(d => d.MemoryDelta).ToList();
                    var errorCount = dataPoints.Count(d => !d.Success);

                    operations[operationName] = new OperationMetrics
                    {
                        OperationName = operationName,
                        ExecutionCount = dataPoints.Count,
                        AverageExecutionTime = executionTimes.Average(),
                        MinExecutionTime = executionTimes.Min(),
                        MaxExecutionTime = executionTimes.Max(),
                        AverageMemoryDelta = (long)memoryDeltas.Average(),
                        ErrorCount = errorCount,
                        LastExecuted = dataPoints.Max(d => d.Timestamp)
                    };
                }
            }

            return new PerformanceReport
            {
                Operations = operations,
                CustomMetrics = new Dictionary<string, double>(_customMetrics),
                TotalMemoryUsage = GC.GetTotalMemory(false),
                AppUptime = DateTimeOffset.UtcNow - _startTime
            };
        });
    }

    private void RecordOperationData(string operationName, long executionTimeMs, long memoryDelta, bool success)
    {
        var data = new OperationData
        {
            ExecutionTimeMs = executionTimeMs,
            MemoryDelta = memoryDelta,
            Success = success,
            Timestamp = DateTimeOffset.UtcNow
        };

        _operationData.AddOrUpdate(operationName,
            new List<OperationData> { data },
            (key, existingList) =>
            {
                lock (existingList)
                {
                    existingList.Add(data);
                    
                    // Keep only the last 100 entries per operation to prevent memory bloat
                    if (existingList.Count > 100)
                    {
                        existingList.RemoveAt(0);
                    }
                    
                    return existingList;
                }
            });
    }

    private class OperationData
    {
        public long ExecutionTimeMs { get; init; }
        public long MemoryDelta { get; init; }
        public bool Success { get; init; }
        public DateTimeOffset Timestamp { get; init; }
    }
}