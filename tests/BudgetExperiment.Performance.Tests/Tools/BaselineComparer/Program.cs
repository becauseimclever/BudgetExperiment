// Copyright (c) BecauseImClever. All rights reserved.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using BaselineComparer;

// Usage:
//   Compare mode:  dotnet run -- <report.csv> <baseline.json> [--thresholds <perf-thresholds.json>]
//   Generate mode: dotnet run -- --generate <report.csv> --output <baseline.json> [--commit-sha <sha>]
//   Output: Markdown summary written to stdout. Exit code 1 if regression detected.
if (args.Length < 1)
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  Compare:  BaselineComparer <report.csv> <baseline.json> [--thresholds <perf-thresholds.json>]");
    Console.Error.WriteLine("  Generate: BaselineComparer --generate <report.csv> --output <baseline.json> [--commit-sha <sha>]");
    return 1;
}

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

if (args[0] == "--generate")
{
    return GenerateBaseline(args);
}

return CompareBaseline(args);

int GenerateBaseline(string[] args)
{
    string? csvPath = null;
    string? outputPath = null;
    string? commitSha = null;

    for (int i = 1; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--output" when i + 1 < args.Length:
                outputPath = args[++i];
                break;
            case "--commit-sha" when i + 1 < args.Length:
                commitSha = args[++i];
                break;
            default:
                csvPath ??= args[i];
                break;
        }
    }

    if (csvPath is null || outputPath is null)
    {
        Console.Error.WriteLine("Error: --generate requires a CSV path and --output path.");
        return 1;
    }

    var scenarios = ParseCsvReport(csvPath);
    if (scenarios.Count == 0)
    {
        Console.Error.WriteLine("Error: No scenario data found in CSV report.");
        return 1;
    }

    var baseline = new BaselineFile
    {
        GeneratedAt = DateTime.UtcNow,
        CommitSha = commitSha,
        Scenarios = scenarios,
    };

    var json = JsonSerializer.Serialize(baseline, jsonOptions);
    File.WriteAllText(outputPath, json);
    Console.WriteLine($"Baseline generated with {scenarios.Count} scenario(s): {outputPath}");
    return 0;
}

int CompareBaseline(string[] args)
{
    var csvPath = args[0];
    var baselinePath = args.Length > 1 ? args[1] : null;
    string? thresholdsPath = null;

    for (int i = 2; i < args.Length; i++)
    {
        if (args[i] == "--thresholds" && i + 1 < args.Length)
        {
            thresholdsPath = args[++i];
        }
    }

    var scenarios = ParseCsvReport(csvPath);
    if (scenarios.Count == 0)
    {
        Console.WriteLine("## Performance Results");
        Console.WriteLine();
        Console.WriteLine("No scenario data found in CSV report.");
        return 0;
    }

    var thresholds = LoadThresholds(thresholdsPath);

    if (baselinePath is null || !File.Exists(baselinePath))
    {
        PrintNoBaselineReport(scenarios);
        return 0;
    }

    var baselineJson = File.ReadAllText(baselinePath);
    var baseline = JsonSerializer.Deserialize<BaselineFile>(baselineJson, jsonOptions);
    if (baseline?.Scenarios is null || baseline.Scenarios.Count == 0)
    {
        PrintNoBaselineReport(scenarios);
        return 0;
    }

    return PrintComparisonReport(scenarios, baseline, thresholds);
}

List<ScenarioMetrics> ParseCsvReport(string csvPath)
{
    var results = new List<ScenarioMetrics>();
    var lines = File.ReadAllLines(csvPath);
    if (lines.Length < 2)
    {
        return results;
    }

    var headers = ParseCsvLine(lines[0]);
    var columnIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    for (int i = 0; i < headers.Length; i++)
    {
        columnIndex[headers[i]] = i;
    }

    for (int lineIdx = 1; lineIdx < lines.Length; lineIdx++)
    {
        var fields = ParseCsvLine(lines[lineIdx]);
        if (fields.Length < headers.Length)
        {
            continue;
        }

        var scenario = new ScenarioMetrics
        {
            Name = GetField(fields, columnIndex, "scenario"),
            P50Ms = GetDouble(fields, columnIndex, "ok_50_percent"),
            P95Ms = GetDouble(fields, columnIndex, "ok_95_percent"),
            P99Ms = GetDouble(fields, columnIndex, "ok_99_percent"),
            ThroughputRps = GetDouble(fields, columnIndex, "ok_rps"),
        };

        var totalRequests = GetDouble(fields, columnIndex, "request_count");
        var failedRequests = GetDouble(fields, columnIndex, "failed");
        scenario.ErrorPercent = totalRequests > 0
            ? (failedRequests / totalRequests) * 100.0
            : 0.0;

        if (!string.IsNullOrEmpty(scenario.Name))
        {
            results.Add(scenario);
        }
    }

    return results;
}

string[] ParseCsvLine(string line)
{
    return line.Split(',');
}

string GetField(string[] fields, Dictionary<string, int> index, string column)
{
    return index.TryGetValue(column, out var i) && i < fields.Length ? fields[i].Trim() : string.Empty;
}

double GetDouble(string[] fields, Dictionary<string, int> index, string column)
{
    var value = GetField(fields, index, column);
    return double.TryParse(value, CultureInfo.InvariantCulture, out var result) ? result : 0.0;
}

