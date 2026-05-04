using TactileCs.Geometry;

namespace TactileCs.Tests.Geometry;

public class Transform2DTests
{
    const double Eps = 1e-10;

    // ── Identity ─────────────────────────────────────────────────────────────

    [Fact]
    public void Identity_LeavesPointUnchanged()
    {
        var p = new Vector2(3, 5);
        Assert.True(p.AlmostEquals(Transform2D.Identity.Apply(p)));
    }

    [Fact]
    public void Identity_HasDeterminantOne()
    {
        Assert.Equal(1.0, Transform2D.Identity.Determinant, 10);
    }

    // ── Apply ────────────────────────────────────────────────────────────────

    [Fact]
    public void Apply_TranslationMoves_Point()
    {
        var t = Transform2D.CreateTranslation(2, 3);
        var result = t.Apply(new Vector2(1, 1));
        Assert.Equal(3.0, result.X, 10);
        Assert.Equal(4.0, result.Y, 10);
    }

    [Fact]
    public void Apply_Rotation90_RotatesUnitX()
    {
        var t = Transform2D.CreateRotation(Math.PI / 2.0);
        var result = t.Apply(Vector2.UnitX);
        Assert.Equal(0.0, result.X, 10);
        Assert.Equal(1.0, result.Y, 10);
    }

    [Fact]
    public void Apply_UniformScale_ScalesPoint()
    {
        var t = Transform2D.CreateScale(3.0);
        var result = t.Apply(new Vector2(2, 4));
        Assert.Equal(6.0, result.X, 10);
        Assert.Equal(12.0, result.Y, 10);
    }

    // ── CreateTranslation ────────────────────────────────────────────────────

    [Fact]
    public void CreateTranslation_VectorOverload_Equivalent()
    {
        var t1 = Transform2D.CreateTranslation(5, 7);
        var t2 = Transform2D.CreateTranslation(new Vector2(5, 7));
        var p = new Vector2(1, 2);
        Assert.True(t1.Apply(p).AlmostEquals(t2.Apply(p)));
    }

    // ── CreateRotation ───────────────────────────────────────────────────────

    [Fact]
    public void CreateRotation_FullCircle_ReturnsToStart()
    {
        var t = Transform2D.CreateRotation(2 * Math.PI);
        var p = new Vector2(3, 5);
        Assert.True(p.AlmostEquals(t.Apply(p)));
    }

    [Fact]
    public void CreateRotation_AboutCenter_LeavesCenter()
    {
        var center = new Vector2(2, 3);
        var t = Transform2D.CreateRotation(Math.PI / 4.0, center);
        Assert.True(center.AlmostEquals(t.Apply(center)));
    }

    // ── CreateScale ──────────────────────────────────────────────────────────

    [Fact]
    public void CreateScale_NonUniform_ScalesEachAxis()
    {
        var t = Transform2D.CreateScale(2, 3);
        var result = t.Apply(new Vector2(1, 1));
        Assert.Equal(2.0, result.X, 10);
        Assert.Equal(3.0, result.Y, 10);
    }

    [Fact]
    public void CreateScale_AboutCenter_LeavesCenter()
    {
        var center = new Vector2(5, 5);
        var t = Transform2D.CreateScale(2, 2, center);
        Assert.True(center.AlmostEquals(t.Apply(center)));
    }

    // ── CreateShear ──────────────────────────────────────────────────────────

    [Fact]
    public void CreateShear_ShiftsX()
    {
        var t = Transform2D.CreateShear(2, 0);
        var result = t.Apply(new Vector2(1, 3));
        Assert.Equal(7.0, result.X, 10); // 1 + 2*3 = 7
        Assert.Equal(3.0, result.Y, 10);
    }

    // ── Multiply ─────────────────────────────────────────────────────────────

    [Fact]
    public void Multiply_TranslatesTwice()
    {
        var t1 = Transform2D.CreateTranslation(1, 0);
        var t2 = Transform2D.CreateTranslation(0, 2);
        var composed = Transform2D.Multiply(t2, t1);
        var result = composed.Apply(Vector2.Zero);
        Assert.Equal(1.0, result.X, 10);
        Assert.Equal(2.0, result.Y, 10);
    }

    [Fact]
    public void Multiply_IdentityIsNeutralElement()
    {
        var t = Transform2D.CreateRotation(0.7);
        var p = new Vector2(3, 4);
        Assert.True(t.Apply(p).AlmostEquals(Transform2D.Multiply(t, Transform2D.Identity).Apply(p)));
        Assert.True(t.Apply(p).AlmostEquals(Transform2D.Multiply(Transform2D.Identity, t).Apply(p)));
    }

    // ── Inverse ──────────────────────────────────────────────────────────────

    [Fact]
    public void Inverse_ThenApply_ReturnsOriginalPoint()
    {
        var t = Transform2D.CreateRotation(1.23);
        var inv = t.Inverse();
        var p = new Vector2(3, 5);
        var roundTripped = inv.Apply(t.Apply(p));
        Assert.Equal(p.X, roundTripped.X, 10);
        Assert.Equal(p.Y, roundTripped.Y, 10);
    }

    [Fact]
    public void Inverse_Translation_MovesBack()
    {
        var t = Transform2D.CreateTranslation(3, 4);
        var inv = t.Inverse();
        var p = new Vector2(1, 1);
        Assert.True(p.AlmostEquals(inv.Apply(t.Apply(p))));
    }

    [Fact]
    public void Inverse_SingularMatrix_ThrowsInvalidOperation()
    {
        var singular = new Transform2D(0, 0, 0, 0, 0, 0);
        Assert.Throws<InvalidOperationException>(() => singular.Inverse());
    }

    // ── Determinant ──────────────────────────────────────────────────────────

    [Fact]
    public void Determinant_Rotation_IsOne()
    {
        var t = Transform2D.CreateRotation(Math.PI / 6.0);
        Assert.Equal(1.0, t.Determinant, 10);
    }

    [Fact]
    public void Determinant_Scale2_IsFour()
    {
        var t = Transform2D.CreateScale(2);
        Assert.Equal(4.0, t.Determinant, 10);
    }

    // ── ToString ─────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_ContainsTransformKeyword()
    {
        Assert.Contains("Transform", Transform2D.Identity.ToString());
    }
}
