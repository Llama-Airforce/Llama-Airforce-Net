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

public static class BribesV3Factory
{
    public record OptionsGetBribes(
        ILogger Logger,
        IWeb3 Web3ETH,
        IWeb3 Web3ZKEVM,
        Func<HttpClient> HttpFactory,
        bool LastEpochOnly);

    public record BribesFunctions(
        Func<EitherAsync<Error, Lst<Dom.EpochV3>>> GetEpochs,
        Func<EitherAsync<Error, Map<Address, CurveApi.Gauge>>> GetGauges);

    public static BribesFunctions GetBribesFunctions(
        Func<HttpClient> httpFactory) =>
        new(
            Subgraphs.Votium.GetEpochsV3.Par(httpFactory),
            CurveApi.GetGauges.Par(httpFactory));

    public static Func<
            OptionsGetBribes,
            Func<long, Address, string, EitherAsync<Error, double>>,
            EitherAsync<Error, Lst<Db.Bribes.EpochV3>>>
        GetBribes = fun((
            OptionsGetBribes options,
            Func<long, Address, string, EitherAsync<Error, double>> getPrice) =>
        {
            var bribeFunctions = GetBribesFunctions(options.HttpFactory);

            var epochs_ = bribeFunctions.GetEpochs();
            var gauges_ = bribeFunctions.GetGauges();

            // Votium V2 rounds start with 51.
            var indexOffset = 51;

            EitherAsync<Error, EitherAsync<Error, Lst<Db.Bribes.EpochV3>>> dbEpochs;
            if (options.LastEpochOnly)
            {
                dbEpochs =
                    from epochs in epochs_
                    from gauges in gauges_
                    select epochs
                        .Reverse()
                        .Take(1)
                        .Map(epoch => ProcessEpoch(
                            options.Logger,
                            options.Web3ETH,
                            options.Web3ZKEVM,
                            getPrice,
                            new OptionsProcessEpoch(
                                epoch,
                                gauges,
                            epochs.Count - 1 + indexOffset)))
                        .SequenceSerial()
                        .Map(toList);
            }
            else
            {
                dbEpochs =
                    from epochs in epochs_
                    from gauges in gauges_
                    select epochs
                        .Map((i, epoch) => ProcessEpoch(
                            options.Logger,
                            options.Web3ETH,
                            options.Web3ZKEVM,
                            getPrice,
                            new OptionsProcessEpoch(
                                epoch,
                                gauges,
                                i + indexOffset)))
                        .SequenceSerial()
                        .Map(toList);
            }

            return dbEpochs.Bind(x => x);
        });

    public record OptionsProcessEpoch(
        Dom.EpochV3 Epoch,
        Map<Address, CurveApi.Gauge> Gauges,
        int Index);

    public static Func<
            ILogger,
            IWeb3,
            IWeb3,
            Func<long, Address, string, EitherAsync<Error, double>>,
            OptionsProcessEpoch,
            EitherAsync<Error, Db.Bribes.EpochV3>>
        ProcessEpoch = fun((
            ILogger logger,
            IWeb3 web3ETH,
            IWeb3 web3ZKEVM,
            Func<long, Address, string, EitherAsync<Error, double>> getPrice,
            OptionsProcessEpoch options) =>
        {
            var epochId = EpochId.Create(
                StringMax.Of(Platform.Votium.ToPlatformString()),
                StringMax.Of(Protocol.ConvexCrv.ToProtocolString()),
                options.Index + 1);

            logger.LogInformation($"Updating bribes: {epochId}");

            var epoch = options.Epoch;

            const int proposalIdOffset = 48; // The round id the L2 proposal indices start with.
            var proposalId = epoch.Round - proposalIdOffset;

            var proposalEnd_ = ConvexL2Voting
               .GetProposal(web3ZKEVM, proposalId)
               .Map(proposal => (long)proposal.EndTime)
               .ToEitherAsync();

            var bribes_ = proposalEnd_.Bind(proposalEnd =>
            {
                var bribes = epoch.Bribes.ToList();

                return bribes
                    // Process each bribe.
                    .Map(par(ProcessBribe, logger, web3ETH, options.Gauges, par(getPrice, proposalEnd)))
                    // Transform the list of tasks to a task of a list.
                    .SequenceSerial()
                    .Map(toList);
            });

            var bribed_ = bribes_
               .Bind(bribes => bribes
                   .DistinctBy(bribe => bribe.Gauge)
                   .Map(async bribe =>
                    {
                        var Score = await ConvexL2Voting
                           .GaugeTotal(web3ZKEVM, proposalId, bribe.Gauge)
                           .Map(x => x.DivideByDecimals(18));

                        return new
                        {
                            bribe.Pool,
                            Score
                        };
                    })
                   .SequenceSerial()
                   .Map(toList)
                   .Map(gauges => gauges
                       .Aggregate(Map<string, double>(), (
                                acc,
                                gauge) =>
                            acc.AddOrUpdate(gauge.Pool, x => x + gauge.Score, gauge.Score)))
                   .ToEitherAsync());

            return from proposalEnd in proposalEnd_
                   from bribes in bribes_
                   from bribed in bribed_
                   select new Db.Bribes.EpochV3
                   {
                       Platform = Platform.Votium.ToPlatformString(),
                       Protocol = Protocol.ConvexCrv.ToProtocolString(),
                       Round = options.Index + 1,
                       End = proposalEnd,
                       Proposal = proposalId.ToString(),
                       Bribed = bribed.ToDictionary(),
                       Bribes = bribes.ToList(),
                       ScoresTotal = 0 // TODO: Awaiting c2tp adding this to the platform contract.
                   };
        });

    public static Func<
            ILogger,
            IWeb3,
            Map<Address, CurveApi.Gauge>,
            Func<Address, string, EitherAsync<Error, double>>,
            Dom.BribeV3,
            EitherAsync<Error, Db.Bribes.BribeV3>>
        ProcessBribe = fun((
            ILogger logger,
            IWeb3 web3,
            Map<Address, CurveApi.Gauge> gauges,
            Func<Address, string, EitherAsync<Error, double>> getPrice,
            Dom.BribeV3 bribe) =>
        {
            var tokenAddress = Address.Of(bribe.Token);
            var gaugeAddress = Address.Of(bribe.Gauge)
               .ToEither(Error.New("Invalid gauge address"))
               .ToAsync();

            var token_ = ERC20.GetSymbol(web3, tokenAddress).ToEitherAsync();
            var decimals_ = ERC20.GetDecimals(web3, tokenAddress).ToEitherAsync();

            var amount_ = decimals_.Map(decimals => BigInteger.Parse(bribe.Amount).DivideByDecimals(decimals));
            var maxPerVote_ = decimals_.Map(decimals => BigInteger.Parse(bribe.MaxPerVote).DivideByDecimals(decimals));
            var gauge_ = gaugeAddress.Bind(ga => gauges
               .Find(ga)
               .ToEither(Error.New($"Could not find pool name for gauge '{ga.Value}'"))
               .ToAsync());

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
                from amount in amount_
                from maxPerVote in maxPerVote_
                select new Db.Bribes.BribeV3
                {
                    Pool = gauge.ShortName,
                    Token = token,
                    Gauge = gauge.Address,
                    Amount = amount,
                    AmountDollars = amount * price,
                    MaxPerVote = maxPerVote,
                    Excluded = new List<string>()
                };
        });
}