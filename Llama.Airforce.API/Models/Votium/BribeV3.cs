namespace Llama.Airforce.API.Models.Votium;
using Db = Database.Models;

public class BribeV3
{
    public string Pool { get; set; }
    public string Gauge { get; set; }
    public string Token { get; set; }
    public double Amount { get; set; }
    public double AmountDollars { get; set; }
    public double MaxPerVote { get; set; }

    public static implicit operator BribeV3(Db.Bribes.BribeV3 bribe) => new()
    {
        Pool = bribe.Pool,
        Gauge = bribe.Gauge,
        Token = bribe.Token,
        Amount = bribe.Amount,
        AmountDollars = bribe.AmountDollars,
        MaxPerVote = bribe.MaxPerVote
    };
}