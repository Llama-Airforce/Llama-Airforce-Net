using System.Collections.Generic;
using Newtonsoft.Json;

namespace Llama.Airforce.Database.Models.Bribes;

public class BribeV2
{
    [JsonProperty("pool")]
    public string Pool { get; set; }

    [JsonProperty("token")]
    public string Token { get; set; }

    [JsonProperty("gauge")]
    public string Gauge { get; set; }

    [JsonProperty("choice")]
    public int Choice { get; set; }

    [JsonProperty("amount")]
    public double Amount { get; set; }

    [JsonProperty("amountDollars")]
    public double AmountDollars { get; set; }

    [JsonProperty("maxPerVote")]
    public double MaxPerVote { get; set; }

    [JsonProperty("recycled")]
    public bool Recycled { get; set; }

    [JsonProperty("excluded")]
    public List<string> Excluded { get; set; }
}