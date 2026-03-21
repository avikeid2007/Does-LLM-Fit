namespace DoesLLMFit.Models;

public enum FitStatus
{
    /// <summary>Model fits with 20%+ headroom — runs great.</summary>
    Green,
    /// <summary>Model fits but within 10-20% — tight, may be slow.</summary>
    Yellow,
    /// <summary>Model exceeds available memory — won't run.</summary>
    Red
}

public record CompatibilityResult
{
    public required LlmModel Model { get; init; }
    public required QuantType Quant { get; init; }
    public required double EstimatedVramGb { get; init; }
    public required double AvailableVramGb { get; init; }
    public required FitStatus Status { get; init; }
    public double? EstimatedToksPerSec { get; init; }
    public required string Explanation { get; init; }

    public string QuantDisplayName => QuantInfo.GetDisplayName(Quant);
    public double VramUsagePercent => AvailableVramGb > 0
        ? EstimatedVramGb / AvailableVramGb * 100.0
        : 100.0;
}

/// <summary>
/// Aggregated result for a model: the best-fit quant and all quant options.
/// </summary>
public record ModelCompatibilitySummary
{
    public required LlmModel Model { get; init; }
    public required CompatibilityResult? BestFit { get; init; }
    public required IReadOnlyList<CompatibilityResult> AllQuants { get; init; }

    public FitStatus OverallStatus => BestFit?.Status ?? FitStatus.Red;
    public string StatusLabel => OverallStatus switch
    {
        FitStatus.Green => "Runs Great",
        FitStatus.Yellow => "Tight Fit",
        FitStatus.Red => "Won't Fit",
        _ => "Unknown"
    };
}
