using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

// ─── Configuration ──────────────────────────────────────────

const string HfApiBase = "https://huggingface.co/api/models";
const string ConfigUrlTemplate = "https://huggingface.co/{0}/resolve/main/config.json";
const int MinDownloads = 1000;
const int MaxConcurrentConfigFetches = 10;

var outputPath = args.Length > 0 ? args[0] : "hf-models.json";

var pipelines = new (string tag, int limit, string[] categories)[]
{
    ("text-generation",              100, ["Text Generation"]),
    ("image-text-to-text",            50, ["Multimodal", "Vision"]),
    ("text-to-image",                 50, ["Image Generation"]),
    ("text-to-video",                 30, ["Video Generation"]),
    ("text-to-audio",                 20, ["Audio Generation"]),
    ("automatic-speech-recognition",  20, ["Speech Recognition"]),
    ("text-to-speech",                20, ["Text to Speech"]),
    ("image-to-image",                20, ["Image Generation"]),
};

var preferredOrgs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "meta-llama", "mistralai", "google", "microsoft", "Qwen",
    "deepseek-ai", "01-ai", "stabilityai", "tiiuae", "NousResearch",
    "black-forest-labs", "openai", "facebook", "CompVis", "runwayml",
    "HuggingFaceTB", "bigscience", "EleutherAI", "mosaicml",
    "databricks", "nvidia", "apple", "allenai", "CohereForAI",
};

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("User-Agent", "DoesLLMFit-CatalogBuilder/1.0");

// ─── Step 1: Fetch models from all pipelines in parallel ────

Console.WriteLine("Step 1: Fetching models from HuggingFace API...");

var allModels = new List<HfModelEntry>();
var fetchTasks = pipelines.Select(async p =>
{
    try
    {
        var url = $"{HfApiBase}?pipeline_tag={Uri.EscapeDataString(p.tag)}&sort=downloads&direction=-1&limit={p.limit}";
        var json = await http.GetStringAsync(url);
        var models = JsonSerializer.Deserialize<List<HfModelEntry>>(json) ?? [];
        foreach (var m in models)
        {
            m.AssignedCategories = p.categories;
            m.AssignedPipelineTag = p.tag;
        }
        Console.WriteLine($"  [{p.tag}] {models.Count} models");
        return models;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"  [{p.tag}] FAILED: {ex.Message}");
        return new List<HfModelEntry>();
    }
}).ToArray();

var results = await Task.WhenAll(fetchTasks);
foreach (var batch in results)
    allModels.AddRange(batch);

Console.WriteLine($"  Total raw: {allModels.Count}");

// ─── Step 2: Filter, extract params, build intermediate list ─

Console.WriteLine("Step 2: Filtering and extracting parameters...");

var candidates = new List<(HfModelEntry hf, double paramB, string name)>();
foreach (var hf in allModels)
{
    if (hf.Downloads < MinDownloads) continue;
    var paramB = ExtractParamCount(hf);
    if (paramB <= 0) continue;
    var name = BuildDisplayName(hf.Id);
    candidates.Add((hf, paramB, name));
}
Console.WriteLine($"  Candidates after filter: {candidates.Count}");

// ─── Step 3: Deduplicate ────────────────────────────────────

Console.WriteLine("Step 3: Deduplicating...");

var deduped = candidates
    .GroupBy(c => NormalizeKey(c.hf.AssignedPipelineTag, c.hf.Id, c.paramB), StringComparer.OrdinalIgnoreCase)
    .Select(g => g
        .OrderByDescending(c => IsPreferred(c.hf.Id) ? 1 : 0)
        .ThenByDescending(c => c.hf.Downloads)
        .First())
    .OrderByDescending(c => c.hf.Downloads)
    .ToList();

Console.WriteLine($"  After dedup: {deduped.Count}");

// ─── Step 4: Enrich with config.json (architecture details) ─

Console.WriteLine($"Step 4: Fetching config.json for {deduped.Count} models (concurrency={MaxConcurrentConfigFetches})...");

var semaphore = new SemaphoreSlim(MaxConcurrentConfigFetches);
var enrichTasks = deduped.Select(async entry =>
{
    await semaphore.WaitAsync();
    try
    {
        var config = await FetchConfigAsync(http, entry.hf.Id);
        return (entry, config);
    }
    finally
    {
        semaphore.Release();
    }
}).ToArray();

var enriched = await Task.WhenAll(enrichTasks);

var successCount = enriched.Count(e => e.config is not null);
Console.WriteLine($"  Config fetched: {successCount}/{deduped.Count}");

// ─── Step 5: Build final output ─────────────────────────────

Console.WriteLine("Step 5: Building catalog JSON...");

var catalog = new List<CatalogModel>();
foreach (var (entry, config) in enriched)
{
    var (hf, paramB, name) = entry;

    var numLayers = config?.NumHiddenLayers ?? 0;
    var numKvHeads = config?.NumKeyValueHeads ?? config?.NumAttentionHeads ?? 0;
    var hiddenSize = config?.HiddenSize ?? 0;
    var numHeads = config?.NumAttentionHeads ?? 0;
    var headDim = numHeads > 0 ? hiddenSize / numHeads : 0;
    var maxContext = config?.MaxPositionEmbeddings ?? 0;

    // Extract license from tags
    var license = hf.Tags
        .Where(t => t.StartsWith("license:", StringComparison.OrdinalIgnoreCase))
        .Select(t => t["license:".Length..])
        .FirstOrDefault() ?? "";

    // Determine quants
    var quants = GetDefaultQuants(hf.AssignedPipelineTag);

    catalog.Add(new CatalogModel
    {
        Name = name,
        ParametersB = Math.Round(paramB, 2),
        NumLayers = numLayers,
        KvHeads = numKvHeads,
        HeadDim = headDim,
        MaxContext = maxContext,
        Categories = hf.AssignedCategories,
        Description = $"HuggingFace model with {FormatDownloads(hf.Downloads)} downloads",
        HuggingFaceId = hf.Id,
        SupportedQuants = quants,
        PipelineTag = hf.AssignedPipelineTag,
        Downloads = hf.Downloads,
        License = license,
    });
}

