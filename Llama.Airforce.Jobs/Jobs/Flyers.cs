using LanguageExt;
using Llama.Airforce.Database.Contexts;
using Llama.Airforce.Jobs.Factories;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using static LanguageExt.Prelude;
using Db = Llama.Airforce.Database.Models;

namespace Llama.Airforce.Jobs.Jobs;

public class Flyers
{
    public static Func<
            ILogger,
            DashboardContext,
            IWeb3,
            Func<HttpClient>,
            Lst<Db.Convex.Pool>,
            Db.Bribes.Epoch,
            Task>
        UpdateFlyerConvex = fun((
            ILogger logger,
            DashboardContext context,
            IWeb3 web3,
            Func<HttpClient> httpFactory,
            Lst<Db.Convex.Pool> pools,
            Db.Bribes.Epoch latestFinishedEpoch) =>
        FlyerFactory
            .CreateFlyerConvex(web3, httpFactory, pools, latestFinishedEpoch)
            .MatchAsync(
                RightAsync: async f =>
                {
                    await context.UpsertAsync(f);
                    logger.LogInformation("Updated Convex flyer");

                    return Unit.Default;
                },
                Left: ex =>
                {
                    logger.LogError($"Failed to update Convex flyer: {ex}");
                    return Unit.Default;
                }));

    public static Func<
            ILogger,
            DashboardContext,
            IWeb3,
            Func<HttpClient>,
            Task>
        UpdateFlyerAura = fun((
            ILogger logger,
            DashboardContext context,
            IWeb3 web3,
            Func<HttpClient> httpFactory) =>
        FlyerFactory
            .CreateFlyerAura(web3, httpFactory)
            .MatchAsync(
                RightAsync: async f =>
                {
                    await context.UpsertAsync(f);
                    logger.LogInformation("Updated Aura flyer");

                    return Unit.Default;
                },
                Left: ex =>
                {
                    logger.LogError($"Failed to update Aura flyer: {ex}");
                    return Unit.Default;
                }));
}
