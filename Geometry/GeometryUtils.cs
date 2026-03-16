namespace TactileCs.Geometry;


/// <summary>
/// Utility functions for geometric operations.
/// </summary>
public static class GeometryUtils {

	/// <summary>
	/// Computes the closest point on a line segment to a given point.
	/// </summary>
	/// <param name="point">The point to test.</param>
	/// <param name="segmentStart">The start of the line segment.</param>
	/// <param name="segmentEnd">The end of the line segment.</param>
	/// <returns>The closest point on the segment to <paramref name="point"/>.</returns>
	public static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd) {
		Vector2 segmentVector = segmentEnd - segmentStart;
		Vector2 pointVector   = point - segmentStart;

		double segmentLengthSquared = segmentVector.LengthSquared;

		if (segmentLengthSquared < Constants.NumericalEpsilon) {
			// Degenerate segment (start == end)
			return segmentStart;
		}

		double t = Vector2.Dot(pointVector, segmentVector) / segmentLengthSquared;

		// Clamp t to [0, 1] to stay on the segment
		t = Math.Max(0.0, Math.Min(1.0, t));

		return segmentStart + (segmentVector * t);
	}


	/// <summary>
	/// Computes the distance from a point to a line segment.
	/// </summary>
	/// <param name="point">The point to test.</param>
	/// <param name="segmentStart">The start of the line segment.</param>
	/// <param name="segmentEnd">The end of the line segment.</param>
	/// <returns>The distance from <paramref name="point"/> to the segment.</returns>
	public static double DistanceToSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd) {
		Vector2 closest = ClosestPointOnSegment(point, segmentStart, segmentEnd);

		return Vector2.Distance(point, closest);
	}


	/// <summary>
	/// Computes the distance from a point to an infinite line defined by two points.
	/// </summary>
	/// <param name="point">The point to test.</param>
	/// <param name="linePoint1">First point on the line.</param>
	/// <param name="linePoint2">Second point on the line.</param>
	/// <returns>The perpendicular distance from <paramref name="point"/> to the line.</returns>
	public static double DistanceToLine(Vector2 point, Vector2 linePoint1, Vector2 linePoint2) {
		Vector2 lineVector  = linePoint2 - linePoint1;
		Vector2 pointVector = point - linePoint1;

		double lineLengthSquared = lineVector.LengthSquared;

		if (lineLengthSquared < Constants.NumericalEpsilon) {
			// Degenerate line (point1 == point2)
			return Vector2.Distance(point, linePoint1);
		}

		// Use cross product to compute perpendicular distance
		double crossProduct = Vector2.Cross(lineVector, pointVector);

		return Math.Abs(crossProduct) / Math.Sqrt(lineLengthSquared);
	}


	/// <summary>
	/// Finds the intersection point of two line segments, if one exists.
	/// </summary>
	/// <param name="a1">First endpoint of the first segment.</param>
	/// <param name="a2">Second endpoint of the first segment.</param>
	/// <param name="b1">First endpoint of the second segment.</param>
	/// <param name="b2">Second endpoint of the second segment.</param>
	/// <param name="intersection">The intersection point if found.</param>
	/// <returns>True if the segments intersect; otherwise false.</returns>
	public static bool SegmentIntersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersection) {
		intersection = Vector2.Zero;

		Vector2 r = a2 - a1;
		Vector2 s = b2 - b1;

		double rxs = Vector2.Cross(r, s);
		Vector2 qp = b1 - a1;
		double qpxr = Vector2.Cross(qp, r);

		// Check if segments are parallel or collinear
		if (Math.Abs(rxs) < Constants.NumericalEpsilon) {
			return false;
		}

		double t = Vector2.Cross(qp, s) / rxs;
		double u = qpxr / rxs;

		// Check if intersection point is within both segments
		if ((t >= 0.0) && (t <= 1.0) && (u >= 0.0) && (u <= 1.0)) {
			intersection = a1 + (r * t);

			return true;
		}

		return false;
	}


	/// <summary>
	/// Finds the intersection point of two infinite lines, if one exists.
	/// </summary>
	/// <param name="a1">First point on the first line.</param>
	/// <param name="a2">Second point on the first line.</param>
	/// <param name="b1">First point on the second line.</param>
	/// <param name="b2">Second point on the second line.</param>
	/// <param name="intersection">The intersection point if found.</param>
	/// <returns>True if the lines intersect (not parallel); otherwise false.</returns>
	public static bool LineIntersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersection) {
		intersection = Vector2.Zero;

		Vector2 r = a2 - a1;
		Vector2 s = b2 - b1;

		double rxs = Vector2.Cross(r, s);

		// Check if lines are parallel
		if (Math.Abs(rxs) < Constants.NumericalEpsilon) {
			return false;
		}

		Vector2 qp = b1 - a1;
		double t = Vector2.Cross(qp, s) / rxs;

		intersection = a1 + (r * t);

		return true;
	}


	/// <summary>
	/// Computes the signed area of a triangle defined by three points.
	/// </summary>
	/// <param name="a">First vertex.</param>
	/// <param name="b">Second vertex.</param>
	/// <param name="c">Third vertex.</param>
	/// <returns>The signed area (positive if counter-clockwise, negative if clockwise).</returns>
	public static double TriangleArea(Vector2 a, Vector2 b, Vector2 c) {
		return 0.5 * (((b.X - a.X) * (c.Y - a.Y)) - ((c.X - a.X) * (b.Y - a.Y)));
	}


	/// <summary>
	/// Tests if three points are collinear (lie on the same line).
	/// </summary>
	/// <param name="a">First point.</param>
	/// <param name="b">Second point.</param>
	/// <param name="c">Third point.</param>
	/// <param name="epsilon">Tolerance for the test (default: 1e-10).</param>
	/// <returns>True if the points are collinear within the tolerance; otherwise false.</returns>
	public static bool AreCollinear(Vector2 a, Vector2 b, Vector2 c, double epsilon = 1e-10) {
		double area = Math.Abs(TriangleArea(a, b, c));

		return area < epsilon;
	}


	/// <summary>
	/// Projects a point onto an infinite line defined by two points.
	/// </summary>
	/// <param name="point">The point to project.</param>
	/// <param name="linePoint1">First point on the line.</param>
	/// <param name="linePoint2">Second point on the line.</param>
	/// <returns>The projection of <paramref name="point"/> onto the line.</returns>
	public static Vector2 ProjectPointOnLine(Vector2 point, Vector2 linePoint1, Vector2 linePoint2) {
		Vector2 lineVector  = linePoint2 - linePoint1;
		Vector2 pointVector = point - linePoint1;

		double lineLengthSquared = lineVector.LengthSquared;

		if (lineLengthSquared < Constants.NumericalEpsilon) {
			// Degenerate line
			return linePoint1;
		}

		double t = Vector2.Dot(pointVector, lineVector) / lineLengthSquared;

		return linePoint1 + (lineVector * t);
	}


	/// <summary>
	/// Computes the circumcenter of a triangle defined by three points.
	/// </summary>
	/// <param name="a">First vertex.</param>
	/// <param name="b">Second vertex.</param>
	/// <param name="c">Third vertex.</param>
	/// <returns>The circumcenter of the triangle.</returns>
	/// <exception cref="ArgumentException">Thrown if the points are collinear.</exception>
	public static Vector2 Circumcenter(Vector2 a, Vector2 b, Vector2 c) {
		double d = 2.0 * (((a.X - c.X) * (b.Y - c.Y)) - ((a.Y - c.Y) * (b.X - c.X)));

		if (Math.Abs(d) < Constants.NumericalEpsilon) {
			throw new ArgumentException("Points are collinear, circumcenter is undefined.");
		}

		double aLenSq = a.LengthSquared;
		double bLenSq = b.LengthSquared;
		double cLenSq = c.LengthSquared;

		double x = (((aLenSq - cLenSq) * (b.Y - c.Y)) - ((bLenSq - cLenSq) * (a.Y - c.Y))) / d;
		double y = (((bLenSq - cLenSq) * (a.X - c.X)) - ((aLenSq - cLenSq) * (b.X - c.X))) / d;

		return new Vector2(x, y);
	}


	/// <summary>
	/// Computes the circumradius of a triangle defined by three points.
	/// </summary>
	/// <param name="a">First vertex.</param>
	/// <param name="b">Second vertex.</param>
	/// <param name="c">Third vertex.</param>
	/// <returns>The radius of the circumcircle.</returns>
	public static double Circumradius(Vector2 a, Vector2 b, Vector2 c) {
		Vector2 center = Circumcenter(a, b, c);

		return Vector2.Distance(center, a);
	}

}
