using Newtonsoft.Json;

namespace Llama.Airforce.Jobs.Subgraphs.Models;

public class RequestEpochsAura
{
    [JsonProperty("data")]
    public EpochsAura Data { get; set; }
}

public class EpochsAura
{
    [JsonProperty("proposals")]
    public List<ProposalAura> ProposalList { get; set; }
}

// A single proposal is actually a 'choice'.
public class ProposalAura
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("bribes")]
    public List<BribeAura> Bribes { get; set; }
}

public class BribeAura
{
    [JsonProperty("token")]
    public string Token { get; set; }

    [JsonProperty("amount")]
    public string Amount { get; set; }
}