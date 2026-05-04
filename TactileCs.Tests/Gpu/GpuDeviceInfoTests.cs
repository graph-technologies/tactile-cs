using TactileCs.Gpu;

namespace TactileCs.Tests.Gpu;

public class GpuDeviceInfoTests
{
	// ------------------------------------------------------------------
	//  Factory helper
	// ------------------------------------------------------------------

	/// <summary>
	/// Creates a <see cref="GpuDeviceInfo"/> via its internal constructor
	/// using reflection so the tests remain independent of any static
	/// factory methods.
	/// </summary>
	private static GpuDeviceInfo Make(
		int index, string name, int major, int minor,
		int smCount = 4, long mem = 4L * 1024 * 1024 * 1024, int driver = 12040)
	{
		// Use the internal constructor directly (same assembly)
		return new GpuDeviceInfo(index, name, major, minor, smCount, mem, driver);
	}

	// ------------------------------------------------------------------
	//  MeetsMinimumRequirements
	// ------------------------------------------------------------------

	[Fact]
	public void MeetsMinimumRequirements_ReturnsFalse_ForCC30()
	{
		var info = Make(0, "Test GPU", 3, 0);
		Assert.False(info.MeetsMinimumRequirements);
	}

	[Fact]
	public void MeetsMinimumRequirements_ReturnsFalse_ForCC21()
	{
		var info = Make(0, "Test GPU", 2, 1);
		Assert.False(info.MeetsMinimumRequirements);
	}

	[Fact]
	public void MeetsMinimumRequirements_ReturnsTrue_ForCC35()
	{
		var info = Make(0, "Test GPU", 3, 5);
		Assert.True(info.MeetsMinimumRequirements);
	}

	[Fact]
	public void MeetsMinimumRequirements_ReturnsTrue_ForCC60()
	{
		var info = Make(0, "Test GPU", 6, 0);
		Assert.True(info.MeetsMinimumRequirements);
	}

	[Fact]
	public void MeetsMinimumRequirements_ReturnsTrue_ForCC86()
	{
		var info = Make(0, "NVIDIA RTX 3080", 8, 6);
		Assert.True(info.MeetsMinimumRequirements);
	}

	// ------------------------------------------------------------------
	//  DriverMeetsMinimumRequirements
	// ------------------------------------------------------------------

	[Fact]
	public void DriverMeetsMinimumRequirements_ReturnsFalse_ForOldDriver()
	{
		// Driver 10.2 → encoded as 10020
		var info = Make(0, "Test GPU", 8, 6, driver: 10020);
		Assert.False(info.DriverMeetsMinimumRequirements);
	}

	[Fact]
	public void DriverMeetsMinimumRequirements_ReturnsTrue_ForMinDriver()
	{
		var info = Make(0, "Test GPU", 8, 6, driver: GpuRequirements.MinDriverVersion);
		Assert.True(info.DriverMeetsMinimumRequirements);
	}

	[Fact]
	public void DriverMeetsMinimumRequirements_ReturnsTrue_ForNewerDriver()
	{
		// Driver 12.6 → 12060
		var info = Make(0, "Test GPU", 8, 6, driver: 12060);
		Assert.True(info.DriverMeetsMinimumRequirements);
	}

	// ------------------------------------------------------------------
	//  ComputeCapability string
	// ------------------------------------------------------------------

	[Theory]
	[InlineData(3, 5, "3.5")]
	[InlineData(8, 6, "8.6")]
	[InlineData(10, 0, "10.0")]
	public void ComputeCapabilityString_FormatsCorrectly(int major, int minor, string expected)
	{
		var info = Make(0, "GPU", major, minor);
		Assert.Equal(expected, info.ComputeCapability);
	}

	// ------------------------------------------------------------------
	//  DriverVersionString
	// ------------------------------------------------------------------

	[Theory]
	[InlineData(11040, "11.4")]
	[InlineData(12040, "12.4")]
	[InlineData(10020, "10.2")]
	public void DriverVersionString_FormatsCorrectly(int encoded, string expected)
	{
		var info = Make(0, "GPU", 8, 6, driver: encoded);
		Assert.Equal(expected, info.DriverVersionString);
	}

	// ------------------------------------------------------------------
	//  TotalMemoryMiB
	// ------------------------------------------------------------------

	[Fact]
	public void TotalMemoryMiB_Computes4GiB()
	{
		long bytes = 4L * 1024 * 1024 * 1024;
		var info = Make(0, "GPU", 8, 6, mem: bytes);
		Assert.Equal(4096.0, info.TotalMemoryMiB, precision: 3);
	}

	[Fact]
	public void TotalMemoryMiB_Computes10GiB()
	{
		long bytes = 10L * 1024 * 1024 * 1024;
		var info = Make(0, "GPU", 8, 6, mem: bytes);
		Assert.Equal(10240.0, info.TotalMemoryMiB, precision: 3);
	}

	// ------------------------------------------------------------------
	//  ToString / diagnostics
	// ------------------------------------------------------------------

	[Fact]
	public void ToString_ContainsNameAndCC()
	{
		var info = Make(0, "NVIDIA RTX 3080", 8, 6);
		string str = info.ToString();
		Assert.Contains("NVIDIA RTX 3080", str);
		Assert.Contains("8.6", str);
	}

	[Fact]
	public void ToString_ContainsDeviceIndex()
	{
		var info = Make(2, "GPU", 8, 6);
		Assert.Contains("[2]", info.ToString());
	}

	[Fact]
	public void ToDiagnosticString_ContainsWarning_WhenBelowMinCC()
	{
		var info = Make(0, "Old GPU", 3, 0);
		string diag = info.ToDiagnosticString();
		Assert.Contains("WARNING", diag);
	}

	[Fact]
	public void ToDiagnosticString_ContainsOk_WhenMeetsCC()
	{
		var info = Make(0, "Modern GPU", 8, 6);
		string diag = info.ToDiagnosticString();
		// Compute capability line should say OK
		Assert.Contains("OK", diag);
	}

	// ------------------------------------------------------------------
	//  Properties round-trip
	// ------------------------------------------------------------------

	[Fact]
	public void Properties_RetainConstructorValues()
	{
		var info = Make(3, "My GPU", 7, 5, smCount: 68, mem: 8L * 1024 * 1024 * 1024, driver: 12040);

		Assert.Equal(3,     info.DeviceIndex);
		Assert.Equal("My GPU", info.Name);
		Assert.Equal(7,     info.ComputeCapabilityMajor);
		Assert.Equal(5,     info.ComputeCapabilityMinor);
		Assert.Equal(68,    info.MultiprocessorCount);
		Assert.Equal(8L * 1024 * 1024 * 1024, info.TotalMemoryBytes);
		Assert.Equal(12040, info.DriverVersion);
	}

	// ------------------------------------------------------------------
	//  GpuAccelerator integration
	// ------------------------------------------------------------------

	[Fact]
	public void GetAllDeviceInfos_ReturnsEmpty_WhenNoGpu()
	{
		// CI has no CUDA GPU.
		var devices = GpuAccelerator.GetAllDeviceInfos();
		Assert.Empty(devices);
	}

	[Fact]
	public void TryGetDeviceInfo_ReturnsFalse_WhenNoGpu()
	{
		bool result = GpuAccelerator.TryGetDeviceInfo(0, out var info);
		Assert.False(result);
		Assert.Null(info);
	}
}
