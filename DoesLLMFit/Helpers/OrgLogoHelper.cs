using Windows.UI;

namespace DoesLLMFit.Helpers;

/// <summary>
/// SVG path data and brand colors for known AI model organizations.
/// Used by both model cards and the org filter ComboBox.
/// </summary>
public static class OrgLogoHelper
{
    // ─── SVG Path Data ───────────────────────────────

    // Meta (Llama) — infinity/meta logo simplified
    private const string MetaPath = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1.12 14.88c-.68.72-1.37 1.12-2.08 1.12-.56 0-.98-.22-1.24-.64-.36-.58-.4-1.48-.1-2.7.34-1.42.94-2.82 1.74-4.14.66-1.08 1.36-1.86 2.04-2.3.54-.35 1.02-.42 1.4-.2.38.22.58.66.58 1.28 0 .78-.26 1.8-.76 3.02-.5 1.24-1.06 2.36-1.58 3.36v1.2zm4.92-1.2c-.52 1-1.08 2.12-1.58 3.36v-1.2c.68-.72 1.37-1.12 2.08-1.12.56 0 .98.22 1.24.64.36.58.4 1.48.1 2.7-.34 1.42-.94 2.82-1.74 4.14-.66 1.08-1.36 1.86-2.04 2.3-.54.35-1.02.42-1.4.2-.38-.22-.58-.66-.58-1.28 0-.78.26-1.8.76-3.02z";

    // Google (Gemma) — G shape
    private const string GooglePath = "M12.48 10.92v3.28h7.84c-.24 1.84-.853 3.187-1.787 4.133-1.147 1.147-2.933 2.4-6.053 2.4-4.827 0-8.6-3.893-8.6-8.72s3.773-8.72 8.6-8.72c2.6 0 4.507 1.027 5.907 2.347l2.307-2.307C18.747 1.44 16.133 0 12.48 0 5.867 0 .307 5.387.307 12s5.56 12 12.173 12c3.573 0 6.267-1.173 8.373-3.36 2.16-2.16 2.84-5.213 2.84-7.667 0-.76-.053-1.467-.173-2.053H12.48z";

    // Microsoft (Phi) — four squares
    private const string MicrosoftPath = "M0 0h10.931v10.931H0zM12.069 0H23v10.931H12.069zM0 12.069h10.931V23H0zM12.069 12.069H23V23H12.069z";

    // Mistral AI — stacked blocks
    private const string MistralPath = "M2 2h4v4H2zm16 0h4v4h-4zM2 8h4v4H2zm8 0h4v4h-4zm8 0h4v4h-4zM2 14h4v4H2zm4 0h4v4H6zm4 0h4v4h-4zm4 0h4v4h-4zm4 0h4v4h-4zM2 20h4v4H2zm16 0h4v4h-4z";

    // Qwen (Alibaba)
    private const string QwenPath = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 3c1.66 0 3 1.34 3 3s-1.34 3-3 3-3-1.34-3-3 1.34-3 3-3zm0 14.2c-2.5 0-4.71-1.28-6-3.22.03-1.99 4-3.08 6-3.08 1.99 0 5.97 1.09 6 3.08-1.29 1.94-3.5 3.22-6 3.22z";

    // DeepSeek — whale/deep sea
    private const string DeepSeekPath = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z";

    // NVIDIA
    private const string NvidiaPath = "M8.948 8.798v-1.43a6.7 6.7 0 0 1 .424-.018c3.922-.124 6.493 3.374 6.493 3.374s-2.774 3.851-5.75 3.851c-.398 0-.787-.062-1.158-.185v-4.346c1.528.185 1.837.857 2.747 2.385l2.04-1.714s-1.492-1.952-4-1.952a6.016 6.016 0 0 0-.796.035m0-4.735v2.138l.424-.027c5.45-.185 9.01 4.47 9.01 4.47s-4.08 4.964-8.33 4.964c-.37 0-.733-.035-1.095-.097v1.325c.3.035.61.062.91.062 3.957 0 6.82-2.023 9.593-4.408.459.371 2.34 1.263 2.73 1.652-2.633 2.208-8.772 3.984-12.253 3.984-.335 0-.653-.018-.971-.053v1.864H24V4.063zm0 10.326v1.131c-3.657-.654-4.673-4.46-4.673-4.46s1.758-1.944 4.673-2.262v1.237H8.94c-1.528-.186-2.73 1.245-2.73 1.245s.68 2.412 2.739 3.11M2.456 10.9s2.164-3.197 6.5-3.533V6.201C4.153 6.59 0 10.653 0 10.653s2.35 6.802 8.948 7.42v-1.237c-4.84-.6-6.492-5.936-6.492-5.936z";

