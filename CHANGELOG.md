# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-05-04

### Added

- **Geometry** module: `Vector2`, `Transform2D`, `Polygon`, `Point`, `GeometryUtils`, `Constants` — foundational 2D primitives and algorithms (distances, intersections, centroids, bounding boxes, winding-number containment).
- **Tiling** module: `IsohedralTiling` with `SetType`, `SetParameters`, `GetTileShape`, `FillRegionBounds`, `ShapeEdges`, `ShapeEdgeParts`, `GetColour`, `CreateGraph`, and `AllTypes` registry; `SymmetryGroup`, `EdgeShape` enum; full support for all 81 isohedral tiling types.
- **Tiling/Analysis** module: `TilingGraph` with `FindCell`, `FindNearestCell`, `GetNeighbours`, `GetAdjacencies`, `GetCentroidDistance`, `GetHopDistance`, `GetShortestPath`, `GetCellsWithinSteps`, `GetConnectedComponents`, `GetDegree`, `GetDegreeDistribution`; `TilingCell`, `TilingAdjacency`, `TilingRegion`.
- **Diagnostics** module: `TactileLogger` (opt-in `ILoggerFactory` facade, silent by default); `PerformanceMonitor` with per-operation timing metrics (count, min, max, average).
- **Gpu** module: `GpuAccelerator` with `BatchDistanceSquared` and `BatchPointInPolygon` (CUDA GPU path with transparent CPU fallback); `CudaInterop` P/Invoke bindings.
- `IsohedralTiling.AllTypes` — static read-only list of all valid isohedral type identifiers.
- NuGet package metadata: `PackageId`, `Version`, `PackageDescription`, `PackageReadmeFile`, `PackageLicenseExpression` (BSD-3-Clause).
- `Directory.Build.props` with shared metadata for all projects.
- CI workflow (`.github/workflows/ci.yml`): build + test + code-coverage on every push and pull request targeting `main`.
- Release workflow (`.github/workflows/release.yml`): automated NuGet pack and publish on `v*.*.*` tags.
- Dependabot configuration for weekly updates of GitHub Actions and NuGet packages.
- Reusable installer workflow (`.github/workflows/install-tactile-cs.yml`) and companion `scripts/Install-TactileCs.ps1`.
- `docs/installation.md` — explains both GitHub Actions and PowerShell installation methods.
- `CONTRIBUTING.md` — prerequisites, build steps, test instructions, branch naming, commit style.
- `SECURITY.md` — responsible-disclosure policy.
- DocFX site configuration (`docfx.json`, `docs/index.md`, `docs/toc.yml`).
- Comprehensive xUnit tests for all modules.
- `TactileCs.Benchmarks` project with BenchmarkDotNet benchmarks for `FillRegionBounds`, `CreateGraph`, and `BatchDistanceSquared`.

### Known limitations

- Only `IsohedralTiling` type 1 has a fully implemented `ComputeVertices` stub; the coefficient-based vertex computation for all 81 types is in progress.
- `FillRegionBounds` uses a conservative heuristic lattice sweep rather than the fully-precise `FillAlgorithm` from the original C++ Tactile library.

[Unreleased]: https://github.com/graph-technologies/tactile-cs/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/graph-technologies/tactile-cs/releases/tag/v1.0.0
