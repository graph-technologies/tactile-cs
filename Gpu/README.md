# GPU Acceleration (CUDA)

Optional GPU-accelerated geometry operations for TactileCs via CUDA + P/Invoke.
When a compatible NVIDIA GPU is present, batch geometry operations run on the
GPU and return results to .NET.  If no GPU is found, every operation falls back
transparently to a pure-C# CPU implementation – no exceptions, no configuration
required.

---

## Architecture

```
C# application
  │
  ▼
GpuAccelerator          (managed; auto-detects CUDA, exposes all public API)
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

---

## Accelerated Operations

| Method | Kernel | Description |
|--------|--------|-------------|
| `BatchDistanceSquared` | `kernel_batch_distance_sq` | Pairwise squared-distance matrix for two point sets |
| `BatchDistance` | `kernel_batch_distance` | Pairwise Euclidean distance matrix for two point sets |
| `BatchPointInPolygon` | `kernel_point_in_polygon` | Winding-number containment test for many points vs. one polygon |
| `BatchTransformPoints` | `kernel_batch_transform_points` | Apply a 2D affine transform to a batch of points |
| `BatchMinDistanceToPolygonEdge` | `kernel_batch_min_dist_to_polygon_edge` | Minimum Euclidean distance from each query point to the nearest polygon edge |

All methods accept `ReadOnlySpan<Vector2>` inputs and return managed arrays.

---

## Hardware Requirements

| Requirement | Minimum |
|-------------|---------|
| GPU Compute Capability | **3.5** (Kepler GK110, e.g. GTX 780 / Tesla K40) |
| CUDA Toolkit (build) | **11.0** |
| CUDA Driver (runtime) | **450.x** (Linux) / **451.x** (Windows) |

### Checking your GPU

```bash
# Linux / Windows (PowerShell)
nvidia-smi
```

Sample output:
```
+-----------------------------------------------------------------------------+
| NVIDIA-SMI 545.23.08    Driver Version: 545.23.08    CUDA Version: 12.3   |
+-----------------------------------------------------------------------------+
| GPU  Name                 Persistence-M | Bus-Id ...  Disp.A | Volatile Uncorr. ECC |
|   0  NVIDIA GeForce RTX 3080        Off | 00000000:09:00.0 Off |  N/A                |
+-----------------------------------------------------------------------------+
```

The **Compute Capability** is not shown directly by `nvidia-smi`; check it at
<https://developer.nvidia.com/cuda-gpus> or via `deviceQuery` (CUDA samples).

---

## Building the Native Library

### Prerequisites

1. Install the [CUDA Toolkit](https://developer.nvidia.com/cuda-toolkit) ≥ 11.0
   and ensure `nvcc` is on your `PATH`.

   ```bash
   nvcc --version   # should print "release 11.x" or later
   ```

2. Verify your driver is compatible:

   ```bash
   nvidia-smi       # shows driver version and max CUDA runtime
   ```

### Linux

```bash
cd Gpu/Kernels
nvcc -shared -o libtactile_cuda.so batch_geometry.cu -Xcompiler -fPIC
# Copy to application output directory or /usr/local/lib
cp libtactile_cuda.so /usr/local/lib/
ldconfig
```

Or place the `.so` next to the application binary.

### Windows

```cmd
cd Gpu\Kernels
nvcc -shared -o tactile_cuda.dll batch_geometry.cu
```

Place `tactile_cuda.dll` next to the application executable.

---

## Hardware & Driver Detection

Use `GpuAccelerator.GetAllDeviceInfos()` to enumerate all CUDA-capable devices
and check whether they meet TactileCs requirements before starting a long job.

```csharp
using TactileCs.Gpu;

// Check at application start-up
var devices = GpuAccelerator.GetAllDeviceInfos();

if (devices.Count == 0)
{
    Console.WriteLine("No CUDA GPU found – running on CPU.");
}
else
{
    foreach (var d in devices)
        Console.WriteLine(d.ToDiagnosticString());
}

