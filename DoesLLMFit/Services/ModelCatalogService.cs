using System.Reflection;
using System.Text.Json;
using Windows.Storage;

namespace DoesLLMFit.Services;

public class ModelCatalogService
{
    private readonly ILogger<ModelCatalogService> _logger;
    private readonly HttpClient _http = new();
    private List<LlmModel> _models = [];
    private List<GpuBandwidthEntry> _gpuDatabase = [];

    private const string BaseUrl = "https://raw.githubusercontent.com/avikeid2007/DoesLLMFit/main/DoesLLMFit/Data/";
    private const string ModelsFileName = "models.json";
    private const string GpuFileName = "gpu-bandwidth.json";

    public ModelCatalogService(ILogger<ModelCatalogService> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<LlmModel> Models => _models;
    public IReadOnlyList<GpuBandwidthEntry> GpuDatabase => _gpuDatabase;

    public async Task InitializeAsync()
    {
        _models = await LoadJsonAsync<List<LlmModel>>(ModelsFileName, "DoesLLMFit.Data.models.json") ?? [];
        _gpuDatabase = await LoadJsonAsync<List<GpuBandwidthEntry>>(GpuFileName, "DoesLLMFit.Data.gpu-bandwidth.json") ?? [];
        _logger.LogInformation("Loaded {ModelCount} models and {GpuCount} GPU entries", _models.Count, _gpuDatabase.Count);
    }

    /// <summary>Try: GitHub raw → local cache → embedded resource.</summary>
    private async Task<T?> LoadJsonAsync<T>(string fileName, string embeddedName)
    {
        // 1. Try remote
        var remote = await TryFetchRemoteAsync<T>(fileName);
        if (remote is not null)
        {
            await SaveToCacheAsync(fileName, remote);
            return remote;
        }

        // 2. Try local cache
        var cached = await TryLoadCacheAsync<T>(fileName);
        if (cached is not null)
        {
            _logger.LogInformation("Using cached {File}", fileName);
            return cached;
        }

        // 3. Fallback to embedded
        _logger.LogInformation("Using embedded {File}", fileName);
        return await LoadEmbeddedJsonAsync<T>(embeddedName);
    }

    private async Task<T?> TryFetchRemoteAsync<T>(string fileName)
    {
        try
        {
            var url = BaseUrl + fileName;
            using var response = await _http.GetAsync(url).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return default;

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<T>(json);
            _logger.LogInformation("Fetched latest {File} from GitHub", fileName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch remote {File}, will use cache/embedded", fileName);
            return default;
        }
    }

    private static async Task SaveToCacheAsync<T>(string fileName, T data)
    {
        try
        {
            var folder = ApplicationData.Current.LocalFolder;
            var file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            var json = JsonSerializer.Serialize(data);
            await FileIO.WriteTextAsync(file, json);
        }
        catch { /* non-critical */ }
    }

    private static async Task<T?> TryLoadCacheAsync<T>(string fileName)
    {
        try
        {
            var folder = ApplicationData.Current.LocalFolder;
            var file = await folder.GetFileAsync(fileName);
            var json = await FileIO.ReadTextAsync(file);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch { return default; }
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
