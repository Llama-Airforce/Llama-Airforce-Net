using Newtonsoft.Json;

namespace Llama.Airforce.Jobs.Snapshots.Models;

public class RequestVotes
{
    [JsonProperty("data")]
    public Votes Data { get; set; }
}

public class Votes
{
    [JsonProperty("votes")]
    public List<Vote> VoteList { get; set; }
}

public class Vote
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("voter")]
    public string Voter { get; set; }

    [JsonProperty("choice")]
    public Dictionary<string, double> Choices { get; set; }
}