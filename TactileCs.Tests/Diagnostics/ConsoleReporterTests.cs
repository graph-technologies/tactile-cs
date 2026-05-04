using TactileCs.Diagnostics;

namespace TactileCs.Tests.Diagnostics;

public class ConsoleReporterTests
{
	// ------------------------------------------------------------------
	//  WriteSection
	// ------------------------------------------------------------------

	[Fact]
	public void WriteSection_WritesHeadingText()
	{
		var sw = new StringWriter();
		ConsoleReporter.WriteSection("My Section", sw);
		Assert.Contains("My Section", sw.ToString());
	}

	[Fact]
	public void WriteSection_WritesSeparatorLines()
	{
		var sw = new StringWriter();
		ConsoleReporter.WriteSection("Test", sw);
		string output = sw.ToString();
		// Should contain the horizontal rule character
		Assert.Contains("─", output);
	}

	[Fact]
	public void WriteSection_ProducesThreeLines()
	{
		var sw = new StringWriter();
		ConsoleReporter.WriteSection("Heading", sw);
		// Separator / Heading / Separator → 3 non-empty lines
		var lines = sw.ToString()
			.Split('\n', StringSplitOptions.RemoveEmptyEntries);
		Assert.Equal(3, lines.Length);
	}

	// ------------------------------------------------------------------
	//  WriteStatus
	// ------------------------------------------------------------------

	[Fact]
	public void WriteStatus_ContainsLabelAndValue()
	{
		var sw = new StringWriter();
		ConsoleReporter.WriteStatus("My Label", "My Value", sw);
		string output = sw.ToString();
		Assert.Contains("My Label", output);
		Assert.Contains("My Value", output);
	}

	[Fact]
	public void WriteStatus_WritesSingleLine()
	{
		var sw = new StringWriter();
		ConsoleReporter.WriteStatus("Label", "Value", sw);
		var lines = sw.ToString()
			.Split('\n', StringSplitOptions.RemoveEmptyEntries);
		Assert.Single(lines);
	}

	[Fact]
	public void WriteStatus_LongLabel_TruncatesToFit()
	{
		var sw = new StringWriter();
		string longLabel = new string('A', 100);
		// Should not throw
		ConsoleReporter.WriteStatus(longLabel, "V", sw);
		Assert.NotEmpty(sw.ToString());
	}

	// ------------------------------------------------------------------
	//  WriteMetricsTable
	// ------------------------------------------------------------------

	[Fact]
	public void WriteMetricsTable_Empty_WritesNoDataMessage()
	{
		var sw = new StringWriter();
		ConsoleReporter.WriteMetricsTable([], sw);
		Assert.Contains("no metrics", sw.ToString());
	}

	[Fact]
	public void WriteMetricsTable_WritesHeader()
	{
		var sw = new StringWriter();
		ConsoleReporter.WriteMetricsTable([], sw);
		string output = sw.ToString();
		Assert.Contains("Operation", output);
		Assert.Contains("Count", output);
	}

	[Fact]
	public void WriteMetricsTable_WritesOneRowPerMetric()
	{
		var monitor = new PerformanceMonitor();
		using (monitor.BeginOperation("OpAlpha")) { }
		using (monitor.BeginOperation("OpBeta"))  { }

		var sw = new StringWriter();
		ConsoleReporter.WriteMetricsTable(monitor.GetAllMetrics(), sw);
		string output = sw.ToString();

		Assert.Contains("OpAlpha", output);
		Assert.Contains("OpBeta",  output);
	}

	[Fact]
	public void WriteMetricsTable_SortsRowsByName()
	{
		var monitor = new PerformanceMonitor();
		using (monitor.BeginOperation("Zebra"))   { }
		using (monitor.BeginOperation("Apple"))   { }
		using (monitor.BeginOperation("Mango"))   { }

		var sw = new StringWriter();
		ConsoleReporter.WriteMetricsTable(monitor.GetAllMetrics(), sw);
		string output = sw.ToString();

		int idxApple = output.IndexOf("Apple", StringComparison.Ordinal);
		int idxMango = output.IndexOf("Mango", StringComparison.Ordinal);
		int idxZebra = output.IndexOf("Zebra", StringComparison.Ordinal);

		Assert.True(idxApple < idxMango);
		Assert.True(idxMango < idxZebra);
	}

