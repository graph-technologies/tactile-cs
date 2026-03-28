# Tiling Module

The `Tiling/` folder contains the isohedral tiling engine and the higher-level graph-analysis layer built on top of it.

## Files and APIs

| File or folder | Main types | Inputs | Outputs | Notes |
| --- | --- | --- | --- | --- |
| `IsohedralTiling.cs` | `IsohedralTiling` | Tiling type id, parameter vectors, sampling bounds | Prototile polygons, placed-tile transforms, colour indices, graph views | Table-driven wrapper over isohedral tiling metadata. |
| `Symmetry.cs` | `SymmetryGroup`, `EdgeShape` | Lattice vectors, rotation count, region bounds | Tiling transforms, edge symmetry categories | Generates approximate wallpaper lattice placements. |
| `Analysis/` | `TilingRegion`, `TilingCell`, `TilingAdjacency`, `TilingGraph` | Sampled bounds and cells | Adjacency graphs, paths, components, degrees, nearest-cell queries | Query/analysis layer for graph-style reasoning over tiles. |

## Engine input / output reference

| API | Inputs | Outputs |
| --- | --- | --- |
| `new IsohedralTiling(int typeId)` | Built-in tiling type id | Configured tiling instance |
| `SetParameters(ReadOnlySpan<double> values)` | Per-type parameter vector | Recomputed prototile geometry |
| `GetTileShape()` | Current tiling state | Local-space `Polygon` prototile |
| `FillRegionBounds(xMin, yMin, xMax, yMax)` | Axis-aligned sample bounds | Sequence of tile placement transforms with lattice metadata |
| `GetColour(t1, t2, aspect)` | Lattice coordinates and aspect | Small integer colour index |
| `CreateGraph(xMin, yMin, xMax, yMax)` | Axis-aligned sample bounds | `TilingGraph` for neighborhood and path analysis |

## Analysis capabilities

The analysis layer can be used to:

- enumerate placed cells in a bounded region,
- retrieve edge-sharing neighbors or corner-touching neighbors,
- compute centroid distance and hop distance between cells,
- find shortest paths and connected components,
- inspect graph degree / degree distributions,
- locate the sampled cell nearest to a query point.

For API details specific to the graph-analysis types, see `Tiling/Analysis/README.md`.
