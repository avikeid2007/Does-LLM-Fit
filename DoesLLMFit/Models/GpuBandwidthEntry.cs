using System.Text.Json.Serialization;

namespace DoesLLMFit.Models;

public record GpuBandwidthEntry
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("vram_gb")]
    public double VramGb { get; init; }

    [JsonPropertyName("bandwidth_gbs")]
    public double BandwidthGBs { get; init; }

    [JsonPropertyName("architecture")]
    public string Architecture { get; init; } = "PC";
}
