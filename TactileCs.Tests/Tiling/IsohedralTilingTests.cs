using TactileCs.Tiling;
using TactileCs.Geometry;

namespace TactileCs.Tests.Tiling;

public class IsohedralTilingTests
{
    // ── AllTypes ─────────────────────────────────────────────────────────────

    [Fact]
    public void AllTypes_NotEmpty()
    {
        Assert.NotEmpty(IsohedralTiling.AllTypes);
    }

    [Fact]
    public void AllTypes_Contains81Types()
    {
        Assert.Equal(81, IsohedralTiling.AllTypes.Count);
    }

    [Fact]
    public void AllTypes_Contains_Type1()
    {
        Assert.Contains(1, IsohedralTiling.AllTypes);
    }

    [Fact]
    public void AllTypes_Contains_Type93()
    {
        Assert.Contains(93, IsohedralTiling.AllTypes);
    }

    [Fact]
    public void AllTypes_DoesNotContain_Type19()
    {
        // Type 19 is not a valid isohedral type
        Assert.DoesNotContain(19, IsohedralTiling.AllTypes);
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ValidType_SetsTypeId()
    {
        var t = new IsohedralTiling(1);
        Assert.Equal(1, t.TypeId);
    }

    [Fact]
    public void Constructor_InvalidType_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new IsohedralTiling(999));
    }

    [Fact]
    public void Constructor_UndefinedType19_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new IsohedralTiling(19));
    }

    // ── SetType ──────────────────────────────────────────────────────────────

    [Fact]
    public void SetType_ChangesTypeId()
    {
        var t = new IsohedralTiling(1);
        t.SetType(9);
        Assert.Equal(9, t.TypeId);
    }

    [Fact]
    public void SetType_InvalidType_ThrowsArgumentException()
    {
        var t = new IsohedralTiling(1);
        Assert.Throws<ArgumentException>(() => t.SetType(0));
    }

    // ── AllTypes: every type constructs without exception ────────────────────

    [Theory]
    [MemberData(nameof(AllTypeIds))]
    public void ConstructAllTypes_DoesNotThrow(int typeId)
    {
        var t = new IsohedralTiling(typeId);
        Assert.Equal(typeId, t.TypeId);
    }

    public static IEnumerable<object[]> AllTypeIds =>
        IsohedralTiling.AllTypes.Select(id => new object[] { id });

    // ── Parameters ───────────────────────────────────────────────────────────

    [Fact]
    public void GetParameters_ReturnsClone_NotSameArray()
    {
        var t = new IsohedralTiling(1);
        var p1 = t.GetParameters();
        var p2 = t.GetParameters();
        Assert.NotSame(p1, p2);
    }

    [Fact]
    public void NumParameters_MatchesDefaultParamsLength()
    {
        var t = new IsohedralTiling(1);
        Assert.Equal(t.GetParameters().Length, t.NumParameters);
    }

    [Fact]
    public void SetParameters_CorrectLength_Succeeds()
    {
        var t = new IsohedralTiling(1);
        int n = t.NumParameters;
        if (n > 0)
        {
            t.SetParameters(new double[n]);
            Assert.Equal(n, t.NumParameters);
        }
    }

    [Fact]
    public void SetParameters_WrongLength_ThrowsArgumentException()
    {
        // Find a type with at least 1 parameter
        var typeId = IsohedralTiling.AllTypes.First(id =>
        {
            var tiling = new IsohedralTiling(id);
            return tiling.NumParameters > 0;
        });
        var t = new IsohedralTiling(typeId);
        Assert.Throws<ArgumentException>(() => t.SetParameters(new double[t.NumParameters + 1]));
    }

    // ── NumEdgeShapes ─────────────────────────────────────────────────────────

    [Fact]
    public void NumEdgeShapes_PositiveForAllTypes()
    {
        foreach (int typeId in IsohedralTiling.AllTypes)
        {
            var t = new IsohedralTiling(typeId);
            Assert.True(t.NumEdgeShapes() >= 1, $"Type {typeId} reports 0 edge shapes");
        }
    }

    // ── GetTileShape ──────────────────────────────────────────────────────────

    [Fact]
    public void GetTileShape_Type1_IsValid()
    {
        var t = new IsohedralTiling(1);
        var poly = t.GetTileShape();
        Assert.True(poly.IsValid());
        Assert.True(poly.Count >= 3);
    }

    [Theory]
    [MemberData(nameof(AllTypeIds))]
    public void GetTileShape_AllTypes_IsValid(int typeId)
    {
        var t = new IsohedralTiling(typeId);
        var poly = t.GetTileShape();
        Assert.True(poly.IsValid(), $"Type {typeId}: polygon not valid");
    }

    // ── ShapeEdges ────────────────────────────────────────────────────────────

    [Fact]
    public void ShapeEdges_Type1_YieldsEdges()
    {
        var t = new IsohedralTiling(1);
        var edges = t.ShapeEdges().ToList();
        Assert.NotEmpty(edges);
    }

    // ── FillRegionBounds ──────────────────────────────────────────────────────

    [Fact]
    public void FillRegionBounds_Type1_ReturnsNonEmpty()
    {
        var t = new IsohedralTiling(1);
        var tiles = t.FillRegionBounds(-2, -2, 2, 2).ToList();
        Assert.NotEmpty(tiles);
    }

    [Theory]
    [MemberData(nameof(AllTypeIds))]
    public void FillRegionBounds_AllTypes_ReturnsNonEmpty(int typeId)
    {
        var t = new IsohedralTiling(typeId);
        var tiles = t.FillRegionBounds(-2, -2, 2, 2).ToList();
        Assert.NotEmpty(tiles);
    }

    [Fact]
    public void FillRegionBounds_EmptyRegion_MayReturnEmpty()
    {
        // An empty/degenerate region (zero area) should not throw
        var t = new IsohedralTiling(1);
        var result = t.FillRegionBounds(0, 0, 0, 0);
        Assert.NotNull(result);
    }

    // ── GetColour ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(1, 1, 1)]
    public void GetColour_ReturnsNonNegative(int t1, int t2, int aspect)
    {
        var tiling = new IsohedralTiling(1);
        int colour = tiling.GetColour(t1, t2, aspect);
        Assert.True(colour >= 0);
    }

    // ── Symmetry ──────────────────────────────────────────────────────────────

    [Fact]
    public void Symmetry_IsNotNull()
    {
        var t = new IsohedralTiling(1);
        Assert.NotNull(t.Symmetry);
    }

    // ── CreateGraph ───────────────────────────────────────────────────────────

    [Fact]
    public void CreateGraph_Type1_ReturnsNonNullGraph()
    {
        var t = new IsohedralTiling(1);
        var g = t.CreateGraph(-2, -2, 2, 2);
        Assert.NotNull(g);
    }

    [Fact]
    public void CreateGraph_Type1_HasCells()
    {
        var t = new IsohedralTiling(1);
        var g = t.CreateGraph(-2, -2, 2, 2);
        Assert.NotEmpty(g.Cells);
    }
}
