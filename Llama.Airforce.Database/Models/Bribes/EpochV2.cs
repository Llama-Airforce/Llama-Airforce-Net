using System.Collections.Generic;
using Newtonsoft.Json;

namespace Llama.Airforce.Database.Models.Bribes;

public class EpochV2
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

    [JsonProperty("scoresTotal")]
    public double ScoresTotal { get; set; }

    [JsonProperty("bribed")]
    public IReadOnlyDictionary<string, double> Bribed { get; set; }

    [JsonProperty("bribes")]
    public List<BribeV2> Bribes { get; set; }
}