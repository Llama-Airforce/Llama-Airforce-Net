using Newtonsoft.Json;

namespace Llama.Airforce.Jobs.Subgraphs.Models;

public class RequestCurvePoolSnapshots
{
    [JsonProperty("data")]
    public CurvePoolSnapshots? Data { get; set; }
}

public class CurvePoolSnapshots
{
    [JsonProperty("pools")]
    public List<CurvePoolSnapshot>? CurveSnapshotList { get; set; }

}

public class CurvePoolSnapshot
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("assetType")]
    public string AssetType { get; set; }

    [JsonProperty("snapshots")]
    public List<FeeSnapshot>? FeeSnapshotList { get; set; }

    [JsonProperty("emissions")]
    public List<EmissionSnapshot>? EmissionSnapshotList { get; set; }
}

public class FeeSnapshot
{
    [JsonProperty("timestamp")]
    public int TimeStamp { get; set; }

    [JsonProperty("block")]
    public int Block { get; set; }

    [JsonProperty("fees")]
    public double Fees { get; set; }

    [JsonProperty("poolTokenPrice")]
    public double PoolTokenPrice { get; set; }

}

public class EmissionSnapshot
{
    [JsonProperty("timestamp")]
    public int TimeStamp { get; set; }

    [JsonProperty("value")]
    public double Value { get; set; }

    [JsonProperty("crvAmount")]
    public double CrvAmount { get; set; }
}