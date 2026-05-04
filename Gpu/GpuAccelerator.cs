using System.Text;
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
/// <para>
/// Call <see cref="GetAllDeviceInfos"/> or <see cref="TryGetDeviceInfo"/>
/// to inspect hardware properties and verify that the installed GPU meets
/// the minimum requirements for TactileCs (Compute Capability ≥ 3.5,
/// driver ≥ 11.4).
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

// ==================================================================
//  Device information
// ==================================================================

/// <summary>
/// Returns a snapshot of the hardware properties for every CUDA-capable
/// device present on the system.  Returns an empty list when no devices
/// are found or the native library is not loaded.
/// </summary>
/// <remarks>
/// Use this method to verify that at least one device meets the minimum
/// requirements for TactileCs (<see cref="GpuRequirements"/>), or to
/// display diagnostics to the user before starting a GPU-intensive job.
/// </remarks>
public static IReadOnlyList<GpuDeviceInfo> GetAllDeviceInfos()
{
try
{
int count = CudaInterop.GetDeviceCount();
var list = new List<GpuDeviceInfo>(count);
for (int i = 0; i < count; i++)
{
if (TryGetDeviceInfo(i, out var info) && info is not null)
list.Add(info);
}
return list;
}
catch (DllNotFoundException)   { return []; }
catch (EntryPointNotFoundException) { return []; }
}

/// <summary>
/// Attempts to retrieve hardware properties for the device at
/// <paramref name="deviceIndex"/>.
/// </summary>
/// <param name="deviceIndex">Zero-based device index.</param>
/// <param name="info">
/// When this method returns <c>true</c>, contains the device snapshot;
/// otherwise <c>null</c>.
/// </param>
/// <returns>
/// <c>true</c> if device info was successfully retrieved; <c>false</c>
/// if the native library is absent, the index is out of range, or a
/// CUDA error occurred.
/// </returns>
public static bool TryGetDeviceInfo(int deviceIndex, out GpuDeviceInfo? info)
{
info = null;
try
{
var native = new CudaInterop.NativeDeviceInfo();
int rc = CudaInterop.GetDeviceInfo(deviceIndex, ref native);
if (rc != 0) return false;

string name = native.Name is not null
? Encoding.UTF8.GetString(native.Name).TrimEnd('\0')
: string.Empty;

info = new GpuDeviceInfo(
deviceIndex,
name,
native.ComputeMajor,
native.ComputeMinor,
native.MultiprocessorCount,
native.TotalMemBytes,
native.DriverVersion);
return true;
}
catch (DllNotFoundException)        { return false; }
catch (EntryPointNotFoundException) { return false; }
}

/// <summary>
/// Logs a diagnostic summary of all CUDA devices found on the system,
/// including whether each meets the minimum requirements for TactileCs.
/// When no devices are found a warning is logged instead.
/// </summary>
public void LogDeviceDiagnostics()
{
var devices = GetAllDeviceInfos();
if (devices.Count == 0)
{
_logger.LogWarning(
"No CUDA-capable GPU detected. TactileCs will use CPU fallbacks for all " +
"GPU operations. To enable GPU acceleration, install an NVIDIA GPU with " +
"Compute Capability >= {Major}.{Minor} and CUDA driver >= {Driver}.",
GpuRequirements.MinComputeMajor,
GpuRequirements.MinComputeMinor,
GpuRequirements.MinDriverVersion / 1000 + "." +
(GpuRequirements.MinDriverVersion % 1000) / 10);
return;
}

foreach (var d in devices)
{
if (!d.MeetsMinimumRequirements)
{
_logger.LogWarning(
"GPU [{Index}] {Name} has Compute Capability {CC}, which is below the " +
"minimum required ({Min}). This device will not be used for " +
"TactileCs GPU operations.",
d.DeviceIndex, d.Name, d.ComputeCapability,
$"{GpuRequirements.MinComputeMajor}.{GpuRequirements.MinComputeMinor}");
}
else if (!d.DriverMeetsMinimumRequirements)
{
_logger.LogWarning(
"GPU [{Index}] {Name}: installed driver {Driver} is below the minimum " +
"required driver version. Please update your NVIDIA driver.",
d.DeviceIndex, d.Name, d.DriverVersionString);
}
else
{
_logger.LogInformation(
"GPU [{Index}] {Name} | CC {CC} | {Mem:F0} MiB | {SMs} SMs | Driver {Driver}",
d.DeviceIndex, d.Name, d.ComputeCapability,
d.TotalMemoryMiB, d.MultiprocessorCount, d.DriverVersionString);
}
}
}

