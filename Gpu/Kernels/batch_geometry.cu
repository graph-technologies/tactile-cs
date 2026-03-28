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

    double *d_ax, *d_ay, *d_bx, *d_by, *d_out;
    size_t sizeA = countA * sizeof(double);
    size_t sizeB = countB * sizeof(double);
    size_t sizeO = (size_t)total * sizeof(double);

    if (cudaMalloc(&d_ax, sizeA) != cudaSuccess) return -1;
    if (cudaMalloc(&d_ay, sizeA) != cudaSuccess) { cudaFree(d_ax); return -1; }
    if (cudaMalloc(&d_bx, sizeB) != cudaSuccess) { cudaFree(d_ax); cudaFree(d_ay); return -1; }
    if (cudaMalloc(&d_by, sizeB) != cudaSuccess) { cudaFree(d_ax); cudaFree(d_ay); cudaFree(d_bx); return -1; }
    if (cudaMalloc(&d_out, sizeO) != cudaSuccess) { cudaFree(d_ax); cudaFree(d_ay); cudaFree(d_bx); cudaFree(d_by); return -1; }

    cudaMemcpy(d_ax, ax, sizeA, cudaMemcpyHostToDevice);
    cudaMemcpy(d_ay, ay, sizeA, cudaMemcpyHostToDevice);
    cudaMemcpy(d_bx, bx, sizeB, cudaMemcpyHostToDevice);
    cudaMemcpy(d_by, by, sizeB, cudaMemcpyHostToDevice);

    int blockSize = 256;
    int gridSize = (total + blockSize - 1) / blockSize;
    kernel_batch_distance_sq<<<gridSize, blockSize>>>(d_ax, d_ay, countA, d_bx, d_by, countB, d_out);

    cudaError_t err = cudaDeviceSynchronize();
    if (err != cudaSuccess)
    {
        cudaFree(d_ax); cudaFree(d_ay); cudaFree(d_bx); cudaFree(d_by); cudaFree(d_out);
        return -2;
    }

    cudaMemcpy(outDistSq, d_out, sizeO, cudaMemcpyDeviceToHost);
    cudaFree(d_ax); cudaFree(d_ay); cudaFree(d_bx); cudaFree(d_by); cudaFree(d_out);
    return 0;
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

    double *d_polyX, *d_polyY, *d_px, *d_py;
    int *d_out;
    size_t sizePoly = vertexCount * sizeof(double);
    size_t sizeP    = pointCount * sizeof(double);
    size_t sizeO    = pointCount * sizeof(int);

    if (cudaMalloc(&d_polyX, sizePoly) != cudaSuccess) return -1;
    if (cudaMalloc(&d_polyY, sizePoly) != cudaSuccess) { cudaFree(d_polyX); return -1; }
    if (cudaMalloc(&d_px, sizeP) != cudaSuccess) { cudaFree(d_polyX); cudaFree(d_polyY); return -1; }
    if (cudaMalloc(&d_py, sizeP) != cudaSuccess) { cudaFree(d_polyX); cudaFree(d_polyY); cudaFree(d_px); return -1; }
    if (cudaMalloc(&d_out, sizeO) != cudaSuccess) { cudaFree(d_polyX); cudaFree(d_polyY); cudaFree(d_px); cudaFree(d_py); return -1; }

    cudaMemcpy(d_polyX, polyX, sizePoly, cudaMemcpyHostToDevice);
    cudaMemcpy(d_polyY, polyY, sizePoly, cudaMemcpyHostToDevice);
    cudaMemcpy(d_px, px, sizeP, cudaMemcpyHostToDevice);
    cudaMemcpy(d_py, py, sizeP, cudaMemcpyHostToDevice);

    int blockSize = 256;
    int gridSize = (pointCount + blockSize - 1) / blockSize;
    kernel_point_in_polygon<<<gridSize, blockSize>>>(d_polyX, d_polyY, vertexCount, d_px, d_py, pointCount, d_out);

    cudaError_t err = cudaDeviceSynchronize();
    if (err != cudaSuccess)
    {
        cudaFree(d_polyX); cudaFree(d_polyY); cudaFree(d_px); cudaFree(d_py); cudaFree(d_out);
        return -2;
    }

    cudaMemcpy(outResults, d_out, sizeO, cudaMemcpyDeviceToHost);
    cudaFree(d_polyX); cudaFree(d_polyY); cudaFree(d_px); cudaFree(d_py); cudaFree(d_out);
    return 0;
}
