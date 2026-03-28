# GPU Acceleration (CUDA)

Optional GPU-accelerated geometry operations for TactileCs via CUDA + P/Invoke.

## Architecture

```
C# application
  │
  ▼
GpuAccelerator          (managed; auto-detects CUDA)
  │
  ├─► CPU fallback      (pure C#, always available)
  │
  └─► CudaInterop       (P/Invoke to native library)
        │
        ▼
  libtactile_cuda.so    (native CUDA library)
        │
        ▼
  batch_geometry.cu     (CUDA kernels)
```

## Accelerated Operations

| Operation | Kernel | Description |
|-----------|--------|-------------|
| `BatchDistanceSquared` | `kernel_batch_distance_sq` | Pairwise squared-distance matrix for two point sets |
| `BatchPointInPolygon` | `kernel_point_in_polygon` | Winding-number containment test for many points against one polygon |

## Building the Native Library

### Prerequisites

* NVIDIA GPU with Compute Capability ≥ 3.5
* [CUDA Toolkit](https://developer.nvidia.com/cuda-toolkit) ≥ 11.0
* `nvcc` on `PATH`

### Linux

```bash
cd Gpu/Kernels
nvcc -shared -o libtactile_cuda.so batch_geometry.cu -Xcompiler -fPIC
```

Place `libtactile_cuda.so` where the .NET runtime can find it (e.g. next to
the application binary, or in `/usr/local/lib`).

### Windows

```cmd
cd Gpu\Kernels
nvcc -shared -o tactile_cuda.dll batch_geometry.cu
```

Place `tactile_cuda.dll` next to the application executable.

## Usage

```csharp
using TactileCs.Gpu;
using TactileCs.Geometry;

// Availability check (optional – GpuAccelerator falls back automatically)
Console.WriteLine($"CUDA available: {GpuAccelerator.IsCudaAvailable}");

var accel = new GpuAccelerator();

// Batch distance
Vector2[] setA = [ new(0, 0), new(1, 1) ];
Vector2[] setB = [ new(2, 2), new(3, 3) ];
double[] distances = accel.BatchDistanceSquared(setA, setB);

// Batch point-in-polygon
var polygon = new Polygon(new Vector2[]
{
    new(0, 0), new(10, 0), new(10, 10), new(0, 10)
});
Vector2[] points = [ new(5, 5), new(20, 20) ];
bool[] inside = accel.BatchPointInPolygon(polygon, points);
// inside[0] == true, inside[1] == false
```

## CPU Fallback

When the native library is absent or CUDA initialisation fails, every
`GpuAccelerator` method silently runs an equivalent CPU implementation.
No exceptions are thrown and no special handling is required.
