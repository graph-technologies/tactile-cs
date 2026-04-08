using TactileCs.Diagnostics;

namespace TactileCs.Tests.Diagnostics;

public class PerformanceMonitorTests
{
    [Fact]
    public void BeginOperation_RecordsTiming()
    {
        var monitor = new PerformanceMonitor();

        using (monitor.BeginOperation("TestOp"))
        {
            Thread.Sleep(10);
        }

        var metrics = monitor.GetMetrics("TestOp");
        Assert.NotNull(metrics);
        Assert.Equal(1, metrics.InvocationCount);
        Assert.True(metrics.TotalDuration > TimeSpan.Zero);
    }

    [Fact]
    public void GetMetrics_ReturnsNull_ForUnknownOperation()
    {
        var monitor = new PerformanceMonitor();

        Assert.Null(monitor.GetMetrics("NonExistent"));
    }

    [Fact]
    public void GetAllMetrics_ReturnsAllRecordedOperations()
    {
        var monitor = new PerformanceMonitor();

        using (monitor.BeginOperation("OpA")) { }
        using (monitor.BeginOperation("OpB")) { }

        var all = monitor.GetAllMetrics();
        Assert.Equal(2, all.Count);

        var names = all.Select(m => m.OperationName).OrderBy(n => n).ToList();
        Assert.Equal(new[] { "OpA", "OpB" }, names);
    }

    [Fact]
    public void Reset_ClearsAllMetrics()
    {
        var monitor = new PerformanceMonitor();

        using (monitor.BeginOperation("OpToReset")) { }
        Assert.NotNull(monitor.GetMetrics("OpToReset"));

        monitor.Reset();

        Assert.Null(monitor.GetMetrics("OpToReset"));
        Assert.Empty(monitor.GetAllMetrics());
    }

    [Fact]
    public void IsEnabled_False_DoesNotRecord()
    {
        var monitor = new PerformanceMonitor { IsEnabled = false };

        using (monitor.BeginOperation("Disabled")) { }

        Assert.Null(monitor.GetMetrics("Disabled"));
        Assert.Empty(monitor.GetAllMetrics());
    }

    [Fact]
    public void MultipleInvocations_TracksMinMaxAvg()
    {
        var monitor = new PerformanceMonitor();

        using (monitor.BeginOperation("Multi"))
        {
            Thread.Sleep(10);
        }

        using (monitor.BeginOperation("Multi"))
        {
            Thread.Sleep(30);
        }

        var metrics = monitor.GetMetrics("Multi");
        Assert.NotNull(metrics);
        Assert.Equal(2, metrics.InvocationCount);
        Assert.True(metrics.MinDuration > TimeSpan.Zero);
        Assert.True(metrics.MaxDuration >= metrics.MinDuration);
        Assert.True(metrics.AverageDuration >= metrics.MinDuration);
        Assert.True(metrics.AverageDuration <= metrics.MaxDuration);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BeginOperation_ThrowsOnNullOrWhitespace(string? name)
    {
        var monitor = new PerformanceMonitor();

        Assert.ThrowsAny<ArgumentException>(() => monitor.BeginOperation(name!));
    }
}
