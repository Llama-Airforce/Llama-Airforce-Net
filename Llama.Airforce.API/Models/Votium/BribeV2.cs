namespace Llama.Airforce.API.Models.Votium;
using Db = Database.Models;

public class BribeV2
{
    public string Pool { get; set; }
    public string Token { get; set; }
    public double Amount { get; set; }
    public double AmountDollars { get; set; }
    public double MaxPerVote { get; set; }

    public static implicit operator BribeV2(Db.Bribes.BribeV2 bribe) => new()
    {
        Pool = bribe.Pool,
        Token = bribe.Token,
        Amount = bribe.Amount,
        AmountDollars = bribe.AmountDollars,
        MaxPerVote = bribe.MaxPerVote
    };
}