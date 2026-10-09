using LanguageExt;
using LanguageExt.Common;
using Llama.Airforce.Jobs.Contracts;
using Llama.Airforce.Jobs.Functions;
using Llama.Airforce.SeedWork.Extensions;
using Nethereum.Web3;
using static LanguageExt.Prelude;
using Db = Llama.Airforce.Database.Models;

namespace Llama.Airforce.Jobs.Factories;

public static class FlyerFactory
{
    public const double BiWeeksPerYear = 26.07145;

    public static Func<
            IWeb3,
            Func<HttpClient>,
            Lst<Db.Bribes.EpochV2>,
            EitherAsync<Error, Db.Convex.Flyer>>
        CreateFlyerConvex = fun((
            IWeb3 web3,
            Func<HttpClient> httpFactory,
            Lst<Db.Bribes.EpochV2> latestFinishedEpochs) =>
        {
            // Coingecko data.
            var marketCap_ = DefiLlama.GetMarketCap(httpFactory, "convex-finance");
            var cvxPrice_ = PriceFunctions.GetPrice(httpFactory, Addresses.Convex.Token, Network.Ethereum, Some(web3));

            // Ethereum data.
            var bribeIncomeBiWeeklyTotal = latestFinishedEpochs
               .Sum(epoch => epoch.Bribes.Sum(bribe => bribe.AmountDollars));

            var votiumApr_ =
                from cvxPrice in cvxPrice_
                select latestFinishedEpochs.Sum(epoch =>
                {
                    var bribeIncomeBiWeekly = epoch.Bribes.Sum(bribe => bribe.AmountDollars);
                    var cvxBribed = epoch.Bribed.Sum(bribed => bribed.Value);

                    return cvxBribed == 0
                        ? 0
                        : bribeIncomeBiWeekly / cvxBribed / (cvxPrice / 100) * BiWeeksPerYear;
                });

            var cvxApr_ =
                from lockedApr in Convex.GetLockedApr(httpFactory, web3)
                from votiumApr in votiumApr_
                select lockedApr * 100 + votiumApr;

            var cvxCrvApr_ = Convex.GetCvxCrvApr(httpFactory, web3).Map(x => x * 100);

            var crvLockedDollars_ = Convex.GetLockedCrvUsd(httpFactory, web3);
            var revenueMonthly = 535_500_000 / ((DateTime.Now - Convex.Genesis).Days / (365.25 / 12));

            var cvxVotingPercentage_ = Curve
                .GetVotingPower(web3, Addresses.Convex.VoterProxyCurve)
                .Map(x => x * 100)
                .ToEitherAsync();

            return
                from marketCap in marketCap_
                from cvxApr in cvxApr_
                from cvxCrvApr in cvxCrvApr_
                from crvLockedDollars in crvLockedDollars_
                from cvxVotingPercentage in cvxVotingPercentage_
                select new Db.Convex.Flyer
                {
                    RevenueMonthly = revenueMonthly,
                    RevenueAnnually = revenueMonthly * 12,

                    CrvLockedDollars = crvLockedDollars,
                    CrvLockedDollarsMonthly = crvLockedDollars / ((DateTime.Now - Convex.Genesis).Days / (365.25 / 12)),
                    CvxTvl = 0,
                    CvxVotingPercentage = cvxVotingPercentage,
                    CvxMarketCap = marketCap,
                    CvxMarketCapFullyDiluted = 0,

                    BribesIncomeAnnually = bribeIncomeBiWeeklyTotal * BiWeeksPerYear,
                    BribesIncomeBiWeekly = bribeIncomeBiWeeklyTotal,

                    CvxApr = cvxApr,
                    CvxCrvApr = cvxCrvApr
                };
        });
}
