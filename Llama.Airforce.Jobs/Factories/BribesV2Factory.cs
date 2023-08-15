using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;
using Llama.Airforce.Database.Models.Bribes;
using Llama.Airforce.Domain.Models;
using Llama.Airforce.Jobs.Extensions;
using Llama.Airforce.SeedWork.Extensions;
using Llama.Airforce.SeedWork.Types;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using static LanguageExt.Prelude;
using Db = Llama.Airforce.Database.Models;
using Dom = Llama.Airforce.Domain.Models;
using Snap = Llama.Airforce.Jobs.Snapshots.Models;

namespace Llama.Airforce.Jobs.Factories;

public static class BribesV2Factory
{
    public record OptionsGetBribes(
        bool LastEpochOnly);

    public record BribesFunctions(
        Func<EitherAsync<Error, Map<string, (int, string)>>> GetProposalIds,
        Func<string, EitherAsync<Error, Snap.Proposal>> GetProposal,
        Func<EitherAsync<Error, Lst<Dom.EpochV2>>> GetEpochs,
        Func<string, EitherAsync<Error, Lst<Snap.Vote>>> GetVotes,
        Func<Lst<Address>, BigInteger, EitherAsync<Error, Map<Address, double>>> GetScores,
        Func<EitherAsync<Error, Map<Address, CurveApi.Gauge>>> GetGauges);

    public static BribesFunctions GetBribesFunctions(
        Func<HttpClient> httpFactory) =>
        new(
            Snapshots.Convex.GetProposalIdsV2.Par(httpFactory),
            Snapshots.Snapshot.GetProposal.Par(httpFactory),
            Subgraphs.Votium.GetEpochsV2.Par(httpFactory),
            Snapshots.Snapshot.GetVotes.Par(httpFactory),
            Snapshots.Convex.GetScores.Par(httpFactory),
            CurveApi.GetGauges.Par(httpFactory));

    public static Func<
            ILogger,
            IWeb3,
            Func<HttpClient>,
            OptionsGetBribes,
            Func<long, Address, string, EitherAsync<Error, double>>,
            EitherAsync<Error, Lst<Db.Bribes.EpochV2>>>
        GetBribes = fun((
            ILogger logger,
            IWeb3 web3,
            Func<HttpClient> httpFactory,
            OptionsGetBribes options,
            Func<long, Address, string, EitherAsync<Error, double>> getPrice) =>
        {
            var bribeFunctions = GetBribesFunctions(httpFactory);

            var proposalIds_ = bribeFunctions.GetProposalIds();
            var epochs_ = bribeFunctions.GetEpochs();
            var gauges_ = bribeFunctions.GetGauges();

            // Votium V2 rounds start with 50 or 51. To be determined.
            var indexOffset = 50;

            EitherAsync<Error, EitherAsync<Error, Lst<Db.Bribes.EpochV2>>> dbEpochs;
            if (options.LastEpochOnly)
            {
                dbEpochs =
                    from proposalIds in proposalIds_
                    from epochs in epochs_
                    from gauges in gauges_
                    select epochs
                        .Reverse()
                        .Take(1)
                        .Map(epoch => ProcessEpoch(
                            logger,
                            web3,
                            new OptionsProcessEpoch(
                                bribeFunctions,
                                proposalIds,
                                epoch,
                                gauges,
                            epochs.Count - 1 + indexOffset),
                            getPrice))
                        .SequenceSerial()
                        .Map(toList);
            }
            else
            {
                dbEpochs =
                    from proposalIds in proposalIds_
                    from epochs in epochs_
                    from gauges in gauges_
                    select epochs
                        .Map((i, epoch) => ProcessEpoch(
                            logger,
                            web3,
                            new OptionsProcessEpoch(
                                bribeFunctions,
                                proposalIds,
                                epoch,
                                gauges,
                                i + indexOffset),
                            getPrice))
                        .SequenceSerial()
                        .Map(toList);
            }

            return dbEpochs.Bind(x => x);
        });

    public record OptionsProcessEpoch(
        BribesFunctions BribesFunctions,
        Map<string, (int Index, string Title)> ProposalIds,
        Dom.EpochV2 Epoch,
        Map<Address, CurveApi.Gauge> Gauges,
        int Index);

