namespace DoesLLMFit.Services;

public class CompatibilityCalculator
{
    private const double OverheadBufferGb = 0.5; // 500MB overhead for runtime/framework

    /// <summary>
    /// Returns an overhead multiplier based on the model's pipeline type.
    /// LLM: 1.15 (KV cache + activations), Diffusion: 1.25 (VAE + text encoder),
    /// Multimodal: 1.20 (vision encoder), Audio/ASR: 1.10.
    /// </summary>
    private static double GetOverheadMultiplier(string pipelineTag) => pipelineTag switch
    {
        "text-to-image" or "image-to-image" => 1.25,
        "text-to-video" => 1.30,
        "image-text-to-text" or "any-to-any" => 1.20,
        "text-to-audio" or "automatic-speech-recognition" or "text-to-speech" => 1.10,
        _ => 1.0, // LLMs use explicit KV cache calculation already
    };

    /// <summary>
    /// Calculates estimated VRAM usage for a model at a given quantization and context length.
    /// For LLMs: VRAM = (Params x BitsPerWeight / 8) + KV-cache + Overhead
    /// For non-LLMs: VRAM = (Params x BitsPerWeight / 8) x OverheadMultiplier + Overhead
    /// </summary>
    public double CalculateVramGb(LlmModel model, QuantType quant, int contextLength)
    {
        double bitsPerWeight = QuantInfo.GetBitsPerWeight(quant);
        double modelWeightsGb = model.ParametersB * bitsPerWeight / 8.0;

        bool isLlm = model.PipelineTag is "text-generation" or "";
        if (isLlm)
        {
            // KV-cache estimation for LLMs
            double kvBytesPerElement = 2.0;
            double kvCacheGb = 0;
            if (model.NumLayers > 0 && model.KvHeads > 0 && model.HeadDim > 0)
            {
                kvCacheGb = 2.0 * model.NumLayers * model.KvHeads * model.HeadDim
                            * contextLength * kvBytesPerElement
                            / (1024.0 * 1024.0 * 1024.0);
            }
            else
            {
                // Fallback: estimate KV cache as ~10-15% of model size per 4K context
                kvCacheGb = modelWeightsGb * 0.12 * (contextLength / 4096.0);
            }

            return modelWeightsGb + kvCacheGb + OverheadBufferGb;
        }
        else
        {
            // Non-LLM models (diffusion, audio, multimodal): use overhead multiplier
            double multiplier = GetOverheadMultiplier(model.PipelineTag);
            return modelWeightsGb * multiplier + OverheadBufferGb;
        }
    }

    /// <summary>
    /// Estimates tokens per second based on memory bandwidth and model size.
    /// tok/s ≈ Bandwidth(GB/s) / ModelSizeInMemory(GB)
    /// </summary>
    public double? EstimateToksPerSec(double modelVramGb, double bandwidthGBs)
    {
        if (bandwidthGBs <= 0 || modelVramGb <= 0)
            return null;

        return bandwidthGBs / modelVramGb;
    }

    /// <summary>
    /// Determines fit status based on estimated vs available VRAM.
    /// Green: fits with 20%+ headroom
    /// Yellow: fits but within 10-20% headroom
    /// Red: exceeds available memory
    /// </summary>
    public FitStatus DetermineFitStatus(double estimatedVramGb, double availableVramGb)
    {
        if (availableVramGb <= 0)
            return FitStatus.Red;

        double ratio = estimatedVramGb / availableVramGb;

        return ratio switch
        {
            <= 0.80 => FitStatus.Green,  // 20%+ headroom
            <= 1.00 => FitStatus.Yellow, // fits but tight (0-20% headroom)
            _ => FitStatus.Red           // exceeds available
        };
    }

