# Tactile CS

[![CI](https://github.com/graph-technologies/tactile-cs/actions/workflows/ci.yml/badge.svg)](https://github.com/graph-technologies/tactile-cs/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/TactileCs.svg)](https://www.nuget.org/packages/TactileCs)

A C# / .NET 8 port of the core isohedral tiling geometry from Craig S. Kaplan’s [Tactile](https://github.com/isohedral/tactile) library.

This project focuses on the development of core geometry, tiling, and analysis engines, not rendering - although attention is paid to the CUDA-acceleration of queries and operations, with CPU fallback. Now that the core library has been ported over, work has shifted to the expansion of the capabilities of the library as well as the mathematical completeness of the tilings.

## Repository Structure

| Folder | Primary contents | Inputs | Outputs |
| --- | --- | --- | --- |
| `Geometry/` | Foundational 2D primitives and helper algorithms | Points, vectors, transforms, polygon vertex lists | Distances, intersections, centroids, bounds, transformed geometry |
| `Tiling/` | Isohedral tiling engine and symmetry definitions | Tiling types, parameters, sample bounds | Prototiles, tile transforms, colours, graph views |
| `Tiling/Analysis/` | Graph-oriented tile analysis | Sample regions and generated cells | Neighbors, hop distance, shortest paths, components, degrees |
| `Diagnostics/` | Logging facade, performance monitoring, and CLI presentation | `ILoggerFactory`, operation names, metrics collections | Structured logs, timing metrics (count, min, max, avg), formatted console tables and progress bars |
| `Gpu/` | Optional CUDA-accelerated geometry operations | Point sets, polygons, query points | Distance matrices, containment results (with automatic CPU fallback) |

Detailed folder-level documentation:

- [`Geometry/README.md`](Geometry/README.md)
- [`Tiling/README.md`](Tiling/README.md)
- [`Tiling/Analysis/README.md`](Tiling/Analysis/README.md)
- [`Diagnostics/README.md`](Diagnostics/README.md)
- [`Gpu/README.md`](Gpu/README.md)

## Core APIs

| API | Inputs | Outputs |
| --- | --- | --- |
| `new IsohedralTiling(typeId)` | Built-in tiling type id | Configured tiling instance |
| `SetParameters(values)` | Per-type parameter vector | Recomputed prototile geometry |
| `GetTileShape()` | Current tiling state | Local-space `Polygon` |
| `FillRegionBounds(xMin, yMin, xMax, yMax)` | Axis-aligned sample bounds | Tile placement transforms |
| `CreateGraph(xMin, yMin, xMax, yMax)` | Axis-aligned sample bounds | `TilingGraph` for graph queries |
| `TilingGraph.GetNeighbours(cell, includeCornerNeighbours)` | A sampled cell and adjacency mode | Adjacent sampled cells |
| `TilingGraph.GetHopDistance(start, target, includeCornerNeighbours)` | Two sampled cells and adjacency mode | Shortest graph distance in hops |
| `TilingGraph.GetCentroidDistance(first, second)` | Two sampled cells | Euclidean centroid distance |
| `TilingGraph.GetConnectedComponents(includeCornerNeighbours)` | Adjacency mode | Connected components in the sampled graph |
| `TactileLogger.Configure(factory)` | `ILoggerFactory` from your DI container | Enables structured logging throughout the library |
| `PerformanceMonitor.Default.BeginOperation(name)` | Operation name | Disposable timer; metrics via `GetAllMetrics()` |
| `ConsoleReporter.WriteMetricsTable(metrics)` | `IEnumerable<OperationMetrics>` | Formatted metrics table to console (or any `TextWriter`) |
| `ConsoleReporter.CreateProgressBar(label, total)` | Label + total steps | `IProgress<int>` progress bar printed to console |
| `GpuAccelerator.BatchDistanceSquared(setA, setB)` | Two `Vector2[]` sets | Flat distance² matrix (GPU or CPU fallback) |
| `GpuAccelerator.BatchDistance(setA, setB)` | Two `Vector2[]` sets | Flat Euclidean distance matrix (GPU or CPU fallback) |
| `GpuAccelerator.BatchPointInPolygon(polygon, points)` | Polygon + query `Vector2[]` | Boolean containment results (GPU or CPU fallback) |
| `GpuAccelerator.BatchTransformPoints(transform, points)` | `Transform2D` + query `Vector2[]` | Batch-transformed points (GPU or CPU fallback) |
| `GpuAccelerator.BatchMinDistanceToPolygonEdge(polygon, points)` | Polygon + query `Vector2[]` | Per-point minimum distance to any polygon edge |
| `GpuAccelerator.GetAllDeviceInfos()` | – | All CUDA devices with CC, memory, driver info |
| `GpuAccelerator.TryGetDeviceInfo(index, out info)` | Device index | Single-device snapshot; includes min-requirements check |

## Examples

```csharp
using TactileCs.Geometry;
using TactileCs.Tiling;
using TactileCs.Tiling.Analysis;

IsohedralTiling tiling = new(1);
TilingGraph graph = tiling.CreateGraph(-1.0, -1.0, 2.0, 2.0);

TilingCell? origin = graph.FindNearestCell(new Vector2(0.25, 0.25));

if (origin is not null) {
	IEnumerable<TilingCell> edgeNeighbours = graph.GetNeighbours(origin);
	IReadOnlyList<TilingCell> nearbyCells = graph.GetCellsWithinSteps(origin, 2);
}
```

### Logging & Performance Monitoring

```csharp
using Microsoft.Extensions.Logging;
using TactileCs.Diagnostics;

// Wire structured logging (optional – silent by default)
using var factory = LoggerFactory.Create(b => b.AddConsole());
TactileLogger.Configure(factory);

// Library classes now emit structured logs automatically.
// Performance metrics are collected on key operations:
var metrics = PerformanceMonitor.Default.GetAllMetrics();
foreach (var m in metrics)
    Console.WriteLine(m);
```

### GPU-Accelerated Batch Geometry

```csharp
using TactileCs.Gpu;
using TactileCs.Geometry;

// Inspect installed GPUs and check minimum requirements
var devices = GpuAccelerator.GetAllDeviceInfos();
foreach (var d in devices)
    Console.WriteLine(d.ToDiagnosticString());

var accel = new GpuAccelerator();

// Batch squared-distance matrix (uses CUDA when available, CPU otherwise)
Vector2[] setA = [ new(0, 0), new(1, 1) ];
Vector2[] setB = [ new(2, 2), new(3, 3) ];
double[] distSq = accel.BatchDistanceSquared(setA, setB);

// Batch Euclidean distance matrix
double[] dist = accel.BatchDistance(setA, setB);

// Batch point-in-polygon
var polygon = new Polygon([ new Vector2(0, 0), new(10, 0), new(10, 10), new(0, 10) ]);
Vector2[] points = [ new(5, 5), new(20, 20) ];
bool[] inside = accel.BatchPointInPolygon(polygon, points);

// Batch affine transform
var rot = Transform2D.CreateRotation(Math.PI / 4);
Vector2[] rotated = accel.BatchTransformPoints(rot, points);

// Batch minimum distance to polygon edge
double[] edgeDist = accel.BatchMinDistanceToPolygonEdge(polygon, points);
```

### CLI Presentation Helpers

```csharp
using TactileCs.Diagnostics;

// Formatted section heading, status lines, and metrics table
ConsoleReporter.WriteSection("Performance Summary");
ConsoleReporter.WriteStatus("CUDA available", GpuAccelerator.IsCudaAvailable.ToString());
ConsoleReporter.WriteMetricsTable(PerformanceMonitor.Default.GetAllMetrics());

// In-place progress bar
using var bar = ConsoleReporter.CreateProgressBar("Filling region", total: 1000);
for (int i = 0; i < 1000; i++)
{
    // ... do work ...
    bar.Report(i + 1);
}
```

## Status

- All 81 isohedral tiling types supported (`IsohedralTiling.AllTypes`).
- Geometry primitives fully implemented and documented.
- Graph-analysis layer for sampled tile neighborhoods, distances, paths, and connectivity.
- Structured logging via `Microsoft.Extensions.Logging` (opt-in, silent by default).
- Performance monitoring with per-operation timing metrics.
- CLI presentation helpers (`ConsoleReporter`) for metrics tables, progress bars, and status lines.
- Optional CUDA GPU acceleration: squared distance, Euclidean distance, point-in-polygon, batch transform, minimum polygon-edge distance – all with transparent CPU fallback.
- GPU hardware detection and minimum-requirements checks (`GpuDeviceInfo`, `GpuRequirements`).
- NuGet package: `dotnet add package TactileCs`
- CI/CD: automated build, test, and release pipeline via GitHub Actions.

## Build

```bash
dotnet build TactileCs.csproj
dotnet test tactile-cs.slnx
```

## License

This is a derivative work of:

> Tactile – Isohedral tilings and decorated tilings  
> Copyright (c) 2018, Craig S. Kaplan

used under the BSD 3-Clause License. See `LICENSE` for full terms.
