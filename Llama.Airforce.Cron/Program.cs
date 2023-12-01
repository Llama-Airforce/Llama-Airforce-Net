using Llama.Airforce.Database.Contexts;
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
var bribesV2Context = serviceProvider.GetService<BribesV2Context>();

logger.LogInformation("Cronjobs starting...");

// Update Prisma bribes.
await Llama.Airforce.Jobs.Jobs.BribesV2.UpdateBribes(
    logger,
    bribesV2Context,
    httpFactory.CreateClient,
    web3ETH,
    new BribesV2Factory.OptionsGetBribes(Protocol.ConvexPrisma, true),
    None);

// Update Convex bribes.
await Llama.Airforce.Jobs.Jobs.BribesV2.UpdateBribes(
    logger,
    bribesV2Context,
    httpFactory.CreateClient,
    web3ETH,
    new BribesV2Factory.OptionsGetBribes(Protocol.ConvexCrv, true),
    None);

logger.LogInformation("Cronjobs done");