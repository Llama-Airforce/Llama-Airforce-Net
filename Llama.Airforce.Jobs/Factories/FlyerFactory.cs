using LanguageExt;
using LanguageExt.Common;
using Llama.Airforce.Jobs.Contracts;
using Llama.Airforce.Jobs.Extensions;
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
            Lst<Db.Convex.Pool>,
            Db.Bribes.Epoch,
            EitherAsync<Error, Db.Convex.Flyer>>
        CreateFlyerConvex = fun((
            IWeb3 web3,
            Lst<Db.Convex.Pool> pools,
            Db.Bribes.Epoch latestFinishedEpoch) =>
        {
            // Coingecko data.
            var caps_ = CoinGecko.GetData(
                    Addresses.Convex.Token,
                    Network.Ethereum)
                .Bind(x => CoinGecko.GetMarketCap(x).ToAsync());

            var cvxPrice_ = PriceFunctions.GetPrice(Addresses.Convex.Token, Network.Ethereum, Some(web3));
            var crvPrice_ = PriceFunctions.GetPrice(Addresses.Curve.Token, Network.Ethereum, Some(web3));

            // Ethereum data.
            var bribeIncomeBiWeekly = latestFinishedEpoch.Bribes.Sum(bribe => bribe.AmountDollars);
            var cvxBribed = latestFinishedEpoch.Bribed.Sum(bribed => bribed.Value);
            var votiumApr_ =
                from cvxPrice in cvxPrice_
                select cvxBribed == 0 ? 0 : bribeIncomeBiWeekly / cvxBribed / (cvxPrice / 100) * BiWeeksPerYear;

            var cvxApr_ =
                from lockedApr in Convex.GetLockedApr(web3)
                from votiumApr in votiumApr_
                select lockedApr * 100 + votiumApr;

            var cvxCrvApr_ = Convex.GetCvxCrvApr(web3).Map(x => x * 100);
            var cvxStaked_ = ERC20.GetTotalSupply(web3, Addresses.Convex.Staked).DivideByDecimals(Convex.CvxDecimals)
                .ToEitherAsync();
            var cvxLocked_ = Convex.GetCvxLocked(web3).DivideByDecimals(Convex.CvxDecimals).ToEitherAsync();
            var crvLocked_ = ERC20.GetTotalSupply(web3, Addresses.CvxCrv.Token).DivideByDecimals(Convex.CurveDecimals)
                .ToEitherAsync();

            var tvl_ =
                from cvxPrice in cvxPrice_
                from crvPrice in crvPrice_
                from cvxStaked in cvxStaked_
                from cvxLocked in cvxLocked_
                from crvLocked in crvLocked_
                select pools.Sum(pool => pool.Tvl) + (cvxStaked + cvxLocked) * cvxPrice + crvLocked + crvPrice;

            var crvLockedDollars_ = Convex.GetLockedCrvUsd(web3);
            var revenueMonthly = 535_500_000 / ((DateTime.Now - Convex.Genesis).Days / (365.25 / 12));

            var cvxVotingPercentage_ = Curve
                .GetVotingPower(web3, Addresses.Convex.VoterProxy)
                .Map(x => x * 100)
                .ToEitherAsync();

            return
                from caps in caps_
                from tvl in tvl_
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
                    CvxTvl = tvl,
                    CvxVotingPercentage = cvxVotingPercentage,
                    CvxMarketCap = caps.MCap,
                    CvxMarketCapFullyDiluted = caps.FDV,

                    BribesIncomeAnnually = bribeIncomeBiWeekly * BiWeeksPerYear,
                    BribesIncomeBiWeekly = bribeIncomeBiWeekly,

                    CvxApr = cvxApr,
                    CvxCrvApr = cvxCrvApr
                };
        });

    public static Func<
            IWeb3,
            EitherAsync<Error, Db.Aura.Flyer>>
        CreateFlyerAura = fun((
            IWeb3 web3) =>
        {
            var auraBalApr_ = Aura.GetAuraBalApr(web3).Map(x => x * 100);
            var auraBalPrice_ = PriceFunctions.GetAuraBalPrice(web3);

            return
                from auraBalApr in auraBalApr_
                from auraBalPrice in auraBalPrice_
                select new Db.Aura.Flyer
                {
                    AuraBalPrice = auraBalPrice,
                    AuraBalApr = auraBalApr
                };
        });
}