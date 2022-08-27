using Newtonsoft.Json;

namespace Llama.Airforce.Database.Models.Aura;

public class Flyer : Dashboard
{
    public const string ID = "flyer-aura";

    [JsonProperty("id")]
    public override string Id { get; set; } = ID;

    // General
    public double AuraBalPrice { get; set; }

    // Farms.
    public double AuraBalApr { get; set; }
}