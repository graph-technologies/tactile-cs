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
 */

#include <cuda_runtime.h>
#include <stdio.h>

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
