using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;
using Llama.Airforce.Database.Models.Bribes;
using Llama.Airforce.Domain.Models;
using Llama.Airforce.Jobs.Contracts;
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
        string GraphApiKey,
        OnchainVotingOptions? OnchainVoting = null);

    public record OnchainVotingOptions(
        int CutoverRound,
        int FirstProposalId = 0)
    {
        public bool IsEnabledForRound(int round) =>
            CutoverRound > 0 && round >= CutoverRound;

        public int ToProposalId(int round) =>
            round - CutoverRound + FirstProposalId;

        public static OnchainVotingOptions? FromStrings(
            string? cutoverRound,
            string? firstProposalId)
        {
            if (!int.TryParse(cutoverRound, out var cutover) || cutover <= 0)
                return null;

            var firstProposal = int.TryParse(firstProposalId, out var proposal)
                ? proposal
                : 0;

            return new OnchainVotingOptions(cutover, firstProposal);
        }
    }

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

            var epochs_ = bribeFunctions.GetEpochs();
            var gauges_ = bribeFunctions.GetGauges();

            // Votium V2 rounds start with 51 for Curve.
            var indexOffset = 51;
            if (options is { Protocol: Protocol.ConvexFxn })
                indexOffset = 0;

            EitherAsync<Error, EitherAsync<Error, Lst<Db.Bribes.EpochV2>>> dbEpochs =
                from epochs in epochs_
                from gauges in gauges_
                select ProcessEpochs(
                    logger,
                    web3,
                    bribeFunctions,
                    options,
                    epochs,
                    gauges,
                    indexOffset,
                    getPrice);

            return dbEpochs.Bind(x => x);
        });

    private record EpochToProcess(
        Dom.EpochV2 Epoch,
        int Index)
    {
        public int PublicRound => Index + 1;
    }

    private static EitherAsync<Error, Lst<Db.Bribes.EpochV2>> ProcessEpochs(
        ILogger logger,
        IWeb3 web3,
        BribesFunctions bribeFunctions,
        OptionsGetBribes options,
        Lst<Dom.EpochV2> epochs,
        Map<string, string> gauges,
        int indexOffset,
        Func<long, Address, string, EitherAsync<Error, double>> getPrice)
    {
        var epochsToProcess = options.LastEpochOnly
            ? toList(epochs
               .Reverse()
               .Take(1)
               .Select(epoch => new EpochToProcess(epoch, epochs.Count - 1 + indexOffset)))
            : toList(epochs
               .Select((epoch, i) => new EpochToProcess(epoch, i + indexOffset)));

        var needsSnapshotProposalIds = epochsToProcess
           .Any(epoch => !IsOnchainRound(options.Protocol, epoch.PublicRound, options.OnchainVoting));

        var proposalIds_ = needsSnapshotProposalIds
            ? bribeFunctions.GetProposalIds()
            : RightAsync<Error, Map<string, (int Index, string Title)>>(Map<string, (int Index, string Title)>());

        return proposalIds_
           .Bind(proposalIds => epochsToProcess
               .Map(epoch => ProcessEpoch(
                    logger,
                    web3,
                    new OptionsProcessEpoch(
                        bribeFunctions,
                        options.Protocol,
                        proposalIds,
                        epoch.Epoch,
                        gauges,
                        epoch.Index,
                        options.OnchainVoting),
                    getPrice))
               .SequenceSerial()
               .Map(toList));
    }

    public record OptionsProcessEpoch(
        BribesFunctions BribesFunctions,
        Protocol Protocol,
        Map<string, (int Index, string Title)> ProposalIds,
        Dom.EpochV2 Epoch,
        Map<string, string> Gauges,
        int Index,
        OnchainVotingOptions? OnchainVoting = null)
    {
        public int PublicRound => Index + 1;
    }

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
            var publicRound = options.PublicRound;
            var epochId = EpochId.Create(
                StringMax.Of(Platform.Votium.ToPlatformString()),
                StringMax.Of(options.Protocol.ToProtocolString()),
                publicRound);

            logger.LogInformation($"Updating bribes: {epochId}");

            // Find proposal id by regex matching all proposal titles with the correct date.
            var epoch = options.Epoch;
            if (IsOnchainRound(options.Protocol, publicRound, options.OnchainVoting))
                return ProcessOnchainEpoch(logger, web3, options, getPrice);

            var epochDate = Subgraphs.Votium.GetEpochDate(options.Protocol, epoch.Round);
            var epochMonth = epochDate.ToString("MMM", CultureInfo.InvariantCulture);
            var titleRegex = $"{epochDate.Day}(st|nd|rd|th) {epochMonth} {epochDate.Year}";

            // There's been a few cases where gauge votes had to be remade on Snapshot, like on 24 Apr 2025.
            var proposalId_ = options.Protocol switch
            {
                Protocol.ConvexCrv when epoch.Round == 95 => "0x3016b4856269e94064a8ddd5bd6d229a03f08471c011b6fa4ddbccd225b4e6aa",
                Protocol.ConvexCrv when epoch.Round == 110 => "0x40a7f783fb51632162a16ae65a10f142547e25eb660f37060ca242f4c68df7cd",
                Protocol.ConvexCrv when epoch.Round == 121 => "0xda3349d575981a16774bc9308ba4202938e2c839899ff53a94725121e285aece",
                Protocol.ConvexFxn when epoch.Round == 110 => "0x33f4e770e566dc8c5e47d9b3b0385ec0816fa9f999f9751d915c171a3176b265",
                Protocol.ConvexFxn when epoch.Round == 56 => "0x138df822ddbf89129e229e52d99ce73fc2527a952c80502628552a258dd7c91c",
                _ => options.ProposalIds
                    .Find(x => Regex.IsMatch(x.Value.Title, titleRegex))
                    .Map(x => x.Key)
                    .ToEitherAsync(Error.New($"Failed to find id for proposal {titleRegex}"))
            };

            var proposal_ = proposalId_.Bind(options.BribesFunctions.GetProposal);

            var bribes_ = proposal_.Bind(proposal =>
            {
                var bribes = epoch.Bribes.ToList();
                return bribes
                    // Process each bribe.
                    .Map(par(
                        ProcessBribe,
                        logger,
                        web3,
                        new ProcessBribeOptions(proposal.Choices),
                        options.Gauges,
                        par(getPrice, proposal.End)))
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
                       Round = publicRound,
                       SourceRound = epoch.Round,
                       End = proposal.End,
                       Proposal = proposal.Id,
                       VoteSource = "snapshot",
                       Bribed = votesPools.ToDictionary(),
                       Bribes = bribes.ToList(),
                       ScoresTotal = proposal.ScoresTotal
                   };
        });

    private static bool IsOnchainRound(
        Protocol protocol,
        int round,
        OnchainVotingOptions? onchainVoting) =>
        protocol is Protocol.ConvexCrv or Protocol.ConvexFxn
        && onchainVoting is not null
        && onchainVoting.IsEnabledForRound(round);

    private static Either<Error, Address> GetOnchainGaugeVotingPlatform(Protocol protocol) =>
        protocol switch
        {
            Protocol.ConvexCrv => Either<Error, Address>.Right(Addresses.Convex.CurveGaugeVoting),
            Protocol.ConvexFxn => Either<Error, Address>.Right(Addresses.Convex.FxGaugeVoting),
            _ => Either<Error, Address>.Left(Error.New($"Unsupported on-chain voting protocol: {protocol}"))
        };

    private static EitherAsync<Error, Db.Bribes.EpochV2> ProcessOnchainEpoch(
        ILogger logger,
        IWeb3 web3,
        OptionsProcessEpoch options,
        Func<long, Address, string, EitherAsync<Error, double>> getPrice)
    {
        if (options.OnchainVoting is null)
            return LeftAsync<Error, Db.Bribes.EpochV2>(Error.New("On-chain voting options not set"));

        var platform_ = GetOnchainGaugeVotingPlatform(options.Protocol).ToAsync();
        var publicRound = options.PublicRound;
        var votiumRound = options.Epoch.Round;
        var proposalId = options.OnchainVoting.ToProposalId(publicRound);
        var proposal_ =
            from platform in platform_
            from proposal in ConvexOnchainGaugeVoting
               .GetProposal(web3, platform, proposalId)
               .ToEitherAsync()
            from expectedEpoch in GetExpectedOnchainVotingEpoch(web3, options.Protocol, votiumRound)
            from validatedProposal in ValidateOnchainProposal(
                options.Protocol,
                publicRound,
                votiumRound,
                proposalId,
                proposal,
                expectedEpoch)
            select validatedProposal;

        var proposalEnd_ = proposal_.Map(proposal => (long)proposal.EndTime);

        var bribes_ = proposalEnd_.Bind(proposalEnd =>
        {
            var bribes = options.Epoch.Bribes.ToList();
            return bribes
               .Map(par(
                    ProcessBribe,
                    logger,
                    web3,
                    new ProcessBribeOptions(),
                    options.Gauges,
                    par(getPrice, proposalEnd)))
               .SequenceSerial()
               .Map(bs => bs
                  .Where(bribe => bribe.Choice != -1))
               .Map(toList);
        });

        var bribed_ = platform_.Bind(platform => bribes_
           .Bind(bribes => bribes
               .DistinctBy(bribe => bribe.Gauge)
               .Map(async bribe =>
                {
                    var score = await ConvexOnchainGaugeVoting
                       .GaugeTotal(web3, platform, proposalId, bribe.Gauge)
                       .Map(x => x.DivideByDecimals(18));

                    return new
                    {
                        bribe.Pool,
                        Score = score
                    };
                })
               .SequenceSerial()
               .Map(toList)
               .Map(gauges => gauges
                   .Where(gauge => gauge.Score > 0)
                   .Aggregate(
                        Map<string, double>(),
                        (acc, gauge) => acc.AddOrUpdate(gauge.Pool, x => x + gauge.Score, gauge.Score)))
               .ToEitherAsync()));

        var scoresTotal_ =
            from platform in platform_
            from total in ConvexOnchainGaugeVoting
               .VoteTotal(web3, platform, proposalId)
               .Map(x => x.DivideByDecimals(18))
               .ToEitherAsync()
            select total;

        return
            from proposalEnd in proposalEnd_
            from bribes in bribes_
            from bribed in bribed_
            from scoresTotal in scoresTotal_
            select new Db.Bribes.EpochV2
            {
                Platform = Platform.Votium.ToPlatformString(),
                Protocol = options.Protocol.ToProtocolString(),
                Round = publicRound,
                SourceRound = votiumRound,
                End = proposalEnd,
                Proposal = proposalId.ToString(),
                VoteSource = "convex-onchain",
                Bribed = bribed.ToDictionary(),
                Bribes = bribes.ToList(),
                ScoresTotal = scoresTotal
            };
    }

    private static EitherAsync<Error, BigInteger> GetExpectedOnchainVotingEpoch(
        IWeb3 web3,
        Protocol protocol,
        int round)
    {
        var expectedRoundDate = Subgraphs.Votium
           .GetEpochDate(protocol, round)
           .ToUnixTimeSeconds();

        return Convex
           .FindEpochId(web3, expectedRoundDate)
           .ToEitherAsync();
    }

    private static EitherAsync<Error, ConvexOnchainGaugeVoting.ProposalOutput> ValidateOnchainProposal(
        Protocol protocol,
        int publicRound,
        int votiumRound,
        int proposalId,
        ConvexOnchainGaugeVoting.ProposalOutput proposal,
        BigInteger expectedEpoch)
    {
        var roundLabel = publicRound == votiumRound
            ? $"round {publicRound}"
            : $"round {publicRound} (Votium round {votiumRound})";

        if (proposal.EndTime == 0)
            return LeftAsync<Error, ConvexOnchainGaugeVoting.ProposalOutput>(
                Error.New(
                    $"On-chain voting proposal {proposalId} for {protocol} {roundLabel} is empty or force-ended"));

        var previousExpectedEpoch = expectedEpoch - BigInteger.One;
        if (proposal.Epoch != expectedEpoch && proposal.Epoch != previousExpectedEpoch)
            return LeftAsync<Error, ConvexOnchainGaugeVoting.ProposalOutput>(
                Error.New(
                    $"On-chain voting proposal {proposalId} for {protocol} {roundLabel} has vlCVX epoch {proposal.Epoch}, expected {expectedEpoch} or {previousExpectedEpoch}"));

        return RightAsync<Error, ConvexOnchainGaugeVoting.ProposalOutput>(proposal);
    }

    public record ProcessBribeOptions(List<string>? SnapshotChoices = null);

    public static Func<
            ILogger,
            IWeb3,
            ProcessBribeOptions,
            Map<string, string>,
            Func<Address, string, EitherAsync<Error, double>>,
            Dom.BribeV2,
            EitherAsync<Error, Db.Bribes.BribeV2>>
        ProcessBribe = fun((
            ILogger logger,
            IWeb3 web3,
            ProcessBribeOptions options,
            Map<string, string> gauges,
            Func<Address, string, EitherAsync<Error, double>> getPrice,
            Dom.BribeV2 bribe) =>
        {
            var tokenAddress = Address.Of(bribe.Token);
            var token_ = ERC20.GetSymbol(web3, tokenAddress).ToEitherAsync();
            var decimals_ = ERC20.GetDecimals(web3, tokenAddress).ToEitherAsync();

            var amount_ = decimals_.Map(decimals => BigInteger.Parse(bribe.Amount).DivideByDecimals(decimals));
            var maxPerVote_ = decimals_.Map(decimals => BigInteger.Parse(bribe.MaxPerVote).DivideByDecimals(decimals));
            var gauge_ = gauges
               .Find(bribe.Gauge)
               .Match(
                    Some: x => Either<Error, string>.Right(x),
                    None: () =>
                    {
                        logger.LogWarning($"Could not find pool name for gauge '{bribe.Gauge}'");
                        return Either<Error, string>.Right("");
                    })
               .ToAsync();

            var choice_ = gauge_.Bind(gauge =>
            {
                if (string.IsNullOrWhiteSpace(gauge))
                    return EitherAsync<Error, int>.Right(-1);

                var index = options.SnapshotChoices is null
                    ? 0
                    : options.SnapshotChoices.FindIndex(gauge.StartsWith);

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