// ==================================================================
//  Batch squared-distance matrix
// ==================================================================

/// <summary>
/// Computes pairwise squared distances between two sets of 2D points.
/// Returns a flat array of length <c>setA.Length × setB.Length</c> in
/// row-major order.
/// </summary>
public double[] BatchDistanceSquared(ReadOnlySpan<Vector2> setA, ReadOnlySpan<Vector2> setB)
{
using var timer = _monitor.BeginOperation("BatchDistanceSquared");

if (IsCudaAvailable)
return BatchDistanceSquaredGpu(setA, setB);

return BatchDistanceSquaredCpu(setA, setB);
}

// ==================================================================
//  Batch Euclidean distance matrix
// ==================================================================

/// <summary>
/// Computes pairwise Euclidean distances between two sets of 2D points.
/// Returns a flat array of length <c>setA.Length × setB.Length</c> in
/// row-major order.
/// </summary>
public double[] BatchDistance(ReadOnlySpan<Vector2> setA, ReadOnlySpan<Vector2> setB)
{
using var timer = _monitor.BeginOperation("BatchDistance");

if (IsCudaAvailable)
return BatchDistanceGpu(setA, setB);

return BatchDistanceCpu(setA, setB);
}

// ==================================================================
//  Batch point-in-polygon
// ==================================================================

/// <summary>
/// Tests multiple query points against a polygon.
/// Returns an array of booleans, one per query point.
/// </summary>
public bool[] BatchPointInPolygon(Polygon polygon, ReadOnlySpan<Vector2> queryPoints)
{
ArgumentNullException.ThrowIfNull(polygon);

using var timer = _monitor.BeginOperation("BatchPointInPolygon");

if (IsCudaAvailable)
return BatchPointInPolygonGpu(polygon, queryPoints);

return BatchPointInPolygonCpu(polygon, queryPoints);
}

// ==================================================================
//  Batch affine transform
// ==================================================================

/// <summary>
/// Applies a 2D affine transform to every point in the input span.
/// Returns an array of transformed <see cref="Vector2"/> values.
/// </summary>
/// <param name="transform">The transform to apply.</param>
/// <param name="points">The points to transform.</param>
public Vector2[] BatchTransformPoints(in Transform2D transform, ReadOnlySpan<Vector2> points)
{
using var timer = _monitor.BeginOperation("BatchTransformPoints");

if (IsCudaAvailable)
return BatchTransformPointsGpu(transform, points);

return BatchTransformPointsCpu(transform, points);
}

// ==================================================================
//  Batch minimum distance to polygon edge
// ==================================================================

