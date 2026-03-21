using System.Reflection;
using System.Text.Json;

namespace DoesLLMFit.Services;

public class ModelCatalogService
{
    private readonly ILogger<ModelCatalogService> _logger;
    private readonly HuggingFaceService _hfService;
    private List<LlmModel> _models = [];
    private List<GpuBandwidthEntry> _gpuDatabase = [];

    public ModelCatalogService(ILogger<ModelCatalogService> logger, HuggingFaceService hfService)
    {
        _logger = logger;
        _hfService = hfService;
    }

    public IReadOnlyList<LlmModel> Models => _models;
    public IReadOnlyList<GpuBandwidthEntry> GpuDatabase => _gpuDatabase;
    public bool HuggingFaceLoaded { get; private set; }

    public async Task InitializeAsync()
    {
        // Load curated + GPU data from embedded resources (instant)
        var curatedTask = LoadEmbeddedJsonAsync<List<LlmModel>>("DoesLLMFit.Data.models.json");
        var gpuTask = LoadEmbeddedJsonAsync<List<GpuBandwidthEntry>>("DoesLLMFit.Data.gpu-bandwidth.json");
        await Task.WhenAll(curatedTask, gpuTask);

        var curated = curatedTask.Result ?? [];
        _gpuDatabase = gpuTask.Result ?? [];

        // Mark all curated models
        _models = curated.Select(m => m with { Source = ModelSource.Curated }).ToList();
        _logger.LogInformation("Loaded {ModelCount} curated models and {GpuCount} GPU entries", _models.Count, _gpuDatabase.Count);
    }

    /// <summary>
    /// Loads HuggingFace models in the background and merges them into the catalog.
    /// Call after InitializeAsync so curated models are immediately available.
    /// </summary>
    public async Task LoadHuggingFaceModelsAsync()
    {
        var hfModels = await _hfService.FetchModelsAsync();

        if (hfModels.Count > 0)
        {
            MergeHuggingFaceModels(hfModels);
            HuggingFaceLoaded = true;
        }
    }

    private void MergeHuggingFaceModels(List<LlmModel> hfModels)
    {
        // Remove any previously merged HF models to avoid duplicates on refresh
        _models.RemoveAll(m => m.Source == ModelSource.HuggingFace);

        // Avoid adding HF models that duplicate curated ones (by HuggingFace ID)
        var curatedIds = _models
            .Where(m => !string.IsNullOrEmpty(m.HuggingFaceId))
            .Select(m => m.HuggingFaceId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unique = hfModels.Where(m => !curatedIds.Contains(m.HuggingFaceId)).ToList();
        _models.AddRange(unique);

        _logger.LogInformation("Merged {Count} unique HuggingFace models (total: {Total})", unique.Count, _models.Count);
    }

    public IReadOnlyList<LlmModel> GetModelsByCategory(string category)
    {
        return _models.Where(m => m.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    public IReadOnlyList<LlmModel> SearchModels(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _models;

        return _models.Where(m =>
            m.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            m.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            m.Categories.Any(c => c.Contains(query, StringComparison.OrdinalIgnoreCase))
        ).ToList();
    }

    public IReadOnlyList<string> GetAllCategories()
    {
        return _models.SelectMany(m => m.Categories).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c).ToList();
    }

    public GpuBandwidthEntry? FindGpu(string gpuName)
    {
        if (string.IsNullOrWhiteSpace(gpuName))
            return null;

        // Exact match first
        var exact = _gpuDatabase.FirstOrDefault(g =>
            g.Name.Equals(gpuName, StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact;

        // Partial match
        return _gpuDatabase.FirstOrDefault(g =>
            gpuName.Contains(g.Name, StringComparison.OrdinalIgnoreCase) ||
            g.Name.Contains(gpuName, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<string> GetGpuNames()
    {
        return _gpuDatabase.Select(g => g.Name).ToList();
    }

    public IReadOnlyList<GpuBandwidthEntry> GetGpusByArchitecture(string architecture)
    {
        return _gpuDatabase.Where(g =>
            g.Architecture.Equals(architecture, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private async Task<T?> LoadEmbeddedJsonAsync<T>(string resourceName)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                _logger.LogWarning("Embedded resource not found: {ResourceName}", resourceName);
                return default;
            }
            return await JsonSerializer.DeserializeAsync<T>(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load embedded resource: {ResourceName}", resourceName);
            return default;
        }
    }
}
