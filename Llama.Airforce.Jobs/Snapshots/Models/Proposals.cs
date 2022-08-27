using Newtonsoft.Json;

namespace Llama.Airforce.Jobs.Snapshots.Models;

public class RequestProposals
{
    [JsonProperty("data")]
    public Proposals Data { get; set; }
}

public class Proposals
{
    [JsonProperty("proposals")]
    public List<Proposal> ProposalList { get; set; }
}

public class Proposal
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("choices")]
    public List<string> Choices { get; set; }

    [JsonProperty("start")]
    public long Start { get; set; }

    [JsonProperty("end")]
    public long End { get; set; }

    [JsonProperty("snapshot")]
    public string Snapshot { get; set; }
}