using TactileCs.Geometry;

namespace TactileCs.Tests.Geometry;

public class GeometryUtilsTests
{
    const double Eps = 1e-10;

    // ── ClosestPointOnSegment ────────────────────────────────────────────────

    [Fact]
    public void ClosestPointOnSegment_ProjectsBeyondEnd_ClampsToEnd()
    {
        var pt = new Vector2(5, 0);
        var closest = GeometryUtils.ClosestPointOnSegment(pt, Vector2.Zero, new Vector2(2, 0));
        Assert.Equal(2.0, closest.X, 10);
        Assert.Equal(0.0, closest.Y, 10);
    }

    [Fact]
    public void ClosestPointOnSegment_ProjectsBeyondStart_ClampsToStart()
    {
        var pt = new Vector2(-5, 0);
        var closest = GeometryUtils.ClosestPointOnSegment(pt, Vector2.Zero, new Vector2(2, 0));
        Assert.Equal(0.0, closest.X, 10);
        Assert.Equal(0.0, closest.Y, 10);
    }

    [Fact]
    public void ClosestPointOnSegment_PointOnSegment_ReturnsSelf()
    {
        var pt = new Vector2(1, 0);
        var closest = GeometryUtils.ClosestPointOnSegment(pt, Vector2.Zero, new Vector2(3, 0));
        Assert.Equal(1.0, closest.X, 10);
        Assert.Equal(0.0, closest.Y, 10);
    }

    [Fact]
    public void ClosestPointOnSegment_DegenerateSegment_ReturnsStart()
    {
        var pt = new Vector2(5, 5);
        var start = new Vector2(1, 1);
        var closest = GeometryUtils.ClosestPointOnSegment(pt, start, start);
        Assert.True(start.AlmostEquals(closest));
    }

    // ── DistanceToSegment ────────────────────────────────────────────────────

    [Fact]
    public void DistanceToSegment_PerpendicularPoint_IsCorrect()
    {
        var pt = new Vector2(1, 3);
        double d = GeometryUtils.DistanceToSegment(pt, Vector2.Zero, new Vector2(2, 0));
        Assert.Equal(3.0, d, 10);
    }

    // ── DistanceToLine ───────────────────────────────────────────────────────

    [Fact]
    public void DistanceToLine_PerpendicularPoint_IsCorrect()
    {
        var pt = new Vector2(0, 5);
        double d = GeometryUtils.DistanceToLine(pt, Vector2.Zero, Vector2.UnitX);
        Assert.Equal(5.0, d, 10);
    }

    [Fact]
    public void DistanceToLine_PointOnLine_IsZero()
    {
        double d = GeometryUtils.DistanceToLine(new Vector2(3, 0), Vector2.Zero, Vector2.UnitX);
        Assert.Equal(0.0, d, 10);
    }

    // ── SegmentIntersection ──────────────────────────────────────────────────

    [Fact]
    public void SegmentIntersection_CrossingSegments_FindsIntersection()
    {
        bool found = GeometryUtils.SegmentIntersection(
            new Vector2(0, 0), new Vector2(2, 2),
            new Vector2(0, 2), new Vector2(2, 0),
            out Vector2 pt);
        Assert.True(found);
        Assert.Equal(1.0, pt.X, 10);
        Assert.Equal(1.0, pt.Y, 10);
    }

    [Fact]
    public void SegmentIntersection_ParallelSegments_ReturnsFalse()
    {
        bool found = GeometryUtils.SegmentIntersection(
            new Vector2(0, 0), new Vector2(2, 0),
            new Vector2(0, 1), new Vector2(2, 1),
            out _);
        Assert.False(found);
    }

    [Fact]
    public void SegmentIntersection_NonIntersecting_ReturnsFalse()
    {
        bool found = GeometryUtils.SegmentIntersection(
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 1), new Vector2(1, 2),
            out _);
        Assert.False(found);
    }

    // ── LineIntersection ─────────────────────────────────────────────────────

    [Fact]
    public void LineIntersection_PerpendicularLines_FindsIntersection()
    {
        bool found = GeometryUtils.LineIntersection(
            new Vector2(1, 0), new Vector2(1, 5),  // vertical x=1
            new Vector2(0, 3), new Vector2(5, 3),  // horizontal y=3
            out Vector2 pt);
        Assert.True(found);
        Assert.Equal(1.0, pt.X, 10);
        Assert.Equal(3.0, pt.Y, 10);
    }

    [Fact]
    public void LineIntersection_ParallelLines_ReturnsFalse()
    {
        bool found = GeometryUtils.LineIntersection(
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 1), new Vector2(1, 1),
            out _);
        Assert.False(found);
    }

    // ── TriangleArea ─────────────────────────────────────────────────────────

    [Fact]
    public void TriangleArea_RightTriangle_IsHalf()
    {
        double area = GeometryUtils.TriangleArea(
            Vector2.Zero, new Vector2(2, 0), new Vector2(0, 2));
        Assert.Equal(2.0, Math.Abs(area), 10);
    }

    [Fact]
    public void TriangleArea_Collinear_IsZero()
    {
        double area = GeometryUtils.TriangleArea(
            Vector2.Zero, new Vector2(1, 0), new Vector2(2, 0));
        Assert.Equal(0.0, area, 10);
    }

    // ── AreCollinear ─────────────────────────────────────────────────────────

    [Fact]
    public void AreCollinear_PointsOnLine_ReturnsTrue()
    {
        Assert.True(GeometryUtils.AreCollinear(
            Vector2.Zero, new Vector2(1, 1), new Vector2(2, 2)));
    }

    [Fact]
    public void AreCollinear_NonCollinearPoints_ReturnsFalse()
    {
        Assert.False(GeometryUtils.AreCollinear(
            Vector2.Zero, new Vector2(1, 0), new Vector2(0, 1)));
    }

    // ── ProjectPointOnLine ───────────────────────────────────────────────────

    [Fact]
    public void ProjectPointOnLine_OffAxis_ProjectsToLine()
    {
        var proj = GeometryUtils.ProjectPointOnLine(
            new Vector2(3, 5), Vector2.Zero, Vector2.UnitX);
        Assert.Equal(3.0, proj.X, 10);
        Assert.Equal(0.0, proj.Y, 10);
    }

    // ── Circumcenter ─────────────────────────────────────────────────────────

    [Fact]
    public void Circumcenter_EquilateralTriangle_IsCenter()
    {
        // Equilateral triangle with known circumcenter
        var a = new Vector2(0, 0);
        var b = new Vector2(1, 0);
        var c = new Vector2(0.5, Math.Sqrt(3.0) / 2.0);
        var center = GeometryUtils.Circumcenter(a, b, c);
        Assert.Equal(0.5, center.X, 6);
        Assert.Equal(Math.Sqrt(3.0) / 6.0, center.Y, 6);
    }

    [Fact]
    public void Circumcenter_Collinear_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            GeometryUtils.Circumcenter(
                Vector2.Zero, new Vector2(1, 0), new Vector2(2, 0)));
    }

    // ── Circumradius ─────────────────────────────────────────────────────────

    [Fact]
    public void Circumradius_IsDistanceFromCenterToVertex()
    {
        var a = new Vector2(0, 0);
        var b = new Vector2(2, 0);
        var c = new Vector2(1, 2);
        double r = GeometryUtils.Circumradius(a, b, c);
        var center = GeometryUtils.Circumcenter(a, b, c);
        Assert.Equal(Vector2.Distance(center, a), r, 10);
    }
}
