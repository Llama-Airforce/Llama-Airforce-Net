using LanguageExt;
using Llama.Airforce.Database.Contexts;
using Llama.Airforce.Domain.Models;
using Llama.Airforce.Jobs.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Functions;

public class Flyers
{
    private readonly ILogger Logger;
    private readonly IWeb3 Web3;
    private readonly PoolContext ConvexPoolContext;
    private readonly BribesV2Context BribesV2Context;
    private readonly DashboardContext DashboardContext;
    private readonly IHttpClientFactory HttpClientFactory;

    public Flyers(
        ILoggerFactory loggerFactory,
        IWeb3 web3,
        PoolContext convexPoolContext,
        BribesV2Context bribesV2Context,
        DashboardContext dashboardContext,
        IHttpClientFactory httpClientFactory)
    {
        Logger = loggerFactory.CreateLogger<Flyers>();
        Web3 = web3;
        ConvexPoolContext = convexPoolContext;
        BribesV2Context = bribesV2Context;
        DashboardContext = dashboardContext;
        HttpClientFactory = httpClientFactory;
    }

    [Function("Flyers")]
    public async Task Run(
        [TimerTrigger("0 */15 * * * *", RunOnStartup = false)] TimerInfo flyerTimer)
    {
        var poolsConvex = await ConvexPoolContext
            .GetAllAsync()
            .Map(toList);

        var epochsVotium = await BribesV2Context.GetAllAsync(
            Platform.Votium.ToPlatformString(),
            Protocol.ConvexCrv.ToProtocolString());

        var latestFinishedEpochVotium = epochsVotium
            .OrderBy(epoch => epoch.End)
            .Last(epoch => epoch.End <= DateTime.UtcNow.ToUnixTimeSeconds());

        var epochsPrisma = await BribesV2Context
           .GetAllAsync(
                Platform.Votium.ToPlatformString(),
                Protocol.ConvexPrisma.ToProtocolString());

        var latestFinishedEpochPrisma = epochsPrisma
           .OrderBy(epoch => epoch.End)
           .Last(epoch => epoch.End <= DateTime.UtcNow.ToUnixTimeSeconds());

        await Jobs.Jobs.Flyers.UpdateFlyerConvex(
            Logger,
            DashboardContext,
            Web3,
            HttpClientFactory.CreateClient,
            poolsConvex,
            List(latestFinishedEpochVotium, latestFinishedEpochPrisma));

        await Jobs.Jobs.Flyers.UpdateFlyerAura(
            Logger,
            DashboardContext,
            Web3,
            HttpClientFactory.CreateClient);
    }
}