// ─── Step 6: Write output ───────────────────────────────────

var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
var outputJson = JsonSerializer.Serialize(catalog, options);
await File.WriteAllTextAsync(outputPath, outputJson);

Console.WriteLine($"Done! Wrote {catalog.Count} models to {Path.GetFullPath(outputPath)}");

// ═══════════════════════════════════════════════════════════
// Helper methods
// ═══════════════════════════════════════════════════════════

static double ExtractParamCount(HfModelEntry hf)
{
    if (hf.Safetensors?.Total > 0)
        return hf.Safetensors.Total / 1_000_000_000.0;
    return ExtractParamFromName(hf.Id);
}

static double ExtractParamFromName(string modelId)
{
    var name = modelId.Split('/').LastOrDefault() ?? modelId;
    var match = Regex.Match(name, @"(?<!\d)(\d+\.?\d*)\s*[Bb]\b");
    if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var val))
        return val;
    return 0;
}

static string BuildDisplayName(string repoId)
{
    var name = repoId.Split('/').LastOrDefault() ?? repoId;
    return name.Replace('-', ' ').Replace('_', ' ');
}

static string FormatDownloads(long downloads) => downloads switch
{
    >= 1_000_000 => $"{downloads / 1_000_000.0:F1}M",
    >= 1_000 => $"{downloads / 1_000.0:F0}K",
    _ => downloads.ToString()
};

string NormalizeKey(string pipeline, string repoId, double paramB)
{
    var name = repoId.Split('/').LastOrDefault() ?? repoId;
    var bucket = paramB switch
    {
        < 1 => "sub1B",
        < 4 => $"{paramB:F0}B",
        < 10 => $"{Math.Round(paramB):F0}B",
        _ => $"{Math.Round(paramB / 5) * 5:F0}B"
    };
    return $"{pipeline}|{name.ToLowerInvariant()}|{bucket}";
}

bool IsPreferred(string repoId)
{
    var org = repoId.Split('/').FirstOrDefault() ?? "";
    return preferredOrgs.Contains(org);
}

static IReadOnlyList<string> GetDefaultQuants(string pipelineTag) => pipelineTag switch
{
    "text-generation" => ["Q2_K", "Q3_K_M", "Q4_K_M", "Q5_K_M", "Q6_K", "Q8_0", "F16"],
    _ => ["Q4_K_M", "Q8_0", "F16"],
};

static async Task<ModelConfig?> FetchConfigAsync(HttpClient http, string repoId)
{
    try
    {
        var url = string.Format(ConfigUrlTemplate, repoId);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var json = await http.GetStringAsync(url, cts.Token);
        return JsonSerializer.Deserialize<ModelConfig>(json);
    }
    catch
    {
        return null;
    }
}

// ═══════════════════════════════════════════════════════════
// DTOs
// ═══════════════════════════════════════════════════════════

class HfModelEntry
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("pipeline_tag")] public string PipelineTag { get; set; } = "";
    [JsonPropertyName("downloads")] public long Downloads { get; set; }
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
    [JsonPropertyName("safetensors")] public HfSafetensors? Safetensors { get; set; }

    [JsonIgnore] public string[] AssignedCategories { get; set; } = [];
    [JsonIgnore] public string AssignedPipelineTag { get; set; } = "";
}

class HfSafetensors
{
    [JsonPropertyName("total")] public long Total { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}

class ModelConfig
{
    [JsonPropertyName("num_hidden_layers")] public int? NumHiddenLayers { get; set; }
    [JsonPropertyName("num_attention_heads")] public int? NumAttentionHeads { get; set; }
    [JsonPropertyName("num_key_value_heads")] public int? NumKeyValueHeads { get; set; }
    [JsonPropertyName("hidden_size")] public int? HiddenSize { get; set; }
    [JsonPropertyName("max_position_embeddings")] public int? MaxPositionEmbeddings { get; set; }
}

class CatalogModel
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("parameters_b")] public double ParametersB { get; set; }
    [JsonPropertyName("num_layers")] public int NumLayers { get; set; }
    [JsonPropertyName("kv_heads")] public int KvHeads { get; set; }
    [JsonPropertyName("head_dim")] public int HeadDim { get; set; }
    [JsonPropertyName("max_context")] public int MaxContext { get; set; }
    [JsonPropertyName("categories")] public IReadOnlyList<string> Categories { get; set; } = [];
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("huggingface_id")] public string HuggingFaceId { get; set; } = "";
    [JsonPropertyName("supported_quants")] public IReadOnlyList<string> SupportedQuants { get; set; } = [];
    [JsonPropertyName("pipeline_tag")] public string PipelineTag { get; set; } = "";
    [JsonPropertyName("downloads")] public long Downloads { get; set; }
    [JsonPropertyName("license")] public string License { get; set; } = "";
}
