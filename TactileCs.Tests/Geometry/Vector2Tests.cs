using TactileCs.Geometry;

namespace TactileCs.Tests.Geometry;

public class Vector2Tests
{
    // ── Construction & properties ───────────────────────────────────────────

    [Fact]
    public void Constructor_SetsComponents()
    {
        var v = new Vector2(3.0, 4.0);
        Assert.Equal(3.0, v.X);
        Assert.Equal(4.0, v.Y);
    }

    [Fact]
    public void Zero_HasZeroComponents()
    {
        Assert.Equal(0.0, Vector2.Zero.X);
        Assert.Equal(0.0, Vector2.Zero.Y);
    }

    [Fact]
    public void One_HasOneComponents()
    {
        Assert.Equal(1.0, Vector2.One.X);
        Assert.Equal(1.0, Vector2.One.Y);
    }

    [Fact]
    public void UnitX_IsCorrect()
    {
        Assert.Equal(new Vector2(1, 0), Vector2.UnitX);
    }

    [Fact]
    public void UnitY_IsCorrect()
    {
        Assert.Equal(new Vector2(0, 1), Vector2.UnitY);
    }

    // ── Length ──────────────────────────────────────────────────────────────

    [Fact]
    public void Length_Computes345Triangle()
    {
        Assert.Equal(5.0, new Vector2(3.0, 4.0).Length, 10);
    }

    [Fact]
    public void LengthSquared_IsSquareOfLength()
    {
        var v = new Vector2(3.0, 4.0);
        Assert.Equal(v.Length * v.Length, v.LengthSquared, 10);
    }

    [Fact]
    public void Length_ZeroVector_IsZero()
    {
        Assert.Equal(0.0, Vector2.Zero.Length);
    }

    // ── Arithmetic operators ────────────────────────────────────────────────

    [Fact]
    public void Add_ReturnsComponentSum()
    {
        Assert.Equal(new Vector2(5, 7), new Vector2(2, 3) + new Vector2(3, 4));
    }

    [Fact]
    public void Subtract_ReturnsComponentDifference()
    {
        Assert.Equal(new Vector2(1, 2), new Vector2(4, 5) - new Vector2(3, 3));
    }

    [Fact]
    public void Negate_FlipsSign()
    {
        Assert.Equal(new Vector2(-1, -2), -new Vector2(1, 2));
    }

    [Fact]
    public void MultiplyByScalar_ScalesComponents()
    {
        Assert.Equal(new Vector2(6, 10), new Vector2(3, 5) * 2.0);
    }

    [Fact]
    public void ScalarMultiply_IsCommutative()
    {
        var v = new Vector2(3, 5);
        Assert.Equal(v * 2.0, 2.0 * v);
    }

    [Fact]
    public void DivideByScalar_ScalesComponents()
    {
        Assert.Equal(new Vector2(1.5, 2.5), new Vector2(3, 5) / 2.0);
    }

    // ── Dot & Cross ─────────────────────────────────────────────────────────

    [Fact]
    public void Dot_PerpendicularVectors_IsZero()
    {
        Assert.Equal(0.0, new Vector2(1, 0).Dot(new Vector2(0, 1)), 10);
    }

    [Fact]
    public void Dot_ParallelVectors_IsProductOfLengths()
    {
        Assert.Equal(9.0, new Vector2(3, 0).Dot(new Vector2(3, 0)));
    }

    [Fact]
    public void StaticDot_MatchesInstanceDot()
    {
        var a = new Vector2(2, 3);
        var b = new Vector2(4, 5);
        Assert.Equal(a.Dot(b), Vector2.Dot(a, b));
    }

    [Fact]
    public void Cross_PerpendicularUnitVectors_IsOne()
    {
        Assert.Equal(1.0, Vector2.UnitX.Cross(Vector2.UnitY), 10);
    }

    [Fact]
    public void Cross_AntiCommutative()
    {
        var a = new Vector2(2, 3);
        var b = new Vector2(5, 7);
        Assert.Equal(-a.Cross(b), b.Cross(a), 10);
    }

    // ── Normalized ──────────────────────────────────────────────────────────

    [Fact]
    public void Normalized_UnitLength()
    {
        var v = new Vector2(3, 4).Normalized();
        Assert.Equal(1.0, v.Length, 10);
    }

    [Fact]
    public void Normalized_ZeroVector_ReturnsZero()
    {
        Assert.Equal(Vector2.Zero, Vector2.Zero.Normalized());
    }

