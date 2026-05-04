/*
 * batch_geometry.cu
 * -----------------
 * CUDA kernels for the TactileCs GPU-accelerated geometry operations.
 *
 * Build (Linux):
 *   nvcc -shared -o libtactile_cuda.so batch_geometry.cu -Xcompiler -fPIC
 *
 * Build (Windows):
 *   nvcc -shared -o tactile_cuda.dll batch_geometry.cu
 *
 * The resulting shared library is loaded at runtime by the C# P/Invoke
 * layer (CudaInterop.cs).  If the library or a CUDA-capable GPU is not
 * present the managed GpuAccelerator class falls back to a CPU path.
 *
 * Requirements
 * ------------
 * - NVIDIA GPU with Compute Capability >= 3.5
 * - CUDA Toolkit >= 11.0 (nvcc on PATH)
 * - Driver >= 450.x (Linux) / 451.x (Windows)
 *
 * Checking your driver version
 * ----------------------------
 *   Linux:   nvidia-smi
 *   Windows: nvidia-smi or Device Manager -> Display Adapters
 *
 * Return codes
 * ------------
 *  0  success
 * -1  cudaMalloc / device-count failure
 * -2  kernel execution or synchronisation failure
 * -3  cudaMemcpy failure
 */

#include <cuda_runtime.h>
#include <math.h>
#include <stdio.h>

/* ======================================================================
 * Minimum supported compute capability
 * ====================================================================== */

#define TACTILE_MIN_CC_MAJOR 3
#define TACTILE_MIN_CC_MINOR 5

/* ======================================================================
 * Device info structure
 * Used by tactile_get_device_info to return device properties in a flat
 * layout that maps directly to the C# GpuDeviceInfo struct.
 * ====================================================================== */

typedef struct
{
    char   name[256];
    int    computeMajor;
    int    computeMinor;
    int    multiprocessorCount;
    int    driverVersion;
    long long totalMemBytes;
} TactileDeviceInfo;

/* ======================================================================
 * Helper: device availability
 * ====================================================================== */

extern "C" int tactile_cuda_available(void)
{
    int deviceCount = 0;
    cudaError_t err = cudaGetDeviceCount(&deviceCount);
    return (err == cudaSuccess && deviceCount > 0) ? 1 : 0;
}

/* ======================================================================
 * Helper: device count
 * Returns the number of CUDA-capable devices, or 0 on error.
 * ====================================================================== */

extern "C" int tactile_get_device_count(void)
{
    int count = 0;
    if (cudaGetDeviceCount(&count) != cudaSuccess) return 0;
    return count;
}

/* ======================================================================
 * Helper: per-device information
 *
 * Fills *out with properties of device at index deviceIndex.
 * Returns 0 on success, -1 if deviceIndex is out of range or a CUDA
 * error occurs.
 * ====================================================================== */

extern "C" int tactile_get_device_info(int deviceIndex, TactileDeviceInfo* out)
{
    if (!out) return -1;

    int count = 0;
    if (cudaGetDeviceCount(&count) != cudaSuccess || deviceIndex < 0 || deviceIndex >= count)
        return -1;

    cudaDeviceProp prop;
    if (cudaGetDeviceProperties(&prop, deviceIndex) != cudaSuccess)
        return -1;

    snprintf(out->name, sizeof(out->name), "%s", prop.name);
    out->computeMajor        = prop.major;
    out->computeMinor        = prop.minor;
    out->multiprocessorCount = prop.multiProcessorCount;
    out->totalMemBytes       = (long long)prop.totalGlobalMem;

    int driverVer = 0;
    cudaDriverGetVersion(&driverVer);   /* best-effort; ignore errors */
    out->driverVersion = driverVer;

    return 0;
}

/* ======================================================================
 * Kernel: pairwise squared distance
 * ====================================================================== */

__global__ void kernel_batch_distance_sq(
    const double* __restrict__ ax,
    const double* __restrict__ ay,
    int countA,
    const double* __restrict__ bx,
    const double* __restrict__ by,
    int countB,
    double* __restrict__ out)
{
    int idx = blockIdx.x * blockDim.x + threadIdx.x;
    int total = countA * countB;
    if (idx >= total) return;

    int i = idx / countB;
    int j = idx % countB;

    double dx = ax[i] - bx[j];
    double dy = ay[i] - by[j];
    out[idx] = dx * dx + dy * dy;
}

