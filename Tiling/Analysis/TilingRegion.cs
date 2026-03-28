// Port of concepts from Tactile (c) 2018 Craig S. Kaplan, BSD 3-Clause. See LICENSE.

using TactileCs.Geometry;


namespace TactileCs.Tiling.Analysis;


/// <summary>
/// Represents an axis-aligned region used to sample and analyze a tiling.
/// </summary>
/// <exception cref="ArgumentException">
/// Thrown when the maximum coordinates are smaller than the minimum coordinates.
/// </exception>
public readonly struct TilingRegion {

	/// <summary>
	/// Gets the minimum X coordinate of the region.
	/// </summary>
	public double XMin {
		get;
	}

	/// <summary>
	/// Gets the minimum Y coordinate of the region.
	/// </summary>
	public double YMin {
		get;
	}

	/// <summary>
	/// Gets the maximum X coordinate of the region.
	/// </summary>
	public double XMax {
		get;
	}

	/// <summary>
	/// Gets the maximum Y coordinate of the region.
	/// </summary>
	public double YMax {
		get;
	}

	/// <summary>
	/// Gets the width of the region.
	/// </summary>
	public double Width => XMax - XMin;

	/// <summary>
	/// Gets the height of the region.
	/// </summary>
	public double Height => YMax - YMin;


	/// <summary>
	/// Initializes a new axis-aligned sampling region.
	/// </summary>
	/// <param name="xMin">Minimum X coordinate.</param>
	/// <param name="yMin">Minimum Y coordinate.</param>
	/// <param name="xMax">Maximum X coordinate.</param>
	/// <param name="yMax">Maximum Y coordinate.</param>
	public TilingRegion(double xMin, double yMin, double xMax, double yMax) {
		if (xMax < xMin) {
			throw new ArgumentException("The maximum X coordinate must be greater than or equal to the minimum X coordinate.", nameof(xMax));
		}

		if (yMax < yMin) {
			throw new ArgumentException("The maximum Y coordinate must be greater than or equal to the minimum Y coordinate.", nameof(yMax));
		}

		XMin = xMin;
		YMin = yMin;
		XMax = xMax;
		YMax = yMax;
	}


	/// <summary>
	/// Determines whether the region contains the specified point.
	/// </summary>
	/// <param name="point">The point to test.</param>
	/// <param name="tolerance">Optional tolerance applied to the region bounds.</param>
	/// <returns><c>true</c> if the point is inside or on the region boundary; otherwise, <c>false</c>.</returns>
	public bool Contains(Vector2 point, double tolerance = 0.0) =>
			(point.X >= (XMin - tolerance))
			&& (point.X <= (XMax + tolerance))
			&& (point.Y >= (YMin - tolerance))
			&& (point.Y <= (YMax + tolerance));


	/// <summary>
	/// Determines whether the region intersects an axis-aligned bounding box.
	/// </summary>
	/// <param name="minX">Bounding-box minimum X coordinate.</param>
	/// <param name="minY">Bounding-box minimum Y coordinate.</param>
	/// <param name="maxX">Bounding-box maximum X coordinate.</param>
	/// <param name="maxY">Bounding-box maximum Y coordinate.</param>
	/// <param name="tolerance">Optional tolerance applied to both boxes.</param>
	/// <returns><c>true</c> if the two boxes overlap; otherwise, <c>false</c>.</returns>
	public bool Intersects(double minX, double minY, double maxX, double maxY, double tolerance = 0.0) =>
			(maxX >= (XMin - tolerance))
			&& (minX <= (XMax + tolerance))
			&& (maxY >= (YMin - tolerance))
			&& (minY <= (YMax + tolerance));


	/// <summary>
	/// Returns a new region expanded by the given amount in all directions.
	/// </summary>
	/// <param name="amount">Expansion amount.</param>
	/// <returns>An expanded region.</returns>
	public TilingRegion Inflate(double amount) => new(XMin - amount, YMin - amount, XMax + amount, YMax + amount);


	/// <summary>
	/// Returns a readable string representation of the region.
	/// </summary>
	/// <returns>A formatted region string.</returns>
	public override string ToString() => $"Region [{XMin:F3}, {YMin:F3}] -> [{XMax:F3}, {YMax:F3}]";

}
