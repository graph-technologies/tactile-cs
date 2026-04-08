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
