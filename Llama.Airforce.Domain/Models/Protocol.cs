namespace Llama.Airforce.Domain.Models;

public enum Protocol
{
    ConvexCrv,
    ConvexPrisma,
    AuraBal
}

public static class ProtocolExt
{
    public static string ToProtocolString(this Protocol protocol) => protocol switch
    {
        Protocol.ConvexCrv => "cvx-crv",
        Protocol.ConvexPrisma => "cvx-prisma",
        Protocol.AuraBal => "aura-bal",
        _ => "unknown-protocol"
    };
}