extern "C" int tactile_batch_distance_sq(
    const double* ax, const double* ay, int countA,
    const double* bx, const double* by, int countB,
    double* outDistSq)
{
    int total = countA * countB;
    if (total <= 0) return 0;

    int rc = 0;
    double *d_ax = NULL, *d_ay = NULL, *d_bx = NULL, *d_by = NULL, *d_out = NULL;
    size_t sizeA = countA * sizeof(double);
    size_t sizeB = countB * sizeof(double);
    size_t sizeO = (size_t)total * sizeof(double);

    if (cudaMalloc(&d_ax, sizeA) != cudaSuccess) { rc = -1; goto cleanup; }
    if (cudaMalloc(&d_ay, sizeA) != cudaSuccess) { rc = -1; goto cleanup; }
    if (cudaMalloc(&d_bx, sizeB) != cudaSuccess) { rc = -1; goto cleanup; }
    if (cudaMalloc(&d_by, sizeB) != cudaSuccess) { rc = -1; goto cleanup; }
    if (cudaMalloc(&d_out, sizeO) != cudaSuccess) { rc = -1; goto cleanup; }

    if (cudaMemcpy(d_ax, ax, sizeA, cudaMemcpyHostToDevice) != cudaSuccess) { rc = -3; goto cleanup; }
    if (cudaMemcpy(d_ay, ay, sizeA, cudaMemcpyHostToDevice) != cudaSuccess) { rc = -3; goto cleanup; }
    if (cudaMemcpy(d_bx, bx, sizeB, cudaMemcpyHostToDevice) != cudaSuccess) { rc = -3; goto cleanup; }
    if (cudaMemcpy(d_by, by, sizeB, cudaMemcpyHostToDevice) != cudaSuccess) { rc = -3; goto cleanup; }

    {
        int blockSize = 256;
        int gridSize = (total + blockSize - 1) / blockSize;
        kernel_batch_distance_sq<<<gridSize, blockSize>>>(d_ax, d_ay, countA, d_bx, d_by, countB, d_out);
    }

    if (cudaDeviceSynchronize() != cudaSuccess) { rc = -2; goto cleanup; }
    if (cudaMemcpy(outDistSq, d_out, sizeO, cudaMemcpyDeviceToHost) != cudaSuccess) { rc = -3; goto cleanup; }

cleanup:
    if (d_out) cudaFree(d_out);
    if (d_by)  cudaFree(d_by);
    if (d_bx)  cudaFree(d_bx);
    if (d_ay)  cudaFree(d_ay);
    if (d_ax)  cudaFree(d_ax);
    return rc;
}

/* ======================================================================
 * Kernel: point-in-polygon (winding number)
 * ====================================================================== */

__global__ void kernel_point_in_polygon(
    const double* __restrict__ polyX,
    const double* __restrict__ polyY,
    int vertexCount,
    const double* __restrict__ px,
    const double* __restrict__ py,
    int pointCount,
    int* __restrict__ outResults)
{
    int idx = blockIdx.x * blockDim.x + threadIdx.x;
    if (idx >= pointCount) return;

    double testX = px[idx];
    double testY = py[idx];
    int winding = 0;

    for (int i = 0; i < vertexCount; i++)
    {
        int next = (i + 1) % vertexCount;
        double y0 = polyY[i];
        double y1 = polyY[next];

        if (y0 <= testY)
        {
            if (y1 > testY)
            {
                double cross = (polyX[next] - polyX[i]) * (testY - y0)
                             - (testX - polyX[i]) * (y1 - y0);
                if (cross > 0.0) winding++;
            }
        }
        else
        {
            if (y1 <= testY)
            {
                double cross = (polyX[next] - polyX[i]) * (testY - y0)
                             - (testX - polyX[i]) * (y1 - y0);
                if (cross < 0.0) winding--;
            }
        }
    }

    outResults[idx] = (winding != 0) ? 1 : 0;
}

