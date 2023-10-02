using LanguageExt;
using Llama.Airforce.Database.Contexts;
using Llama.Airforce.Domain.Models;
using Llama.Airforce.Jobs.Extensions;
using Llama.Airforce.Jobs.Factories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Functions;

public class Dashboards
{
    private readonly ILogger Logger;
    private readonly IWeb3 Web3;
    private readonly BribesContext BribesContext;
    private readonly BribesV2Context BribesV2Context;
    private readonly DashboardContext DashboardContext;
    private readonly IHttpClientFactory HttpClientFactory;

    public Dashboards(
        ILoggerFactory loggerFactory,
        IWeb3 web3,
        BribesContext bribesContext,
        BribesV2Context bribesV2Context,
        DashboardContext dashboardContext,
        IHttpClientFactory httpClientFactory)
    {
        Logger = loggerFactory.CreateLogger<Dashboards>();
        Web3 = web3;
        BribesContext = bribesContext;
        BribesV2Context = bribesV2Context;
        DashboardContext = dashboardContext;
        HttpClientFactory = httpClientFactory;
    }

    [Function("Dashboards")]
    public async Task Run(
        [TimerTrigger("0 */15 * * * *", RunOnStartup = false)] TimerInfo dashboardsTimer)
    {
        // Get Votium data.
        var epochsVotiumV1 = await BribesContext
            .GetAllAsync(
                Platform.Votium.ToPlatformString(),
                Protocol.ConvexCrv.ToProtocolString())
            .Map(toList);

        var epochsVotiumV2 = await BribesV2Context
           .GetAllAsync(
                Platform.Votium.ToPlatformString(),
                Protocol.ConvexCrv.ToProtocolString())
           .Map(toList);

        var latestFinishedEpochVotium = epochsVotiumV2
            .OrderBy(epoch => epoch.End)
            .Last(epoch => epoch.End <= DateTime.UtcNow.ToUnixTimeSeconds());

        var votiumDataV1 = new DashboardFactory.VotiumDataV1(
            epochsVotiumV1);

        var votiumDataV2 = new DashboardFactory.VotiumDataV2(
            epochsVotiumV2,
            latestFinishedEpochVotium);

        // Get Aura data.
        var epochsAura = await BribesContext
            .GetAllAsync(
                Platform.HiddenHand.ToPlatformString(),
                Protocol.AuraBal.ToProtocolString())
            .Map(toList);

        var latestFinishedEpochAura = epochsAura
            .OrderBy(epoch => epoch.End)
            .Last(epoch => epoch.End <= DateTime.UtcNow.ToUnixTimeSeconds());

        var auraData = new DashboardFactory.AuraData(
            epochsAura,
            latestFinishedEpochAura);

        await Jobs.Jobs.Dashboards.UpdateDashboards(
            Logger,
            Web3,
            HttpClientFactory.CreateClient,
            DashboardContext,
            votiumDataV1,
            votiumDataV2,
            auraData);
    }
}