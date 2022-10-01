using LanguageExt;
using Llama.Airforce.Database.Contexts;
using Llama.Airforce.Database.Models.Bribes;
using Llama.Airforce.Domain.Models;
using Llama.Airforce.Jobs.Extensions;
using Llama.Airforce.Jobs.Factories;
using Llama.Airforce.Jobs.Functions;
using Llama.Airforce.SeedWork.Types;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using static LanguageExt.Prelude;
using Db = Llama.Airforce.Database.Models;
using Snap = Llama.Airforce.Jobs.Snapshots.Models;

namespace Llama.Airforce.Jobs.Jobs;

public class Bribes
{
    public static Func<
            ILogger,
            BribesContext,
            IWeb3,
            BribesFactory.OptionsGetBribes,
            Option<DateTime>,
            Task<Lst<Db.Bribes.Epoch>>>
        UpdateBribes = fun((
            ILogger logger,
            BribesContext context,
            IWeb3 web3,
            BribesFactory.OptionsGetBribes options,
            Option<DateTime> customTime) =>
        {
            var getPrice = fun((Snap.Proposal proposal, Address tokenAddress, string token) =>
            {
                // Try to get the price at the end of the day of the deadline.
                // If the proposal ends later than now (ongoing proposal), we take current prices.
                var time = DateTimeExt.FromUnixTimeSeconds(proposal.End);
                time = time > DateTime.UtcNow ? DateTime.UtcNow : time;
                time = customTime.IfNone(time);

                var network = PriceFunctions.GetNetwork(token);
                var priceAtTime = PriceFunctions.GetPriceExt(
                    tokenAddress,
                    network,
                    Some(web3),
                    time,
                    token);

                var price = priceAtTime
                    // If the price fails for a given time, use spot price.
                    .BindLeft(ex => PriceFunctions.GetPrice(
                        tokenAddress,
                        network,
                        Some(web3)));

                return price;
            });

            return BribesFactory
                .GetBribes(logger, web3, options, getPrice)
                .MatchAsync(
                    RightAsync: async epochs =>
                    {
                        // Update pool.
                        foreach (var epoch in epochs)
                        {
                            await context.UpsertAsync(epoch);

                            var epochId = EpochId.Create(
                                StringMax.Of(options.Platform.ToPlatformString()),
                                StringMax.Of(options.Protocol.ToProtocolString()),
                                epoch.Round);

                            logger.LogInformation($"Updated bribes: {epochId}");
                        }

                        return epochs;
                    },
                    Left: ex =>
                    {
                        logger.LogError($"Failed to update bribes: {ex}");
                        return LanguageExt.List.empty<Db.Bribes.Epoch>();
                    });
        });
}