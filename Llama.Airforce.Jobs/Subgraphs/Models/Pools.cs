using Newtonsoft.Json;

namespace Llama.Airforce.Jobs.Subgraphs.Models;

public class RequestPools
{
    [JsonProperty("data")]
    public Pools Data { get; set; }
}

public class Pools
{
    [JsonProperty("pools")]
    public List<Pool> PoolList { get; set; }
}

public class Pool
{
    [JsonProperty("name")]
    public string Name { get; set; }

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