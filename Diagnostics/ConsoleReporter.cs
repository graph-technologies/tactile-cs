namespace TactileCs.Diagnostics;

/// <summary>
/// Reusable CLI presentation helpers for TactileCs applications.
/// <para>
/// All methods write to <see cref="Console.Out"/> by default; pass a
/// <see cref="TextWriter"/> to redirect output (useful in tests).
/// </para>
/// </summary>
public static class ConsoleReporter
{
	private const int DefaultTableWidth = 72;

	// ------------------------------------------------------------------
	//  Section headings
	// ------------------------------------------------------------------

	/// <summary>
	/// Writes a section heading surrounded by horizontal rules.
	/// </summary>
	/// <example>
	/// <code>
	/// ConsoleReporter.WriteSection("Performance Metrics");
	/// // ────────────────────────────────────────────────────────
	/// //  Performance Metrics
	/// // ────────────────────────────────────────────────────────
	/// </code>
	/// </example>
	/// <param name="heading">The heading text.</param>
	/// <param name="output">
	/// Output writer; defaults to <see cref="Console.Out"/> when <c>null</c>.
	/// </param>
	public static void WriteSection(string heading, TextWriter? output = null)
	{
		var w = output ?? Console.Out;
		var rule = new string('─', DefaultTableWidth);
		w.WriteLine(rule);
		w.WriteLine($" {heading}");
		w.WriteLine(rule);
	}

	// ------------------------------------------------------------------
	//  Status lines
	// ------------------------------------------------------------------

	/// <summary>
	/// Writes a dot-leader status line:
	/// <c>  Label .......... Value</c>
	/// </summary>
	/// <param name="label">The label on the left.</param>
	/// <param name="value">The value on the right.</param>
	/// <param name="output">
	/// Output writer; defaults to <see cref="Console.Out"/> when <c>null</c>.
	/// </param>
	public static void WriteStatus(string label, string value, TextWriter? output = null)
	{
		var w = output ?? Console.Out;
		const int labelWidth = 28;
		const int totalWidth = DefaultTableWidth;

		string paddedLabel = label.Length >= labelWidth
			? label[..(labelWidth - 1)] + "…"
			: label + ' ' + new string('.', labelWidth - label.Length - 1);

		int valueSpace = totalWidth - paddedLabel.Length - 2;
		string paddedValue = value.Length > valueSpace
			? value[..(valueSpace - 1)] + "…"
			: value.PadLeft(valueSpace);

		w.WriteLine($"  {paddedLabel} {paddedValue}");
	}

	// ------------------------------------------------------------------
	//  Metrics table
	// ------------------------------------------------------------------

	/// <summary>
	/// Writes a formatted table of <see cref="OperationMetrics"/> to the
	/// output writer.
	/// </summary>
	/// <param name="metrics">The metrics to display.</param>
	/// <param name="output">
	/// Output writer; defaults to <see cref="Console.Out"/> when <c>null</c>.
	/// </param>
	public static void WriteMetricsTable(
		IEnumerable<OperationMetrics> metrics,
		TextWriter? output = null)
	{
		var w = output ?? Console.Out;
		var rows = metrics.ToList();

		string header = $"  {"Operation",-28}  {"Count",7}  {"Total ms",10}  {"Avg ms",10}  {"Min ms",10}  {"Max ms",10}";
		string rule   = "  " + new string('─', header.Length - 2);

		w.WriteLine(header);
		w.WriteLine(rule);

		if (rows.Count == 0)
		{
			w.WriteLine("  (no metrics recorded)");
			return;
		}

		foreach (var m in rows.OrderBy(x => x.OperationName))
		{
			const int nameW = 28;
			string name = m.OperationName.Length > nameW
				? m.OperationName[..(nameW - 1)] + "…"
				: m.OperationName;

			w.WriteLine(
				$"  {name,-28}  " +
				$"{m.InvocationCount,7}  " +
				$"{m.TotalDuration.TotalMilliseconds,10:F2}  " +
				$"{m.AverageDuration.TotalMilliseconds,10:F2}  " +
				$"{m.MinDuration.TotalMilliseconds,10:F2}  " +
				$"{m.MaxDuration.TotalMilliseconds,10:F2}");
		}
	}

