# Diagnostics

Logging and performance-monitoring infrastructure for TactileCs.

## Overview

| Type | Purpose |
|------|---------|
| `TactileLogger` | Static facade that wires TactileCs into any `Microsoft.Extensions.Logging` provider (console, Serilog, NLog, etc.). No-op by default. |
| `PerformanceMonitor` | Thread-safe collector of operation-level timing metrics. |
| `OperationTimer` | Disposable stopwatch returned by `PerformanceMonitor.BeginOperation()`. |
| `OperationMetrics` | Immutable snapshot of timing stats (count, total, min, max, average). |

## Quick start

### Logging

```csharp
using Microsoft.Extensions.Logging;
using TactileCs.Diagnostics;

// Wire up at application start-up:
using var factory = LoggerFactory.Create(b => b.AddConsole());
TactileLogger.Configure(factory);

// Library classes now emit structured logs.
```

### Performance monitoring

```csharp
var monitor = PerformanceMonitor.Default;

using (monitor.BeginOperation("BuildGraph"))
{
    // expensive work …
}

foreach (var m in monitor.GetAllMetrics())
    Console.WriteLine(m);
```

## ConsoleReporter

`ConsoleReporter` is a static class with reusable CLI presentation helpers.
All methods write to `Console.Out` by default; pass a `TextWriter` to redirect
output (useful in tests or when writing to log files).

### Section headings

```csharp
ConsoleReporter.WriteSection("Performance Summary");
// ────────────────────────────────────────────────────────────────────────
//  Performance Summary
// ────────────────────────────────────────────────────────────────────────
```

### Status lines

```csharp
ConsoleReporter.WriteStatus("CUDA available", "true");
// CUDA available ............... true
```

### Metrics table

```csharp
ConsoleReporter.WriteSection("Timings");
ConsoleReporter.WriteMetricsTable(PerformanceMonitor.Default.GetAllMetrics());
//   Operation                     Count    Total ms      Avg ms      Min ms      Max ms
//   ──────────────────────────────────────────────────────────────────────────────────
//   BatchDistanceSquared              5       18.40        3.68        3.21        4.31
//   CreateGraph                       3       94.12       31.37       30.01       33.10
```

### Progress bar

```csharp
using var bar = ConsoleReporter.CreateProgressBar("Filling region", total: 1000);
for (int i = 0; i < 1000; i++)
{
    // ... do work ...
    bar.Report(i + 1);
}
// Filling region       [████████████████████████████████] 100% (1000/1000)
```

`ConsoleProgressBar` implements `IProgress<int>` and `IDisposable`. It
updates in place (using `\r`) when writing to the real console, or emits one
line per `Report` call when output is redirected.
