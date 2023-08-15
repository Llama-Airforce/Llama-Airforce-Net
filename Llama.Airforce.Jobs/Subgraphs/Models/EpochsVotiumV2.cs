using Newtonsoft.Json;
using Dom = Llama.Airforce.Domain.Models;

namespace Llama.Airforce.Jobs.Subgraphs.Models;

public class RequestEpochsVotiumV2
{
    [JsonProperty("data")]
    public EpochsVotiumV2 Data { get; set; }
}

public class EpochsVotiumV2
{
    [JsonProperty("rounds")]
    public List<EpochVotiumV2> EpochList { get; set; }
}

public class EpochVotiumV2
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("initiatedAt")]
    public long InitiatedAt { get; set; }

    [JsonProperty("bribeCount")]
    public int BribeCount { get; set; }

    [JsonProperty("incentives")]
    public List<BribeVotiumV2> Bribes { get; set; }

    public static implicit operator Dom.EpochV2(EpochVotiumV2 epoch) => new(
        Round: epoch.Id,
        Bribes: epoch.Bribes.Select(bribe => (Dom.BribeV2)bribe).ToList());
}

public class BribeVotiumV2
{
    [JsonProperty("gauge")]
    public string Gauge { get; set; }

    [JsonProperty("token")]
    public string Token { get; set; }

    [JsonProperty("amount")]
    public string Amount { get; set; }

    [JsonProperty("maxPerVote")]
    public string MaxPerVote { get; set; }

    public static implicit operator Dom.BribeV2(BribeVotiumV2 bribe) => new(
        Gauge: bribe.Gauge,
        Token: bribe.Token,
        Amount: bribe.Amount,
        MaxPerVote: bribe.MaxPerVote);
}