extern "C" int tactile_batch_point_in_polygon(
    const double* polyX, const double* polyY, int vertexCount,
    const double* px, const double* py, int pointCount,
    int* outResults)
{
    if (pointCount <= 0) return 0;

    int rc = 0;
    double *d_polyX = NULL, *d_polyY = NULL, *d_px = NULL, *d_py = NULL;
    int *d_out = NULL;
    size_t sizePoly = vertexCount * sizeof(double);
    size_t sizeP    = pointCount * sizeof(double);
    size_t sizeO    = pointCount * sizeof(int);

    if (cudaMalloc(&d_polyX, sizePoly) != cudaSuccess) { rc = -1; goto cleanup; }
    if (cudaMalloc(&d_polyY, sizePoly) != cudaSuccess) { rc = -1; goto cleanup; }
    if (cudaMalloc(&d_px, sizeP) != cudaSuccess) { rc = -1; goto cleanup; }
    if (cudaMalloc(&d_py, sizeP) != cudaSuccess) { rc = -1; goto cleanup; }
    if (cudaMalloc(&d_out, sizeO) != cudaSuccess) { rc = -1; goto cleanup; }

    if (cudaMemcpy(d_polyX, polyX, sizePoly, cudaMemcpyHostToDevice) != cudaSuccess) { rc = -3; goto cleanup; }
    if (cudaMemcpy(d_polyY, polyY, sizePoly, cudaMemcpyHostToDevice) != cudaSuccess) { rc = -3; goto cleanup; }
    if (cudaMemcpy(d_px, px, sizeP, cudaMemcpyHostToDevice) != cudaSuccess) { rc = -3; goto cleanup; }
    if (cudaMemcpy(d_py, py, sizeP, cudaMemcpyHostToDevice) != cudaSuccess) { rc = -3; goto cleanup; }

    {
        int blockSize = 256;
        int gridSize = (pointCount + blockSize - 1) / blockSize;
        kernel_point_in_polygon<<<gridSize, blockSize>>>(d_polyX, d_polyY, vertexCount, d_px, d_py, pointCount, d_out);
    }

    if (cudaDeviceSynchronize() != cudaSuccess) { rc = -2; goto cleanup; }
    if (cudaMemcpy(outResults, d_out, sizeO, cudaMemcpyDeviceToHost) != cudaSuccess) { rc = -3; goto cleanup; }

cleanup:
    if (d_out)   cudaFree(d_out);
    if (d_py)    cudaFree(d_py);
    if (d_px)    cudaFree(d_px);
    if (d_polyY) cudaFree(d_polyY);
    if (d_polyX) cudaFree(d_polyX);
    return rc;
}

/* ======================================================================
 * Kernel: batch Euclidean distance (non-squared)
 * ====================================================================== */

__global__ void kernel_batch_distance(
    const double* __restrict__ ax,
    const double* __restrict__ ay,
    int countA,
    const double* __restrict__ bx,
    const double* __restrict__ by,
    int countB,
    double* __restrict__ out)
{
    int idx = blockIdx.x * blockDim.x + threadIdx.x;
    int total = countA * countB;
    if (idx >= total) return;

    int i = idx / countB;
    int j = idx % countB;

    double dx = ax[i] - bx[j];
    double dy = ay[i] - by[j];
    out[idx] = sqrt(dx * dx + dy * dy);
}

extern "C" int tactile_batch_distance(
    const double* ax, const double* ay, int countA,
    const double* bx, const double* by, int countB,
    double* outDist)
{
    int total = countA * countB;
    if (total <= 0) return 0;

    int rc = 0;
    double *d_ax = NULL, *d_ay = NULL, *d_bx = NULL, *d_by = NULL, *d_out = NULL;
    size_t sizeA = countA * sizeof(double);
    size_t sizeB = countB * sizeof(double);
    size_t sizeO = (size_t)total * sizeof(double);

    if (cudaMalloc(&d_ax, sizeA) != cudaSuccess) { rc = -1; goto cleanup; }
    if (cudaMalloc(&d_ay, sizeA) != cudaSuccess) { rc = -1; goto cleanup; }
    if (cudaMalloc(&d_bx, sizeB) != cudaSuccess) { rc = -1; goto cleanup; }
    if (cudaMalloc(&d_by, sizeB) != cudaSuccess) { rc = -1; goto cleanup; }
    if (cudaMalloc(&d_out, sizeO) != cudaSuccess) { rc = -1; goto cleanup; }

    if (cudaMemcpy(d_ax, ax, sizeA, cudaMemcpyHostToDevice) != cudaSuccess) { rc = -3; goto cleanup; }
    if (cudaMemcpy(d_ay, ay, sizeA, cudaMemcpyHostToDevice) != cudaSuccess) { rc = -3; goto cleanup; }
    if (cudaMemcpy(d_bx, bx, sizeB, cudaMemcpyHostToDevice) != cudaSuccess) { rc = -3; goto cleanup; }
    if (cudaMemcpy(d_by, by, sizeB, cudaMemcpyHostToDevice) != cudaSuccess) { rc = -3; goto cleanup; }

    {
        int blockSize = 256;
        int gridSize = (total + blockSize - 1) / blockSize;
        kernel_batch_distance<<<gridSize, blockSize>>>(d_ax, d_ay, countA, d_bx, d_by, countB, d_out);
    }

    if (cudaDeviceSynchronize() != cudaSuccess) { rc = -2; goto cleanup; }
    if (cudaMemcpy(outDist, d_out, sizeO, cudaMemcpyDeviceToHost) != cudaSuccess) { rc = -3; goto cleanup; }

cleanup:
    if (d_out) cudaFree(d_out);
    if (d_by)  cudaFree(d_by);
    if (d_bx)  cudaFree(d_bx);
    if (d_ay)  cudaFree(d_ay);
    if (d_ax)  cudaFree(d_ax);
    return rc;
}

