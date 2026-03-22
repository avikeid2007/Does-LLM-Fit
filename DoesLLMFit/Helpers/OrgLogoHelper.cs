namespace DoesLLMFit.Helpers;

/// <summary>
/// Real avatar image URLs and brand colors for known AI model organizations.
/// Avatar URLs sourced from HuggingFace org profiles.
/// </summary>
public static class OrgLogoHelper
{
    private const string HfDefaultAvatar = "https://huggingface.co/front/assets/huggingface_logo-noborder.svg";

    private static readonly Dictionary<string, string> OrgAvatarUrls = new(StringComparer.OrdinalIgnoreCase)
    {
        ["meta-llama"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/646cf8084eefb026fb8fd8bc/oCTqufkdTkjyGodsx1vo1.png",
        ["meta"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/646cf8084eefb026fb8fd8bc/oCTqufkdTkjyGodsx1vo1.png",
        ["facebook"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/646cf8084eefb026fb8fd8bc/oCTqufkdTkjyGodsx1vo1.png",
        ["google"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/5dd96eb166059660ed1ee413/WtA3YYitedOr9n02eHfJe.png",
        ["microsoft"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/1583646260758-5e64858c87403103f9f1055d.png",
        ["mistralai"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/634c17653d11eaedd88b314d/9OgyfKstSZtbmsmuG8MbU.png",
        ["qwen"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/620760a26e3b7210c2ff1943/-s1gyJfvbE1RgO5iBeNOi.png",
        ["deepseek-ai"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/6538815d1bdb3c40db94fbfa/xMBly9PUMphrFVMxLX4kq.png",
        ["nvidia"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/1613114437487-60262a8e0703121c822a80b6.png",
        ["apple"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/1653390727490-5dd96eb166059660ed1ee413.jpeg",
        ["stabilityai"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/643feeb67bc3fbde1385cc25/7vmYr2XwVcPtkLzac_jxQ.png",
        ["huggingfacetb"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/651e96991b97c9f33d26bde6/e4VK7uW5sTeCYupD0s_ob.png",
        ["black-forest-labs"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/633f7a8f4be90e06da248e0f/m5YoF33abJ09vcwFxt1Mj.png",
        ["tiiuae"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/61a8d1aac664736898ffc84f/AT6cAB5ZNwCcqFMal71WD.jpeg",
        ["nousresearch"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/6317aade83d8d2fd903192d9/tPLjYEeP6q1w0j_G2TJG_.png",
        ["01-ai"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/6536187279f1de44b5e02d0f/-T8Xw0mX67_R73b7Re1y-.png",
        ["eleutherai"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/1614054059123-603481bb60e3dd96631c9095.png",
        ["cohereforai"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/660eb9ff338e9556c90a6bbc/9DrmMdvUZKoHP3hRTngvc.png",
        ["databricks"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/1659441070587-62e90f4cf7e720c6b13648d8.png",
        ["mosaicml"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/1676334017493-63ead3a0d0b894bbc77b199c.png",
        ["bigscience"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/1634806038075-5df7e9e5da6d0311fd3d53f9.png",
        ["allenai"] = "https://cdn-avatars.huggingface.co/v1/production/uploads/652db071b62cf1f8463221e2/CxxwFiaomTa1MCX_B7-pT.png",
    };

    /// <summary>Returns the avatar image URL for an org. Falls back to HF default logo for unknown orgs.</summary>
    public static string GetAvatarUrl(string orgName)
    {
        return OrgAvatarUrls.TryGetValue(orgName, out var url) ? url : HfDefaultAvatar;
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
