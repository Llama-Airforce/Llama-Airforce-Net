using LanguageExt;
using Llama.Airforce.Database.Contexts;
using Llama.Airforce.Database.Models.Bribes;
using Llama.Airforce.Domain.Models;
using Llama.Airforce.Jobs.Extensions;
using Llama.Airforce.Jobs.Factories;
using Llama.Airforce.Jobs.Functions;
using Llama.Airforce.SeedWork.Types;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;
using Db = Llama.Airforce.Database.Models;

namespace Llama.Airforce.Jobs.Jobs;

public class BribesV3
{
    public static Func<
            BribesV3Context,
            BribesV3Factory.OptionsGetBribes,
            Option<DateTime>,
            Task<Lst<Db.Bribes.EpochV3>>>
        UpdateBribes = fun((
            BribesV3Context context,
            BribesV3Factory.OptionsGetBribes options,
            Option<DateTime> customTime) =>
        {
            var getPrice = fun((long proposalEnd, Address tokenAddress, string token) =>
            {
                // Try to get the price at the end of the day of the deadline.
                // If the proposal ends later than now (ongoing proposal), we take current prices.
                var time = DateTimeExt.FromUnixTimeSeconds(proposalEnd);
                time = time > DateTime.UtcNow ? DateTime.UtcNow : time;
                time = customTime.IfNone(time);

                var network = PriceFunctions.GetNetwork(token);
                var priceAtTime = PriceFunctions.GetPriceExt(
                    options.HttpFactory,
                    tokenAddress,
                    network,
                    Some(options.Web3ETH),
                    time,
                    token);

                var price = priceAtTime
                    // If the price fails for a given time, use spot price.
                    .BindLeft(ex => PriceFunctions.GetPrice(
                        options.HttpFactory,
                        tokenAddress,
                        network,
                        Some(options.Web3ETH)));

                return price;
            });

            return BribesV3Factory
                .GetBribes(options, getPrice)
                .MatchAsync(
                    RightAsync: async epochs =>
                    {
                        // Update pool.
                        foreach (var epoch in epochs)
                        {
                            await context.UpsertAsync(epoch);

                            var epochId = EpochId.Create(
                                StringMax.Of(Platform.Votium.ToPlatformString()),
                                StringMax.Of(Protocol.ConvexCrv.ToProtocolString()),
                                epoch.Round);

                            options.Logger.LogInformation($"Updated bribes: {epochId}");
                        }

                        return epochs;
                    },
                    Left: ex =>
                    {
                        options.Logger.LogError($"Failed to update bribes: {ex}");
                        return LanguageExt.List.empty<Db.Bribes.EpochV3>();
                    });
        });
}