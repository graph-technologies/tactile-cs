# Geometry Module

The `Geometry/` folder contains the low-level, reusable 2D primitives that power tiling construction and analysis.

## Files and APIs

| File | Main types | Inputs | Outputs | Notes |
| --- | --- | --- | --- | --- |
| `Constants.cs` | `Constants` | Runtime tolerance values | Shared epsilon settings | Centralizes numeric tolerances used across geometry and tiling code. |
| `Vector2.cs` | `Vector2` | Scalar coordinates, vector operands | Distances, angles, transformed vectors | Immutable 2D vector type with arithmetic, comparisons, and utility methods. |
| `Point.cs` | `Point` | Mutable point coordinates, vectors | Distance checks, translated points | Convenience class for point-centric APIs and segment tests. |
| `Transform2D.cs` | `Transform2D` | Matrix components, points, vectors | Affine transforms, transformed points | Encodes 2D affine transforms for tile placement and edge mapping. |
| `Polygon.cs` | `Polygon` | Ordered vertex sequences | Area, centroid, bounds, containment, transformed polygons | Main polygon container used for prototiles and placed tiles. |
| `GeometryUtils.cs` | `GeometryUtils` | Segments, lines, points, triangles | Intersections, projections, distances | Static helper algorithms for geometric analysis. |

## Key parameter / result reference

| Type or method | Important inputs | Return values / outputs |
| --- | --- | --- |
| `Vector2(double x, double y)` | Raw `x`, `y` coordinates | Immutable vector instance |
| `Transform2D.Apply(Vector2 p)` | Local-space point `p` | World-space transformed point |
| `Polygon(IEnumerable<Vector2> vertices)` | Vertex list in winding order | Polygon copy with independent storage |
| `Polygon.Centroid()` | Polygon vertices | Geometric center of the polygon |
| `Polygon.BoundingBox()` | Polygon vertices | `(minX, minY, maxX, maxY)` tuple |
| `Polygon.Contains(Vector2 point)` | Query point | `true` if the point is inside or on the boundary |
| `GeometryUtils.SegmentIntersection(...)` | Two segments | Boolean hit flag and intersection point |

## How this folder is used

- `Tiling/IsohedralTiling.cs` uses `Vector2`, `Transform2D`, and `Polygon` to define a prototile and place copies of it across the plane.
- `Tiling/Analysis/` uses the same geometry primitives to derive adjacency, centroid distances, and region filtering for sampled tile graphs.
