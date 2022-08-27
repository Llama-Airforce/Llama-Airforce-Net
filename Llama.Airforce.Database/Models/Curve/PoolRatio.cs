using Newtonsoft.Json;
using System.Collections.Generic;

namespace Llama.Airforce.Database.Models.Curve;

public class CurvePoolRatios
{
    [JsonProperty("id")]
    public string Name { get; set; }

    [JsonProperty("ratios")]
    public List<PoolRatio> Ratios { get; set; }
}

public class PoolRatio
{
    [JsonProperty("timestamp")]
    public int TimeStamp { get; set; }

    [JsonProperty("ratio")]
    public double Ratio { get; set; }

}
