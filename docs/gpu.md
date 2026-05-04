# GPU Acceleration Guide

TactileCs ships optional CUDA GPU acceleration for batch geometry operations.
This guide covers installation, hardware requirements, driver setup, and usage.

---

## Overview

The GPU subsystem lives in the `TactileCs.Gpu` namespace and is built around
three layers:

| Layer | Role |
|-------|------|
| `GpuAccelerator` | Public C# API – dispatches to GPU or CPU fallback |
| `CudaInterop` | P/Invoke bindings to the native CUDA library |
| `batch_geometry.cu` | CUDA kernels compiled into `libtactile_cuda` |

All GPU operations have CPU fallback implementations. When the native library
is absent or a GPU is not found, every method runs the CPU path without
throwing an exception.

---

## Supported Hardware

| Requirement | Minimum |
|-------------|---------|
| GPU architecture | Kepler (GK110) or newer |
| Compute Capability | **≥ 3.5** |
| CUDA Toolkit | **≥ 11.0** (build only) |
| CUDA Driver | **≥ 450.x** (Linux) / **≥ 451.x** (Windows) |

Well-known GPU families that meet these requirements:

| GPU Family | CC | Notes |
|------------|----|-------|
| GTX 780 / Tesla K40 (Kepler) | 3.5 | Minimum supported |
| GTX 980 / Tesla M40 (Maxwell) | 5.2 | Recommended minimum |
| GTX 1080 / Tesla P100 (Pascal) | 6.1 / 6.0 | Good performance |
| RTX 2080 (Turing) | 7.5 | Recommended |
| RTX 3080 / A100 (Ampere) | 8.6 / 8.0 | High-end |
| RTX 4090 (Ada Lovelace) | 8.9 | Latest generation |

---

## Installation

### Step 1 – Install the CUDA Toolkit

Download and install from:
<https://developer.nvidia.com/cuda-toolkit>

Verify the installation:

```bash
nvcc --version
# nvcc: NVIDIA (R) Cuda compiler driver
# Cuda compilation tools, release 12.3, ...
```

### Step 2 – Verify your driver

```bash
nvidia-smi
# +-----------------------------------------------------------------------------+
# | NVIDIA-SMI 545.23    Driver Version: 545.23    CUDA Version: 12.3           |
```

If `nvidia-smi` is not found, install the NVIDIA driver from:
<https://www.nvidia.com/Download/index.aspx>

### Step 3 – Build the native library

**Linux:**

```bash
cd Gpu/Kernels
nvcc -shared -o libtactile_cuda.so batch_geometry.cu -Xcompiler -fPIC
```

Then place `libtactile_cuda.so` where the .NET runtime can find it:

```bash
# Option A: copy next to your application binary
cp libtactile_cuda.so /path/to/your/app/

# Option B: install system-wide
sudo cp libtactile_cuda.so /usr/local/lib/
sudo ldconfig
```

**Windows:**

```cmd
cd Gpu\Kernels
nvcc -shared -o tactile_cuda.dll batch_geometry.cu
```

Copy `tactile_cuda.dll` next to the application executable.

---

## Checking Hardware at Runtime

Use `GpuAccelerator.GetAllDeviceInfos()` to inspect all CUDA devices:

```csharp
using TactileCs.Gpu;

var devices = GpuAccelerator.GetAllDeviceInfos();

if (devices.Count == 0)
{
    Console.WriteLine("No CUDA-capable GPU detected.");
    Console.WriteLine("All operations will run on the CPU.");
}

foreach (var d in devices)
{
    Console.WriteLine(d.ToDiagnosticString());
    // Example output:
    // [0] NVIDIA GeForce RTX 3080 | CC 8.6 | 10240 MiB | 68 SMs | Driver 12.4
    //   Compute Capability : 8.6 (OK – requires >= 3.5)
    //   Driver Version     : 12.4 (OK – requires >= 11.4)
    //   Total Memory       : 10240 MiB
}
```

`GpuDeviceInfo` provides individual checks:

```csharp
if (GpuAccelerator.TryGetDeviceInfo(0, out var info) && info is not null)
{
    if (!info.MeetsMinimumRequirements)
        Console.Error.WriteLine(
            $"GPU {info.Name} (CC {info.ComputeCapability}) is below the minimum CC 3.5.");

    if (!info.DriverMeetsMinimumRequirements)
        Console.Error.WriteLine(
            $"Driver {info.DriverVersionString} is too old. Please update.");
}
```

