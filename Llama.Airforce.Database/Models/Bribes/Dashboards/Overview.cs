using System.Collections.Generic;
using Newtonsoft.Json;

namespace Llama.Airforce.Database.Models.Bribes.Dashboards;

public class Overview : Dashboard
{
    public const string Votium = "bribes-overview-votium";
    public const string Fxn = "bribes-overview-fxn";

    [JsonProperty("id")]
    public override string Id { get; set; }

    [JsonProperty("rewardPerDollarBribe")]
    public double RewardPerDollarBribe { get; set; }

    [JsonProperty("epochs")]
    public List<EpochOverview> Epochs { get; set; }
}
