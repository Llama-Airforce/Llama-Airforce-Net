namespace Llama.Airforce.API.Models.Votium;
using Db = Database.Models;

public class EpochV2
{
    public string Platform { get; set; }
    public string Protocol { get; set; }
    public int Round { get; set; }
    public string Proposal { get; set; }
    public long End { get; set; }
    public Dictionary<string, double> Bribed { get; set; }
    public List<BribeV2> Bribes { get; set; } = null!;

    public static implicit operator EpochV2(Db.Bribes.EpochV2 epoch) => new()
    {
        Platform = epoch.Platform,
        Protocol = epoch.Protocol,
        Round = epoch.Round,
        Proposal = epoch.Proposal,
        End = epoch.End,
        Bribed = epoch.Bribed.ToDictionary(x => x.Key, x => x.Value),
        Bribes = epoch.Bribes.Select(x => (BribeV2)x).ToList()
    };
}