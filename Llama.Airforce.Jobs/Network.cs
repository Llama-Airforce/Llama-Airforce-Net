namespace Llama.Airforce.Jobs;

public enum Network
{
    Ethereum,
    Fantom
}

public static class NetworkExt
{
    public static string NetworkToString(this Network network) => network switch
    {
        Network.Ethereum => "ethereum",
        Network.Fantom => "fantom",
        _ => "unknown-network"
    };
}
