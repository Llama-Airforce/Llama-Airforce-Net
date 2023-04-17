using Llama.Airforce.Database.Contexts;
using Llama.Airforce.Domain.Models;
using Llama.Airforce.Jobs.Factories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Functions;

public class Bribes
{
    private readonly ILogger Logger;
    private readonly IConfiguration Config;
    private readonly IWeb3 Web3;
    private readonly BribesContext BribesContext;
    private readonly IHttpClientFactory HttpClientFactory;

    public Bribes(
        ILoggerFactory loggerFactory,
        IConfiguration config,
        IWeb3 web3,
        BribesContext bribesContext,
        IHttpClientFactory httpClientFactory)
    {
        Logger = loggerFactory.CreateLogger<Bribes>();
        Config = config;
        Web3 = web3;
        BribesContext = bribesContext;
        HttpClientFactory = httpClientFactory;
    }

    [Function("Bribes")]
    public async Task Run(
        [TimerTrigger("0 */15 * * * *", RunOnStartup = true)] TimerInfo bribeTimer)
    {
        var lastEpochOnly = Config.GetValue<bool>("LastEpochOnly");

        // Aura moved to a new gauge location and new proposalIndex offset starting at 1 million
        // This constant is so that when needed, we can rerun the code for the older version.
        const int AURA_VERSION = 2;

        await Jobs.Jobs.Bribes.UpdateBribes(
            Logger,
            BribesContext,
            HttpClientFactory.CreateClient,
            Web3,
            new BribesFactory.OptionsGetBribes(
                Platform.Votium,
                Protocol.ConvexCrv,
                lastEpochOnly,
                AURA_VERSION),
            None);

        await Jobs.Jobs.Bribes.UpdateBribes(
            Logger,
            BribesContext,
            HttpClientFactory.CreateClient,
            Web3,
            new BribesFactory.OptionsGetBribes(
                Platform.HiddenHand,
                Protocol.AuraBal,
                lastEpochOnly,
                AURA_VERSION),
            None);
    }
}