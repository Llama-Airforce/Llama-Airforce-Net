using Newtonsoft.Json;

namespace Llama.Airforce.Database.Models.Bribes;

public class Bribe
{
    [JsonProperty("pool")]
    public string Pool { get; set; }

    [JsonProperty("token")]
    public string Token { get; set; }

    [JsonProperty("choice")]
    public int Choice { get; set; }

    [JsonProperty("amount")]
    public double Amount { get; set; }

    [JsonProperty("amountDollars")]
    public double AmountDollars { get; set; }
}