    // ── Perpendicular ───────────────────────────────────────────────────────

    [Fact]
    public void Perpendicular_DotIsZero()
    {
        var v = new Vector2(3, 5);
        Assert.Equal(0.0, v.Dot(v.Perpendicular()), 10);
    }

    // ── Angle ───────────────────────────────────────────────────────────────

    [Fact]
    public void Angle_UnitX_IsZero()
    {
        Assert.Equal(0.0, Vector2.UnitX.Angle(), 10);
    }

    [Fact]
    public void Angle_UnitY_IsHalfPi()
    {
        Assert.Equal(Math.PI / 2.0, Vector2.UnitY.Angle(), 10);
    }

    // ── Lerp ────────────────────────────────────────────────────────────────

    [Fact]
    public void Lerp_AtZero_ReturnsStart()
    {
        var a = new Vector2(1, 2);
        var b = new Vector2(5, 6);
        Assert.True(a.AlmostEquals(a.Lerp(b, 0.0)));
    }

    [Fact]
    public void Lerp_AtOne_ReturnsEnd()
    {
        var a = new Vector2(1, 2);
        var b = new Vector2(5, 6);
        Assert.True(b.AlmostEquals(a.Lerp(b, 1.0)));
    }

    [Fact]
    public void Lerp_AtHalf_ReturnsMidpoint()
    {
        var a = new Vector2(0, 0);
        var b = new Vector2(4, 6);
        var mid = a.Lerp(b, 0.5);
        Assert.True(new Vector2(2, 3).AlmostEquals(mid));
    }

    // ── Distance ────────────────────────────────────────────────────────────

    [Fact]
    public void Distance_Static_MatchesLength()
    {
        var a = new Vector2(1, 1);
        var b = new Vector2(4, 5);
        Assert.Equal((b - a).Length, Vector2.Distance(a, b), 10);
    }

    [Fact]
    public void DistanceSquared_Static_MatchesLengthSquared()
    {
        var a = new Vector2(1, 1);
        var b = new Vector2(4, 5);
        Assert.Equal((b - a).LengthSquared, Vector2.DistanceSquared(a, b), 10);
    }

    // ── Reflect ─────────────────────────────────────────────────────────────

    [Fact]
    public void Reflect_OffXAxis_FlipsY()
    {
        var v = new Vector2(3, 4);
        var reflected = v.Reflect(Vector2.UnitY);
        Assert.Equal(3.0, reflected.X, 10);
        Assert.Equal(-4.0, reflected.Y, 10);
    }

    // ── Rotate ──────────────────────────────────────────────────────────────

    [Fact]
    public void Rotate_ByHalfPi_RotatesUnitX_To_UnitY()
    {
        var rotated = Vector2.UnitX.Rotate(Math.PI / 2.0);
        Assert.Equal(0.0, rotated.X, 10);
        Assert.Equal(1.0, rotated.Y, 10);
    }

    // ── Equality ────────────────────────────────────────────────────────────

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        Assert.Equal(new Vector2(1, 2), new Vector2(1, 2));
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        Assert.NotEqual(new Vector2(1, 2), new Vector2(1, 3));
    }

    [Fact]
    public void EqualityOperator_Works()
    {
        Assert.True(new Vector2(1, 2) == new Vector2(1, 2));
        Assert.False(new Vector2(1, 2) == new Vector2(1, 3));
    }

    [Fact]
    public void AlmostEquals_WithinEpsilon_ReturnsTrue()
    {
        var a = new Vector2(1.0, 2.0);
        var b = new Vector2(1.0 + 1e-10, 2.0 - 1e-10);
        Assert.True(a.AlmostEquals(b));
    }

    [Fact]
    public void AlmostEquals_OutsideEpsilon_ReturnsFalse()
    {
        var a = new Vector2(1.0, 2.0);
        var b = new Vector2(1.0 + 1e-5, 2.0);
        Assert.False(a.AlmostEquals(b));
    }

    // ── GetHashCode ──────────────────────────────────────────────────────────

    [Fact]
    public void GetHashCode_EqualVectors_SameHash()
    {
        Assert.Equal(new Vector2(1, 2).GetHashCode(), new Vector2(1, 2).GetHashCode());
    }

    // ── ToString ─────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_ContainsComponents()
    {
        var s = new Vector2(1.5, 2.5).ToString();
        Assert.Contains("1.500000", s);
        Assert.Contains("2.500000", s);
    }
}
