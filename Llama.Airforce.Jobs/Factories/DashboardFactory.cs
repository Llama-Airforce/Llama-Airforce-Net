using LanguageExt;
using LanguageExt.Common;
using Llama.Airforce.Jobs.Contracts;
using Llama.Airforce.Jobs.Extensions;
using Llama.Airforce.Jobs.Functions;
using Llama.Airforce.SeedWork.Extensions;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using static LanguageExt.Prelude;
using Db = Llama.Airforce.Database.Models;

namespace Llama.Airforce.Jobs.Factories;

public static class DashboardFactory
{
    public record VotiumData(
        Lst<Db.Bribes.Epoch> Epochs,
        Db.Bribes.Epoch LatestFinishedEpoch);

    public record AuraData(
        Lst<Db.Bribes.Epoch> Epochs,
        Db.Bribes.Epoch LatestFinishedEpoch);

    public static Func<
            ILogger,
            IWeb3,
            VotiumData,
            AuraData,
            EitherAsync<Error, Lst<Database.Dashboard>>>
        CreateDashboards = fun((
            ILogger logger,
            IWeb3 web3,
            VotiumData votiumData,
            AuraData auraData) =>
        {
            var overviewVotium_ =
                CreateOverviewVotium(
                    logger,
                    web3,
                    votiumData)
                .Map(x => (Database.Dashboard)x);

            var overviewAura_ =
                CreateOverviewAura(
                        logger,
                        web3,
                        auraData)
                    .Map(x => (Database.Dashboard)x);

            return
                from overviewVotium in overviewVotium_
                from overviewAura in overviewAura_
                select List(overviewVotium, overviewAura);
        });

    public static Func<
            ILogger,
            IWeb3,
            VotiumData,
            EitherAsync<Error, Db.Bribes.Dashboards.Overview>>
        CreateOverviewVotium = fun((
            ILogger logger,
            IWeb3 web3,
            VotiumData data) =>
        {
            var totalBribes = data.LatestFinishedEpoch.Bribes.Sum(bribe => bribe.AmountDollars);
            var totalBribed = data.LatestFinishedEpoch.Bribed.Sum(bribed => bribed.Value);
            var dollarPerVlCvx = totalBribes / totalBribed;

            var crvPrice_ = PriceFunctions.GetPrice(Addresses.Curve.Token, Network.Ethereum, Some(web3));
            var cvxPrice_ = PriceFunctions.GetPrice(Addresses.Convex.Token, Network.Ethereum, Some(web3));

            var cvxPerCrv_ = Convex.GetCvxMintAmount(web3, 1).ToEitherAsync();
            var crvPerDay_ = Curve.GetRate(web3).DivideByDecimals(Convex.CurveDecimals).Map(x => x * 86400).ToEitherAsync();
            var votingPower_ = Curve.GetVotingPower(web3, Addresses.Convex.VoterProxy).ToEitherAsync();

            // Total supply is all CVX locked eligible for voting.
            var lockedSupply_ = ERC20.GetLockedSupply(web3, Addresses.Convex.Locked2).DivideByDecimals(Convex.CvxDecimals).ToEitherAsync();

            // https://docs.google.com/spreadsheets/d/1SCO33fU-4EglqD9h191c5z3curC3SqJP-yshA1MjVqE/edit#gid=0
            var crvPerCvxPerRound_ =
                from crvPerDay in crvPerDay_
                from votingPower in votingPower_
                from lockedSupply in lockedSupply_
                select crvPerDay * 14 * votingPower / (lockedSupply);

            var rewardPerDollarBribe_ =
                from crvPrice in crvPrice_
                from cvxPrice in cvxPrice_
                from cvxPerCrv in cvxPerCrv_
                from crvPerCvxPerRound in crvPerCvxPerRound_
                select crvPerCvxPerRound / dollarPerVlCvx * (crvPrice + cvxPerCrv * cvxPrice) * (1 - Convex.RewardFee);

            var epochOverviews = data
                .Epochs
                .Map(epoch =>
                {
                    var totalAmountDollars = epoch.Bribes.Sum(bribe => bribe.AmountDollars);
                    var totalAmountBribed = epoch.Bribed.Values.Sum();

                    return new Db.Bribes.EpochOverview
                    {
                        Platform = epoch.Platform,
                        Protocol = epoch.Protocol,
                        Round = epoch.Round,
                        Proposal = epoch.Proposal,
                        End = epoch.End,
                        TotalAmountDollars = totalAmountDollars,
                        DollarPerVlAsset = totalAmountBribed > 0
                            ? totalAmountDollars / totalAmountBribed
                            : 0
                    };
                })
                .ToList();

            return
                from rewardPerDollarBribe in rewardPerDollarBribe_
                select new Db.Bribes.Dashboards.Overview
                {
                    Id = Db.Bribes.Dashboards.Overview.Votium,
                    RewardPerDollarBribe = rewardPerDollarBribe,
                    Epochs = epochOverviews
                };
        });

