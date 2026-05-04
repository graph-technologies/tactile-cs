using TactileCs.Diagnostics;
using TactileCs.Geometry;
using TactileCs.Gpu;

namespace TactileCs.Tests.Gpu;

public class GpuAcceleratorTests
{
[Fact]
public void IsCudaAvailable_ReturnsFalse_WhenNoGpu()
{
// CI environments do not have CUDA-capable GPUs.
Assert.False(GpuAccelerator.IsCudaAvailable);
}

[Fact]
public void BatchDistanceSquared_CpuFallback_ComputesCorrectDistances()
{
var monitor = new PerformanceMonitor();
var accelerator = new GpuAccelerator(monitor);

var setA = new Vector2[] { new(0, 0), new(1, 0) };
var setB = new Vector2[] { new(3, 4), new(0, 0) };

var result = accelerator.BatchDistanceSquared(setA, setB);

// Expected (row-major): A0->B0, A0->B1, A1->B0, A1->B1
// (0,0)->(3,4) = 9+16 = 25
// (0,0)->(0,0) = 0
// (1,0)->(3,4) = 4+16 = 20
// (1,0)->(0,0) = 1
Assert.Equal(4, result.Length);
Assert.Equal(25.0, result[0], precision: 10);
Assert.Equal(0.0, result[1], precision: 10);
Assert.Equal(20.0, result[2], precision: 10);
Assert.Equal(1.0, result[3], precision: 10);
}

[Fact]
public void BatchDistanceSquared_EmptySets_ReturnsEmpty()
{
var accelerator = new GpuAccelerator(new PerformanceMonitor());

var result = accelerator.BatchDistanceSquared(
ReadOnlySpan<Vector2>.Empty,
ReadOnlySpan<Vector2>.Empty);

Assert.Empty(result);
}

[Fact]
public void BatchPointInPolygon_CpuFallback_DetectsContainment()
{
var accelerator = new GpuAccelerator(new PerformanceMonitor());

// Unit square: (0,0), (1,0), (1,1), (0,1)
var square = new Polygon(new[]
{
new Vector2(0, 0),
new Vector2(1, 0),
new Vector2(1, 1),
new Vector2(0, 1),
});

var queryPoints = new Vector2[]
{
new(0.5, 0.5),  // inside
new(2.0, 2.0),  // outside
new(-1, -1),    // outside
};

var results = accelerator.BatchPointInPolygon(square, queryPoints);

Assert.Equal(3, results.Length);
Assert.True(results[0]);   // inside
Assert.False(results[1]);  // outside
Assert.False(results[2]);  // outside
}

[Fact]
public void BatchPointInPolygon_ThrowsOnNullPolygon()
{
var accelerator = new GpuAccelerator(new PerformanceMonitor());

Assert.Throws<ArgumentNullException>(() =>
accelerator.BatchPointInPolygon(null!, ReadOnlySpan<Vector2>.Empty));
}

// ------------------------------------------------------------------
//  BatchDistance (Euclidean)
// ------------------------------------------------------------------

[Fact]
public void BatchDistance_CpuFallback_ComputesCorrectDistances()
{
var accelerator = new GpuAccelerator(new PerformanceMonitor());

var setA = new Vector2[] { new(0, 0) };
var setB = new Vector2[] { new(3, 4) };

var result = accelerator.BatchDistance(setA, setB);

Assert.Single(result);
Assert.Equal(5.0, result[0], precision: 10);
}

[Fact]
public void BatchDistance_CpuFallback_2x2_ComputesMatrix()
{
var accelerator = new GpuAccelerator(new PerformanceMonitor());

var setA = new Vector2[] { new(0, 0), new(1, 0) };
var setB = new Vector2[] { new(3, 4), new(0, 0) };

var result = accelerator.BatchDistance(setA, setB);

// (0,0)->(3,4) = 5
// (0,0)->(0,0) = 0
// (1,0)->(3,4) = sqrt(4+16) = sqrt(20)
// (1,0)->(0,0) = 1
Assert.Equal(4, result.Length);
Assert.Equal(5.0, result[0], precision: 10);
Assert.Equal(0.0, result[1], precision: 10);
Assert.Equal(Math.Sqrt(20.0), result[2], precision: 10);
Assert.Equal(1.0, result[3], precision: 10);
}

[Fact]
public void BatchDistance_EmptySets_ReturnsEmpty()
{
var accelerator = new GpuAccelerator(new PerformanceMonitor());

var result = accelerator.BatchDistance(
ReadOnlySpan<Vector2>.Empty,
ReadOnlySpan<Vector2>.Empty);

Assert.Empty(result);
}

[Fact]
public void BatchDistance_IsSquareRootOfDistanceSquared()
{
var accelerator = new GpuAccelerator(new PerformanceMonitor());

var setA = new Vector2[] { new(1, 2), new(3, 4) };
var setB = new Vector2[] { new(5, 6), new(7, 8) };

var distSq = accelerator.BatchDistanceSquared(setA, setB);
var dist   = accelerator.BatchDistance(setA, setB);

Assert.Equal(distSq.Length, dist.Length);
for (int i = 0; i < distSq.Length; i++)
Assert.Equal(Math.Sqrt(distSq[i]), dist[i], precision: 10);
}

// ------------------------------------------------------------------
//  BatchTransformPoints
// ------------------------------------------------------------------

[Fact]
public void BatchTransformPoints_Identity_ReturnsUnchangedPoints()
{
var accelerator = new GpuAccelerator(new PerformanceMonitor());
var identity = Transform2D.Identity;

var points = new Vector2[] { new(1, 2), new(3, 4), new(-1, -1) };
var result = accelerator.BatchTransformPoints(identity, points);

Assert.Equal(points.Length, result.Length);
for (int i = 0; i < points.Length; i++)
{
Assert.Equal(points[i].X, result[i].X, precision: 10);
Assert.Equal(points[i].Y, result[i].Y, precision: 10);
}
}

[Fact]
public void BatchTransformPoints_Translation_ShiftsPoints()
{
var accelerator = new GpuAccelerator(new PerformanceMonitor());
var t = Transform2D.CreateTranslation(10.0, -5.0);

var points = new Vector2[] { new(0, 0), new(1, 1) };
var result = accelerator.BatchTransformPoints(t, points);

Assert.Equal(2, result.Length);
Assert.Equal(10.0, result[0].X, precision: 10);
Assert.Equal(-5.0, result[0].Y, precision: 10);
Assert.Equal(11.0, result[1].X, precision: 10);
Assert.Equal(-4.0, result[1].Y, precision: 10);
}

[Fact]
public void BatchTransformPoints_Scale_ScalesPoints()
{
var accelerator = new GpuAccelerator(new PerformanceMonitor());
var t = Transform2D.CreateScale(2.0, 3.0);

var points = new Vector2[] { new(1, 1), new(2, 2) };
var result = accelerator.BatchTransformPoints(t, points);

Assert.Equal(2.0, result[0].X, precision: 10);
Assert.Equal(3.0, result[0].Y, precision: 10);
Assert.Equal(4.0, result[1].X, precision: 10);
Assert.Equal(6.0, result[1].Y, precision: 10);
}

[Fact]
public void BatchTransformPoints_Rotation90_RotatesPoints()
{
var accelerator = new GpuAccelerator(new PerformanceMonitor());
var t = Transform2D.CreateRotation(Math.PI / 2.0);

var points = new Vector2[] { new(1, 0) };
var result = accelerator.BatchTransformPoints(t, points);

// (1,0) rotated 90 deg CCW -> (0,1)
Assert.Single(result);
Assert.Equal(0.0, result[0].X, precision: 8);
Assert.Equal(1.0, result[0].Y, precision: 8);
}

[Fact]
public void BatchTransformPoints_Empty_ReturnsEmpty()
{
var accelerator = new GpuAccelerator(new PerformanceMonitor());
var result = accelerator.BatchTransformPoints(
Transform2D.Identity,
ReadOnlySpan<Vector2>.Empty);

Assert.Empty(result);
}

[Fact]
public void BatchTransformPoints_MatchesSingleApply()
{
var accelerator = new GpuAccelerator(new PerformanceMonitor());
var t = Transform2D.Multiply(
Transform2D.CreateRotation(0.7),
Transform2D.CreateTranslation(3.0, -2.0));

var points = new Vector2[]
{
new(0, 0), new(1, 0), new(0, 1), new(1, 1), new(-1, -1),
};

var result = accelerator.BatchTransformPoints(t, points);

for (int i = 0; i < points.Length; i++)
{
var expected = t.Apply(points[i]);
Assert.Equal(expected.X, result[i].X, precision: 10);
Assert.Equal(expected.Y, result[i].Y, precision: 10);
}
}

// ------------------------------------------------------------------
//  BatchMinDistanceToPolygonEdge
// ------------------------------------------------------------------

[Fact]
public void BatchMinDistanceToPolygonEdge_ThrowsOnNullPolygon()
{
var accelerator = new GpuAccelerator(new PerformanceMonitor());

Assert.Throws<ArgumentNullException>(() =>
accelerator.BatchMinDistanceToPolygonEdge(null!, ReadOnlySpan<Vector2>.Empty));
}

[Fact]
public void BatchMinDistanceToPolygonEdge_PointOnEdge_ReturnsZero()
{
var accelerator = new GpuAccelerator(new PerformanceMonitor());

// Unit square
var square = new Polygon(new[]
{
new Vector2(0, 0), new Vector2(1, 0),
new Vector2(1, 1), new Vector2(0, 1),
});

// Point exactly on the bottom edge midpoint
var pts = new Vector2[] { new(0.5, 0.0) };
var result = accelerator.BatchMinDistanceToPolygonEdge(square, pts);

Assert.Single(result);
Assert.Equal(0.0, result[0], precision: 10);
}

[Fact]
public void BatchMinDistanceToPolygonEdge_InsidePoint_HasPositiveDistance()
{
var accelerator = new GpuAccelerator(new PerformanceMonitor());

// Square from (0,0) to (2,2)
var square = new Polygon(new[]
{
new Vector2(0, 0), new Vector2(2, 0),
new Vector2(2, 2), new Vector2(0, 2),
});

// Centre point - distance to each edge is 1.0
var pts = new Vector2[] { new(1, 1) };
var result = accelerator.BatchMinDistanceToPolygonEdge(square, pts);

Assert.Single(result);
Assert.Equal(1.0, result[0], precision: 10);
}

[Fact]
public void BatchMinDistanceToPolygonEdge_OutsidePoint_DistanceToNearestEdge()
{
var accelerator = new GpuAccelerator(new PerformanceMonitor());

// Unit square
var square = new Polygon(new[]
{
new Vector2(0, 0), new Vector2(1, 0),
new Vector2(1, 1), new Vector2(0, 1),
});

// Point directly above the top edge at y=3
var pts = new Vector2[] { new(0.5, 3.0) };
var result = accelerator.BatchMinDistanceToPolygonEdge(square, pts);

Assert.Single(result);
Assert.Equal(2.0, result[0], precision: 10);
}

[Fact]
public void BatchMinDistanceToPolygonEdge_MultiplePts_AllCorrect()
{
var accelerator = new GpuAccelerator(new PerformanceMonitor());

// 2x2 square centred at origin: corners at (+/-1, +/-1)
var square = new Polygon(new[]
{
new Vector2(-1, -1), new Vector2(1, -1),
new Vector2(1,  1),  new Vector2(-1, 1),
});

var pts = new Vector2[]
{
new(0,  0),   // centre, dist = 1.0 to each edge
new(2,  0),   // right of square, dist = 1.0
new(0, -3),   // below square, dist = 2.0
};

var result = accelerator.BatchMinDistanceToPolygonEdge(square, pts);

Assert.Equal(3, result.Length);
Assert.Equal(1.0, result[0], precision: 10);
Assert.Equal(1.0, result[1], precision: 10);
Assert.Equal(2.0, result[2], precision: 10);
}

[Fact]
public void BatchMinDistanceToPolygonEdge_EmptyQueryPoints_ReturnsEmpty()
{
var accelerator = new GpuAccelerator(new PerformanceMonitor());
var square = new Polygon(new[]
{
new Vector2(0, 0), new Vector2(1, 0),
new Vector2(1, 1), new Vector2(0, 1),
});

var result = accelerator.BatchMinDistanceToPolygonEdge(
square, ReadOnlySpan<Vector2>.Empty);

Assert.Empty(result);
}

// ------------------------------------------------------------------
//  PerformanceMonitor integration
// ------------------------------------------------------------------

[Fact]
public void AllOperations_RecordMetrics()
{
var monitor = new PerformanceMonitor();
var accel   = new GpuAccelerator(monitor);
var pts     = new Vector2[] { new(0, 0) };
var square  = new Polygon(new[]
{
new Vector2(0, 0), new Vector2(1, 0),
new Vector2(1, 1), new Vector2(0, 1),
});

accel.BatchDistanceSquared(pts, pts);
accel.BatchDistance(pts, pts);
accel.BatchPointInPolygon(square, pts);
accel.BatchTransformPoints(Transform2D.Identity, pts);
accel.BatchMinDistanceToPolygonEdge(square, pts);

var names = monitor.GetAllMetrics()
.Select(m => m.OperationName)
.OrderBy(n => n)
.ToList();

Assert.Contains("BatchDistance",                names);
Assert.Contains("BatchDistanceSquared",         names);
Assert.Contains("BatchMinDistanceToPolygonEdge", names);
Assert.Contains("BatchPointInPolygon",          names);
Assert.Contains("BatchTransformPoints",         names);
}
}
