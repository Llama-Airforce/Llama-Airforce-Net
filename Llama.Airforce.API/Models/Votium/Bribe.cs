namespace Llama.Airforce.API.Models.Votium;
using Db = Database.Models;

public class Bribe
{
    public string Pool { get; set; }
    public string Token { get; set; }
    public double Amount { get; set; }
    public double AmountDollars { get; set; }

    public static implicit operator Bribe(Db.Bribes.Bribe bribe) => new()
    {
        Pool = bribe.Pool,
        Token = bribe.Token,
        Amount = bribe.Amount,
        AmountDollars = bribe.AmountDollars
    };
}