        public static Func<
            ILogger,
            IWeb3,
            AuraData,
            EitherAsync<Error, Db.Bribes.Dashboards.Overview>>
        CreateOverviewAura = fun((
            ILogger logger,
            IWeb3 web3,
            AuraData data) =>
        {
            var totalBribes = data.LatestFinishedEpoch.Bribes.Sum(bribe => bribe.AmountDollars);
            var totalBribed = data.LatestFinishedEpoch.Bribed.Sum(bribed => bribed.Value);
            var dollarPerVlAura = totalBribes / totalBribed;

            var balPrice_ = PriceFunctions.GetPrice(Addresses.Balancer.Token, Network.Ethereum, Some(web3));
            var auraPrice_ = PriceFunctions.GetPrice(Addresses.Aura.Token, Network.Ethereum, Some(web3));

            var auraPerBal_ = Aura.GetAuraMintAmount(web3, 1).ToEitherAsync();
            var balPerDay_ = Balancer.GetRate(web3).DivideByDecimals(Aura.BalancerDecimals).Map(x => x * 86400).ToEitherAsync();
            var votingPower_ = Balancer.GetVotingPower(web3, Addresses.Aura.VoterProxy).ToEitherAsync();

            // Total supply is all CVX locked eligible for voting.
            var lockedSupply_ = ERC20.GetLockedSupply(web3, Addresses.Aura.Locked).DivideByDecimals(Aura.AuraDecimals).ToEitherAsync();

            // https://docs.google.com/spreadsheets/d/1SCO33fU-4EglqD9h191c5z3curC3SqJP-yshA1MjVqE/edit#gid=0
            var balPerAuraPerRound_ =
                from balPerDay in balPerDay_
                from votingPower in votingPower_
                from lockedSupply in lockedSupply_
                select balPerDay * 14 * votingPower / lockedSupply;

            var rewardPerDollarBribe_ =
                from balPrice in balPrice_
                from auraPrice in auraPrice_
                from auraPerBal in auraPerBal_
                from balPerAuraPerRound in balPerAuraPerRound_
                select balPerAuraPerRound / dollarPerVlAura * (balPrice + auraPerBal * auraPrice) * (1 - Aura.RewardFee);

            var epochOverviews = data
                .Epochs
                .Map(epoch =>
                {
                    var totalAmountDollars = epoch.Bribes.Sum(bribe => bribe.AmountDollars);
                    var totalAmountBribed = epoch.Bribed.Values.Sum();

                    return new Db.Bribes.EpochOverview
                    {
                        Platform = epoch.Platform,
                        Protocol = epoch.Protocol,
                        Round = epoch.Round,
                        Proposal = epoch.Proposal,
                        End = epoch.End,
                        TotalAmountDollars = totalAmountDollars,
                        DollarPerVlAsset = totalAmountBribed > 0
                            ? totalAmountDollars / totalAmountBribed
                            : 0
                    };
                })
                .ToList();

            return
                from rewardPerDollarBribe in rewardPerDollarBribe_
                select new Db.Bribes.Dashboards.Overview
                {
                    Id = Db.Bribes.Dashboards.Overview.Aura,
                    RewardPerDollarBribe = rewardPerDollarBribe,
                    Epochs = epochOverviews
                };
        });
}