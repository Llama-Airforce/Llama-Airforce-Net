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

    private readonly IWeb3 Web3ETH;
    private readonly IWeb3 Web3ZKEVM;

    private readonly BribesContext BribesContext;
    private readonly BribesV2Context BribesV2Context;
    private readonly BribesV3Context BribesV3Context;
    private readonly IHttpClientFactory HttpClientFactory;

    public Bribes(
        ILoggerFactory loggerFactory,
        IConfiguration config,
        IEnumerable<IWeb3> web3,
        BribesContext bribesContext,
        BribesV2Context bribesV2Context,
        BribesV3Context bribesV3Context,
        IHttpClientFactory httpClientFactory)
    {
        Logger = loggerFactory.CreateLogger<Bribes>();
        Config = config;

        var webs = web3.ToArray();
        Web3ZKEVM = webs[0];
        Web3ETH = webs[1];

        BribesContext = bribesContext;
        BribesV2Context = bribesV2Context;
        BribesV3Context = bribesV3Context;
        HttpClientFactory = httpClientFactory;
    }

    [Function("Bribes")]
    public async Task Run(
        [TimerTrigger("0 */15 * * * *", RunOnStartup = false)] TimerInfo bribeTimer)
    {
        var lastEpochOnly = Config.GetValue<bool>("LastEpochOnly");

        // Aura moved to a new gauge location and new proposalIndex offset starting at 1 million
        // This constant is so that when needed, we can rerun the code for the older version.
        const int AURA_VERSION = 2;

        const int VOTIUM_VERSION = 2;

        if (VOTIUM_VERSION == 2)
        {
            await Jobs.Jobs.BribesV2.UpdateBribes(
                Logger,
                BribesV2Context,
                HttpClientFactory.CreateClient,
                Web3ETH,
                new BribesV2Factory.OptionsGetBribes(lastEpochOnly),
                None);

            await Jobs.Jobs.BribesV3.UpdateBribes(
                BribesV3Context,
                new BribesV3Factory.OptionsGetBribes(
                    Logger,
                    Web3ETH,
                    Web3ZKEVM,
                    HttpClientFactory.CreateClient,
                    lastEpochOnly),
                None);
        }
        else
            await Jobs.Jobs.Bribes.UpdateBribes(
                Logger,
                BribesContext,
                HttpClientFactory.CreateClient,
                Web3ETH,
                new BribesFactory.OptionsGetBribes(
                    Platform.Votium,
                    Protocol.ConvexCrv,
                    lastEpochOnly,
                    AURA_VERSION),
                None);

        const bool AURA_HOLIDAY = true;

        if (!AURA_HOLIDAY)
        {
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
}