    public static Func<
            ILogger,
            IWeb3,
            OptionsProcessEpoch,
            Func<long, Address, string, EitherAsync<Error, double>>,
            EitherAsync<Error, Db.Bribes.EpochV2>>
        ProcessEpoch = fun((
            ILogger logger,
            IWeb3 web3,
            OptionsProcessEpoch options,
            Func<long, Address, string, EitherAsync<Error, double>> getPrice) =>
        {
            var epochId = EpochId.Create(
                StringMax.Of(Platform.Votium.ToPlatformString()),
                StringMax.Of(Protocol.ConvexCrv.ToProtocolString()),
                options.Index + 1);

            logger.LogInformation($"Updating bribes: {epochId}");

            // Find proposal id my regex matching all proposal titles with the correct date.
            var epoch = options.Epoch;
            var epochStart = 1348 * 86400 * 14 + epoch.Round * 86400 * 14;
            var epochDate = DateTimeExt.FromUnixTimeSeconds(epochStart);
            var epochMonth = epochDate.ToString("MMM", CultureInfo.InvariantCulture);
            var titleRegex = $"{epochDate.Day}(st|nd|rd|th) {epochMonth} {epochDate.Year}";

            var proposalId_ = options.ProposalIds
                .Find(x => Regex.IsMatch(x.Value.Title, titleRegex))
                .Map(x => x.Key)
                .ToEitherAsync(Error.New($"Failed to find id for proposal {titleRegex}"));

            var proposal_ = proposalId_.Bind(options.BribesFunctions.GetProposal);

            var bribes_ = proposal_.Bind(proposal =>
            {
                var bribes = epoch.Bribes.ToList();
                return bribes
                    // Process each bribe.
                    .Map(par(ProcessBribe, logger, web3, proposal, options.Gauges, par(getPrice, proposal.End)))
                    // Transform the list of tasks to a task of a list.
                    .SequenceSerial()
                    .Map(toList);
            });

            var bribeChoices_ =
                from proposal in proposal_
                from bribes in bribes_
                select bribes
                    .Distinct()
                    .Map(bribe => (
                        Pool: proposal.Choices[bribe.Choice],
                        // We need to undo the Snapshot index offset.
                        Choice: (bribe.Choice + 1).ToString()))
                    .toList();

            var votes_ = proposal_.Bind(proposal => options
                .BribesFunctions
                .GetVotes(proposal.Id));

            var snapshot_ = proposal_.MapTry(proposal => BigInteger.Parse(proposal.Snapshot));

            var scores_ = (
                from votes in votes_
                from snapshot in snapshot_
                select options.BribesFunctions.GetScores(
                    votes.Map(vote => Address.Of(vote.Voter)).Somes().toList(),
                    snapshot))
                .Bind(x => x);

            var votesPools_ =
                from proposalId in proposalId_
                from votes in votes_
                from scores in scores_
                from bribeChoices in bribeChoices_
                select votes
                    .Aggregate(
                        Map<string, double>(),
                        (acc, vote) =>
                        {
                            var voteTotal = vote.Choices.Values.Sum();

                            // It's possible people vote on 'nothing' through Snapshot.
                            if (voteTotal == 0) return acc;

                            var voter = Address.Of(vote.Voter);
                            var voteWeight = scores.Find(voter).ValueUnsafe();

                            // Only bother with choices that can be bribed.
                            var choices = vote.Choices
                                .Map(x => bribeChoices
                                    .Find(b => b.Choice == x.Key)
                                    .Map(y => (y.Pool, Score: x.Value)))
                                .Somes();

                            foreach (var choice in choices)
                            {
                                var scoreNormalized = choice.Score / voteTotal;
                                var scoreWeighted = voteWeight * scoreNormalized;
                                acc = acc.AddOrUpdate(choice.Pool, x => x + scoreWeighted, scoreWeighted);
                            }

                            return acc;
                        });

            return from proposal in proposal_
                   from bribes in bribes_
                   from votesPools in votesPools_
                   select new Db.Bribes.EpochV2
                   {
                       Platform = Platform.Votium.ToPlatformString(),
                       Protocol = Protocol.ConvexCrv.ToProtocolString(),
                       Round = options.Index + 1,
                       End = proposal.End,
                       Proposal = proposal.Id,
                       Bribed = votesPools.ToDictionary(),
                       Bribes = bribes.ToList(),
                       ScoresTotal = proposal.ScoresTotal
                   };
        });

    public static Func<
            ILogger,
            IWeb3,
            Snap.Proposal,
            Map<Address, CurveApi.Gauge>,
            Func<Address, string, EitherAsync<Error, double>>,
            Dom.BribeV2,
            EitherAsync<Error, Db.Bribes.BribeV2>>
        ProcessBribe = fun((
            ILogger logger,
            IWeb3 web3,
            Snap.Proposal proposal,
            Map<Address, CurveApi.Gauge> gauges,
            Func<Address, string, EitherAsync<Error, double>> getPrice,
            Dom.BribeV2 bribe) =>
        {
            var tokenAddress = Address.Of(bribe.Token);
            var gaugeAddress = Address.Of(bribe.Gauge)
               .ToEither(Error.New("Invalid gauge address"))
               .ToAsync();

            var token_ = Contracts.ERC20.GetSymbol(web3, tokenAddress).ToEitherAsync();
            var decimals_ = Contracts.ERC20.GetDecimals(web3, tokenAddress).ToEitherAsync();

            var amount_ = decimals_.Map(decimals => BigInteger.Parse(bribe.Amount).DivideByDecimals(decimals));
            var maxPerVote_ = decimals_.Map(decimals => BigInteger.Parse(bribe.MaxPerVote).DivideByDecimals(decimals));
            var gauge_ = gaugeAddress.Bind(ga => gauges
               .Find(ga)
               .ToEither(Error.New($"Could not find pool name for gauge '{ga.Value}'"))
               .ToAsync());

            var choice_ = gauge_.Bind(gauge =>
            {
                var index = proposal
                   .Choices
                   .FindIndex(gauge.ShortName.StartsWith);

                return index == -1
                    ? EitherAsync<Error, int>.Left($"Choice index was not found for gauge '{gauge.ShortName}'")
                    : EitherAsync<Error, int>.Right(index);
            });

            // Convert any price error to $0, but log it. Then convert to EitherAsync for the applicative below.
            var price_ = token_.Bind(token => getPrice(tokenAddress, token))
                .Match(
                    Right: x => x,
                    Left: ex =>
                    {
                        logger.LogError(ex.Message);
                        return 0;
                    })
                .ToEitherAsync();

            return
                from gauge in gauge_
                from price in price_
                from token in token_
                from choice in choice_
                from amount in amount_
                from maxPerVote in maxPerVote_
                select new Db.Bribes.BribeV2
                {
                    Pool = gauge.ShortName,
                    Token = token,
                    Choice = choice,
                    Gauge = gauge.Address,
                    Amount = amount,
                    AmountDollars = amount * price,
                    MaxPerVote = maxPerVote,
                    Excluded = new List<string>()
                };
        });
}