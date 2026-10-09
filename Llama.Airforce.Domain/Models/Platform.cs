namespace Llama.Airforce.Domain.Models;

public enum Platform
{
    Votium
}

public static class PlatformExt
{
    public static string ToPlatformString(this Platform platform) => platform switch
    {
        Platform.Votium => "votium",
        _ => "unknown-platform"
    };
}
