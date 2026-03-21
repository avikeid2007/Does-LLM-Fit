using DoesLLMFit.Services;

namespace DoesLLMFit.ViewModels;

public partial class HardwareSetupViewModel : ObservableObject
{
    private readonly ModelCatalogService _catalogService;

    [ObservableProperty]
    private HardwareProfile _hardware = new();

    [ObservableProperty]
    private IReadOnlyList<string> _gpuNames = [];

    [ObservableProperty]
    private string? _selectedGpuName;

    [ObservableProperty]
    private IReadOnlyList<int> _contextLengthOptions = [2048, 4096, 8192, 16384, 32768];

    [ObservableProperty]
    private int _selectedContextLength = 4096;

    [ObservableProperty]
    private bool _isAppleSilicon;

    public HardwareSetupViewModel(ModelCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task InitializeAsync()
    {
        await _catalogService.InitializeAsync();
        UpdateGpuList();
    }

    partial void OnIsAppleSiliconChanged(bool value)
    {
        Hardware.Architecture = value ? ArchitectureType.AppleSilicon : ArchitectureType.PC;
        UpdateGpuList();
        SelectedGpuName = null;
    }

    partial void OnSelectedGpuNameChanged(string? value)
    {
        if (value is not null)
        {
            var gpu = _catalogService.FindGpu(value);
            if (gpu is not null)
            {
                Hardware.VramGb = gpu.VramGb;
                Hardware.MemoryBandwidthGBs = gpu.BandwidthGBs;
                if (Hardware.Architecture == ArchitectureType.AppleSilicon)
                {
                    Hardware.SystemRamGb = gpu.VramGb; // Unified memory
                }
            }
        }
    }

    partial void OnSelectedContextLengthChanged(int value)
    {
        Hardware.ContextLength = value;
    }

    private void UpdateGpuList()
    {
        var arch = IsAppleSilicon ? "AppleSilicon" : "PC";
        GpuNames = _catalogService.GetGpusByArchitecture(arch)
            .Select(g => g.Name)
            .ToList();
    }
}
