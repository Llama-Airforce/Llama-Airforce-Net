using Newtonsoft.Json;

namespace Llama.Airforce.Database.Models.Aura;

public class Flyer : Dashboard
{
    public const string ID = "flyer-aura";

    [JsonProperty("id")]
    public override string Id { get; set; } = ID;

    [JsonProperty("auraBalPrice")]
    public double AuraBalPrice { get; set; }

    [JsonProperty("auraBalApr")]
    public double AuraBalApr { get; set; }
}