using System.Reflection;
using System.Text.Json;

namespace DoesLLMFit.Services;

public class ModelCatalogService
{
    private readonly ILogger<ModelCatalogService> _logger;
    private List<LlmModel> _models = [];
    private List<GpuBandwidthEntry> _gpuDatabase = [];

    public ModelCatalogService(ILogger<ModelCatalogService> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<LlmModel> Models => _models;
    public IReadOnlyList<GpuBandwidthEntry> GpuDatabase => _gpuDatabase;

    public async Task InitializeAsync()
    {
        _models = await LoadEmbeddedJsonAsync<List<LlmModel>>("DoesLLMFit.Data.models.json") ?? [];
        _gpuDatabase = await LoadEmbeddedJsonAsync<List<GpuBandwidthEntry>>("DoesLLMFit.Data.gpu-bandwidth.json") ?? [];
        _logger.LogInformation("Loaded {ModelCount} models and {GpuCount} GPU entries", _models.Count, _gpuDatabase.Count);
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
