using Newtonsoft.Json;
using System.Collections.Generic;

namespace Llama.Airforce.Database.Models.Convex;

public class PoolSnapshots
{
    [JsonProperty("id")]
    public string Name { get; set; }

    [JsonProperty("snapshots")]
    public List<PoolSnapshotData> Snapshots { get; set; }
}

public class PoolSnapshotData
{
    [JsonProperty("timestamp")]
    public int TimeStamp { get; set; }

    [JsonProperty("baseApr")]
    public double BaseApr { get; set; }

    [JsonProperty("crvApr")]
    public double CrvApr { get; set; }

    [JsonProperty("cvxApr")]
    public double CvxApr { get; set; }

    [JsonProperty("extraRewardsApr")]
    public double ExtraRewardsApr { get; set; }

    [JsonProperty("tvl")]
    public double Tvl { get; set; }
}