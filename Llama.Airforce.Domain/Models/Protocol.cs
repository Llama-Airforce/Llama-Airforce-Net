namespace Llama.Airforce.Domain.Models;

public enum Protocol
{
    ConvexCrv,
    AuraBal
}

public static class ProtocolExt
{
    public static string ToProtocolString(this Protocol protocol) => protocol switch
    {
        Protocol.ConvexCrv => "cvx-crv",
        Protocol.AuraBal => "aura-bal",
        _ => "unknown-protocol"
    };
}