// Or inspect a single device
if (GpuAccelerator.TryGetDeviceInfo(0, out var info) && info is not null)
{
    if (!info.MeetsMinimumRequirements)
        Console.WriteLine($"WARNING: {info.Name} (CC {info.ComputeCapability}) " +
                          "is below the minimum CC 3.5 for TactileCs GPU operations.");

    if (!info.DriverMeetsMinimumRequirements)
        Console.WriteLine($"WARNING: driver {info.DriverVersionString} is too old. " +
                          "Please update your NVIDIA driver.");
}
```

`GpuDeviceInfo` exposes:

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | GPU marketing name |
| `ComputeCapability` | `string` | e.g. `"8.6"` |
| `ComputeCapabilityMajor/Minor` | `int` | Separate components |
| `TotalMemoryBytes` | `long` | Total global memory in bytes |
| `TotalMemoryMiB` | `double` | Total global memory in MiB |
| `MultiprocessorCount` | `int` | Number of streaming multiprocessors |
| `DriverVersion` | `int` | Encoded driver version (`major*1000 + minor*10`) |
| `DriverVersionString` | `string` | Human-readable, e.g. `"12.4"` |
| `MeetsMinimumRequirements` | `bool` | CC ≥ 3.5 |
| `DriverMeetsMinimumRequirements` | `bool` | Driver meets minimum version |

### Structured logging of diagnostics

```csharp
using Microsoft.Extensions.Logging;
using TactileCs.Diagnostics;
using TactileCs.Gpu;

using var factory = LoggerFactory.Create(b => b.AddConsole());
TactileLogger.Configure(factory);

var accel = new GpuAccelerator();
accel.LogDeviceDiagnostics();   // emits INFO or WARNING for each device
```

---

## Usage

```csharp
using TactileCs.Gpu;
using TactileCs.Geometry;

// --- Availability check (optional) -------------------------------------------
Console.WriteLine($"CUDA available: {GpuAccelerator.IsCudaAvailable}");

var accel = new GpuAccelerator();

// --- Batch squared-distance matrix -------------------------------------------
Vector2[] setA = [ new(0, 0), new(1, 1) ];
Vector2[] setB = [ new(2, 2), new(3, 3) ];
double[] distSq = accel.BatchDistanceSquared(setA, setB);

// --- Batch Euclidean distance matrix -----------------------------------------
double[] dist = accel.BatchDistance(setA, setB);

// --- Batch point-in-polygon --------------------------------------------------
var polygon = new Polygon(new Vector2[]
{
    new(0, 0), new(10, 0), new(10, 10), new(0, 10)
});
Vector2[] points = [ new(5, 5), new(20, 20) ];
bool[] inside = accel.BatchPointInPolygon(polygon, points);
// inside[0] == true, inside[1] == false

// --- Batch affine transform --------------------------------------------------
var rotation = Transform2D.CreateRotation(Math.PI / 4);
Vector2[] rotated = accel.BatchTransformPoints(rotation, points);

// --- Batch minimum distance to polygon edge ----------------------------------
double[] edgeDist = accel.BatchMinDistanceToPolygonEdge(polygon, points);
// edgeDist[0] = 5.0  (centre of square, equidistant from all edges)
// edgeDist[1] = 10.0 (outside corner)
```

---

## CPU Fallback

When the native library is absent, CUDA initialisation fails, or a kernel
returns a non-zero error code, every `GpuAccelerator` method silently runs the
equivalent CPU implementation.  No exceptions are thrown and no special handling
is required.

The fallback is also always used on the CI/CD pipeline, where no GPU is present,
ensuring the managed code paths are always tested.

---

## CUDA Return Codes

The native C entry points return:

| Code | Meaning |
|------|---------|
| `0` | Success |
| `-1` | `cudaMalloc` failure or invalid device index |
| `-2` | Kernel execution or `cudaDeviceSynchronize` failure |
| `-3` | `cudaMemcpy` failure |

A non-zero code causes the managed layer to fall back to CPU and emit a
`LogWarning` via `TactileLogger`.
