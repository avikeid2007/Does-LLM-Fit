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

    [JsonPropertyName("max_context")]
    public int MaxContext { get; init; }

    [JsonPropertyName("categories")]
    public IReadOnlyList<string> Categories { get; init; } = [];

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("huggingface_id")]
    public string HuggingFaceId { get; init; } = string.Empty;

    [JsonPropertyName("supported_quants")]
    public IReadOnlyList<string> SupportedQuants { get; init; } = [];

    [JsonPropertyName("pipeline_tag")]
    public string PipelineTag { get; init; } = "text-generation";

    [JsonPropertyName("downloads")]
    public long Downloads { get; init; }

    [JsonPropertyName("license")]
    public string License { get; init; } = string.Empty;

    /// <summary>Curated = from static JSON, HuggingFace = fetched from HF API.</summary>
    [JsonIgnore]
    public ModelSource Source { get; init; } = ModelSource.Curated;
}

public enum ModelSource
{
    Curated,
    HuggingFace
}
