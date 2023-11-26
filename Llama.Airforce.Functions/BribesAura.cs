using Llama.Airforce.Database.Contexts;
using Llama.Airforce.Domain.Models;
using Llama.Airforce.Jobs.Factories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Functions;

public class BribesAura
{
    private readonly ILogger Logger;
    private readonly IConfiguration Config;

    private readonly IWeb3 Web3ETH;

    private readonly BribesContext BribesContext;
    private readonly IHttpClientFactory HttpClientFactory;

    public BribesAura(
        ILoggerFactory loggerFactory,
        IConfiguration config,
        IEnumerable<IWeb3> web3,
        BribesContext bribesContext,
        IHttpClientFactory httpClientFactory)
    {
        Logger = loggerFactory.CreateLogger<BribesAura>();
        Config = config;

        var webs = web3.ToArray();
        Web3ETH = webs[1];

        BribesContext = bribesContext;
        HttpClientFactory = httpClientFactory;
    }

    [Function("BribesAura")]
    public async Task Run(
        [TimerTrigger("0 */15 * * * *", RunOnStartup = false)] TimerInfo bribeTimer)
    {
        var lastEpochOnly = Config.GetValue<bool>("LastEpochOnly");

        // Aura moved to a new gauge location and new proposalIndex offset starting at 1 million
        // This constant is so that when needed, we can rerun the code for the older version.
        const int AURA_VERSION = 2;

        await Jobs.Jobs.Bribes.UpdateBribes(
            Logger,
            BribesContext,
            HttpClientFactory.CreateClient,
            Web3ETH,
            new BribesFactory.OptionsGetBribes(
                Platform.HiddenHand,
                Protocol.AuraBal,
                lastEpochOnly,
                AURA_VERSION),
            None);
    }
}