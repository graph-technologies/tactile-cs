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
    //  Availability
    // ------------------------------------------------------------------

    /// <summary>
    /// Returns non-zero if CUDA is available on the current system.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "tactile_cuda_available")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int IsAvailable();

    // ------------------------------------------------------------------
    //  Batch distance computation
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
}
