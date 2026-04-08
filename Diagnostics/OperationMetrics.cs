using System.Diagnostics;

namespace TactileCs.Diagnostics;

/// <summary>
/// Immutable snapshot of performance metrics for a single named operation.
/// </summary>
public sealed class OperationMetrics
{
    public string OperationName { get; }
    public int InvocationCount { get; }
    public TimeSpan TotalDuration { get; }
    public TimeSpan MinDuration { get; }
    public TimeSpan MaxDuration { get; }
    public TimeSpan AverageDuration { get; }

    internal OperationMetrics(
        string operationName,
        int invocationCount,
        TimeSpan totalDuration,
        TimeSpan minDuration,
        TimeSpan maxDuration)
    {
        OperationName = operationName;
        InvocationCount = invocationCount;
        TotalDuration = totalDuration;
        MinDuration = minDuration;
        MaxDuration = maxDuration;
        AverageDuration = invocationCount > 0
            ? TimeSpan.FromTicks(totalDuration.Ticks / invocationCount)
            : TimeSpan.Zero;
    }

    public override string ToString() =>
        $"{OperationName}: count={InvocationCount}, " +
        $"total={TotalDuration.TotalMilliseconds:F2}ms, " +
        $"avg={AverageDuration.TotalMilliseconds:F2}ms, " +
        $"min={MinDuration.TotalMilliseconds:F2}ms, " +
        $"max={MaxDuration.TotalMilliseconds:F2}ms";
}

/// <summary>
/// Lightweight disposable timer returned by <see cref="PerformanceMonitor.BeginOperation"/>.
/// Records elapsed time when disposed.
/// </summary>
public sealed class OperationTimer : IDisposable
{
    private readonly PerformanceMonitor _monitor;
    private readonly string _operationName;
    private readonly Stopwatch _stopwatch;
    private bool _disposed;

    internal OperationTimer(PerformanceMonitor monitor, string operationName)
    {
        _monitor = monitor;
        _operationName = operationName;
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Elapsed time since the timer was started.
    /// </summary>
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _stopwatch.Stop();
        _monitor.RecordDuration(_operationName, _stopwatch.Elapsed);
    }
}