ThresholdConfig LoadThresholds(string? path)
{
    if (path is not null && File.Exists(path))
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ThresholdConfig>(json, jsonOptions) ?? new ThresholdConfig();
    }

    return new ThresholdConfig();
}

void PrintNoBaselineReport(List<ScenarioMetrics> scenarios)
{
    Console.WriteLine("## Performance Results (No Baseline Yet)");
    Console.WriteLine();
    Console.WriteLine("No baseline file found. Run a scheduled build and commit the baseline to enable regression detection.");
    Console.WriteLine();
    Console.WriteLine("### Current Metrics");
    Console.WriteLine();
    Console.WriteLine("| Scenario | p50 (ms) | p95 (ms) | p99 (ms) | RPS | Error % |");
    Console.WriteLine("|----------|----------|----------|----------|-----|---------|");
    foreach (var s in scenarios)
    {
        Console.WriteLine($"| {s.Name} | {s.P50Ms:F1} | {s.P95Ms:F1} | {s.P99Ms:F1} | {s.ThroughputRps:F1} | {s.ErrorPercent:F2}% |");
    }
}

int PrintComparisonReport(List<ScenarioMetrics> current, BaselineFile baseline, ThresholdConfig thresholds)
{
    var regressions = new List<string>();

    Console.WriteLine("## \u26a1 Performance Report");
    Console.WriteLine();
    Console.WriteLine($"Baseline: `{baseline.CommitSha ?? "unknown"}` ({baseline.GeneratedAt:yyyy-MM-dd})");
    Console.WriteLine();
    Console.WriteLine("| Scenario | p95 (ms) | Baseline p95 | \u0394 | p99 (ms) | Baseline p99 | \u0394 | RPS | Baseline RPS | \u0394 | Error % | Status |");
    Console.WriteLine("|----------|----------|-------------|---|----------|-------------|---|-----|-------------|---|---------|--------|");

    foreach (var s in current)
    {
        var baseScenario = baseline.Scenarios!.FirstOrDefault(b => b.Name == s.Name);
        if (baseScenario is null)
        {
            Console.WriteLine($"| {s.Name} | {s.P95Ms:F1} | _new_ | | {s.P99Ms:F1} | _new_ | | {s.ThroughputRps:F1} | _new_ | | {s.ErrorPercent:F2}% | \u2139\ufe0f New |");
            continue;
        }

        var p95Delta = CalculateDelta(s.P95Ms, baseScenario.P95Ms);
        var p99Delta = CalculateDelta(s.P99Ms, baseScenario.P99Ms);
        var rpsDelta = CalculateDelta(s.ThroughputRps, baseScenario.ThroughputRps);

        var status = "\u2705";
        var issues = new List<string>();

        if (p95Delta > thresholds.MaxLatencyRegressionPercent)
        {
            issues.Add($"p95 +{p95Delta:F1}% > {thresholds.MaxLatencyRegressionPercent}%");
        }

        if (p99Delta > thresholds.MaxLatencyRegressionPercent)
        {
            issues.Add($"p99 +{p99Delta:F1}% > {thresholds.MaxLatencyRegressionPercent}%");
        }

        // For RPS, a negative delta means regression (lower throughput)
        if (rpsDelta < -thresholds.MaxThroughputRegressionPercent)
        {
            issues.Add($"RPS {rpsDelta:F1}% > {thresholds.MaxThroughputRegressionPercent}% drop");
        }

        if (s.ErrorPercent > thresholds.MaxErrorRateAbsolute)
        {
            issues.Add($"Error {s.ErrorPercent:F2}% > {thresholds.MaxErrorRateAbsolute}%");
        }

        if (issues.Count > 0)
        {
            status = $"\u274c {string.Join("; ", issues)}";
            regressions.AddRange(issues.Select(i => $"[{s.Name}] {i}"));
        }

        Console.WriteLine(
            $"| {s.Name} " +
            $"| {s.P95Ms:F1} | {baseScenario.P95Ms:F1} | {FormatDelta(p95Delta)} " +
            $"| {s.P99Ms:F1} | {baseScenario.P99Ms:F1} | {FormatDelta(p99Delta)} " +
            $"| {s.ThroughputRps:F1} | {baseScenario.ThroughputRps:F1} | {FormatDelta(rpsDelta)} " +
            $"| {s.ErrorPercent:F2}% | {status} |");
    }

    Console.WriteLine();

    if (regressions.Count > 0)
    {
        Console.WriteLine($"**Result: FAIL** \u2014 {regressions.Count} regression(s) detected.");
        Console.WriteLine();
        foreach (var r in regressions)
        {
            Console.WriteLine($"- {r}");
        }

        return thresholds.FailOnRegression ? 1 : 0;
    }

    Console.WriteLine("**Result: PASS** \u2014 All metrics within acceptable thresholds.");
    return 0;
}

double CalculateDelta(double current, double baseline)
{
    if (baseline == 0)
    {
        return 0;
    }

    return ((current - baseline) / baseline) * 100.0;
}

string FormatDelta(double delta)
{
    return delta switch
    {
        > 0 => $"+{delta:F1}%",
        < 0 => $"{delta:F1}%",
        _ => "0.0%",
    };
}
