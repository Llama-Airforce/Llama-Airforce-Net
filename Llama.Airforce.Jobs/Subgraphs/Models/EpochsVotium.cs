using Newtonsoft.Json;
using Dom = Llama.Airforce.Domain.Models;

namespace Llama.Airforce.Jobs.Subgraphs.Models;

public class RequestEpochsVotium
{
    [JsonProperty("data")]
    public EpochsVotium Data { get; set; }
}

public class EpochsVotium
{
    [JsonProperty("epoches")]
    public List<EpochVotium> EpochList { get; set; }
}

public class EpochVotium
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("deadline")]
    public long Deadline { get; set; }

    [JsonProperty("initiatedAt")]
    public long InitiatedAt { get; set; }

    [JsonProperty("bribeCount")]
    public int BribeCount { get; set; }

    [JsonProperty("bribes")]
    public List<BribeVotium> Bribes { get; set; }

    public static implicit operator Dom.Epoch(EpochVotium epoch) => new(
        SnapshotId: epoch.Id,
        Deadline: epoch.Deadline,
        Bribes: epoch.Bribes.Select(bribe => (Dom.Bribe)bribe).ToList());
}

public class BribeVotium
{
    [JsonProperty("choiceIndex")]
    public int Choice { get; set; }

    [JsonProperty("token")]
    public string Token { get; set; }

    [JsonProperty("amount")]
    public string Amount { get; set; }

    public static implicit operator Dom.Bribe(BribeVotium bribe) => new(
        Choice: bribe.Choice,
        Token: bribe.Token,
        Amount: bribe.Amount);
}