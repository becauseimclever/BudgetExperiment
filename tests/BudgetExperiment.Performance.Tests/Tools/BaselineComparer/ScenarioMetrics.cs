// Copyright (c) BecauseImClever. All rights reserved.

using System.Text.Json.Serialization;

namespace BaselineComparer;

/// <summary>
/// Per-scenario performance metrics.
/// </summary>
internal sealed class ScenarioMetrics
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("p50Ms")]
    public double P50Ms
    {
        get; set;
    }

    [JsonPropertyName("p95Ms")]
    public double P95Ms
    {
        get; set;
    }

    [JsonPropertyName("p99Ms")]
    public double P99Ms
    {
        get; set;
    }

    [JsonPropertyName("throughputRps")]
    public double ThroughputRps
    {
        get; set;
    }

    [JsonPropertyName("errorPercent")]
    public double ErrorPercent
    {
        get; set;
    }
}
