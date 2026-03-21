using System.Text.Json;
using System.Text.Json.Serialization;

namespace DoesLLMFit.Services;

/// <summary>
/// Fetches popular models from the Hugging Face API across multiple pipeline categories.
/// </summary>
public class HuggingFaceService
{
    private readonly ILogger<HuggingFaceService> _logger;
    private readonly HttpClient _http;

    private const string HfApiBase = "https://huggingface.co/api/models";
    private const int MinDownloads = 1000;

    /// <summary>
    /// Pipeline tags to fetch, with per-category limits.
    /// </summary>
    private static readonly (string tag, int limit, string[] categories)[] PipelineQueries =
    [
        ("text-generation",              100, ["Text Generation"]),
        ("image-text-to-text",            50, ["Multimodal", "Vision"]),
        ("text-to-image",                 50, ["Image Generation"]),
        ("text-to-video",                 30, ["Video Generation"]),
        ("text-to-audio",                 20, ["Audio Generation"]),
        ("automatic-speech-recognition",  20, ["Speech Recognition"]),
        ("text-to-speech",                20, ["Text to Speech"]),
        ("image-to-image",                20, ["Image Generation"]),
    ];

    /// <summary>Known orgs that publish canonical (non-fork) models.</summary>
    private static readonly HashSet<string> PreferredOrgs = new(StringComparer.OrdinalIgnoreCase)
    {
        "meta-llama", "mistralai", "google", "microsoft", "Qwen",
        "deepseek-ai", "01-ai", "stabilityai", "tiiuae", "NousResearch",
        "black-forest-labs", "openai", "facebook", "CompVis", "runwayml",
        "HuggingFaceTB", "bigscience", "EleutherAI", "mosaicml",
        "databricks", "nvidia", "apple", "allenai", "CohereForAI",
    };

    public HuggingFaceService(ILogger<HuggingFaceService> logger)
    {
        _logger = logger;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("User-Agent", "DoesLLMFit/1.0");
    }

    /// <summary>
    /// Fetches models across all configured pipeline categories, deduplicates, and returns LlmModel list.
    /// </summary>
    public async Task<List<LlmModel>> FetchModelsAsync(CancellationToken ct = default)
    {
        // Fire all pipeline requests in parallel
        var tasks = PipelineQueries.Select(async q =>
        {
            try
            {
                var models = await FetchByPipelineAsync(q.tag, q.limit, q.categories, ct);
                _logger.LogInformation("Fetched {Count} models for pipeline '{Tag}'", models.Count, q.tag);
                return models;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch HF models for pipeline '{Tag}'", q.tag);
                return new List<LlmModel>();
            }
        }).ToArray();

        var results = await Task.WhenAll(tasks);
        var allModels = results.SelectMany(r => r).ToList();

        // Deduplicate: keep preferred org version when multiple repos share the same base model name
        var deduplicated = DeduplicateModels(allModels);

        _logger.LogInformation("HuggingFace total: {Total} models after deduplication", deduplicated.Count);
        return deduplicated;
    }

    private async Task<List<LlmModel>> FetchByPipelineAsync(
        string pipelineTag, int limit, string[] categories, CancellationToken ct)
    {
        var url = $"{HfApiBase}?pipeline_tag={Uri.EscapeDataString(pipelineTag)}" +
                  $"&sort=downloads&direction=-1&limit={limit}";

        using var response = await _http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("HF API returned {StatusCode} for {Tag}", response.StatusCode, pipelineTag);
            return [];
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var hfModels = JsonSerializer.Deserialize<List<HfModelEntry>>(json) ?? [];

        var result = new List<LlmModel>();
        foreach (var hf in hfModels)
        {
            if (hf.Downloads < MinDownloads) continue;

            var paramB = ExtractParamCount(hf);
            if (paramB <= 0) continue; // skip if we can't determine size

            var name = BuildDisplayName(hf.Id);

            result.Add(new LlmModel
            {
                Name = name,
                ParametersB = paramB,
                HuggingFaceId = hf.Id,
                Categories = categories,
                PipelineTag = pipelineTag,
                Downloads = hf.Downloads,
                Description = $"HuggingFace model with {FormatDownloads(hf.Downloads)} downloads",
                SupportedQuants = GetDefaultQuants(pipelineTag),
                Source = ModelSource.HuggingFace,
            });
        }

        return result;
    }