	[Fact]
	public void WriteMetricsTable_LongOperationName_Truncated()
	{
		var monitor = new PerformanceMonitor();
		string longName = new string('X', 60);
		using (monitor.BeginOperation(longName)) { }

		var sw = new StringWriter();
		// Should not throw
		ConsoleReporter.WriteMetricsTable(monitor.GetAllMetrics(), sw);
		Assert.NotEmpty(sw.ToString());
	}

	[Fact]
	public void WriteMetricsTable_DefaultsToConsoleOut()
	{
		// Smoke-test: passing null uses Console.Out and should not throw.
		var monitor = new PerformanceMonitor();
		ConsoleReporter.WriteMetricsTable(monitor.GetAllMetrics(), null);
	}

	// ------------------------------------------------------------------
	//  CreateProgressBar / ConsoleProgressBar
	// ------------------------------------------------------------------

	[Fact]
	public void CreateProgressBar_ReturnsNonNull()
	{
		var sw  = new StringWriter();
		using var pb = ConsoleReporter.CreateProgressBar("Test", 10, sw);
		Assert.NotNull(pb);
	}

	[Fact]
	public void ConsoleProgressBar_InitialOutput_ContainsLabel()
	{
		var sw = new StringWriter();
		using var _ = new ConsoleProgressBar("Loading", 100, sw);
		Assert.Contains("Loading", sw.ToString());
	}

	[Fact]
	public void ConsoleProgressBar_Report_UpdatesCurrentValue()
	{
		var sw = new StringWriter();
		var pb = new ConsoleProgressBar("Work", 50, sw);
		pb.Report(25);
		Assert.Equal(25, pb.Current);
		pb.Dispose();
	}

	[Fact]
	public void ConsoleProgressBar_Report_FullProgress_ShowsHundredPercent()
	{
		var sw = new StringWriter();
		using var pb = new ConsoleProgressBar("Op", 10, sw);
		pb.Report(10);
		Assert.Contains("100%", sw.ToString());
	}

	[Fact]
	public void ConsoleProgressBar_Dispose_SetsCurrentToTotal()
	{
		var sw = new StringWriter();
		var pb = new ConsoleProgressBar("Finish", 20, sw);
		pb.Report(10);
		pb.Dispose();
		Assert.Equal(20, pb.Current);
	}

	[Fact]
	public void ConsoleProgressBar_Report_AfterDispose_IsIgnored()
	{
		var sw = new StringWriter();
		var pb = new ConsoleProgressBar("Test", 10, sw);
		pb.Dispose();

		string outputBeforeExtraReport = sw.ToString();
		pb.Report(5);   // should be a no-op

		Assert.Equal(outputBeforeExtraReport, sw.ToString());
	}

	[Fact]
	public void ConsoleProgressBar_Report_ClampsToTotal()
	{
		var sw = new StringWriter();
		using var pb = new ConsoleProgressBar("Clamp", 10, sw);
		pb.Report(999);
		Assert.Equal(10, pb.Current);
	}

	[Fact]
	public void ConsoleProgressBar_Report_ClampsToZero()
	{
		var sw = new StringWriter();
		using var pb = new ConsoleProgressBar("Clamp", 10, sw);
		pb.Report(-5);
		Assert.Equal(0, pb.Current);
	}

	[Fact]
	public void ConsoleProgressBar_TotalProperty_ReflectsConstructorArg()
	{
		var sw = new StringWriter();
		using var pb = new ConsoleProgressBar("T", 42, sw);
		Assert.Equal(42, pb.Total);
	}

	[Fact]
	public void ConsoleProgressBar_InvalidTotal_ThrowsArgumentOutOfRange()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			new ConsoleProgressBar("X", 0, new StringWriter()));
	}

	[Fact]
	public void ConsoleProgressBar_NullOrEmptyLabel_ThrowsArgumentException()
	{
		Assert.ThrowsAny<ArgumentException>(() =>
			new ConsoleProgressBar("", 10, new StringWriter()));

		Assert.ThrowsAny<ArgumentException>(() =>
			new ConsoleProgressBar("   ", 10, new StringWriter()));
	}
}
