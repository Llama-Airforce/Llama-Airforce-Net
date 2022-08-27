using Newtonsoft.Json;
using System.Collections.Generic;

namespace Llama.Airforce.Database.Models.Curve;

public class CurvePoolSnapshots
{
    [JsonProperty("id")]
    public string Name { get; set; }

    [JsonProperty("snapshots")]
    public List<FeeSnapshot> FeeSnapshots { get; set; }

    [JsonProperty("emissions")]
    public List<EmissionSnapshot> EmissionSnapshots { get; set; }
}

public class FeeSnapshot
{
    [JsonProperty("timestamp")]
    public int TimeStamp { get; set; }

    [JsonProperty("fees")]
    public double Value { get; set; }

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
