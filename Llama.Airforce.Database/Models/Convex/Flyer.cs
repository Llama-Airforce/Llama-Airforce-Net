using Newtonsoft.Json;

namespace Llama.Airforce.Database.Models.Convex;

public class Flyer : Dashboard
{
    public const string ID = "flyer-convex";

    [JsonProperty("id")]
    public override string Id { get; set; } = ID;

    // Revenue.
    public double RevenueMonthly { get; set; }
    public double RevenueAnnually { get; set; }

    // General.
    public double CrvLockedDollars { get; set; }
    public double CrvLockedDollarsMonthly { get; set; }
    public double CvxTvl { get; set; }
    public double CvxVotingPercentage { get; set; }
    public double CvxMarketCap { get; set; }
    public double CvxMarketCapFullyDiluted { get; set; }

    // Bribes.
    public double BribesIncomeAnnually { get; set; }
    public double BribesIncomeBiWeekly { get; set; }

    // Farms.
    public double CvxApr { get; set; }
    public double CvxCrvApr { get; set; }
}