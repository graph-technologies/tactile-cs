# Tiling Analysis Module

The `Tiling/Analysis/` folder adds a graph-oriented query layer on top of `IsohedralTiling`.

## Types

| Type | Purpose | Inputs | Outputs |
| --- | --- | --- | --- |
| `TilingRegion` | Describes the sampled axis-aligned region | `xMin`, `yMin`, `xMax`, `yMax` | Bounds checks and region intersection helpers |
| `TilingCell` | Represents one placed tile in the sampled graph | Tile id, transform, polygon, centroid, lattice metadata | Stable node for graph queries |
| `TilingAdjacency` | Represents an undirected connection between two cells | Source/target cells, shared-vertex info | Edge metadata including centroid distance and edge/corner contact |
| `TilingGraph` | Builds and queries the graph | `IsohedralTiling`, `TilingRegion` | Cells, adjacency lists, path and component analysis |

## Query table

| API | Inputs | Outputs |
| --- | --- | --- |
| `new TilingGraph(tiling, region)` | Source tiling and sample region | Fully sampled graph of placed tiles |
| `FindCell(cellId)` | Graph-local cell id | Matching `TilingCell` or `null` |
| `FindNearestCell(point)` | Query `Vector2` | Closest sampled cell by centroid |
| `GetNeighbours(cell, includeCornerNeighbours)` | Source cell and adjacency mode | Neighboring cells |
| `GetAdjacencies(cell, includeCornerNeighbours)` | Source cell and adjacency mode | Rich adjacency records |
| `GetCentroidDistance(first, second)` | Two cells | Euclidean centroid distance |
| `GetHopDistance(start, target, includeCornerNeighbours)` | Two cells and adjacency mode | Shortest graph distance in hops, or `null` if disconnected |
| `GetShortestPath(start, target, includeCornerNeighbours)` | Two cells and adjacency mode | Ordered list of cells along the shortest route |
| `GetCellsWithinSteps(origin, steps, includeCornerNeighbours)` | Origin cell and hop limit | Reachable cells within the hop budget |
| `GetConnectedComponents(includeCornerNeighbours)` | Adjacency mode | Connected-component partition of the graph |
| `GetConnectedComponentCount(includeCornerNeighbours)` | Adjacency mode | Number of connected components |
| `GetDegree(cell, includeCornerNeighbours)` | Cell and adjacency mode | Number of adjacent cells |
| `GetDegreeDistribution(includeCornerNeighbours)` | Adjacency mode | Degree histogram across sampled cells |

## Relationship model

- Cells are sampled from `IsohedralTiling.FillRegionBounds(...)`.
- Each placed cell stores its transformed polygon and centroid.
- Two cells are treated as adjacent when their polygons share vertices within the repository geometry tolerance.
- `SharesEdge = true` means at least two coincident vertices were found.
- `includeCornerNeighbours = true` promotes corner-only contacts into graph edges for looser connectivity analysis.