### Logging diagnostics

Wire in structured logging and call `LogDeviceDiagnostics()`:

```csharp
using Microsoft.Extensions.Logging;
using TactileCs.Diagnostics;
using TactileCs.Gpu;

using var factory = LoggerFactory.Create(b => b.AddConsole());
TactileLogger.Configure(factory);

var accel = new GpuAccelerator();
accel.LogDeviceDiagnostics();
// INFO  GpuAccelerator: GPU [0] NVIDIA RTX 3080 | CC 8.6 | 10240 MiB | ...
```

---

## Available GPU Operations

### `BatchDistanceSquared`

Computes the **pairwise squared Euclidean distance** between two sets of 2D
points.  Returns a flat `double[]` of length `setA.Length × setB.Length` in
row-major order (index `i * setB.Length + j` is the distance² from `setA[i]`
to `setB[j]`).

```csharp
var result = accel.BatchDistanceSquared(setA, setB);
```

### `BatchDistance`

Same layout as `BatchDistanceSquared` but returns actual Euclidean distances
(i.e., the square root of the squared distance).

```csharp
var result = accel.BatchDistance(setA, setB);
```

### `BatchPointInPolygon`

Tests each query point against a single polygon using the **winding-number
algorithm**.  Returns a `bool[]` with one entry per query point.

```csharp
var polygon = new Polygon(new Vector2[] { ... });
bool[] inside = accel.BatchPointInPolygon(polygon, queryPoints);
```

### `BatchTransformPoints`

Applies a 2D affine `Transform2D` to every point in the input span.  Returns
a `Vector2[]` of transformed points.

```csharp
var t = Transform2D.CreateRotation(Math.PI / 6);
Vector2[] rotated = accel.BatchTransformPoints(t, points);
```

### `BatchMinDistanceToPolygonEdge`

For each query point, returns the minimum Euclidean distance to the nearest
**edge** of the supplied polygon (not to its interior).  Useful for collision
proximity queries and nearest-edge lookups.

```csharp
double[] edgeDist = accel.BatchMinDistanceToPolygonEdge(polygon, queryPoints);
```

---

## CLI Diagnostics with ConsoleReporter

The `TactileCs.Diagnostics.ConsoleReporter` class provides reusable helpers
for displaying GPU diagnostics and performance metrics in CLI applications:

```csharp
using TactileCs.Diagnostics;
using TactileCs.Gpu;

// Print a section heading
ConsoleReporter.WriteSection("GPU Diagnostics");

// Print each device
foreach (var d in GpuAccelerator.GetAllDeviceInfos())
    ConsoleReporter.WriteStatus(d.Name, d.ComputeCapability);

// Show a progress bar during batch work
using var bar = ConsoleReporter.CreateProgressBar("Processing tiles", total: 1000);
for (int i = 0; i < 1000; i++)
{
    // ... do work ...
    bar.Report(i + 1);
}

// Print performance metrics table
ConsoleReporter.WriteSection("Performance Summary");
ConsoleReporter.WriteMetricsTable(PerformanceMonitor.Default.GetAllMetrics());
```

---

## CPU Fallback Details

The CPU fallback paths are pure C# and are always active:

| GPU method | CPU fallback |
|------------|-------------|
| `BatchDistanceSquared` | Nested loop over `Vector2.DistanceSquared` |
| `BatchDistance` | Nested loop over `Vector2.Distance` |
| `BatchPointInPolygon` | `Polygon.Contains` per point |
| `BatchTransformPoints` | `Transform2D.Apply` per point |
| `BatchMinDistanceToPolygonEdge` | `GeometryUtils.DistanceToSegment` per edge per point |

The CPU paths are always exercised in CI (where no GPU is available), ensuring
correctness of the managed code paths at every build.

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| `GpuAccelerator.IsCudaAvailable` is `false` | Native library not found or no GPU | Place `libtactile_cuda.so` / `.dll` next to the binary |
| Operations return CPU results despite GPU present | Kernel failed (check logs) | Look for `LogWarning` in structured logs (rc=-1/-2/-3) |
| `nvcc` not found | CUDA Toolkit not installed or not on PATH | Install CUDA Toolkit ≥ 11.0 |
| Build fails with CC mismatch | Targeting an unsupported compute capability | GPU requires CC ≥ 3.5 |
| Poor performance on small batches | GPU overhead exceeds compute benefit | GPU acceleration is beneficial for batches of ≥ ~10 000 points |
