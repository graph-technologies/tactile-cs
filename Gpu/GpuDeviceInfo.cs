namespace TactileCs.Gpu;

/// <summary>
/// Hardware and driver requirements for TactileCs GPU operations.
/// </summary>
public static class GpuRequirements
{
	/// <summary>Minimum CUDA Compute Capability major version (3 for CC 3.5).</summary>
	public const int MinComputeMajor = 3;

	/// <summary>Minimum CUDA Compute Capability minor version (5 for CC 3.5).</summary>
	public const int MinComputeMinor = 5;

	/// <summary>Minimum CUDA Toolkit version string required to build the native library.</summary>
	public const string MinCudaToolkitVersion = "11.0";

	/// <summary>
	/// Minimum CUDA driver version required at runtime, encoded as
	/// <c>major × 1000 + minor × 10</c> (e.g., 11040 → driver 11.4).
	/// </summary>
	public const int MinDriverVersion = 11040;
}

/// <summary>
/// Immutable snapshot of a single CUDA device's hardware properties.
/// <para>
/// Obtain instances via <see cref="GpuAccelerator.GetAllDeviceInfos"/> or
/// <see cref="GpuAccelerator.TryGetDeviceInfo"/>.
/// </para>
/// </summary>
public sealed class GpuDeviceInfo
{
	/// <summary>Zero-based index of the device as reported by the CUDA runtime.</summary>
	public int DeviceIndex { get; }

	/// <summary>Marketing name of the GPU (e.g., <c>"NVIDIA GeForce RTX 3080"</c>).</summary>
	public string Name { get; }

	/// <summary>Major component of the CUDA Compute Capability (e.g., 8 for CC 8.6).</summary>
	public int ComputeCapabilityMajor { get; }

	/// <summary>Minor component of the CUDA Compute Capability (e.g., 6 for CC 8.6).</summary>
	public int ComputeCapabilityMinor { get; }

	/// <summary>Number of streaming multiprocessors on the device.</summary>
	public int MultiprocessorCount { get; }

	/// <summary>Total global memory in bytes.</summary>
	public long TotalMemoryBytes { get; }

	/// <summary>
	/// CUDA driver version encoded as <c>major × 1000 + minor × 10</c>.
	/// For example, <c>12040</c> represents driver version 12.4.
	/// </summary>
	public int DriverVersion { get; }

	// ------------------------------------------------------------------
	//  Derived properties
	// ------------------------------------------------------------------

	/// <summary>Human-readable compute capability string, e.g. <c>"8.6"</c>.</summary>
	public string ComputeCapability => $"{ComputeCapabilityMajor}.{ComputeCapabilityMinor}";

	/// <summary>Total global memory in mebibytes (MiB).</summary>
	public double TotalMemoryMiB => TotalMemoryBytes / (1024.0 * 1024.0);

	/// <summary>
	/// Human-readable CUDA driver version string derived from
	/// <see cref="DriverVersion"/>, e.g. <c>"12.4"</c>.
	/// </summary>
	public string DriverVersionString => FormatDriverVersion(DriverVersion);

	/// <summary>
	/// Returns <c>true</c> when this device meets the minimum requirements
	/// for TactileCs GPU operations (Compute Capability ≥ 3.5).
	/// </summary>
	public bool MeetsMinimumRequirements =>
		ComputeCapabilityMajor > GpuRequirements.MinComputeMajor ||
		(ComputeCapabilityMajor == GpuRequirements.MinComputeMajor &&
		 ComputeCapabilityMinor >= GpuRequirements.MinComputeMinor);

	/// <summary>
	/// Returns <c>true</c> when the installed CUDA driver meets the minimum
	/// version requirement (<see cref="GpuRequirements.MinDriverVersion"/>).
	/// </summary>
	public bool DriverMeetsMinimumRequirements =>
		DriverVersion >= GpuRequirements.MinDriverVersion;

	// ------------------------------------------------------------------
	//  Construction
	// ------------------------------------------------------------------

	internal GpuDeviceInfo(
		int deviceIndex,
		string name,
		int computeMajor,
		int computeMinor,
		int multiprocessorCount,
		long totalMemBytes,
		int driverVersion)
	{
		DeviceIndex          = deviceIndex;
		Name                 = name;
		ComputeCapabilityMajor = computeMajor;
		ComputeCapabilityMinor = computeMinor;
		MultiprocessorCount  = multiprocessorCount;
		TotalMemoryBytes     = totalMemBytes;
		DriverVersion        = driverVersion;
	}

	// ------------------------------------------------------------------
	//  Diagnostics
	// ------------------------------------------------------------------

	/// <summary>
	/// Returns a one-line description of the device, its compute capability,
	/// available memory, and driver version. Example:
	/// <code>
	/// [0] NVIDIA GeForce RTX 3080 | CC 8.6 | 10240 MiB | 64 SMs | Driver 12.4
	/// </code>
	/// </summary>
	public override string ToString() =>
		$"[{DeviceIndex}] {Name} | " +
		$"CC {ComputeCapability} | " +
		$"{TotalMemoryMiB:F0} MiB | " +
		$"{MultiprocessorCount} SMs | " +
		$"Driver {DriverVersionString}";

	/// <summary>
	/// Returns a multi-line diagnostic string that includes requirement
	/// checks and a warning when the device does not meet the minimum
	/// requirements for TactileCs.
	/// </summary>
	public string ToDiagnosticString()
	{
		string minDriver = FormatDriverVersion(GpuRequirements.MinDriverVersion);
		string minCC = $"{GpuRequirements.MinComputeMajor}.{GpuRequirements.MinComputeMinor}";

		var sb = new System.Text.StringBuilder();
		sb.AppendLine(ToString());
		sb.AppendLine($"  Compute Capability : {ComputeCapability} " +
			(MeetsMinimumRequirements
				? $"(OK – requires >= {minCC})"
				: $"(WARNING: requires >= {minCC})"));
		sb.AppendLine($"  Driver Version     : {DriverVersionString} " +
			(DriverMeetsMinimumRequirements
				? $"(OK – requires >= {minDriver})"
				: $"(WARNING: requires >= {minDriver})"));
		sb.Append($"  Total Memory       : {TotalMemoryMiB:F0} MiB");
		return sb.ToString();
	}

	/// <summary>
	/// Formats a CUDA driver version integer (e.g., 12040) as a human-readable
	/// string (e.g., <c>"12.4"</c>).
	/// </summary>
	internal static string FormatDriverVersion(int encodedVersion) =>
		$"{encodedVersion / 1000}.{(encodedVersion % 1000) / 10}";
}
