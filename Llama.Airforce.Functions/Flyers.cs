using System;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Llama.Airforce.Database.Contexts;
using Llama.Airforce.Domain.Models;
using Llama.Airforce.Jobs.Extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Functions;

public class Flyers
{
    private readonly IWeb3 Web3;
    private readonly PoolContext ConvexPoolContext;
    private readonly BribesContext BribesContext;
    private readonly DashboardContext DashboardContext;

    public Flyers(
        IWeb3 web3,
        PoolContext convexPoolContext,
        BribesContext bribesContext,
        DashboardContext dashboardContext)
    {
        Web3 = web3;
        ConvexPoolContext = convexPoolContext;
        BribesContext = bribesContext;
        DashboardContext = dashboardContext;
    }

    [FunctionName("Flyers")]
    public async Task Run(
        [TimerTrigger("0 */15 * * * *", RunOnStartup = false)] TimerInfo flyerTimer,
        ILogger log)
    {
        var poolsConvex = await ConvexPoolContext
            .GetAllAsync()
            .Map(toList);

        var epochs = await BribesContext.GetAllAsync(
            Platform.Votium.ToPlatformString(),
            Protocol.ConvexCrv.ToProtocolString());

        var latestFinishedEpoch = epochs
            .Where(epoch => epoch.Platform == Platform.Votium.ToPlatformString())
            .OrderBy(epoch => epoch.End)
            .Last(epoch => epoch.End <= DateTime.UtcNow.ToUnixTimeSeconds());

        await Jobs.Jobs.Flyers.UpdateFlyerConvex(
            log,
            DashboardContext,
            Web3,
            poolsConvex,
            latestFinishedEpoch);

        await Jobs.Jobs.Flyers.UpdateFlyerAura(
            log,
            DashboardContext,
            Web3);
    }
}