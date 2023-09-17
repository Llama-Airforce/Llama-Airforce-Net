using System.Collections.Generic;
using Llama.Airforce.SeedWork;
using Llama.Airforce.SeedWork.Types;

namespace Llama.Airforce.Database.Models.Bribes;

public class EpochId : ValueObject
{
    public StringMax Platform { get; }
    public StringMax Protocol { get; }
    public int Round { get; }

    private EpochId(
        StringMax platform,
        StringMax protocol,
        int round)
    {
        Platform = platform;
        Protocol = protocol;
        Round = round;
    }

    public static EpochId Create(
        StringMax platform,
        StringMax protocol,
        int round) =>
        new(
            platform,
            protocol,
            round);

    public static implicit operator EpochId(Epoch epoch) => new(
        StringMax.Of(epoch.Platform),
        StringMax.Of(epoch.Protocol),
        epoch.Round);

    public static implicit operator EpochId(EpochV2 epoch) => new(
        StringMax.Of(epoch.Platform),
        StringMax.Of(epoch.Protocol),
        epoch.Round);

    public static implicit operator EpochId(EpochV3 epoch) => new(
        StringMax.Of(epoch.Platform),
        StringMax.Of(epoch.Protocol),
        epoch.Round);

    public static implicit operator string(EpochId epochId) => epochId.ToString();

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Platform;
        yield return Protocol;
        yield return Round;
    }

    public override string ToString() => $"{Platform}-{Protocol}-{Round}";
}