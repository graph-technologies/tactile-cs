# TactileCs

**TactileCs** is a C# / .NET 8 port of Craig S. Kaplan's [Tactile](https://github.com/isohedral/tactile) isohedral tiling geometry library.

## What is an isohedral tiling?

An *isohedral tiling* is a tessellation of the plane by a single prototile shape, where
every tile can be mapped onto every other tile by a symmetry of the tiling. There are
exactly 81 distinct combinatorial types of isohedral tilings, catalogued by Heesch and
Kienzle (1963).

## Getting started

```csharp
using TactileCs.Tiling;

// Create a tiling of type 1
var tiling = new IsohedralTiling(1);

// Get the prototile polygon
var prototile = tiling.GetTileShape();

// Fill a 4×4 region with tiles
foreach (var (transform, t1, t2, aspect) in tiling.FillRegionBounds(-2, -2, 2, 2))
{
    var placed = prototile.Transform(transform);
    // draw placed polygon...
}
```

See the [Installation](installation.md) page to add TactileCs to your project.

## Modules

| Module | Description |
|---|---|
| [Geometry](../api/TactileCs.Geometry.yml) | 2D geometry primitives: `Vector2`, `Transform2D`, `Polygon`, `GeometryUtils` |
| [Tiling](../api/TactileCs.Tiling.yml) | `IsohedralTiling` engine for all 81 isohedral types |
| [Tiling.Analysis](../api/TactileCs.Tiling.Analysis.yml) | `TilingGraph`: neighbours, paths, components, distance |
| [Diagnostics](../api/TactileCs.Diagnostics.yml) | Structured logging and performance monitoring |
| [Gpu](../api/TactileCs.Gpu.yml) | Optional CUDA GPU acceleration with CPU fallback |