/* ======================================================================
 * Kernel: batch affine transform of 2D points
 *
 * Applies the 2×3 affine matrix [A B C; D E F] to every point.
 *   x' = A*x + B*y + C
 *   y' = D*x + E*y + F
 * ====================================================================== */

__global__ void kernel_batch_transform_points(
    double matA, double matB, double matC,
    double matD, double matE, double matF,
    const double* __restrict__ px,
    const double* __restrict__ py,
    int count,
    double* __restrict__ outX,
    double* __restrict__ outY)
{
    int idx = blockIdx.x * blockDim.x + threadIdx.x;
    if (idx >= count) return;

    outX[idx] = matA * px[idx] + matB * py[idx] + matC;
    outY[idx] = matD * px[idx] + matE * py[idx] + matF;
}

extern "C" int tactile_batch_transform_points(
    double matA, double matB, double matC,
    double matD, double matE, double matF,
    const double* px, const double* py, int count,
    double* outX, double* outY)
{
    if (count <= 0) return 0;

    int rc = 0;
    double *d_px = NULL, *d_py = NULL, *d_outX = NULL, *d_outY = NULL;
    size_t sz = count * sizeof(double);

    if (cudaMalloc(&d_px,   sz) != cudaSuccess) { rc = -1; goto cleanup; }
    if (cudaMalloc(&d_py,   sz) != cudaSuccess) { rc = -1; goto cleanup; }
    if (cudaMalloc(&d_outX, sz) != cudaSuccess) { rc = -1; goto cleanup; }
    if (cudaMalloc(&d_outY, sz) != cudaSuccess) { rc = -1; goto cleanup; }

    if (cudaMemcpy(d_px, px, sz, cudaMemcpyHostToDevice) != cudaSuccess) { rc = -3; goto cleanup; }
    if (cudaMemcpy(d_py, py, sz, cudaMemcpyHostToDevice) != cudaSuccess) { rc = -3; goto cleanup; }

    {
        int blockSize = 256;
        int gridSize = (count + blockSize - 1) / blockSize;
        kernel_batch_transform_points<<<gridSize, blockSize>>>(
            matA, matB, matC, matD, matE, matF,
            d_px, d_py, count, d_outX, d_outY);
    }

    if (cudaDeviceSynchronize() != cudaSuccess) { rc = -2; goto cleanup; }
    if (cudaMemcpy(outX, d_outX, sz, cudaMemcpyDeviceToHost) != cudaSuccess) { rc = -3; goto cleanup; }
    if (cudaMemcpy(outY, d_outY, sz, cudaMemcpyDeviceToHost) != cudaSuccess) { rc = -3; goto cleanup; }

cleanup:
    if (d_outY) cudaFree(d_outY);
    if (d_outX) cudaFree(d_outX);
    if (d_py)   cudaFree(d_py);
    if (d_px)   cudaFree(d_px);
    return rc;
}

/* ======================================================================
 * Kernel: batch minimum distance from query points to polygon edges
 *
 * Each thread handles one query point and iterates over all polygon
 * edges, computing the shortest distance to any edge of the polygon.
 * ====================================================================== */

