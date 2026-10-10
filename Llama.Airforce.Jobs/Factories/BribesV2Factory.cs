using System.Numerics;
using LanguageExt;
using LanguageExt.Common;
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

namespace Llama.Airforce.Jobs.Factories;

public static class BribesV2Factory
{
    public record OptionsGetBribes(Protocol Protocol, bool LastEpochOnly, string GraphApiKey, int CutoverRound, int FirstProposalId)
    {
        public bool IsEnabledForRound(int round) => CutoverRound > 0 && round >= CutoverRound;
        public int ToProposalId(int round) => round - CutoverRound + FirstProposalId;

        public static OptionsGetBribes FromStrings(
            Protocol protocol,
            bool lastEpochOnly,
            string graphApiKey,
            string? cutoverRound,
            string? firstProposalId)
        {
            var cutover = int.Parse(cutoverRound!);
            if (cutover <= 0)
                throw new InvalidOperationException("On-chain voting cutover round must be a positive integer");

            var proposal = int.Parse(firstProposalId!);
            if (proposal < 0)
                throw new InvalidOperationException("On-chain voting first proposal ID must be non-negative");

            return new(protocol, lastEpochOnly, graphApiKey, cutover, proposal);
        }
    }

    public static EitherAsync<Error, Lst<Db.Bribes.EpochV2>> GetBribes(
        ILogger logger,
        IWeb3 web3,
        Func<HttpClient> httpFactory,
        OptionsGetBribes options,
        Func<long, Address, string, EitherAsync<Error, double>> getPrice)
    {
        var epochs_ = Subgraphs.Votium.GetEpochsV2(httpFactory, options.GraphApiKey, options.Protocol);
        var gauges_ = options.Protocol switch
        {
            Protocol.ConvexCrv => CurveApi.GetGauges(httpFactory),
            Protocol.ConvexFxn => FxnApi.GetGauges(httpFactory),
            _ => throw new Exception($"Unsupported protocol")
        };

        // Votium V2 rounds start with 51 for Curve.
        var indexOffset = 51;
        if (options is { Protocol: Protocol.ConvexFxn })
            indexOffset = 0;

        return
            from epochs in epochs_
            from gauges in gauges_
            from dbEpochs in ProcessEpochs(logger, web3, options, epochs, gauges, indexOffset, getPrice)
            select dbEpochs;
    }

    private static EitherAsync<Error, Lst<Db.Bribes.EpochV2>> ProcessEpochs(
        ILogger logger,
        IWeb3 web3,
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
               .Select(epoch => (Epoch: epoch, Index: epochs.Count - 1 + indexOffset)))
            : toList(epochs
               .Select((epoch, i) => (Epoch: epoch, Index: i + indexOffset)));

