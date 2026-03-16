// Copyright (c) BecauseImClever. All rights reserved.

using System.Text.Json.Serialization;

namespace BaselineComparer;

/// <summary>
/// Regression threshold configuration.
/// </summary>
internal sealed class ThresholdConfig
{
    [JsonPropertyName("maxLatencyRegressionPercent")]
    public double MaxLatencyRegressionPercent { get; set; } = 15;

    [JsonPropertyName("maxThroughputRegressionPercent")]
    public double MaxThroughputRegressionPercent { get; set; } = 10;

    [JsonPropertyName("maxErrorRateAbsolute")]
    public double MaxErrorRateAbsolute { get; set; } = 1.0;

    [JsonPropertyName("failOnRegression")]
    public bool FailOnRegression { get; set; } = true;
}
