using Newtonsoft.Json;
using Dom = Llama.Airforce.Domain.Models;

namespace Llama.Airforce.Jobs.Subgraphs.Models;

public class RequestEpochsVotiumV3
{
    [JsonProperty("data")]
    public EpochsVotiumV3 Data { get; set; }
}

public class EpochsVotiumV3
{
    [JsonProperty("rounds")]
    public List<EpochVotiumV3> EpochList { get; set; }
}

public class EpochVotiumV3
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("initiatedAt")]
    public long InitiatedAt { get; set; }

    [JsonProperty("bribeCount")]
    public int BribeCount { get; set; }

    [JsonProperty("incentives")]
    public List<BribeVotiumV3> Bribes { get; set; }

    public static implicit operator Dom.EpochV3(EpochVotiumV3 epoch) => new(
        Round: epoch.Id,
        Bribes: epoch.Bribes.Select(bribe => (Dom.BribeV3)bribe).ToList());
}

public class BribeVotiumV3
{
    [JsonProperty("gauge")]
    public string Gauge { get; set; }

    [JsonProperty("token")]
    public string Token { get; set; }

    [JsonProperty("amount")]
    public string Amount { get; set; }

    [JsonProperty("maxPerVote")]
    public string MaxPerVote { get; set; }

    public static implicit operator Dom.BribeV3(BribeVotiumV3 bribe) => new(
        Gauge: bribe.Gauge,
        Token: bribe.Token,
        Amount: bribe.Amount,
        MaxPerVote: bribe.MaxPerVote);
}