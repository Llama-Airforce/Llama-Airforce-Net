using Newtonsoft.Json;
using System.Collections.Generic;

namespace Llama.Airforce.Database.Models.Curve;

public class Pool
{
    [JsonProperty("id")]
    public string Name { get; set; }

    [JsonProperty("tvl")]
    public double Tvl { get; set; }

    [JsonProperty("swap")]
    public string Swap { get; set; }

    [JsonProperty("lpToken")]
    public string LpToken { get; set; }

    [JsonProperty("assetType")]
    public int AssetType { get; set; }

    [JsonProperty("isV2")]
    public bool IsV2 { get; set; }

    [JsonProperty("coins")]
    public List<string> CoinList { get; set; }
}