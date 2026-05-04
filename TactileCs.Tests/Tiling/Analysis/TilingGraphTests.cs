using TactileCs.Tiling;
using TactileCs.Tiling.Analysis;
using TactileCs.Geometry;

namespace TactileCs.Tests.Tiling.Analysis;

public class TilingGraphTests
{
    static TilingGraph BuildGraph(int typeId = 1, double range = 2.0) =>
        new IsohedralTiling(typeId).CreateGraph(-range, -range, range, range);

    // ── Construction ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_HasNonEmptyCells()
    {
        Assert.NotEmpty(BuildGraph().Cells);
    }

    [Fact]
    public void Constructor_RegionIsPreserved()
    {
        var tiling = new IsohedralTiling(1);
        var region = new TilingRegion(-3, -3, 3, 3);
        var g = new TilingGraph(tiling, region);
        Assert.Equal(-3.0, g.Region.XMin);
        Assert.Equal(3.0, g.Region.YMax);
    }

    [Fact]
    public void Constructor_ThrowsIfTilingIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TilingGraph(null!, new TilingRegion(-1, -1, 1, 1)));
    }

    // ── Cells ────────────────────────────────────────────────────────────────

    [Fact]
    public void Cells_HaveUniqueIds()
    {
        var g = BuildGraph();
        var ids = g.Cells.Select(c => c.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void Cells_HaveValidShapes()
    {
        var g = BuildGraph();
        foreach (var cell in g.Cells)
        {
            Assert.True(cell.Shape.IsValid(), $"Cell {cell.Id} has invalid shape");
        }
    }

    // ── Connections ──────────────────────────────────────────────────────────

    [Fact]
    public void Connections_NotNull()
    {
        Assert.NotNull(BuildGraph().Connections);
    }

    // ── FindCell ─────────────────────────────────────────────────────────────

    [Fact]
    public void FindCell_KnownId_ReturnsCell()
    {
        var g = BuildGraph();
        var first = g.Cells[0];
        Assert.NotNull(g.FindCell(first.Id));
        Assert.Same(first, g.FindCell(first.Id));
    }

    [Fact]
    public void FindCell_UnknownId_ReturnsNull()
    {
        var g = BuildGraph();
        Assert.Null(g.FindCell(-999));
    }

    // ── FindNearestCell ──────────────────────────────────────────────────────

    [Fact]
    public void FindNearestCell_OriginPoint_ReturnsCell()
    {
        var g = BuildGraph();
        var cell = g.FindNearestCell(Vector2.Zero);
        Assert.NotNull(cell);
    }

    [Fact]
    public void FindNearestCell_EmptyGraph_ReturnsNull()
    {
        var tiling = new IsohedralTiling(1);
        // Zero-size region → no cells
        var g = new TilingGraph(tiling, new TilingRegion(0, 0, 0, 0));
        Assert.Null(g.FindNearestCell(Vector2.Zero));
    }

    [Fact]
    public void FindNearestCell_IsCloserThanOtherCells()
    {
        var g = BuildGraph();
        var query = new Vector2(0.5, 0.5);
        var nearest = g.FindNearestCell(query)!;
        double nearestDist = Vector2.Distance(nearest.Center, query);

        foreach (var cell in g.Cells)
        {
            Assert.True(
                Vector2.Distance(cell.Center, query) >= nearestDist - 1e-10,
                $"Cell {cell.Id} is closer than the reported nearest cell {nearest.Id}");
        }
    }

    // ── GetNeighbours ─────────────────────────────────────────────────────────

    [Fact]
    public void GetNeighbours_ThrowsIfCellIsNull()
    {
        var g = BuildGraph();
        Assert.Throws<ArgumentNullException>(() => g.GetNeighbours(null!).ToList());
    }

    [Fact]
    public void GetNeighbours_ThrowsIfCellFromDifferentGraph()
    {
        var g1 = BuildGraph(1, 1.0);
        var g2 = BuildGraph(1, 2.0);
        var cell = g2.Cells[0];
        Assert.Throws<ArgumentException>(() => g1.GetNeighbours(cell).ToList());
    }

    [Fact]
    public void GetNeighbours_EdgeSharing_SubsetOfCornerNeighbours()
    {
        var g = BuildGraph();
        var cell = g.Cells[g.Cells.Count / 2];
        var edgeNeighbours = g.GetNeighbours(cell, false).ToHashSet();
        var allNeighbours = g.GetNeighbours(cell, true).ToHashSet();
        Assert.True(edgeNeighbours.IsSubsetOf(allNeighbours));
    }

    // ── GetAdjacencies ────────────────────────────────────────────────────────

    [Fact]
    public void GetAdjacencies_ThrowsIfCellIsNull()
    {
        var g = BuildGraph();
        Assert.Throws<ArgumentNullException>(() => g.GetAdjacencies(null!).ToList());
    }

    [Fact]
    public void GetAdjacencies_AdjacencySourceOrTargetIsCell()
    {
        var g = BuildGraph();
        var cell = g.Cells[g.Cells.Count / 2];
        foreach (var adj in g.GetAdjacencies(cell, true))
        {
            Assert.True(
                ReferenceEquals(adj.Source, cell) || ReferenceEquals(adj.Target, cell),
                "Adjacency should reference the queried cell");
        }
    }

    // ── GetCentroidDistance ───────────────────────────────────────────────────

    [Fact]
    public void GetCentroidDistance_SameCell_IsZero()
    {
        var g = BuildGraph();
        var cell = g.Cells[0];
        Assert.Equal(0.0, g.GetCentroidDistance(cell, cell), 10);
    }

    [Fact]
    public void GetCentroidDistance_NonNegative()
    {
        var g = BuildGraph();
        var a = g.Cells[0];
        var b = g.Cells[g.Cells.Count - 1];
        Assert.True(g.GetCentroidDistance(a, b) >= 0.0);
    }

    // ── GetHopDistance ────────────────────────────────────────────────────────

    [Fact]
    public void GetHopDistance_SameCell_IsZero()
    {
        var g = BuildGraph();
        var cell = g.Cells[g.Cells.Count / 2];
        Assert.Equal(0, g.GetHopDistance(cell, cell));
    }

    // ── GetShortestPath ───────────────────────────────────────────────────────

    [Fact]
    public void GetShortestPath_SameCell_ReturnsSingletonPath()
    {
        var g = BuildGraph();
        var cell = g.Cells[0];
        var path = g.GetShortestPath(cell, cell);
        Assert.Equal(1, path.Count);
        Assert.Same(cell, path[0]);
    }

    [Fact]
    public void GetShortestPath_ThrowsIfStartIsNull()
    {
        var g = BuildGraph();
        Assert.Throws<ArgumentNullException>(() =>
            g.GetShortestPath(null!, g.Cells[0]));
    }

    [Fact]
    public void GetShortestPath_ThrowsIfTargetIsNull()
    {
        var g = BuildGraph();
        Assert.Throws<ArgumentNullException>(() =>
            g.GetShortestPath(g.Cells[0], null!));
    }

    [Fact]
    public void GetShortestPath_StartAndEndAreCorrect()
    {
        var g = BuildGraph();
        if (g.Cells.Count < 2) return;
        var start = g.Cells[0];
        var end = g.Cells[g.Cells.Count - 1];
        var path = g.GetShortestPath(start, end);
        if (path.Count > 0)
        {
            Assert.Same(start, path[0]);
            Assert.Same(end, path[path.Count - 1]);
        }
    }

    // ── GetCellsWithinSteps ───────────────────────────────────────────────────

    [Fact]
    public void GetCellsWithinSteps_ZeroSteps_ReturnsOnlyOrigin()
    {
        var g = BuildGraph();
        var cell = g.Cells[g.Cells.Count / 2];
        var result = g.GetCellsWithinSteps(cell, 0);
        Assert.Single(result);
        Assert.Same(cell, result[0]);
    }

    [Fact]
    public void GetCellsWithinSteps_NegativeSteps_ThrowsArgumentOutOfRange()
    {
        var g = BuildGraph();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            g.GetCellsWithinSteps(g.Cells[0], -1));
    }

    [Fact]
    public void GetCellsWithinSteps_MoreSteps_MoreCells()
    {
        var g = BuildGraph(1, 3.0);
        if (g.Cells.Count < 5) return;
        var cell = g.Cells[g.Cells.Count / 2];
        var within1 = g.GetCellsWithinSteps(cell, 1);
        var within2 = g.GetCellsWithinSteps(cell, 2);
        Assert.True(within2.Count >= within1.Count);
    }

    // ── GetConnectedComponents ────────────────────────────────────────────────

    [Fact]
    public void GetConnectedComponents_AtLeastOneComponent()
    {
        var g = BuildGraph();
        if (!g.Cells.Any()) return;
        Assert.True(g.GetConnectedComponentCount() >= 1);
    }

    [Fact]
    public void GetConnectedComponents_AllCellsInSomeComponent()
    {
        var g = BuildGraph();
        var components = g.GetConnectedComponents();
        int total = components.Sum(c => c.Count);
        Assert.Equal(g.Cells.Count, total);
    }

    // ── GetDegree ─────────────────────────────────────────────────────────────

    [Fact]
    public void GetDegree_NonNegative()
    {
        var g = BuildGraph();
        foreach (var cell in g.Cells)
        {
            Assert.True(g.GetDegree(cell) >= 0);
        }
    }

    // ── GetDegreeDistribution ─────────────────────────────────────────────────

    [Fact]
    public void GetDegreeDistribution_CountsSumToTotalCells()
    {
        var g = BuildGraph();
        var dist = g.GetDegreeDistribution();
        Assert.Equal(g.Cells.Count, dist.Values.Sum());
    }
}
