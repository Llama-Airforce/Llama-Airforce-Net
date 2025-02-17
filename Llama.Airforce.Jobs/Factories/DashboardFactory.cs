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
    public record VotiumDataV1(
        Lst<Db.Bribes.Epoch> Epochs);

    public record VotiumDataV2(
        Lst<Db.Bribes.EpochV2> Epochs,
        Db.Bribes.EpochV2 LatestFinishedEpoch);

    public record FxnData(
        Lst<Db.Bribes.EpochV2> Epochs,
        Db.Bribes.EpochV2 LatestFinishedEpoch);

    public record AuraData(
        Lst<Db.Bribes.Epoch> Epochs,
        Db.Bribes.Epoch LatestFinishedEpoch);

    public record Data(
        VotiumDataV1 VotiumDataV1,
        VotiumDataV2 VotiumDataV2,
        FxnData FxnData,
        AuraData AuraData);

    public static Func<
            ILogger,
            IWeb3,
            Func<HttpClient>,
            Data,
            EitherAsync<Error, Lst<Database.Dashboard>>>
        CreateDashboards = fun((
            ILogger logger,
            IWeb3 web3,
            Func<HttpClient> httpFactory,
            Data data) =>
        {
            var overviewVotium_ =
                CreateOverviewVotium(
                    logger,
                    web3,
                    httpFactory,
                    data.VotiumDataV1,
                    data.VotiumDataV2)
                .Map(x => (Database.Dashboard)x);

            var overviewFxn_ =
                CreateOverviewFxn(
                        logger,
                        web3,
                        httpFactory,
                        data.FxnData)
                   .Map(x => (Database.Dashboard)x);

            var overviewAura_ =
                CreateOverviewAura(
                    logger,
                    web3,
                    httpFactory,
                    data.AuraData)
                .Map(x => (Database.Dashboard)x);

            return
                from overviewVotium in overviewVotium_
                from overviewFxn in overviewFxn_
                from overviewAura in overviewAura_
                select List(overviewVotium, overviewFxn, overviewAura);
        });

    public static Func<
            ILogger,
            IWeb3,
            Func<HttpClient>,
            VotiumDataV1,
            VotiumDataV2,
            EitherAsync<Error, Db.Bribes.Dashboards.Overview>>
        CreateOverviewVotium = fun((
            ILogger logger,
            IWeb3 web3,
            Func<HttpClient> httpFactory,
            VotiumDataV1 dataV1,
            VotiumDataV2 dataV2) =>
        {
            var totalBribes = dataV2.LatestFinishedEpoch.Bribes.Sum(bribe => bribe.AmountDollars);
            var totalBribed = dataV2.LatestFinishedEpoch.Bribed.Sum(bribed => bribed.Value);
            var dollarPerVlCvx = totalBribes / totalBribed;

            var crvPrice_ = PriceFunctions.GetPrice(httpFactory, Addresses.Curve.Token, Network.Ethereum, Some(web3));

            var crvPerDay_ = Curve.GetRate(web3).DivideByDecimals(Convex.CurveDecimals).Map(x => x * 86400).ToEitherAsync();
            var votingPower_ = Curve.GetVotingPower(web3, Addresses.Convex.VoterProxyCurve).ToEitherAsync();

            var scoresTotal_ = dataV2.LatestFinishedEpoch.ScoresTotal > 0
                ? RightAsync<Error, double>(dataV2.LatestFinishedEpoch.ScoresTotal)
                : LeftAsync<Error, double>(Error.New("Total scores is zero"));

            // https://docs.google.com/spreadsheets/d/1SCO33fU-4EglqD9h191c5z3curC3SqJP-yshA1MjVqE/edit#gid=0
            var crvPerCvxPerRound_ =
                from crvPerDay in crvPerDay_
                from votingPower in votingPower_
                from scoresTotal in scoresTotal_
                select crvPerDay * 14 * votingPower / scoresTotal;

            var rewardPerDollarBribe_ =
                from crvPrice in crvPrice_
                from crvPerCvxPerRound in crvPerCvxPerRound_
                select crvPerCvxPerRound / dollarPerVlCvx * crvPrice;

            var epochOverviewsV1 = dataV1
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
                });

            var epochOverviewsV2 = dataV2
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
                });

            var epochOverviews = epochOverviewsV1
               .Concat(epochOverviewsV2)
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
            Func<HttpClient>,
            FxnData,
            EitherAsync<Error, Db.Bribes.Dashboards.Overview>>
        CreateOverviewFxn = fun((
            ILogger logger,
            IWeb3 web3,
            Func<HttpClient> httpFactory,
            FxnData data) =>
        {
            var totalBribes = data.LatestFinishedEpoch.Bribes.Sum(bribe => bribe.AmountDollars);
            var totalBribed = data.LatestFinishedEpoch.Bribed.Sum(bribed => bribed.Value);
            var dollarPerVlCvx = totalBribes / totalBribed;

            var fxnPrice_ = PriceFunctions.GetPrice(httpFactory, Addresses.Fxn.Token, Network.Ethereum, Some(web3));

            var end = DateTimeExt.FromUnixTimeSeconds(data.LatestFinishedEpoch.End);
            var start = end.AddDays(-14);
            var fxnPerDay_ = 
                from emissions in Fxn.GetMintedTokensInTimeframe(web3, start, end).ToEitherAsync()
                select emissions.DivideByDecimals(18) / 14;

            var votingPower_ = Fxn.GetVotingPower(web3, Addresses.Convex.VoterProxyFxn).ToEitherAsync();

            var scoresTotal_ = data.LatestFinishedEpoch.ScoresTotal > 0
                ? RightAsync<Error, double>(data.LatestFinishedEpoch.ScoresTotal)
                : LeftAsync<Error, double>(Error.New("Total scores is zero"));

            // https://docs.google.com/spreadsheets/d/1SCO33fU-4EglqD9h191c5z3curC3SqJP-yshA1MjVqE/edit#gid=0
            var fxnPerCvxPerRound_ =
                from fxnPerDay in fxnPerDay_
                from votingPower in votingPower_
                from scoresTotal in scoresTotal_
                select fxnPerDay * 14 * votingPower / scoresTotal;

            var rewardPerDollarBribe_ =
                from fxnPrice in fxnPrice_
                from fxnPerCvxPerRound in fxnPerCvxPerRound_
                select fxnPerCvxPerRound / dollarPerVlCvx * fxnPrice;

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
                    Id = Db.Bribes.Dashboards.Overview.Fxn,
                    RewardPerDollarBribe = rewardPerDollarBribe,
                    Epochs = epochOverviews
                };
        });

        public static Func<
            ILogger,
            IWeb3,
            Func<HttpClient>,
            AuraData,
            EitherAsync<Error, Db.Bribes.Dashboards.Overview>>
        CreateOverviewAura = fun((
            ILogger logger,
            IWeb3 web3,
            Func<HttpClient> httpFactory,
            AuraData data) =>
        {
            var totalBribes = data.LatestFinishedEpoch.Bribes.Sum(bribe => bribe.AmountDollars);
            var totalBribed = data.LatestFinishedEpoch.Bribed.Sum(bribed => bribed.Value);
            var dollarPerVlAura = totalBribes / totalBribed;

            var balPrice_ = PriceFunctions.GetPrice(httpFactory, Addresses.Balancer.Token, Network.Ethereum, Some(web3));
            var auraPrice_ = PriceFunctions.GetPrice(httpFactory, Addresses.Aura.Token, Network.Ethereum, Some(web3));

            var auraPerBal_ = Aura.GetAuraMintAmount(web3, 1).ToEitherAsync();
            var balPerDay_ = Balancer.GetRate(web3).DivideByDecimals(Aura.BalancerDecimals).Map(x => x * 86400).ToEitherAsync();
            var votingPower_ = Balancer.GetVotingPower(web3, Addresses.Aura.VoterProxy).ToEitherAsync();

            var scoresTotal_ = data.LatestFinishedEpoch.ScoresTotal > 0
                ? RightAsync<Error, double>(data.LatestFinishedEpoch.ScoresTotal)
                : LeftAsync<Error, double>(Error.New("Total scores is zero"));

            // https://docs.google.com/spreadsheets/d/1SCO33fU-4EglqD9h191c5z3curC3SqJP-yshA1MjVqE/edit#gid=0
            var balPerAuraPerRound_ =
                from balPerDay in balPerDay_
                from votingPower in votingPower_
                from scoresTotal in scoresTotal_
                select balPerDay * 14 * votingPower / scoresTotal;

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