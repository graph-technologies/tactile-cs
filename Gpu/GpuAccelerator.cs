using Microsoft.Extensions.Logging;
using TactileCs.Diagnostics;
using TactileCs.Geometry;

namespace TactileCs.Gpu;

/// <summary>
/// High-level API for GPU-accelerated geometry operations.
/// <para>
/// When a CUDA-capable GPU and the native helper library are present the
/// accelerator dispatches work to the GPU.  Otherwise it falls back to an
/// equivalent CPU implementation automatically, so callers never need to
/// check availability themselves.
/// </para>
/// </summary>
public sealed class GpuAccelerator
{
    private static readonly Lazy<bool> _cudaAvailable = new(ProbeCuda);

    private readonly ILogger _logger;
    private readonly PerformanceMonitor _monitor;

    /// <summary>
    /// Creates a new accelerator that reports metrics to the given monitor.
    /// </summary>
    public GpuAccelerator(PerformanceMonitor? monitor = null)
    {
        _monitor = monitor ?? PerformanceMonitor.Default;
        _logger = TactileLogger.CreateLogger(nameof(GpuAccelerator));
    }

    /// <summary>
    /// Returns <c>true</c> when CUDA acceleration is available on this system.
    /// </summary>
    public static bool IsCudaAvailable => _cudaAvailable.Value;

    // ------------------------------------------------------------------
    //  Batch squared-distance matrix
    // ------------------------------------------------------------------

    /// <summary>
    /// Computes pairwise squared distances between two sets of 2D points.
    /// Returns a flat array of length <c>setA.Length × setB.Length</c> in
    /// row-major order.
    /// </summary>
    public double[] BatchDistanceSquared(ReadOnlySpan<Vector2> setA, ReadOnlySpan<Vector2> setB)
    {
        using var timer = _monitor.BeginOperation("BatchDistanceSquared");

        if (IsCudaAvailable)
        {
            return BatchDistanceSquaredGpu(setA, setB);
        }

        return BatchDistanceSquaredCpu(setA, setB);
    }

    // ------------------------------------------------------------------
    //  Batch point-in-polygon
    // ------------------------------------------------------------------

    /// <summary>
    /// Tests multiple query points against a polygon.
    /// Returns an array of booleans, one per query point.
    /// </summary>
    public bool[] BatchPointInPolygon(Polygon polygon, ReadOnlySpan<Vector2> queryPoints)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        using var timer = _monitor.BeginOperation("BatchPointInPolygon");

        if (IsCudaAvailable)
        {
            return BatchPointInPolygonGpu(polygon, queryPoints);
        }

        return BatchPointInPolygonCpu(polygon, queryPoints);
    }

    // ==================================================================
    //  GPU paths
    // ==================================================================

    private double[] BatchDistanceSquaredGpu(ReadOnlySpan<Vector2> setA, ReadOnlySpan<Vector2> setB)
    {
        _logger.LogDebug("BatchDistanceSquared: GPU path ({A}×{B})", setA.Length, setB.Length);

        var ax = new double[setA.Length];
        var ay = new double[setA.Length];
        for (int i = 0; i < setA.Length; i++) { ax[i] = setA[i].X; ay[i] = setA[i].Y; }

        var bx = new double[setB.Length];
        var by = new double[setB.Length];
        for (int i = 0; i < setB.Length; i++) { bx[i] = setB[i].X; by[i] = setB[i].Y; }

        var result = new double[setA.Length * setB.Length];
        int rc = CudaInterop.BatchDistanceSq(ax, ay, setA.Length, bx, by, setB.Length, result);
        if (rc != 0)
        {
            _logger.LogWarning("CUDA BatchDistanceSq failed (rc={Rc}); falling back to CPU", rc);
            return BatchDistanceSquaredCpu(setA, setB);
        }

        return result;
    }

    private bool[] BatchPointInPolygonGpu(Polygon polygon, ReadOnlySpan<Vector2> queryPoints)
    {
        _logger.LogDebug("BatchPointInPolygon: GPU path ({Pts} points, {Verts} vertices)",
            queryPoints.Length, polygon.Count);

        var polyX = new double[polygon.Count];
        var polyY = new double[polygon.Count];
        var verts = polygon.Vertices;
        for (int i = 0; i < polygon.Count; i++) { polyX[i] = verts[i].X; polyY[i] = verts[i].Y; }

        var px = new double[queryPoints.Length];
        var py = new double[queryPoints.Length];
        for (int i = 0; i < queryPoints.Length; i++) { px[i] = queryPoints[i].X; py[i] = queryPoints[i].Y; }

        var rawResults = new int[queryPoints.Length];
        int rc = CudaInterop.BatchPointInPolygon(polyX, polyY, polygon.Count, px, py, queryPoints.Length, rawResults);
        if (rc != 0)
        {
            _logger.LogWarning("CUDA BatchPointInPolygon failed (rc={Rc}); falling back to CPU", rc);
            return BatchPointInPolygonCpu(polygon, queryPoints);
        }

        var results = new bool[queryPoints.Length];
        for (int i = 0; i < queryPoints.Length; i++) results[i] = rawResults[i] != 0;
        return results;
    }

    // ==================================================================
    //  CPU fallback paths
    // ==================================================================

    private static double[] BatchDistanceSquaredCpu(ReadOnlySpan<Vector2> setA, ReadOnlySpan<Vector2> setB)
    {
        var result = new double[setA.Length * setB.Length];
        for (int i = 0; i < setA.Length; i++)
        {
            for (int j = 0; j < setB.Length; j++)
            {
                result[i * setB.Length + j] = Vector2.DistanceSquared(setA[i], setB[j]);
            }
        }
        return result;
    }

    private static bool[] BatchPointInPolygonCpu(Polygon polygon, ReadOnlySpan<Vector2> queryPoints)
    {
        var results = new bool[queryPoints.Length];
        for (int i = 0; i < queryPoints.Length; i++)
        {
            results[i] = polygon.Contains(queryPoints[i]);
        }
        return results;
    }

    // ==================================================================
    //  Native probe
    // ==================================================================

    private static bool ProbeCuda()
    {
        try
        {
            return CudaInterop.IsAvailable() != 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }
}
