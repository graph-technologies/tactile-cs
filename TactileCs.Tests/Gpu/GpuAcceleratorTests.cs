using TactileCs.Diagnostics;
using TactileCs.Geometry;
using TactileCs.Gpu;

namespace TactileCs.Tests.Gpu;

public class GpuAcceleratorTests
{
    [Fact]
    public void IsCudaAvailable_ReturnsFalse_WhenNoGpu()
    {
        // CI environments do not have CUDA-capable GPUs.
        Assert.False(GpuAccelerator.IsCudaAvailable);
    }

    [Fact]
    public void BatchDistanceSquared_CpuFallback_ComputesCorrectDistances()
    {
        var monitor = new PerformanceMonitor();
        var accelerator = new GpuAccelerator(monitor);

        var setA = new Vector2[] { new(0, 0), new(1, 0) };
        var setB = new Vector2[] { new(3, 4), new(0, 0) };

        var result = accelerator.BatchDistanceSquared(setA, setB);

        // Expected (row-major): A0→B0, A0→B1, A1→B0, A1→B1
        // (0,0)→(3,4) = 9+16 = 25
        // (0,0)→(0,0) = 0
        // (1,0)→(3,4) = 4+16 = 20
        // (1,0)→(0,0) = 1
        Assert.Equal(4, result.Length);
        Assert.Equal(25.0, result[0], precision: 10);
        Assert.Equal(0.0, result[1], precision: 10);
        Assert.Equal(20.0, result[2], precision: 10);
        Assert.Equal(1.0, result[3], precision: 10);
    }

    [Fact]
    public void BatchDistanceSquared_EmptySets_ReturnsEmpty()
    {
        var accelerator = new GpuAccelerator(new PerformanceMonitor());

        var result = accelerator.BatchDistanceSquared(
            ReadOnlySpan<Vector2>.Empty,
            ReadOnlySpan<Vector2>.Empty);

        Assert.Empty(result);
    }

    [Fact]
    public void BatchPointInPolygon_CpuFallback_DetectsContainment()
    {
        var accelerator = new GpuAccelerator(new PerformanceMonitor());

        // Unit square: (0,0), (1,0), (1,1), (0,1)
        var square = new Polygon(new[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),
        });

        var queryPoints = new Vector2[]
        {
            new(0.5, 0.5),  // inside
            new(2.0, 2.0),  // outside
            new(-1, -1),    // outside
        };

        var results = accelerator.BatchPointInPolygon(square, queryPoints);

        Assert.Equal(3, results.Length);
        Assert.True(results[0]);   // inside
        Assert.False(results[1]);  // outside
        Assert.False(results[2]);  // outside
    }

    [Fact]
    public void BatchPointInPolygon_ThrowsOnNullPolygon()
    {
        var accelerator = new GpuAccelerator(new PerformanceMonitor());

        Assert.Throws<ArgumentNullException>(() =>
            accelerator.BatchPointInPolygon(null!, ReadOnlySpan<Vector2>.Empty));
    }
}
