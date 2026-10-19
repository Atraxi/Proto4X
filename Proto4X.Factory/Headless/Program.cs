using Proto4X.Factory.Headless;
using System;
using System.Linq;

// Usage:
//   dotnet run -c Release                        scenario checks + determinism
//   dotnet run -c Release -- bench [modules] [idle-first]   benchmark (default 2000 modules, ~56k buildings)
//   dotnet run -c Release -- snapshot [directory]           render the game's draw systems to PNGs, no window
if (args.Length > 0 && args[0] == "snapshot")
{
    Proto4X.Factory.Rendering.Snapshot.CaptureAll(args.Length > 1 ? args[1] : "snapshots");
    return 0;
}

if (args.Length > 0 && args[0] == "bench")
{
    var modules = args.Length > 1 ? int.Parse(args[1]) : 2_000;
    Benchmark.Run(modules, warmupTicks: 600, measuredTicks: 600, idleFirst: args.Contains("idle-first"));
    return 0;
}

var failures = Scenarios.RunAll();
return failures == 0 ? 0 : 1;
