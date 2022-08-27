using Newtonsoft.Json;

namespace Llama.Airforce.Database.Models.Convex;

public class Pool
{
    [JsonProperty("id")]
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