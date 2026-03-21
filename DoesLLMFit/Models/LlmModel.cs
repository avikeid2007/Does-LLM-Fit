using System.Text.Json.Serialization;

namespace DoesLLMFit.Models;

public record LlmModel
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("parameters_b")]
    public double ParametersB { get; init; }

    [JsonPropertyName("num_layers")]
    public int NumLayers { get; init; }

    [JsonPropertyName("kv_heads")]
    public int KvHeads { get; init; }

    [JsonPropertyName("head_dim")]
    public int HeadDim { get; init; }

    [JsonPropertyName("categories")]
    public IReadOnlyList<string> Categories { get; init; } = [];

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("huggingface_id")]
    public string HuggingFaceId { get; init; } = string.Empty;

    [JsonPropertyName("supported_quants")]
    public IReadOnlyList<string> SupportedQuants { get; init; } = [];
}
