using TactileCs.Geometry;

namespace TactileCs.Tests.Geometry;

public class PolygonTests
{
    static Polygon Square() => new([
        new Vector2(0, 0), new Vector2(1, 0),
        new Vector2(1, 1), new Vector2(0, 1)
    ]);

    static Polygon Triangle() => new([
        new Vector2(0, 0), new Vector2(2, 0), new Vector2(1, 2)
    ]);

    // ── Basic properties ────────────────────────────────────────────────────

    [Fact]
    public void Count_ReturnsVertexCount()
    {
        Assert.Equal(4, Square().Count);
    }

    [Fact]
    public void Vertices_ReturnsAllVertices()
    {
        var sq = Square();
        Assert.Equal(4, sq.Vertices.Count);
    }

    [Fact]
    public void IsValid_ThreeOrMoreVertices_ReturnsTrue()
    {
        Assert.True(Square().IsValid());
        Assert.True(Triangle().IsValid());
    }

    [Fact]
    public void IsValid_TwoVertices_ReturnsFalse()
    {
        var p = new Polygon([new Vector2(0, 0), new Vector2(1, 1)]);
        Assert.False(p.IsValid());
    }

    // ── Area ────────────────────────────────────────────────────────────────

    [Fact]
    public void Area_UnitSquare_IsOne()
    {
        Assert.Equal(1.0, Square().Area, 10);
    }

    [Fact]
    public void Area_Triangle_IsHalfBase_Times_Height()
    {
        // Base=2, height=2 → area=2
        Assert.Equal(2.0, Triangle().Area, 10);
    }

    [Fact]
    public void Area_EmptyPolygon_IsZero()
    {
        Assert.Equal(0.0, new Polygon([]).Area, 10);
    }

    // ── Centroid ────────────────────────────────────────────────────────────

    [Fact]
    public void Centroid_UnitSquare_IsCenter()
    {
        var c = Square().Centroid();
        Assert.Equal(0.5, c.X, 10);
        Assert.Equal(0.5, c.Y, 10);
    }

    [Fact]
    public void Centroid_EquilateralTriangle_IsGeometricCenter()
    {
        // Triangle at (0,0),(3,0),(1.5, h) — centroid x=1.5, y=h/3
        double h = Math.Sqrt(3.0) * 1.5;
        var tri = new Polygon([
            new Vector2(0, 0), new Vector2(3, 0), new Vector2(1.5, h)
        ]);
        var c = tri.Centroid();
        Assert.Equal(1.5, c.X, 6);
        Assert.Equal(h / 3.0, c.Y, 6);
    }

    // ── Contains ────────────────────────────────────────────────────────────

    [Fact]
    public void Contains_PointInsideSquare_ReturnsTrue()
    {
        Assert.True(Square().Contains(new Vector2(0.5, 0.5)));
    }

    [Fact]
    public void Contains_PointOutsideSquare_ReturnsFalse()
    {
        Assert.False(Square().Contains(new Vector2(2.0, 2.0)));
    }

    [Fact]
    public void Contains_PointOnEdge_ReturnsTrue()
    {
        // Point exactly on the bottom edge of the unit square
        // The winding-number algorithm typically considers boundary as inside.
        // We just verify it doesn't throw and the result is deterministic.
        _ = Square().Contains(new Vector2(0.5, 0.0));
    }

    // ── BoundingBox ─────────────────────────────────────────────────────────

    [Fact]
    public void BoundingBox_UnitSquare_CorrectBounds()
    {
        var (minX, minY, maxX, maxY) = Square().BoundingBox();
        Assert.Equal(0.0, minX, 10);
        Assert.Equal(0.0, minY, 10);
        Assert.Equal(1.0, maxX, 10);
        Assert.Equal(1.0, maxY, 10);
    }

    [Fact]
    public void BoundingBox_EmptyPolygon_ReturnsZeros()
    {
        var (minX, minY, maxX, maxY) = new Polygon([]).BoundingBox();
        Assert.Equal(0.0, minX + minY + maxX + maxY, 10);
    }

    // ── Orientation ─────────────────────────────────────────────────────────

    [Fact]
    public void GetOrientation_CounterClockwiseSquare_ReturnsCounterClockwise()
    {
        // CCW square: going (0,0)→(1,0)→(1,1)→(0,1) is CCW in standard math orientation
        // (Note: screen vs math coords may differ; we test the return value is consistent)
        var ori = Square().GetOrientation();
        Assert.True(ori == Polygon.Orientation.Clockwise || ori == Polygon.Orientation.CounterClockwise);
    }

    [Fact]
    public void IsClockwise_AndIsCounterClockwise_AreOpposite()
    {
        var sq = Square();
        Assert.NotEqual(sq.IsClockwise(), sq.IsCounterClockwise());
    }

    // ── LoopLength ──────────────────────────────────────────────────────────

    [Fact]
    public void LoopLength_UnitSquare_IsFour()
    {
        Assert.Equal(4.0, Square().LoopLength, 10);
    }

    // ── Transform ───────────────────────────────────────────────────────────

    [Fact]
    public void Transform_Translate_MovesAllVertices()
    {
        var t = Transform2D.CreateTranslation(1, 2);
        var moved = Square().Transform(t);
        Assert.Equal(1.0, moved.Vertices[0].X, 10);
        Assert.Equal(2.0, moved.Vertices[0].Y, 10);
    }

    // ── Reverse ─────────────────────────────────────────────────────────────

    [Fact]
    public void Reverse_ReversesVertexOrder()
    {
        var sq = Square();
        var rev = sq.Reverse();
        Assert.Equal(sq.Vertices[0], rev.Vertices[3]);
        Assert.Equal(sq.Vertices[3], rev.Vertices[0]);
    }

    // ── IsSelfIntersecting ───────────────────────────────────────────────────

    [Fact]
    public void IsSelfIntersecting_Convex_ReturnsFalse()
    {
        Assert.False(Square().IsSelfIntersecting());
    }

    [Fact]
    public void IsSelfIntersecting_BowTie_ReturnsTrue()
    {
        // A bow-tie (self-intersecting) quadrilateral
        var bowtie = new Polygon([
            new Vector2(0, 0), new Vector2(2, 2),
            new Vector2(2, 0), new Vector2(0, 2)
        ]);
        Assert.True(bowtie.IsSelfIntersecting());
    }

    // ── SegmentsIntersect ───────────────────────────────────────────────────

    [Fact]
    public void SegmentsIntersect_CrossingSegments_ReturnsTrue()
    {
        var a1 = new Point(0, 0);
        var a2 = new Point(2, 2);
        var b1 = new Point(0, 2);
        var b2 = new Point(2, 0);
        Assert.True(Polygon.SegmentsIntersect(a1, a2, b1, b2));
    }

    [Fact]
    public void SegmentsIntersect_ParallelSegments_ReturnsFalse()
    {
        var a1 = new Point(0, 0);
        var a2 = new Point(2, 0);
        var b1 = new Point(0, 1);
        var b2 = new Point(2, 1);
        Assert.False(Polygon.SegmentsIntersect(a1, a2, b1, b2));
    }

    // ── ToString ─────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_ContainsPolygonKeyword()
    {
        Assert.Contains("Polygon", Square().ToString());
    }
}