/// <summary>
/// For each query point, computes the minimum Euclidean distance to the
/// nearest edge of the supplied polygon.
/// Returns an array of distances, one per query point.
/// </summary>
/// <param name="polygon">The reference polygon.</param>
/// <param name="queryPoints">The points to test.</param>
public double[] BatchMinDistanceToPolygonEdge(Polygon polygon, ReadOnlySpan<Vector2> queryPoints)
{
ArgumentNullException.ThrowIfNull(polygon);

using var timer = _monitor.BeginOperation("BatchMinDistanceToPolygonEdge");

if (IsCudaAvailable)
return BatchMinDistToPolygonEdgeGpu(polygon, queryPoints);

return BatchMinDistToPolygonEdgeCpu(polygon, queryPoints);
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

private double[] BatchDistanceGpu(ReadOnlySpan<Vector2> setA, ReadOnlySpan<Vector2> setB)
{
_logger.LogDebug("BatchDistance: GPU path ({A}×{B})", setA.Length, setB.Length);

var ax = new double[setA.Length];
var ay = new double[setA.Length];
for (int i = 0; i < setA.Length; i++) { ax[i] = setA[i].X; ay[i] = setA[i].Y; }

var bx = new double[setB.Length];
var by = new double[setB.Length];
for (int i = 0; i < setB.Length; i++) { bx[i] = setB[i].X; by[i] = setB[i].Y; }

var result = new double[setA.Length * setB.Length];
int rc = CudaInterop.BatchDistance(ax, ay, setA.Length, bx, by, setB.Length, result);
if (rc != 0)
{
_logger.LogWarning("CUDA BatchDistance failed (rc={Rc}); falling back to CPU", rc);
return BatchDistanceCpu(setA, setB);
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

private Vector2[] BatchTransformPointsGpu(in Transform2D transform, ReadOnlySpan<Vector2> points)
{
_logger.LogDebug("BatchTransformPoints: GPU path ({N} points)", points.Length);

var px = new double[points.Length];
var py = new double[points.Length];
for (int i = 0; i < points.Length; i++) { px[i] = points[i].X; py[i] = points[i].Y; }

var outX = new double[points.Length];
var outY = new double[points.Length];

int rc = CudaInterop.BatchTransformPoints(
transform.A, transform.B, transform.C,
transform.D, transform.E, transform.F,
px, py, points.Length, outX, outY);

if (rc != 0)
{
_logger.LogWarning("CUDA BatchTransformPoints failed (rc={Rc}); falling back to CPU", rc);
return BatchTransformPointsCpu(transform, points);
}

var result = new Vector2[points.Length];
for (int i = 0; i < points.Length; i++) result[i] = new Vector2(outX[i], outY[i]);
return result;
}

private double[] BatchMinDistToPolygonEdgeGpu(Polygon polygon, ReadOnlySpan<Vector2> queryPoints)
{
_logger.LogDebug("BatchMinDistanceToPolygonEdge: GPU path ({Pts} points, {Verts} vertices)",
queryPoints.Length, polygon.Count);

var polyX = new double[polygon.Count];
var polyY = new double[polygon.Count];
var verts = polygon.Vertices;
for (int i = 0; i < polygon.Count; i++) { polyX[i] = verts[i].X; polyY[i] = verts[i].Y; }

var px = new double[queryPoints.Length];
var py = new double[queryPoints.Length];
for (int i = 0; i < queryPoints.Length; i++) { px[i] = queryPoints[i].X; py[i] = queryPoints[i].Y; }

var result = new double[queryPoints.Length];
int rc = CudaInterop.BatchMinDistToPolygonEdge(
polyX, polyY, polygon.Count, px, py, queryPoints.Length, result);

if (rc != 0)
{
_logger.LogWarning("CUDA BatchMinDistToPolygonEdge failed (rc={Rc}); falling back to CPU", rc);
return BatchMinDistToPolygonEdgeCpu(polygon, queryPoints);
}

return result;
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

private static double[] BatchDistanceCpu(ReadOnlySpan<Vector2> setA, ReadOnlySpan<Vector2> setB)
{
var result = new double[setA.Length * setB.Length];
for (int i = 0; i < setA.Length; i++)
{
for (int j = 0; j < setB.Length; j++)
{
result[i * setB.Length + j] = Vector2.Distance(setA[i], setB[j]);
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

private static Vector2[] BatchTransformPointsCpu(in Transform2D transform, ReadOnlySpan<Vector2> points)
{
var result = new Vector2[points.Length];
for (int i = 0; i < points.Length; i++)
result[i] = transform.Apply(points[i]);
return result;
}

private static double[] BatchMinDistToPolygonEdgeCpu(Polygon polygon, ReadOnlySpan<Vector2> queryPoints)
{
var verts = polygon.Vertices;
int n = polygon.Count;
var result = new double[queryPoints.Length];

for (int q = 0; q < queryPoints.Length; q++)
{
var pt = queryPoints[q];
double minDist = double.MaxValue;

for (int i = 0; i < n; i++)
{
var a = verts[i];
var b = verts[(i + 1) % n];
double d = Geometry.GeometryUtils.DistanceToSegment(pt, a, b);
if (d < minDist) minDist = d;
}

result[q] = minDist;
}

return result;
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
