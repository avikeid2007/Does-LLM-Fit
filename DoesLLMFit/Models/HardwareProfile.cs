namespace DoesLLMFit.Models;

public enum ArchitectureType
{
    PC,
    AppleSilicon
}

public partial class HardwareProfile : ObservableObject
{
    [ObservableProperty]
    private ArchitectureType _architecture = ArchitectureType.PC;

    [ObservableProperty]
    private string _gpuName = string.Empty;

    [ObservableProperty]
    private double _vramGb;

    [ObservableProperty]
    private double _systemRamGb;

    /// <summary>
    /// Percentage of unified memory available for LLM inference (Apple Silicon only).
    /// Default 75% since macOS reserves some for OS/apps.
    /// </summary>
    [ObservableProperty]
    private int _unifiedMemoryPercent = 75;

    /// <summary>
    /// Memory bandwidth in GB/s. Auto-filled from GPU lookup, with manual override.
    /// </summary>
    [ObservableProperty]
    private double _memoryBandwidthGBs;

    [ObservableProperty]
    private int _contextLength = 4096;

    /// <summary>
    /// Returns the effective VRAM available for model loading.
    /// For Apple Silicon, applies the unified memory percentage.
    /// </summary>
    public double EffectiveVramGb => Architecture == ArchitectureType.AppleSilicon
        ? (VramGb > 0 ? VramGb : SystemRamGb) * UnifiedMemoryPercent / 100.0
        : VramGb;
}
