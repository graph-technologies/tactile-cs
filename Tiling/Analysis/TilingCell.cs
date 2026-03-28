// Port of concepts from Tactile (c) 2018 Craig S. Kaplan, BSD 3-Clause. See LICENSE.

using TactileCs.Geometry;


namespace TactileCs.Tiling.Analysis;


/// <summary>
/// Represents a single placed tile within a sampled tiling graph.
/// </summary>
public sealed class TilingCell {

	/// <summary>
	/// Gets the stable graph-local cell identifier.
	/// </summary>
	public int Id {
		get;
	}

	/// <summary>
	/// Gets the affine transform used to place the prototile in world coordinates.
	/// </summary>
	public Transform2D Transform {
		get;
	}

	/// <summary>
	/// Gets the polygon describing the cell in world coordinates.
	/// </summary>
	public Polygon Shape {
		get;
	}

	/// <summary>
	/// Gets the geometric centroid of the cell.
	/// </summary>
	public Vector2 Center {
		get;
	}

	/// <summary>
	/// Gets the first lattice coordinate reported by the tiling engine.
	/// </summary>
	public int T1 {
		get;
	}

	/// <summary>
	/// Gets the second lattice coordinate reported by the tiling engine.
	/// </summary>
	public int T2 {
		get;
	}

	/// <summary>
	/// Gets the aspect identifier reported by the tiling engine.
	/// </summary>
	public int Aspect {
		get;
	}


	/// <summary>
	/// Creates a new sampled tiling cell.
	/// </summary>
	/// <param name="id">Stable graph-local cell identifier.</param>
	/// <param name="transform">Affine transform used to place the prototile.</param>
	/// <param name="shape">The transformed polygon for the placed tile.</param>
	/// <param name="center">Centroid of the placed tile.</param>
	/// <param name="t1">First lattice coordinate reported by the tiling engine.</param>
	/// <param name="t2">Second lattice coordinate reported by the tiling engine.</param>
	/// <param name="aspect">Aspect identifier reported by the tiling engine.</param>
	public TilingCell(int id, Transform2D transform, Polygon shape, Vector2 center, int t1, int t2, int aspect) {
		Id        = id;
		Transform = transform;
		Shape     = shape;
		Center    = center;
		T1        = t1;
		T2        = t2;
		Aspect    = aspect;
	}

}
