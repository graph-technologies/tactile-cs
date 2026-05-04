using System.Runtime.InteropServices;

namespace TactileCs.Gpu;

/// <summary>
/// Low-level P/Invoke bindings to the native TactileCs CUDA helper library
/// (<c>libtactile_cuda</c>).
/// <para>
/// The native library exposes simple C entry points that wrap CUDA kernels
/// for batch geometry operations. If the library is not present or the
/// system has no CUDA-capable GPU, the managed <see cref="GpuAccelerator"/>
/// class transparently falls back to a CPU implementation.
/// </para>
/// </summary>
internal static partial class CudaInterop
{
/// <summary>
/// Name of the native shared library (without platform-specific prefix/extension).
/// The runtime searches the standard library paths.
/// </summary>
private const string LibraryName = "tactile_cuda";

// ------------------------------------------------------------------
//  Internal struct that mirrors the C TactileDeviceInfo layout:
//    char   name[256];          // 256 bytes, offset 0
//    int    computeMajor;       //   4 bytes, offset 256
//    int    computeMinor;       //   4 bytes, offset 260
//    int    multiprocessorCount;//   4 bytes, offset 264
//    int    driverVersion;      //   4 bytes, offset 268
//    long long totalMemBytes;   //   8 bytes, offset 272 (8-byte aligned)
// ------------------------------------------------------------------

[StructLayout(LayoutKind.Sequential, Pack = 0)]
internal struct NativeDeviceInfo
{
[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
public byte[] Name;
public int ComputeMajor;
public int ComputeMinor;
public int MultiprocessorCount;
public int DriverVersion;
public long TotalMemBytes;
}

// ------------------------------------------------------------------
//  Availability
// ------------------------------------------------------------------

/// <summary>
/// Returns non-zero if CUDA is available on the current system.
/// </summary>
[LibraryImport(LibraryName, EntryPoint = "tactile_cuda_available")]
[UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
internal static partial int IsAvailable();

// ------------------------------------------------------------------
//  Device enumeration
// ------------------------------------------------------------------

/// <summary>
/// Returns the number of CUDA-capable devices present on the system,
/// or 0 if no devices are found or the native library is unavailable.
/// </summary>
[LibraryImport(LibraryName, EntryPoint = "tactile_get_device_count")]
[UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
internal static partial int GetDeviceCount();

/// <summary>
/// Fills <paramref name="info"/> with properties of the device at
/// <paramref name="deviceIndex"/>.
/// </summary>
/// <returns>0 on success, non-zero on failure.</returns>
/// <remarks>
/// Uses <c>[DllImport]</c> rather than <c>[LibraryImport]</c> because the
/// struct contains a marshalled fixed-size array which is not supported by
/// the source-generated P/Invoke generator.
/// </remarks>
[DllImport(LibraryName, EntryPoint = "tactile_get_device_info",
	CallingConvention = CallingConvention.Cdecl)]
internal static extern int GetDeviceInfo(int deviceIndex, ref NativeDeviceInfo info);

// ------------------------------------------------------------------
//  Batch distance computation (squared)
// ------------------------------------------------------------------

/// <summary>
/// Computes pairwise squared distances between two sets of 2D points
/// on the GPU.
/// </summary>
/// <param name="ax">X coordinates of set A (length <paramref name="countA"/>).</param>
/// <param name="ay">Y coordinates of set A.</param>
/// <param name="countA">Number of points in set A.</param>
/// <param name="bx">X coordinates of set B (length <paramref name="countB"/>).</param>
/// <param name="by">Y coordinates of set B.</param>
/// <param name="countB">Number of points in set B.</param>
/// <param name="outDistSq">
/// Output buffer of length <c>countA × countB</c> that receives the
/// squared distances in row-major order.
/// </param>
/// <returns>0 on success, non-zero on failure.</returns>
[LibraryImport(LibraryName, EntryPoint = "tactile_batch_distance_sq")]
[UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
internal static partial int BatchDistanceSq(
[In] double[] ax, [In] double[] ay, int countA,
[In] double[] bx, [In] double[] by, int countB,
[Out] double[] outDistSq);

// ------------------------------------------------------------------
//  Batch distance computation (Euclidean)
// ------------------------------------------------------------------

/// <summary>
/// Computes pairwise Euclidean distances between two sets of 2D points
/// on the GPU.
/// </summary>
/// <param name="ax">X coordinates of set A (length <paramref name="countA"/>).</param>
/// <param name="ay">Y coordinates of set A.</param>
/// <param name="countA">Number of points in set A.</param>
/// <param name="bx">X coordinates of set B (length <paramref name="countB"/>).</param>
/// <param name="by">Y coordinates of set B.</param>
/// <param name="countB">Number of points in set B.</param>
/// <param name="outDist">
/// Output buffer of length <c>countA × countB</c> that receives the
/// Euclidean distances in row-major order.
/// </param>
/// <returns>0 on success, non-zero on failure.</returns>
[LibraryImport(LibraryName, EntryPoint = "tactile_batch_distance")]
[UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
internal static partial int BatchDistance(
[In] double[] ax, [In] double[] ay, int countA,
[In] double[] bx, [In] double[] by, int countB,
[Out] double[] outDist);

// ------------------------------------------------------------------
//  Batch point-in-polygon
// ------------------------------------------------------------------

/// <summary>
/// Tests multiple points against a single polygon on the GPU using
/// the winding-number algorithm.
/// </summary>
/// <param name="polyX">X coordinates of polygon vertices.</param>
/// <param name="polyY">Y coordinates of polygon vertices.</param>
/// <param name="vertexCount">Number of polygon vertices.</param>
/// <param name="px">X coordinates of query points.</param>
/// <param name="py">Y coordinates of query points.</param>
/// <param name="pointCount">Number of query points.</param>
/// <param name="outResults">
/// Output buffer of length <paramref name="pointCount"/>.
/// Each element is 1 if the point is inside the polygon, 0 otherwise.
/// </param>
/// <returns>0 on success, non-zero on failure.</returns>
[LibraryImport(LibraryName, EntryPoint = "tactile_batch_point_in_polygon")]
[UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
internal static partial int BatchPointInPolygon(
[In] double[] polyX, [In] double[] polyY, int vertexCount,
[In] double[] px, [In] double[] py, int pointCount,
[Out] int[] outResults);

// ------------------------------------------------------------------
//  Batch affine transform of 2D points
// ------------------------------------------------------------------

/// <summary>
/// Applies the 2×3 affine matrix <c>[A B C; D E F]</c> to every point
/// in the input arrays on the GPU.
/// </summary>
/// <param name="matA">Matrix element A (row 0, col 0).</param>
/// <param name="matB">Matrix element B (row 0, col 1).</param>
/// <param name="matC">Matrix element C – x translation.</param>
/// <param name="matD">Matrix element D (row 1, col 0).</param>
/// <param name="matE">Matrix element E (row 1, col 1).</param>
/// <param name="matF">Matrix element F – y translation.</param>
/// <param name="px">X coordinates of the input points.</param>
/// <param name="py">Y coordinates of the input points.</param>
/// <param name="count">Number of points.</param>
/// <param name="outX">Output X coordinates (length <paramref name="count"/>).</param>
/// <param name="outY">Output Y coordinates (length <paramref name="count"/>).</param>
/// <returns>0 on success, non-zero on failure.</returns>
[LibraryImport(LibraryName, EntryPoint = "tactile_batch_transform_points")]
[UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
internal static partial int BatchTransformPoints(
double matA, double matB, double matC,
double matD, double matE, double matF,
[In] double[] px, [In] double[] py, int count,
[Out] double[] outX, [Out] double[] outY);

// ------------------------------------------------------------------
//  Batch minimum distance to polygon edge
// ------------------------------------------------------------------

/// <summary>
/// For each query point, computes the minimum Euclidean distance to any
/// edge of the supplied polygon on the GPU.
/// </summary>
/// <param name="polyX">X coordinates of polygon vertices.</param>
/// <param name="polyY">Y coordinates of polygon vertices.</param>
/// <param name="vertexCount">Number of polygon vertices.</param>
/// <param name="px">X coordinates of query points.</param>
/// <param name="py">Y coordinates of query points.</param>
/// <param name="pointCount">Number of query points.</param>
/// <param name="outDist">
/// Output buffer of length <paramref name="pointCount"/> that receives
/// the minimum distance from each query point to any polygon edge.
/// </param>
/// <returns>0 on success, non-zero on failure.</returns>
[LibraryImport(LibraryName, EntryPoint = "tactile_batch_min_dist_to_polygon_edge")]
[UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
internal static partial int BatchMinDistToPolygonEdge(
[In] double[] polyX, [In] double[] polyY, int vertexCount,
[In] double[] px, [In] double[] py, int pointCount,
[Out] double[] outDist);
}
