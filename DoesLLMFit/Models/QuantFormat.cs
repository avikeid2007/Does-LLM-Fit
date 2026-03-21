namespace DoesLLMFit.Models;

public enum QuantType
{
    Q2_K,
    Q3_K_M,
    Q4_K_M,
    Q5_K_M,
    Q6_K,
    Q8_0,
    F16,
    F32
}

public static class QuantInfo
{
    /// <summary>
    /// Effective bits-per-weight for each GGUF quantization format.
    /// These values account for block overhead in the quantization scheme.
    /// </summary>
    public static double GetBitsPerWeight(QuantType quant) => quant switch
    {
        QuantType.Q2_K   => 2.5625,
        QuantType.Q3_K_M => 3.4375,
        QuantType.Q4_K_M => 4.8125,
        QuantType.Q5_K_M => 5.5625,
        QuantType.Q6_K   => 6.5625,
        QuantType.Q8_0   => 8.5,
        QuantType.F16    => 16.0,
        QuantType.F32    => 32.0,
        _ => 4.8125 // default to Q4_K_M
    };

    public static string GetDisplayName(QuantType quant) => quant switch
    {
        QuantType.Q2_K   => "Q2_K",
        QuantType.Q3_K_M => "Q3_K_M",
        QuantType.Q4_K_M => "Q4_K_M",
        QuantType.Q5_K_M => "Q5_K_M",
        QuantType.Q6_K   => "Q6_K",
        QuantType.Q8_0   => "Q8_0",
        QuantType.F16    => "F16",
        QuantType.F32    => "F32",
        _ => quant.ToString()
    };

    public static IReadOnlyList<QuantType> All { get; } =
    [
        QuantType.Q2_K,
        QuantType.Q3_K_M,
        QuantType.Q4_K_M,
        QuantType.Q5_K_M,
        QuantType.Q6_K,
        QuantType.Q8_0,
        QuantType.F16,
        QuantType.F32
    ];
}
