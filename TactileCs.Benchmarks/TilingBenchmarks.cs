using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using TactileCs.Tiling;
using TactileCs.Geometry;
using TactileCs.Gpu;

namespace TactileCs.Benchmarks;

/// <summary>
/// Benchmarks for the core hot paths in TactileCs.
/// Run with: dotnet run -c Release --project TactileCs.Benchmarks
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class TilingBenchmarks
{
    private IsohedralTiling _tiling = null!;
    private GpuAccelerator _gpu = null!;
    private Vector2[] _origins = null!;
    private Vector2[] _targets = null!;

    [GlobalSetup]
    public void Setup()
    {
        _tiling = new IsohedralTiling(1);
        _gpu = new GpuAccelerator();

        _origins = Enumerable.Range(0, 100)
            .Select(i => new Vector2(i * 0.1, (i % 10) * 0.1))
            .ToArray();

        _targets = Enumerable.Range(0, 100)
            .Select(i => new Vector2((100 - i) * 0.1, (i % 7) * 0.1))
            .ToArray();
    }

    /// <summary>Fills a 4×4 unit region with the tiling from type 1.</summary>
    [Benchmark]
    public int FillRegionBounds()
    {
        int count = 0;
        foreach (var _ in _tiling.FillRegionBounds(-2, -2, 2, 2))
            count++;
        return count;
    }

    /// <summary>Builds a graph over a 4×4 unit region.</summary>
    [Benchmark]
    public int CreateGraph()
    {
        var g = _tiling.CreateGraph(-2, -2, 2, 2);
        return g.Cells.Count;
    }

    /// <summary>CPU fallback for batch squared-distance computation.</summary>
    [Benchmark]
    public double[] BatchDistanceSquared()
    {
        return _gpu.BatchDistanceSquared(_origins, _targets);
    }

    /// <summary>Fills a larger 10×10 region.</summary>
    [Benchmark]
    public int FillRegionBounds_LargeRegion()
    {
        int count = 0;
        foreach (var _ in _tiling.FillRegionBounds(-5, -5, 5, 5))
            count++;
        return count;
    }
}
