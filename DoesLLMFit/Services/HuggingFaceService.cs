using System.Reflection;
using System.Text.Json;

namespace DoesLLMFit.Services;

/// <summary>
/// Loads HuggingFace models from the pre-built embedded catalog (hf-models.json).
/// The catalog is generated offline by the HfCatalogBuilder tool and includes
/// architecture details (layers, KV heads, head dim, context length) and license info.
/// </summary>
public class HuggingFaceService
{
    private readonly ILogger<HuggingFaceService> _logger;

    private const string EmbeddedResourceName = "DoesLLMFit.Data.hf-models.json";

    public HuggingFaceService(ILogger<HuggingFaceService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loads models from the embedded hf-models.json catalog.
    /// </summary>
    public async Task<List<LlmModel>> FetchModelsAsync(CancellationToken ct = default)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName);
            if (stream is null)
            {
                _logger.LogWarning("Embedded HF catalog not found: {Resource}", EmbeddedResourceName);
                return [];
            }

            var models = await JsonSerializer.DeserializeAsync<List<LlmModel>>(stream, cancellationToken: ct) ?? [];

            // Mark all as HuggingFace source
            var result = models.Select(m => m with { Source = ModelSource.HuggingFace }).ToList();

            _logger.LogInformation("Loaded {Count} models from embedded HF catalog", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load embedded HF catalog");
            return [];
        }
    }
}
