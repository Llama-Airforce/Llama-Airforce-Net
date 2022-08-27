using Newtonsoft.Json;

namespace Llama.Airforce.Jobs.Subgraphs.Models;

public class RequestCurvePools
{
    [JsonProperty("data")]
    public CurvePools Data { get; set; }
}

public class CurvePools
{
    [JsonProperty("pools")]
    public List<CurvePool> CurvePoolList { get; set; }
}

public class CurvePool
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("tvl")]
    public double Tvl { get; set; }

    [JsonProperty("swap")]
    public string Swap { get; set; }

    [JsonProperty("lpToken")]
    public string LpToken { get; set; }

    [JsonProperty("isV2")]
    public bool IsV2 { get; set; }

    [JsonProperty("assetType")]
    public int AssetType { get; set; }

    [JsonProperty("coins")]
    public List<string> CoinList { get; set; }
}