    // Apple
    private const string ApplePath = "M18.71 19.5C17.88 20.74 17 21.95 15.66 21.97C14.32 22 13.89 21.18 12.37 21.18C10.84 21.18 10.37 21.95 9.09997 22C7.78997 22.05 6.79997 20.68 5.95997 19.47C4.24997 17 2.93997 12.45 4.69997 9.39C5.56997 7.87 7.12997 6.91 8.81997 6.88C10.1 6.86 11.32 7.75 12.11 7.75C12.89 7.75 14.37 6.68 15.92 6.84C16.57 6.87 18.39 7.1 19.56 8.82C19.47 8.88 17.39 10.1 17.41 12.63C17.44 15.65 20.06 16.66 20.09 16.67C20.06 16.74 19.67 18.11 18.71 19.5ZM13 3.5C13.73 2.67 14.94 2.04 15.94 2C16.07 3.17 15.6 4.35 14.9 5.19C14.21 6.04 13.07 6.7 11.95 6.61C11.8 5.46 12.36 4.26 13 3.5Z";

    // Stability AI
    private const string StabilityPath = "M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5";

    // Hugging Face
    private const string HuggingFacePath = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zM8.5 8C9.33 8 10 8.67 10 9.5S9.33 11 8.5 11 7 10.33 7 9.5 7.67 8 8.5 8zm7 0c.83 0 1.5.67 1.5 1.5s-.67 1.5-1.5 1.5S14 10.33 14 9.5 14.67 8 15.5 8zm-3.5 9.5c-2.33 0-4.31-1.46-5.11-3.5h10.22c-.8 2.04-2.78 3.5-5.11 3.5z";

    // Default circle with first letter
    private const string DefaultOrgPath = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2z";

    /// <summary>Returns (pathData, viewboxSize, brandColor) for a given organization name.</summary>
    public static (string PathData, double ViewboxSize, Color BrandColor) GetOrgLogo(string orgName)
    {
        return orgName.ToLowerInvariant() switch
        {
            "meta-llama" or "meta" or "facebook" => (MetaPath, 24, Color.FromArgb(255, 0, 120, 215)),
            "google" => (GooglePath, 24, Color.FromArgb(255, 66, 133, 244)),
            "microsoft" => (MicrosoftPath, 23, Color.FromArgb(255, 0, 120, 215)),
            "mistralai" => (MistralPath, 24, Color.FromArgb(255, 255, 140, 0)),
            "qwen" or "alibaba" => (QwenPath, 24, Color.FromArgb(255, 108, 92, 231)),
            "deepseek-ai" or "deepseek" => (DeepSeekPath, 24, Color.FromArgb(255, 76, 175, 80)),
            "nvidia" => (NvidiaPath, 24, Color.FromArgb(255, 118, 185, 0)),
            "apple" => (ApplePath, 24, Color.FromArgb(255, 162, 170, 173)),
            "stabilityai" or "stability" => (StabilityPath, 24, Color.FromArgb(255, 190, 80, 255)),
            "huggingfacetb" or "bigscience" => (HuggingFacePath, 24, Color.FromArgb(255, 255, 208, 65)),
            "black-forest-labs" => (StabilityPath, 24, Color.FromArgb(255, 40, 200, 160)),
            "tiiuae" => (DefaultOrgPath, 24, Color.FromArgb(255, 200, 160, 60)),
            "nousresearch" => (DefaultOrgPath, 24, Color.FromArgb(255, 255, 100, 100)),
            "01-ai" => (DefaultOrgPath, 24, Color.FromArgb(255, 100, 180, 255)),
            "eleutherai" => (DefaultOrgPath, 24, Color.FromArgb(255, 180, 120, 255)),
            "cohereforai" or "cohere" => (DefaultOrgPath, 24, Color.FromArgb(255, 57, 89, 215)),
            "databricks" => (DefaultOrgPath, 24, Color.FromArgb(255, 255, 50, 50)),
            "mosaicml" => (DefaultOrgPath, 24, Color.FromArgb(255, 100, 200, 100)),
            _ => (DefaultOrgPath, 24, Color.FromArgb(255, 100, 140, 180)),
        };
    }

    /// <summary>Returns just the brand color for an org (useful for text-only display).</summary>
    public static Color GetOrgColor(string orgName)
    {
        return GetOrgLogo(orgName).BrandColor;
    }

    /// <summary>Gets a short display name for an org.</summary>
    public static string GetDisplayName(string orgName)
    {
        return orgName.ToLowerInvariant() switch
        {
            "meta-llama" => "Meta",
            "mistralai" => "Mistral AI",
            "deepseek-ai" => "DeepSeek",
            "stabilityai" => "Stability AI",
            "huggingfacetb" => "HuggingFace",
            "black-forest-labs" => "Black Forest Labs",
            "tiiuae" => "TII UAE",
            "nousresearch" => "Nous Research",
            "01-ai" => "01.AI",
            "eleutherai" => "EleutherAI",
            "cohereforai" => "Cohere",
            "mosaicml" => "MosaicML",
            _ => orgName,
        };
    }
}
