namespace Llama.Airforce.API.Models.Votium;
using Db = Database.Models;

public class EpochV3
{
    public string Platform { get; set; }
    public string Protocol { get; set; }
    public int Round { get; set; }
    public string Proposal { get; set; }
    public long End { get; set; }
    public Dictionary<string, double> Bribed { get; set; }
    public List<BribeV3> Bribes { get; set; } = null!;

    public static implicit operator EpochV3(Db.Bribes.EpochV3 epoch) => new()
    {
        Platform = epoch.Platform,
        Protocol = epoch.Protocol,
        Round = epoch.Round,
        Proposal = epoch.Proposal,
        End = epoch.End,
        Bribed = epoch.Bribed.ToDictionary(x => x.Key, x => x.Value),
        Bribes = epoch.Bribes.Select(x => (BribeV3)x).ToList()
    };
}