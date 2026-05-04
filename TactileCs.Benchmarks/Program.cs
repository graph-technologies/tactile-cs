using BenchmarkDotNet.Running;
using TactileCs.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(TilingBenchmarks).Assembly).Run(args);