__device__ static double device_point_to_segment_dist(
    double testX, double testY,
    double ax, double ay,
    double bx, double by)
{
    double dx = bx - ax;
    double dy = by - ay;
    double lenSq = dx * dx + dy * dy;

    if (lenSq < 1e-14)
    {
        /* Degenerate edge: both endpoints are the same */
        double ex = testX - ax;
        double ey = testY - ay;
        return sqrt(ex * ex + ey * ey);
    }

    double t = ((testX - ax) * dx + (testY - ay) * dy) / lenSq;
    t = t < 0.0 ? 0.0 : (t > 1.0 ? 1.0 : t);

    double cx = ax + t * dx - testX;
    double cy = ay + t * dy - testY;
    return sqrt(cx * cx + cy * cy);
}

__global__ void kernel_batch_min_dist_to_polygon_edge(
    const double* __restrict__ polyX,
    const double* __restrict__ polyY,
    int vertexCount,
    const double* __restrict__ px,
    const double* __restrict__ py,
    int pointCount,
    double* __restrict__ outDist)
{
    int idx = blockIdx.x * blockDim.x + threadIdx.x;
    if (idx >= pointCount) return;

    double testX  = px[idx];
    double testY  = py[idx];
    double minDist = 1e308; /* effectively DBL_MAX */

    for (int i = 0; i < vertexCount; i++)
    {
        int next = (i + 1) % vertexCount;
        double d = device_point_to_segment_dist(
            testX, testY,
            polyX[i], polyY[i],
            polyX[next], polyY[next]);
        if (d < minDist) minDist = d;
    }

    outDist[idx] = minDist;
}

extern "C" int tactile_batch_min_dist_to_polygon_edge(
    const double* polyX, const double* polyY, int vertexCount,
    const double* px, const double* py, int pointCount,
    double* outDist)
{
    if (pointCount <= 0) return 0;

    int rc = 0;
    double *d_polyX = NULL, *d_polyY = NULL, *d_px = NULL, *d_py = NULL, *d_out = NULL;
    size_t sizePoly = vertexCount * sizeof(double);
    size_t sizeP    = pointCount  * sizeof(double);

    if (cudaMalloc(&d_polyX, sizePoly) != cudaSuccess) { rc = -1; goto cleanup; }
    if (cudaMalloc(&d_polyY, sizePoly) != cudaSuccess) { rc = -1; goto cleanup; }
    if (cudaMalloc(&d_px,    sizeP)    != cudaSuccess) { rc = -1; goto cleanup; }
    if (cudaMalloc(&d_py,    sizeP)    != cudaSuccess) { rc = -1; goto cleanup; }
    if (cudaMalloc(&d_out,   sizeP)    != cudaSuccess) { rc = -1; goto cleanup; }

    if (cudaMemcpy(d_polyX, polyX, sizePoly, cudaMemcpyHostToDevice) != cudaSuccess) { rc = -3; goto cleanup; }
    if (cudaMemcpy(d_polyY, polyY, sizePoly, cudaMemcpyHostToDevice) != cudaSuccess) { rc = -3; goto cleanup; }
    if (cudaMemcpy(d_px, px, sizeP, cudaMemcpyHostToDevice) != cudaSuccess) { rc = -3; goto cleanup; }
    if (cudaMemcpy(d_py, py, sizeP, cudaMemcpyHostToDevice) != cudaSuccess) { rc = -3; goto cleanup; }

    {
        int blockSize = 256;
        int gridSize = (pointCount + blockSize - 1) / blockSize;
        kernel_batch_min_dist_to_polygon_edge<<<gridSize, blockSize>>>(
            d_polyX, d_polyY, vertexCount, d_px, d_py, pointCount, d_out);
    }

    if (cudaDeviceSynchronize() != cudaSuccess) { rc = -2; goto cleanup; }
    if (cudaMemcpy(outDist, d_out, sizeP, cudaMemcpyDeviceToHost) != cudaSuccess) { rc = -3; goto cleanup; }

cleanup:
    if (d_out)   cudaFree(d_out);
    if (d_py)    cudaFree(d_py);
    if (d_px)    cudaFree(d_px);
    if (d_polyY) cudaFree(d_polyY);
    if (d_polyX) cudaFree(d_polyX);
    return rc;
}
