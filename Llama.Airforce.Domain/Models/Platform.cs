namespace Llama.Airforce.Domain.Models;

public enum Platform
{
    Votium,
    HiddenHand
}

public static class PlatformExt
{
    public static string ToPlatformString(this Platform platform) => platform switch
    {
        Platform.Votium => "votium",
        Platform.HiddenHand => "hh",
        _ => "unknown-platform"
    };
}