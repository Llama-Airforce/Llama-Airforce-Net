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
        Protocol Protocol,
        bool LastEpochOnly,
        string GraphApiKey);

    public record BribesFunctions(
        Func<EitherAsync<Error, Map<string, (int, string)>>> GetProposalIds,
        Func<string, EitherAsync<Error, Snap.Proposal>> GetProposal,
        Func<EitherAsync<Error, Lst<Dom.EpochV2>>> GetEpochs,
        Func<string, EitherAsync<Error, Lst<Snap.Vote>>> GetVotes,
        Func<Lst<Address>, BigInteger, EitherAsync<Error, Map<Address, double>>> GetScores,
        Func<EitherAsync<Error, Map<string, string>>> GetGauges);

    public static BribesFunctions GetBribesFunctions(
        Protocol protocol,
        Func<HttpClient> httpFactory,
        string graphApiKey) =>
        protocol switch
        {
            Protocol.ConvexCrv => new(
                Snapshots.Convex.GetProposalIdsV2.Par(httpFactory),
                Snapshots.Snapshot.GetProposal.Par(httpFactory),
                Subgraphs.Votium.GetEpochsV2.Par(httpFactory).Par(graphApiKey).Par(Protocol.ConvexCrv),
                Snapshots.Snapshot.GetVotes.Par(httpFactory),
                Snapshots.Convex.GetScores.Par(httpFactory),
                CurveApi.GetGaugesGaugeToShortName.Par(httpFactory)),

            Protocol.ConvexFxn => new(
                Snapshots.Convex.GetProposalIdsFxn.Par(httpFactory),
                Snapshots.Snapshot.GetProposal.Par(httpFactory),
                Subgraphs.Votium.GetEpochsV2.Par(httpFactory).Par(graphApiKey).Par(Protocol.ConvexFxn),
                Snapshots.Snapshot.GetVotes.Par(httpFactory),
                Snapshots.Convex.GetScores.Par(httpFactory),
                FxnApi.GetGauges.Par(httpFactory)),

            _ => throw new Exception($"Unsupported protocol")
        };

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
            var bribeFunctions = GetBribesFunctions(
                options.Protocol,
                httpFactory,
                options.GraphApiKey);

            var proposalIds_ = bribeFunctions.GetProposalIds();
            var epochs_ = bribeFunctions.GetEpochs();
            var gauges_ = bribeFunctions.GetGauges();

            // Votium V2 rounds start with 51 for Curve.
            var indexOffset = 51;
            if (options is { Protocol: Protocol.ConvexFxn })
                indexOffset = 0;

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
                                options.Protocol,
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
                                options.Protocol,
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
        Protocol Protocol,
        Map<string, (int Index, string Title)> ProposalIds,
        Dom.EpochV2 Epoch,
        Map<string, string> Gauges,
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
                StringMax.Of(options.Protocol.ToProtocolString()),
                options.Index + 1);

            logger.LogInformation($"Updating bribes: {epochId}");

            // Find proposal id by regex matching all proposal titles with the correct date.
            var epoch = options.Epoch;
            var epochDate = Subgraphs.Votium.GetEpochDate(options.Protocol, epoch.Round);
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
                    // Filter out problematic bribes.
                    .Map(bs => bs
                       .Where(bribe => bribe.Choice != -1))
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
                       Protocol = options.Protocol.ToProtocolString(),
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
            Map<string, string>,
            Func<Address, string, EitherAsync<Error, double>>,
            Dom.BribeV2,
            EitherAsync<Error, Db.Bribes.BribeV2>>
        ProcessBribe = fun((
            ILogger logger,
            IWeb3 web3,
            Snap.Proposal proposal,
            Map<string, string> gauges,
            Func<Address, string, EitherAsync<Error, double>> getPrice,
            Dom.BribeV2 bribe) =>
        {
            var tokenAddress = Address.Of(bribe.Token);
            var token_ = Contracts.ERC20.GetSymbol(web3, tokenAddress).ToEitherAsync();
            var decimals_ = Contracts.ERC20.GetDecimals(web3, tokenAddress).ToEitherAsync();

            var amount_ = decimals_.Map(decimals => BigInteger.Parse(bribe.Amount).DivideByDecimals(decimals));
            var maxPerVote_ = decimals_.Map(decimals => BigInteger.Parse(bribe.MaxPerVote).DivideByDecimals(decimals));
            var gauge_ = gauges
               .Find(bribe.Gauge)
               .Match(
                    Some: x => Either<Error, string>.Right(x),
                    None: () => {
                        logger.LogWarning($"Could not find pool name for gauge '{bribe.Gauge}'");
                        return "0x0";
                    })
               .ToAsync();

            var choice_ = gauge_.Bind(gauge =>
            {
                var index = proposal
                   .Choices
                   .FindIndex(gauge.StartsWith);

                if (index == -1)
                    logger.LogWarning($"Choice index was not found for gauge '{gauge}'");

                return EitherAsync<Error, int>.Right(index);

                /*
                 * I no longer want to bubble up the error and fail early, because sometimes a smol $1k bribe
                 * can cause an additional $150k or more in bribes to go unaccounted for. Yes, it works in that people
                 * will notify me sooner, but it also prevents important users from accessing up-to-date data they require,
                 * data that has nothing to do with the problematic bribe. Instead, keep an eye on the logs, and the code
                 * will continue to function properly for the non-problematic cases. Therefore, return -1 and filter these out
                 * later with .Where(bribe => bribe.Choice != -1)). If I'm unable to fix issues in time before the round ends,
                 * at least some major, important bribes will still be recorded.
                 *
                 * return index == -1
                 *   ? EitherAsync<Error, int>.Left($"Choice index was not found for gauge '{gauge}'")
                 *   : EitherAsync<Error, int>.Right(index);
                 */
            });

            // Convert any price error to $0, but log it. Then convert to EitherAsync for the applicative below.
            var price_ = token_.Bind(token => getPrice(tokenAddress, token))
                .Match(
                    Right: x => x,
                    Left: ex =>
                    {
                        logger.LogWarning(ex.Message);
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
                    Pool = gauge,
                    Token = token,
                    Choice = choice,
                    Gauge = bribe.Gauge,
                    Amount = amount,
                    AmountDollars = amount * price,
                    MaxPerVote = maxPerVote,
                    Excluded = new List<string>()
                };
        });
}