	// ------------------------------------------------------------------
	//  Progress bar factory
	// ------------------------------------------------------------------

	/// <summary>
	/// Creates a new console progress bar for the given label and total count.
	/// Report progress by calling <see cref="ConsoleProgressBar.Report"/>;
	/// dispose the bar to mark it complete.
	/// </summary>
	/// <param name="label">Short description shown next to the bar.</param>
	/// <param name="total">Total number of steps (must be ≥ 1).</param>
	/// <param name="output">
	/// Output writer; defaults to <see cref="Console.Out"/> when <c>null</c>.
	/// </param>
	/// <returns>A new <see cref="ConsoleProgressBar"/>.</returns>
	public static ConsoleProgressBar CreateProgressBar(
		string label,
		int total,
		TextWriter? output = null)
	{
		return new ConsoleProgressBar(label, total, output);
	}
}

/// <summary>
/// A simple console progress bar that updates in place using a carriage
/// return when writing to the real console, or emits one line per step
/// when redirected.
/// </summary>
/// <remarks>
/// Dispose the bar to finalize the output (appends a newline and marks it
/// 100 % complete).
/// </remarks>
public sealed class ConsoleProgressBar : IProgress<int>, IDisposable
{
	private const int BarWidth = 30;

	private readonly string _label;
	private readonly int _total;
	private readonly TextWriter _output;
	private readonly bool _isInteractive;
	private int _current;
	private bool _disposed;

	/// <summary>
	/// Gets the most recently reported value.
	/// </summary>
	public int Current => _current;

	/// <summary>
	/// Gets the total number of steps supplied at construction.
	/// </summary>
	public int Total => _total;

	/// <summary>
	/// Creates a new <see cref="ConsoleProgressBar"/>.
	/// </summary>
	/// <param name="label">Short description shown next to the bar.</param>
	/// <param name="total">Total number of steps (must be ≥ 1).</param>
	/// <param name="output">
	/// Output writer; defaults to <see cref="Console.Out"/> when <c>null</c>.
	/// </param>
	public ConsoleProgressBar(string label, int total, TextWriter? output = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(label);
		ArgumentOutOfRangeException.ThrowIfLessThan(total, 1);

		_label  = label;
		_total  = total;
		_output = output ?? Console.Out;

		// Use in-place updates only when writing directly to the console
		_isInteractive = ReferenceEquals(_output, Console.Out) &&
		                 !Console.IsOutputRedirected;

		Render(0);
	}

	/// <inheritdoc/>
	public void Report(int value)
	{
		if (_disposed) return;
		_current = Math.Clamp(value, 0, _total);
		Render(_current);
	}

	/// <summary>
	/// Completes the progress bar (sets it to 100 %) and writes a
	/// trailing newline.
	/// </summary>
	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;
		_current  = _total;
		Render(_total, finalise: true);
	}

	// ------------------------------------------------------------------
	//  Internals
	// ------------------------------------------------------------------

	private void Render(int value, bool finalise = false)
	{
		double fraction  = _total > 0 ? (double)value / _total : 0.0;
		int    filled    = (int)Math.Round(fraction * BarWidth);
		int    empty     = BarWidth - filled;
		int    percent   = (int)Math.Round(fraction * 100.0);

		string bar  = new string('█', filled) + new string('░', empty);
		string line = $" {_label,-20} [{bar}] {percent,3}% ({value}/{_total})";

		if (_isInteractive)
		{
			_output.Write('\r');
			_output.Write(line);
			if (finalise) _output.WriteLine();
		}
		else
		{
			// Redirected output: emit one line per render call
			_output.WriteLine(line);
		}
	}
}
