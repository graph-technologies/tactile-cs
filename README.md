# TactileCs

C# / .NET 8 port of the core isohedral tiling geometry from Craig S. Kaplan’s
[Tactile](https://github.com/isohedral/tactile) library.

This project focuses on the **geometry, tiling, and tiling-analysis engine**, not rendering.

## Repository structure

| Folder | Primary contents | Inputs | Outputs |
| --- | --- | --- | --- |
| `Geometry/` | Foundational 2D primitives and helper algorithms | Points, vectors, transforms, polygon vertex lists | Distances, intersections, centroids, bounds, transformed geometry |
| `Tiling/` | Isohedral tiling engine and symmetry definitions | Tiling types, parameters, sample bounds | Prototiles, tile transforms, colours, graph views |
| `Tiling/Analysis/` | Graph-oriented tile analysis | Sample regions and generated cells | Neighbors, hop distance, shortest paths, components, degrees |

Detailed folder-level documentation:

- [`Geometry/README.md`](Geometry/README.md)
- [`Tiling/README.md`](Tiling/README.md)
- [`Tiling/Analysis/README.md`](Tiling/Analysis/README.md)

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

## Example

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

## Status

- Table-driven tiling wrapper with a built-in Type 1 sample definition.
- Geometry primitives are implemented and documented.
- A graph-analysis layer is available for sampled tile neighborhoods, distances, paths, and connectivity analysis.

## Build

```bash
dotnet build /home/runner/work/tactile-cs/tactile-cs/TactileCs.csproj
dotnet test /home/runner/work/tactile-cs/tactile-cs/tactile-cs.slnx
```

## License

This is a derivative work of:

> Tactile – Isohedral tilings and decorated tilings  
> Copyright (c) 2018, Craig S. Kaplan

used under the **BSD 3-Clause License**. See `LICENSE` for full terms.
