using LanguageExt;
using Llama.Airforce.Database.Contexts;
using Llama.Airforce.Database.Models.Bribes;
using Llama.Airforce.Domain.Models;
using Llama.Airforce.Jobs.Extensions;
using Llama.Airforce.Jobs.Factories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using static LanguageExt.Prelude;
using EpochV2 = Llama.Airforce.Database.Models.Bribes.EpochV2;

// Build configuration
var configuration = new ConfigurationBuilder()
   .AddJsonFile("appsettings.json")
   .AddUserSecrets<Program>()
   .AddEnvironmentVariables()
   .Build();

// Set up dependency injection
var alchemy = configuration["ALCHEMY"];
var web3ETH = new Web3(alchemy);

var serviceProvider = new ServiceCollection()
   .AddLogging(configure => configure.AddConsole())
   .AddHttpClient()
    // Remove annoying HTTP logging which ignores host.json.
   .RemoveAll<IHttpMessageHandlerBuilderFilter>()
   .AddContexts(configuration)
   .AddSingleton<IWeb3>(web3ETH)
   .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Information)
   .BuildServiceProvider();

var logger = serviceProvider.GetService<ILogger<Program>>();
var httpFactory = serviceProvider.GetService<IHttpClientFactory>();

var bribesContext = serviceProvider.GetService<BribesContext>();
var bribesV2Context = serviceProvider.GetService<BribesV2Context>();
var dashboardContext = serviceProvider.GetService<DashboardContext>();
var convexPoolContext = serviceProvider.GetService<PoolContext>();

logger.LogInformation("Cronjobs starting...");

var graphApiKey = configuration["GRAPH_API_KEY"];
var liveEpochCheck = configuration.GetValue<bool>("LIVE_EPOCH_CHECK");
if (!liveEpochCheck)
{
    logger.LogInformation("Skipping live epoch check");
}

// Check if there's an active Convex voting round, otherwise skip to save on API costs.
var epochStart = await Llama.Airforce.Jobs.Contracts.Votium.GetCurrentEpoch(web3ETH);
var epochEnd = epochStart.AddDays(5).AddHours(4);

if (DateTime.UtcNow > epochEnd && liveEpochCheck)
{
    logger.LogInformation($"Skipping cronjob, epoch ending date {epochEnd} exceeds current time {DateTime.UtcNow}");
}
else
{
    // Update f(x) Protocol bribes.
    await Llama.Airforce.Jobs.Jobs.BribesV2.UpdateBribes(
        logger,
        bribesV2Context,
        httpFactory.CreateClient,
        web3ETH,
        new BribesV2Factory.OptionsGetBribes(Protocol.ConvexFxn, true, graphApiKey),
        None);

    // Update Prisma bribes.
    await Llama.Airforce.Jobs.Jobs.BribesV2.UpdateBribes(
        logger,
        bribesV2Context,
        httpFactory.CreateClient,
        web3ETH,
        new BribesV2Factory.OptionsGetBribes(Protocol.ConvexPrisma, true, graphApiKey),
        None);

    // Update Convex bribes.
    await Llama.Airforce.Jobs.Jobs.BribesV2.UpdateBribes(
        logger,
        bribesV2Context,
        httpFactory.CreateClient,
        web3ETH,
        new BribesV2Factory.OptionsGetBribes(Protocol.ConvexCrv, true, graphApiKey),
        None);
}

// Get Votium data.
var epochsVotiumV1 = await bribesContext
    .GetAllAsync(
        Platform.Votium.ToPlatformString(),
        Protocol.ConvexCrv.ToProtocolString())
    .Map(toList);

var epochsVotiumV2 = await bribesV2Context
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

// Get Prisma data.
var epochsPrisma = await bribesV2Context
   .GetAllAsync(
        Platform.Votium.ToPlatformString(),
        Protocol.ConvexPrisma.ToProtocolString())
   .Map(toList);

var latestFinishedEpochPrisma = epochsPrisma
   .OrderBy(epoch => epoch.End)
   .Last(epoch => epoch.End <= DateTime.UtcNow.ToUnixTimeSeconds());

var prismaData = new DashboardFactory.PrismaData(
    epochsPrisma,
    latestFinishedEpochPrisma);

// Get f(x) Protocol data.
var epochsFxn = await bribesV2Context
   .GetAllAsync(
        Platform.Votium.ToPlatformString(),
        Protocol.ConvexFxn.ToProtocolString())
   .Map(toList);

var latestFinishedEpochFxn = epochsFxn
   .OrderBy(epoch => epoch.End)
   .Last(epoch => epoch.End <= DateTime.UtcNow.ToUnixTimeSeconds());

var fxnData = new DashboardFactory.FxnData(
    epochsFxn,
    latestFinishedEpochFxn);

// Get Aura data.
var epochsAura = await bribesContext
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

var data = new DashboardFactory.Data(
    votiumDataV1,
    votiumDataV2,
    prismaData,
    fxnData,
    auraData);

await Llama.Airforce.Jobs.Jobs.Dashboards.UpdateDashboards(
    logger,
    web3ETH,
    httpFactory.CreateClient,
    dashboardContext,
    data);

// Update flyers
var poolsConvex = await convexPoolContext
   .GetAllAsync()
   .Map(toList);

await Llama.Airforce.Jobs.Jobs.Flyers.UpdateFlyerConvex(
    logger,
    dashboardContext,
    web3ETH,
    httpFactory.CreateClient,
    poolsConvex,
    List(latestFinishedEpochVotium, latestFinishedEpochPrisma));

await Llama.Airforce.Jobs.Jobs.Flyers.UpdateFlyerAura(
    logger,
    dashboardContext,
    web3ETH,
    httpFactory.CreateClient);

logger.LogInformation("Cronjobs done");