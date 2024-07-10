using Newtonsoft.Json;

namespace Llama.Airforce.Database.Models.Convex;

public class Flyer : Dashboard
{
    public const string ID = "flyer-convex";

    [JsonProperty("id")]
    public override string Id { get; set; } = ID;

    [JsonProperty("revenueMonthly")]
    public double RevenueMonthly { get; set; }

    [JsonProperty("revenueAnnually")]
    public double RevenueAnnually { get; set; }

    [JsonProperty("crvLockedDollars")]
    public double CrvLockedDollars { get; set; }

    [JsonProperty("crvLockedDollarsMonthly")]
    public double CrvLockedDollarsMonthly { get; set; }

    [JsonProperty("cvxTvl")]
    public double CvxTvl { get; set; }

    [JsonProperty("cvxVotingPercentage")]
    public double CvxVotingPercentage { get; set; }

    [JsonProperty("cvxMarketCap")]
    public double CvxMarketCap { get; set; }

    [JsonProperty("cvxMarketCapFullyDiluted")]
    public double CvxMarketCapFullyDiluted { get; set; }

    [JsonProperty("bribesIncomeAnnually")]
    public double BribesIncomeAnnually { get; set; }

    [JsonProperty("bribesIncomeBiWeekly")]
    public double BribesIncomeBiWeekly { get; set; }

    [JsonProperty("cvxApr")]
    public double CvxApr { get; set; }

    [JsonProperty("cvxCrvApr")]
    public double CvxCrvApr { get; set; }
}