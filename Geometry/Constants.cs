namespace TactileCs.Geometry;


/// <summary>
/// Global geometry settings and constants. Provides default and runtime-configurable tolerances
/// used throughout the geometry library (positional, angular, relative, etc.).
/// </summary>
public static class Constants {

	/// <summary>
	/// Generic default comparison epsilon for geometry operations. Use this for general component-wise comparisons.
	/// </summary>
	public const double DefaultEpsilon = 1e-9;

	/// <summary>
	/// Generic runtime-configurable epsilon used by geometry comparisons. Initialized to <see cref="DefaultEpsilon"/>.
	/// </summary>
	public static double Epsilon { get; set; } = DefaultEpsilon;

	/// <summary>
	/// Default positional/distance tolerance (in world units). Useful for point equality and snapping operations.
	/// </summary>
	public const double PositionEpsilonDefault = 1e-9;

	/// <summary>
	/// Runtime-configurable positional/distance tolerance (in world units).
	/// </summary>
	public static double PositionEpsilon { get; set; } = PositionEpsilonDefault;

	/// <summary>
	/// Default distance tolerance (alias for positional tolerance). Provided for clarity in APIs that operate on lengths.
	/// </summary>
	public const double DistanceEpsilonDefault = PositionEpsilonDefault;

	/// <summary>
	/// Runtime-configurable distance tolerance (in world units).
	/// </summary>
	public static double DistanceEpsilon { get; set; } = DistanceEpsilonDefault;

	/// <summary>
	/// Default angular tolerance expressed in radians. Useful for angle comparisons and angular snapping.
	/// </summary>
	public const double AngleEpsilonRadiansDefault = 1e-6; // ~0.000057 degrees

	/// <summary>
	/// Runtime-configurable angular tolerance (radians).
	/// </summary>
	public static double AngleEpsilonRadians { get; set; } = AngleEpsilonRadiansDefault;

	/// <summary>
	/// Angular tolerance in degrees, derived from <see cref="AngleEpsilonRadians"/>.
	/// </summary>
	public static double AngleEpsilonDegrees {
		get {
			return AngleEpsilonRadians * (180.0 / Math.PI);
		}
	}

	/// <summary>
	/// Default relative tolerance used for relative comparisons (e.g., comparing values with scale-dependent error).
	/// </summary>
	public const double RelativeEpsilonDefault = 1e-12;

	/// <summary>
	/// Runtime-configurable relative tolerance.
	/// </summary>
	public static double RelativeEpsilon { get; set; } = RelativeEpsilonDefault;

	/// <summary>
	/// Default area tolerance (squared units) for tests that operate on areas or squared distances.
	/// </summary>
	public const double AreaEpsilonDefault = 1e-12;

	/// <summary>
	/// Runtime-configurable area tolerance (squared units).
	/// </summary>
	public static double AreaEpsilon { get; set; } = AreaEpsilonDefault;

	/// <summary>
	/// Small epsilon used for numerical stability checks in matrix operations, determinants, and geometric calculations.
	/// </summary>
	public const double NumericalEpsilon = 1e-10;


	/// <summary>
	/// Resets all runtime-configurable tolerances to their default values.
	/// </summary>
	public static void ResetToDefaults() {
		Epsilon             = DefaultEpsilon;
		PositionEpsilon     = PositionEpsilonDefault;
		DistanceEpsilon     = DistanceEpsilonDefault;
		AngleEpsilonRadians = AngleEpsilonRadiansDefault;
		RelativeEpsilon     = RelativeEpsilonDefault;
		AreaEpsilon         = AreaEpsilonDefault;
	}

}