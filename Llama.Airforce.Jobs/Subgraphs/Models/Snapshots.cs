using Newtonsoft.Json;

namespace Llama.Airforce.Jobs.Subgraphs.Models;

public class RequestSnapshots
{
    [JsonProperty("data")]
    public Snapshots? Data { get; set; }
}

public class Snapshots
{
    [JsonProperty("dailyPoolSnapshots")]
    public List<Snapshot>? SnapshotList { get; set; }
}

public class Snapshot
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