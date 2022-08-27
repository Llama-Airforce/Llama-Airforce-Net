using Newtonsoft.Json;
using System.Collections.Generic;

namespace Llama.Airforce.Database.Models.Airforce;

public class Claim
{
    [JsonProperty("index")]
    public int Index { get; set; }

    [JsonProperty("amount")]
    public string Amount { get; set; }

    [JsonProperty("proof")]
    public string[] Proof { get; set; }
}

public class Airdrop
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("merkleRoot")]
    public string MerkleRoot { get; set; }

    [JsonProperty("tokenTotal")]
    public string Total { get; set; }

    [JsonProperty("claims")]
    public Dictionary<string, Claim> Claims { get; set; }
}