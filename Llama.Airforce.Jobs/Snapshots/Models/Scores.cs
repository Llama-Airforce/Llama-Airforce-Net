using Newtonsoft.Json;

namespace Llama.Airforce.Jobs.Snapshots.Models;

public class RequestScores
{
    [JsonProperty("result")]
    public Result Result { get; set; }
}

public class Result
{
    [JsonProperty("state")]
    public string State { get; set; }

    [JsonProperty("scores")]
    public List<Dictionary<string, double>> Scores { get; set; }
}