        return epochsToProcess
           .Map(epoch => ProcessEpoch(logger, web3, options, epoch.Epoch, gauges, epoch.Index, getPrice))
           .SequenceSerial()
           .Map(toList);
    }

    public static EitherAsync<Error, Db.Bribes.EpochV2> ProcessEpoch(
        ILogger logger,
        IWeb3 web3,
        OptionsGetBribes options,
        Dom.EpochV2 epoch,
        Map<string, string> gauges,
        int index,
        Func<long, Address, string, EitherAsync<Error, double>> getPrice)
    {
        var publicRound = index + 1;
        var epochId = EpochId.Create(
            StringMax.Of(Platform.Votium.ToPlatformString()),
            StringMax.Of(options.Protocol.ToProtocolString()),
            publicRound);

        logger.LogInformation($"Updating bribes: {epochId}");

        if (!options.IsEnabledForRound(publicRound))
            return LeftAsync<Error, Db.Bribes.EpochV2>(
                Error.New($"Round {publicRound} for {options.Protocol} is not eligible for on-chain voting"));

        var platform_ = RightAsync<Error, Address>(options.Protocol switch
        {
            Protocol.ConvexCrv => Addresses.Convex.CurveGaugeVoting,
            Protocol.ConvexFxn => Addresses.Convex.FxGaugeVoting,
            _ => throw new Exception($"Unsupported protocol")
        });

        var proposalId = options.ToProposalId(publicRound);
        var proposal_ =
            from platform in platform_
            from proposal in ConvexOnchainGaugeVoting.GetProposal(web3, platform, proposalId).ToEitherAsync()
            from expectedEpoch in Convex
               .FindEpochId(web3, Subgraphs.Votium.GetEpochDate(options.Protocol, epoch.Round).ToUnixTimeSeconds())
               .ToEitherAsync()
            from validatedProposal in ValidateOnchainProposal(proposal, expectedEpoch).ToAsync()
            select validatedProposal;

        var proposalEnd_ = proposal_.Map(proposal => (long)proposal.EndTime);

        var bribes_ = proposalEnd_.Bind(proposalEnd => epoch.Bribes.ToList()
           .Map(bribe => ProcessBribe(logger, web3, gauges, par(getPrice, proposalEnd), bribe))
           .SequenceSerial()
           .Map(bs => bs.Where(bribe => bribe.Choice != -1))
           .Map(toList)
        );
        var bribed_ = platform_.Bind(platform => bribes_.Bind(bribes => GetBribedPools(web3, platform, proposalId, bribes)));

        var scoresTotal_ =
            from platform in platform_
            from total in ConvexOnchainGaugeVoting.VoteTotal(web3, platform, proposalId).Map(x => x.DivideByDecimals(18)).ToEitherAsync()
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
                SourceRound = epoch.Round,
                End = proposalEnd,
                Proposal = proposalId.ToString(),
                VoteSource = "convex-onchain",
                Bribed = bribed.ToDictionary(),
                Bribes = bribes.ToList(),
                ScoresTotal = scoresTotal
            };

        Either<Error, ConvexOnchainGaugeVoting.ProposalOutput> ValidateOnchainProposal(
            ConvexOnchainGaugeVoting.ProposalOutput proposal,
            BigInteger expectedEpoch)
        {
            var protocol = options.Protocol;
            var roundLabel = publicRound == epoch.Round
                ? $"round {publicRound}"
                : $"round {publicRound} (Votium round {epoch.Round})";

            if (proposal.EndTime == 0)
                return Error.New(
                    $"On-chain voting proposal {proposalId} for {protocol} {roundLabel} is empty or force-ended");

            var previousExpectedEpoch = expectedEpoch - BigInteger.One;
            var matchesExpectedEpoch = proposal.Epoch == expectedEpoch || proposal.Epoch == previousExpectedEpoch;
            if (!matchesExpectedEpoch)
                return Error.New(
                    $"On-chain voting proposal {proposalId} for {protocol} {roundLabel} has vlCVX epoch {proposal.Epoch}, expected {expectedEpoch} or {previousExpectedEpoch}");

            return proposal;
        }
    }

    private static EitherAsync<Error, Map<string, double>> GetBribedPools(
        IWeb3 web3,
        Address platform,
        int proposalId,
        Lst<Db.Bribes.BribeV2> bribes) =>
        bribes
           .DistinctBy(bribe => bribe.Gauge)
           .Map(async bribe => (
               bribe.Pool,
               Score: await ConvexOnchainGaugeVoting.GaugeTotal(web3, platform, proposalId, bribe.Gauge).Map(x => x.DivideByDecimals(18))))
           .SequenceSerial()
           .Map(gauges => gauges
               .Where(gauge => gauge.Score > 0)
               .Aggregate(
                    Map<string, double>(),
                    (acc, gauge) => acc.AddOrUpdate(gauge.Pool, x => x + gauge.Score, gauge.Score)))
           .ToEitherAsync();

    public static EitherAsync<Error, Db.Bribes.BribeV2> ProcessBribe(
        ILogger logger,
        IWeb3 web3,
        Map<string, string> gauges,
        Func<Address, string, EitherAsync<Error, double>> getPrice,
        Dom.BribeV2 bribe)
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

        // Unknown gauges are excluded later without failing the entire epoch.
        var choice_ = gauge_.Map(gauge => string.IsNullOrWhiteSpace(gauge) ? -1 : 0);

        // Convert any price error to $0, but log it. Then convert to EitherAsync for the applicative below.
        var price_ = token_.Bind(token => getPrice(tokenAddress, token))
            .Match(
                Right: x => x,
                Left: ex => { logger.LogWarning(ex.Message); return 0; })
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
    }
}