    private static double ExtractParamCount(HfModelEntry hf)
    {
        // Try safetensors.total first
        if (hf.Safetensors?.Total > 0)
            return hf.Safetensors.Total / 1_000_000_000.0;

        // Try safetensors.parameters (sometimes used)
        if (hf.Safetensors?.Parameters > 0)
            return hf.Safetensors.Parameters / 1_000_000_000.0;

        // Try extracting from model name (e.g., "Llama-3.1-8B" → 8.0)
        return ExtractParamFromName(hf.Id);
    }

    private static double ExtractParamFromName(string modelId)
    {
        var name = modelId.Split('/').LastOrDefault() ?? modelId;

        // Patterns: 7B, 70B, 0.5B, 1.5B, 8b, 13b, 180b
        var match = System.Text.RegularExpressions.Regex.Match(
            name, @"(\d+\.?\d*)\s*[Bb]\b", System.Text.RegularExpressions.RegexOptions.None);

        if (match.Success && double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var val))
        {
            return val;
        }

        return 0;
    }

    private static string BuildDisplayName(string repoId)
    {
        // "meta-llama/Llama-3.1-8B-Instruct" → "Llama 3.1 8B Instruct"
        var name = repoId.Split('/').LastOrDefault() ?? repoId;
        return name.Replace('-', ' ').Replace('_', ' ');
    }

    private static string FormatDownloads(long downloads)
    {
        return downloads switch
        {
            >= 1_000_000 => $"{downloads / 1_000_000.0:F1}M",
            >= 1_000 => $"{downloads / 1_000.0:F0}K",
            _ => downloads.ToString()
        };
    }

    private static IReadOnlyList<string> GetDefaultQuants(string pipelineTag)
    {
        // LLMs commonly use GGUF quants; diffusion/audio models typically run at FP16/BF16
        return pipelineTag switch
        {
            "text-generation" => ["Q2_K", "Q3_K_M", "Q4_K_M", "Q5_K_M", "Q6_K", "Q8_0", "F16"],
            _ => ["Q4_K_M", "Q8_0", "F16"],
        };
    }

    private static List<LlmModel> DeduplicateModels(List<LlmModel> models)
    {
        // Group by a normalized base name (remove org prefix variants)
        // Keep the one from a preferred org, or highest downloads
        var groups = models
            .GroupBy(m => NormalizeModelKey(m), StringComparer.OrdinalIgnoreCase);

        var result = new List<LlmModel>();
        foreach (var group in groups)
        {
            var best = group
                .OrderByDescending(m => IsPreferredOrg(m.HuggingFaceId) ? 1 : 0)
                .ThenByDescending(m => m.Downloads)
                .First();
            result.Add(best);
        }

        return result
            .OrderByDescending(m => m.Downloads)
            .ToList();
    }

    private static string NormalizeModelKey(LlmModel m)
    {
        // Use the model name portion (after org/) + pipeline + approximate param count
        var name = m.HuggingFaceId.Split('/').LastOrDefault() ?? m.Name;
        var paramBucket = m.ParametersB switch
        {
            < 1 => "sub1B",
            < 4 => $"{m.ParametersB:F0}B",
            < 10 => $"{Math.Round(m.ParametersB):F0}B",
            _ => $"{Math.Round(m.ParametersB / 5) * 5:F0}B"
        };
        return $"{m.PipelineTag}|{name.ToLowerInvariant()}|{paramBucket}";
    }

    private static bool IsPreferredOrg(string repoId)
    {
        var org = repoId.Split('/').FirstOrDefault() ?? "";
        return PreferredOrgs.Contains(org);
    }
}

// ── HuggingFace API response DTOs ──

internal class HfModelEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("pipeline_tag")]
    public string PipelineTag { get; set; } = string.Empty;

    [JsonPropertyName("downloads")]
    public long Downloads { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonPropertyName("safetensors")]
    public HfSafetensors? Safetensors { get; set; }
}

internal class HfSafetensors
{
    [JsonPropertyName("total")]
    public long Total { get; set; }

    [JsonPropertyName("parameters")]
    public long Parameters { get; set; }
}
