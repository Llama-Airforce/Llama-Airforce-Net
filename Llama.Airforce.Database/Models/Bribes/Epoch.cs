using System.Collections.Generic;
using LanguageExt.UnsafeValueAccess;
using Llama.Airforce.SeedWork;
using Llama.Airforce.SeedWork.Types;
using Newtonsoft.Json;

namespace Llama.Airforce.Database.Models.Bribes;

public class Epoch
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("platform")]
    public string Platform { get; set; }

    [JsonProperty("protocol")]
    public string Protocol { get; set; }

    [JsonProperty("round")]
    public int Round { get; set; }

    [JsonProperty("proposal")]
    public string Proposal { get; set; }

    [JsonProperty("end")]
    public long End { get; set; }

    [JsonProperty("bribed")]
    public IReadOnlyDictionary<string, double> Bribed { get; set; }

    [JsonProperty("bribes")]
    public List<Bribe> Bribes { get; set; }
}

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
        StringMax.Of(epoch.Platform).ValueUnsafe(),
        StringMax.Of(epoch.Protocol).ValueUnsafe(),
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