// Port of concepts from Tactile (c) 2018 Craig S. Kaplan, BSD 3-Clause. See LICENSE.


namespace TactileCs.Tiling.Analysis;


/// <summary>
/// Represents an undirected relationship between two sampled tiling cells.
/// </summary>
public sealed class TilingAdjacency {

	/// <summary>
	/// Gets the first cell in the adjacency relationship.
	/// </summary>
	public TilingCell Source {
		get;
	}

	/// <summary>
	/// Gets the second cell in the adjacency relationship.
	/// </summary>
	public TilingCell Target {
		get;
	}

	/// <summary>
	/// Gets a value indicating whether the cells share an entire edge.
	/// </summary>
	public bool SharesEdge {
		get;
	}

	/// <summary>
	/// Gets the number of coincident vertices found while comparing the two polygons.
	/// </summary>
	public int SharedVertexCount {
		get;
	}

	/// <summary>
	/// Gets the Euclidean distance between the two cell centroids.
	/// </summary>
	public double CenterDistance {
		get;
	}

	/// <summary>
	/// Gets a value indicating whether the cells touch at least at one corner.
	/// </summary>
	public bool TouchesAtCorner => SharedVertexCount > 0;


	/// <summary>
	/// Creates a new adjacency record between two cells.
	/// </summary>
	/// <param name="source">The first cell.</param>
	/// <param name="target">The second cell.</param>
	/// <param name="sharesEdge">True when the cells share at least one boundary edge.</param>
	/// <param name="sharedVertexCount">Number of coincident vertices detected between the two cell polygons.</param>
	/// <param name="centerDistance">Euclidean distance between cell centroids.</param>
	public TilingAdjacency(TilingCell source, TilingCell target, bool sharesEdge, int sharedVertexCount, double centerDistance) {
		Source            = source;
		Target            = target;
		SharesEdge        = sharesEdge;
		SharedVertexCount = sharedVertexCount;
		CenterDistance    = centerDistance;
	}

}
