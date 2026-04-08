using System.Collections.Concurrent;

namespace TactileCs.Diagnostics;

/// <summary>
/// Thread-safe performance monitor that collects operation timing metrics.
/// Use <see cref="BeginOperation"/> to start timing a named operation;
/// dispose the returned <see cref="OperationTimer"/> to record its duration.
/// </summary>
public sealed class PerformanceMonitor
{
    /// <summary>
    /// Shared global instance for convenience.  Applications can also create
    /// dedicated instances to isolate metrics by subsystem.
    /// </summary>
    public static PerformanceMonitor Default { get; } = new();

    private readonly ConcurrentDictionary<string, OperationLog> _operations = new();

    /// <summary>
    /// Whether metric collection is enabled.  When <c>false</c>,
    /// <see cref="BeginOperation"/> returns a lightweight no-op timer
    /// to minimise overhead in production.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Starts timing a named operation.  Dispose the returned timer to
    /// record its duration.
    /// </summary>
    /// <example>
    /// <code>
    /// using (monitor.BeginOperation("BuildConnections"))
    /// {
    ///     // work …
    /// }
    /// </code>
    /// </example>
    public OperationTimer BeginOperation(string operationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        return new OperationTimer(this, operationName);
    }

    /// <summary>
    /// Returns a snapshot of metrics for every recorded operation.
    /// </summary>
    public IReadOnlyList<OperationMetrics> GetAllMetrics()
    {
        var results = new List<OperationMetrics>();
        foreach (var kvp in _operations)
        {
            var log = kvp.Value;
            lock (log)
            {
                results.Add(new OperationMetrics(
                    kvp.Key,
                    log.Count,
                    log.Total,
                    log.Min,
                    log.Max));
            }
        }
        return results;
    }

    /// <summary>
    /// Returns a snapshot of metrics for a single operation, or <c>null</c>
    /// if no data has been recorded for the given name.
    /// </summary>
    public OperationMetrics? GetMetrics(string operationName)
    {
        if (!_operations.TryGetValue(operationName, out var log))
            return null;

        lock (log)
        {
            return new OperationMetrics(
                operationName,
                log.Count,
                log.Total,
                log.Min,
                log.Max);
        }
    }

    /// <summary>
    /// Clears all recorded metrics.
    /// </summary>
    public void Reset() => _operations.Clear();

    // ------------------------------------------------------------------
    //  Internal
    // ------------------------------------------------------------------

    internal void RecordDuration(string operationName, TimeSpan duration)
    {
        if (!IsEnabled) return;

        var log = _operations.GetOrAdd(operationName, _ => new OperationLog());
        lock (log)
        {
            log.Count++;
            log.Total += duration;
            if (duration < log.Min) log.Min = duration;
            if (duration > log.Max) log.Max = duration;
        }
    }

    /// <summary>
    /// Mutable accumulator – always accessed under <c>lock</c>.
    /// </summary>
    private sealed class OperationLog
    {
        public int Count;
        public TimeSpan Total = TimeSpan.Zero;
        public TimeSpan Min = TimeSpan.MaxValue;
        public TimeSpan Max = TimeSpan.MinValue;
    }
}
