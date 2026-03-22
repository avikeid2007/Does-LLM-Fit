using DoesLLMFit.Helpers;
using System.Collections.ObjectModel;
using DoesLLMFit.Services;

namespace DoesLLMFit.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly ModelCatalogService _catalogService;
    private readonly CompatibilityCalculator _calculator;
    private readonly HuggingFaceService _hfService;
    private bool _isRebuildingOrgs;

    // ─── Hardware properties ─────────────────────────
    [ObservableProperty]
    private bool _isAppleSilicon;

    [ObservableProperty]
    private double _vramGb = 12;

    [ObservableProperty]
    private double _systemRamGb = 32;

    [ObservableProperty]
    private double _memoryBandwidthGBs;

    [ObservableProperty]
    private int _contextLength = 4096;

    [ObservableProperty]
    private int _unifiedMemoryPercent = 75;

    [ObservableProperty]
    private string _vramDisplayText = "12";

    [ObservableProperty]
    private string _vramLabelText = "12 GB VRAM";

    [ObservableProperty]
    private string _vramTypeText = "Dedicated GPU VRAM";

    [ObservableProperty]
    private string _ramDisplayText = "32";

    [ObservableProperty]
    private string _unifiedMemoryPercentText = "(75%)";

    [ObservableProperty]
    private bool _showUnifiedMemory;

    [ObservableProperty]
    private Microsoft.UI.Xaml.GridLength _unifiedMemoryColumnWidth = new(0);

    // ─── GPU list ────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<GpuItemViewModel> _gpuItems = [];

    [ObservableProperty]
    private GpuItemViewModel? _selectedGpu;

    // ─── Filters ─────────────────────────────────────
    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]    private string _selectedOrganization = "All";

    [ObservableProperty]
    private ObservableCollection<string> _organizationList = ["All"];

    [ObservableProperty]    private bool _includeHuggingFace = true;

    // ─── Results ─────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<ModelCardViewModel> _modelCards = [];

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private int _greenCount;

    [ObservableProperty]
    private int _yellowCount;

    [ObservableProperty]
    private int _redCount;

    [ObservableProperty]
    private string _resultsCountText = "";

    [ObservableProperty]
    private string _emptyHintCountText = "We'll check available LLMs against your specs";

    // ─── HF Loading ──────────────────────────────────
    [ObservableProperty]
    private bool _isLoadingHuggingFace;

    [ObservableProperty]
    private string _hfLoadingText = "Loading models from HuggingFace...";

    public MainPageViewModel(ModelCatalogService catalogService, CompatibilityCalculator calculator, HuggingFaceService hfService)
    {
        _catalogService = catalogService;
        _calculator = calculator;
        _hfService = hfService;
    }

    public async Task InitializeAsync()
    {
        await _catalogService.InitializeAsync();
        UpdateGpuList();
        UpdateModelCount();
        RebuildOrganizationList();

        // Background HF loading
        IsLoadingHuggingFace = true;
        try
        {
            await _catalogService.LoadHuggingFaceModelsAsync();
            HfLoadingText = $"Loaded {_catalogService.Models.Count} models from HuggingFace + curated list";
        }
        catch
        {
            HfLoadingText = "Could not reach HuggingFace — using curated models only";
        }
        finally
        {
            UpdateModelCount();
            RebuildOrganizationList();
            IsLoadingHuggingFace = false;
        }
    }

    // ─── Hardware change handlers ────────────────────

    partial void OnIsAppleSiliconChanged(bool value)
    {
        VramTypeText = value ? "Unified Memory" : "Dedicated GPU VRAM";
        ShowUnifiedMemory = value;
        UnifiedMemoryColumnWidth = value
            ? new Microsoft.UI.Xaml.GridLength(1, Microsoft.UI.Xaml.GridUnitType.Star)
            : new Microsoft.UI.Xaml.GridLength(0);
        UpdateGpuList();
    }

    partial void OnVramGbChanged(double value)
    {
        var v = (int)value;
        VramDisplayText = v.ToString();
        VramLabelText = $"{v} GB VRAM";
    }

    partial void OnSystemRamGbChanged(double value)
    {
        RamDisplayText = ((int)value).ToString();
    }

    partial void OnUnifiedMemoryPercentChanged(int value)
    {
        UnifiedMemoryPercentText = $"({value}%)";
    }

    partial void OnSelectedGpuChanged(GpuItemViewModel? value)
    {
        if (value is null) return;
        var gpu = _catalogService.FindGpu(value.Name);
        if (gpu is null) return;

        VramGb = gpu.VramGb;
        MemoryBandwidthGBs = gpu.BandwidthGBs;

        if (IsAppleSilicon)
        {
            SystemRamGb = gpu.VramGb;
        }
    }

    // ─── Filter change handlers ─────────────────────

    partial void OnSearchQueryChanged(string value) => RefreshIfHasResults();
    partial void OnSelectedCategoryChanged(string value) => RefreshIfHasResults();
    partial void OnSelectedOrganizationChanged(string value)
    {
        if (!_isRebuildingOrgs)
            RefreshIfHasResults();
    }
    partial void OnIncludeHuggingFaceChanged(bool value) => RefreshIfHasResults();

    private void RefreshIfHasResults()
    {
        if (HasResults)
            CheckFit();
    }

    // ─── GPU list builder ────────────────────────────

    private void UpdateGpuList()
    {
        var arch = IsAppleSilicon ? "AppleSilicon" : "PC";
        var gpuNames = _catalogService.GetGpusByArchitecture(arch).Select(g => g.Name).ToList();
        GpuItems.Clear();
        foreach (var name in gpuNames)
        {
            GpuItems.Add(new GpuItemViewModel(name));
        }
        SelectedGpu = null;
    }

    // ─── Filter helpers ──────────────────────────────

    private void UpdateModelCount()
    {
        var count = GetFilteredModels().Count;
        EmptyHintCountText = $"We'll check {count} models against your specs";
    }

    private IReadOnlyList<LlmModel> GetFilteredModels()
    {
        IEnumerable<LlmModel> models = _catalogService.Models;

        if (!IncludeHuggingFace)
            models = models.Where(m => m.Source == ModelSource.Curated);

        if (SelectedCategory is not "All" and not "")
            models = models.Where(m => m.Categories.Any(c =>
                c.Contains(SelectedCategory, StringComparison.OrdinalIgnoreCase)));

        if (SelectedOrganization is not "All" and not "")
            models = models.Where(m => GetOrganization(m) == SelectedOrganization);

        if (!string.IsNullOrWhiteSpace(SearchQuery))
            models = models.Where(m =>
                m.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                m.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                m.HuggingFaceId.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));

        return models.ToList();
    }

    private HardwareProfile BuildHardwareProfile()
    {
        return new HardwareProfile
        {
            Architecture = IsAppleSilicon ? ArchitectureType.AppleSilicon : ArchitectureType.PC,
            GpuName = SelectedGpu?.Name ?? "",
            VramGb = VramGb,
            SystemRamGb = SystemRamGb,
            MemoryBandwidthGBs = MemoryBandwidthGBs,
            ContextLength = ContextLength,
            UnifiedMemoryPercent = UnifiedMemoryPercent,
        };
    }

    // ─── Commands ────────────────────────────────────

    [RelayCommand]
    private void CheckFit()
    {
        var models = GetFilteredModels();
        if (models.Count == 0)
        {
            ModelCards.Clear();
            GreenCount = 0;
            YellowCount = 0;
            RedCount = 0;
            ResultsCountText = "(0 models evaluated)";
            return;
        }

        var hw = BuildHardwareProfile();
        var results = models.Select(m => _calculator.EvaluateModel(m, hw)).ToList();

        GreenCount = results.Count(r => r.OverallStatus == FitStatus.Green);
        YellowCount = results.Count(r => r.OverallStatus == FitStatus.Yellow);
        RedCount = results.Count(r => r.OverallStatus == FitStatus.Red);
        ResultsCountText = $"({results.Count} models evaluated)";

        var sorted = results
            .OrderBy(r => r.OverallStatus)
            .ThenByDescending(r => r.BestFit?.EstimatedToksPerSec ?? 0)
            .ToList();

        ModelCards.Clear();
        foreach (var summary in sorted)
        {
            ModelCards.Add(new ModelCardViewModel(summary, hw));
        }

        HasResults = true;
    }

    private void RebuildOrganizationList()
    {
        var orgs = _catalogService.Models
            .Select(GetOrganization)
            .Where(o => !string.IsNullOrEmpty(o))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(o => o, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var current = SelectedOrganization;
        _isRebuildingOrgs = true;
        try
        {
            OrganizationList.Clear();
            OrganizationList.Add("All");
            foreach (var org in orgs)
                OrganizationList.Add(org);

            SelectedOrganization = OrganizationList.Contains(current) ? current : "All";
        }
        finally
        {
            _isRebuildingOrgs = false;
        }
    }

    public static string GetOrganization(LlmModel model)
    {
        // HuggingFace models: org/model-name format
        if (!string.IsNullOrEmpty(model.HuggingFaceId) && model.HuggingFaceId.Contains('/'))
            return model.HuggingFaceId.Split('/')[0];

        // Curated models: extract from name (e.g. "Llama 3.1" → "Meta")
        var name = model.Name;
        if (name.StartsWith("Llama", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("CodeLlama", StringComparison.OrdinalIgnoreCase))
            return "meta-llama";
        if (name.StartsWith("Mistral", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Mixtral", StringComparison.OrdinalIgnoreCase))
            return "mistralai";
        if (name.StartsWith("Gemma", StringComparison.OrdinalIgnoreCase))
            return "google";
        if (name.StartsWith("Phi", StringComparison.OrdinalIgnoreCase))
            return "microsoft";
        if (name.StartsWith("Qwen", StringComparison.OrdinalIgnoreCase))
            return "Qwen";
        if (name.StartsWith("DeepSeek", StringComparison.OrdinalIgnoreCase))
            return "deepseek-ai";
        if (name.StartsWith("Falcon", StringComparison.OrdinalIgnoreCase))
            return "tiiuae";
        if (name.StartsWith("Stable", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("SDXL", StringComparison.OrdinalIgnoreCase))
            return "stabilityai";

        return "Other";
    }
}

// ─── GPU item for ComboBox binding ──────────────────

public partial class GpuItemViewModel : ObservableObject
{
    public string Name { get; }

    public GpuItemViewModel(string name)
    {
        Name = name;
    }

    public override string ToString() => Name;
}

// ─── Model card view model for GridView binding ─────

public partial class ModelCardViewModel : ObservableObject
{
    public ModelCompatibilitySummary Summary { get; }
    public HardwareProfile Hardware { get; }

    // Pre-computed display properties
    public string ModelName => Summary.Model.Name;
    public string ParameterText => Summary.Model.ParametersB < 10
        ? $"{Summary.Model.ParametersB:G3}B"
        : $"{Summary.Model.ParametersB:F0}B";
    public FitStatus Status => Summary.OverallStatus;
    public string StatusGlyph => Status switch
    {
        FitStatus.Green => "\uE73E",
        FitStatus.Yellow => "\uE7BA",
        _ => "\uE711",
    };
    public Windows.UI.Color StatusColor => Status switch
    {
        FitStatus.Green => Windows.UI.Color.FromArgb(255, 76, 175, 80),
        FitStatus.Yellow => Windows.UI.Color.FromArgb(255, 255, 193, 7),
        _ => Windows.UI.Color.FromArgb(255, 244, 67, 54),
    };
    public Windows.UI.Color CardBackground => Status switch
    {
        FitStatus.Green => Windows.UI.Color.FromArgb(255, 14, 36, 28),
        FitStatus.Yellow => Windows.UI.Color.FromArgb(255, 38, 33, 14),
        _ => Windows.UI.Color.FromArgb(255, 40, 18, 20),
    };

    // VRAM
    public bool HasBestFit => Summary.BestFit is not null;
    public double VramEstimated => Summary.BestFit?.EstimatedVramGb ?? 0;
    public string VramText => $"VRAM: ~{VramEstimated:F1} GB";
    public double VramPercent
    {
        get
        {
            var avail = Hardware.EffectiveVramGb;
            return avail > 0 ? Math.Min(VramEstimated / avail, 1.0) : 1.0;
        }
    }
    public string VramPercentText => $"{VramPercent * 100:F0}%";

    // Tok/s
    public bool HasToksPerSec => Summary.BestFit?.EstimatedToksPerSec is not null;
    public string ToksPerSecText => Summary.BestFit?.EstimatedToksPerSec is { } t ? $"~{t:F0} tok/s" : "";
    public string QuantDisplayName => Summary.BestFit?.QuantDisplayName ?? "";

    // Category
    public string PrimaryCategory => Summary.Model.Categories.FirstOrDefault() ?? "General";
    public (string label, string glyph, Windows.UI.Color color) CategoryInfo => PrimaryCategory switch
    {
        "General Chat" => ("General Chat", "\uE8BD", Windows.UI.Color.FromArgb(255, 68, 164, 255)),
        "Text Generation" => ("Text Generation", "\uE8BD", Windows.UI.Color.FromArgb(255, 68, 164, 255)),
        "Coding" => ("Developer", "\uE943", Windows.UI.Color.FromArgb(255, 130, 180, 80)),
        "Reasoning" => ("Reasoning", "\uEA80", Windows.UI.Color.FromArgb(255, 180, 130, 255)),
        "Small & Fast" => ("Lightweight", "\uE916", Windows.UI.Color.FromArgb(255, 255, 180, 60)),
        "Multimodal" or "Vision" => ("Multimodal", "\uE8B9", Windows.UI.Color.FromArgb(255, 255, 120, 150)),
        "Image Generation" => ("Image Gen", "\uE8B3", Windows.UI.Color.FromArgb(255, 200, 100, 255)),
        "Video Generation" => ("Video Gen", "\uE714", Windows.UI.Color.FromArgb(255, 255, 140, 60)),
        "Audio Generation" => ("Audio Gen", "\uE8D6", Windows.UI.Color.FromArgb(255, 100, 200, 200)),
        "Speech Recognition" => ("Speech", "\uE720", Windows.UI.Color.FromArgb(255, 100, 180, 255)),
        "Text to Speech" => ("TTS", "\uE767", Windows.UI.Color.FromArgb(255, 180, 200, 100)),
        _ => (PrimaryCategory, "\uE8F1", Windows.UI.Color.FromArgb(255, 180, 180, 180)),
    };
    public string CategoryLabel => CategoryInfo.label;
    public string CategoryGlyph => CategoryInfo.glyph;
    public Windows.UI.Color CategoryColor => CategoryInfo.color;

    // Source
    public bool IsHuggingFace => Summary.Model.Source == ModelSource.HuggingFace;

    // Organization
    public string Organization => MainPageViewModel.GetOrganization(Summary.Model);
    public string OrgDisplayName => OrgLogoHelper.GetDisplayName(Organization);
    public string OrgInitial => string.IsNullOrEmpty(OrgDisplayName) ? "?" : OrgDisplayName[..1].ToUpperInvariant();
    public string OrgAvatarUrl => OrgLogoHelper.GetAvatarUrl(Organization);

    public ModelCardViewModel(ModelCompatibilitySummary summary, HardwareProfile hardware)
    {
        Summary = summary;
        Hardware = hardware;
    }
}
