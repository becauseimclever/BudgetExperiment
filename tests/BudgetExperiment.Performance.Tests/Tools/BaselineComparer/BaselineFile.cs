// Copyright (c) BecauseImClever. All rights reserved.

using System.Text.Json.Serialization;

namespace BaselineComparer;

/// <summary>
/// Represents the baseline file containing historical performance metrics.
/// </summary>
internal sealed class BaselineFile
{
    [JsonPropertyName("generatedAt")]
    public DateTime GeneratedAt { get; set; }

    [JsonPropertyName("commitSha")]
    public string? CommitSha { get; set; }

    [JsonPropertyName("scenarios")]
    public List<ScenarioMetrics> Scenarios { get; set; } = [];
}