    /// <summary>
    /// Evaluates a single model at a specific quantization against the given hardware.
    /// </summary>
    public CompatibilityResult Evaluate(LlmModel model, QuantType quant, HardwareProfile hardware)
    {
        double estimatedVram = CalculateVramGb(model, quant, hardware.ContextLength);
        double availableVram = hardware.EffectiveVramGb;
        var status = DetermineFitStatus(estimatedVram, availableVram);
        double? toksPerSec = EstimateToksPerSec(estimatedVram, hardware.MemoryBandwidthGBs);

        string explanation = BuildExplanation(model, quant, estimatedVram, availableVram, status, toksPerSec, hardware);

        return new CompatibilityResult
        {
            Model = model,
            Quant = quant,
            EstimatedVramGb = Math.Round(estimatedVram, 2),
            AvailableVramGb = Math.Round(availableVram, 2),
            Status = status,
            EstimatedToksPerSec = toksPerSec.HasValue ? Math.Round(toksPerSec.Value, 1) : null,
            Explanation = explanation
        };
    }

    /// <summary>
    /// Evaluates a model across all supported quantizations, returning best-fit and all options.
    /// </summary>
    public ModelCompatibilitySummary EvaluateModel(LlmModel model, HardwareProfile hardware)
    {
        var supportedQuants = GetSupportedQuants(model);
        var allResults = supportedQuants
            .Select(q => Evaluate(model, q, hardware))
            .OrderBy(r => r.EstimatedVramGb)
            .ToList();

        // Best fit = the highest quality quant that still fits (Green or Yellow)
        var bestFit = allResults
            .Where(r => r.Status != FitStatus.Red)
            .OrderByDescending(r => QuantInfo.GetBitsPerWeight(r.Quant))
            .FirstOrDefault();

        return new ModelCompatibilitySummary
        {
            Model = model,
            BestFit = bestFit,
            AllQuants = allResults
        };
    }

    private static IReadOnlyList<QuantType> GetSupportedQuants(LlmModel model)
    {
        if (model.SupportedQuants.Count > 0)
        {
            var parsed = new List<QuantType>();
            foreach (var name in model.SupportedQuants)
            {
                if (Enum.TryParse<QuantType>(name, ignoreCase: true, out var qt))
                    parsed.Add(qt);
            }
            return parsed.Count > 0 ? parsed : (IReadOnlyList<QuantType>)QuantInfo.All;
        }
        return QuantInfo.All;
    }

    private static string BuildExplanation(
        LlmModel model, QuantType quant,
        double estimatedVram, double availableVram,
        FitStatus status, double? toksPerSec,
        HardwareProfile hardware)
    {
        var quantName = QuantInfo.GetDisplayName(quant);
        var isLlm = model.PipelineTag is "text-generation" or "";
        var lines = new List<string>
        {
            $"{model.Name} at {quantName} quantization:",
            $"  Model weights: {model.ParametersB:F1}B params × {QuantInfo.GetBitsPerWeight(quant):F1} bits/weight = {model.ParametersB * QuantInfo.GetBitsPerWeight(quant) / 8.0:F1} GB",
        };

        if (isLlm)
        {
            lines.Add($"  + KV cache for {hardware.ContextLength:N0} context tokens");
        }
        else
        {
            var multiplier = GetOverheadMultiplier(model.PipelineTag);
            lines.Add($"  × {multiplier:F2} overhead ({model.PipelineTag})");
        }

        lines.AddRange([
            $"  + 0.5 GB runtime overhead",
            $"  = {estimatedVram:F1} GB estimated total",
            $"  Available: {availableVram:F1} GB"
        ]);

        if (hardware.Architecture == ArchitectureType.AppleSilicon)
        {
            lines.Add($"  (Unified memory: {hardware.UnifiedMemoryPercent}% allocated to inference)");
        }

        lines.Add(status switch
        {
            FitStatus.Green => "  ✓ Fits comfortably with headroom to spare.",
            FitStatus.Yellow => "  ⚠ Tight fit — may experience slower performance.",
            FitStatus.Red => "  ✗ Exceeds available memory — will not load.",
            _ => string.Empty
        });

        if (toksPerSec.HasValue)
        {
            lines.Add($"  Estimated speed: ~{toksPerSec.Value:F1} tok/s");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
