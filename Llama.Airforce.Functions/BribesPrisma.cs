using Llama.Airforce.Database.Contexts;
using Llama.Airforce.Domain.Models;
using Llama.Airforce.Jobs.Factories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Functions;

public class BribesPrisma
{
    private readonly ILogger Logger;
    private readonly IConfiguration Config;

    private readonly IWeb3 Web3ETH;

    private readonly BribesV2Context BribesV2Context;
    private readonly IHttpClientFactory HttpClientFactory;

    public BribesPrisma(
        ILoggerFactory loggerFactory,
        IConfiguration config,
        IEnumerable<IWeb3> web3,
        BribesV2Context bribesV2Context,
        IHttpClientFactory httpClientFactory)
    {
        Logger = loggerFactory.CreateLogger<BribesPrisma>();
        Config = config;

        var webs = web3.ToArray();
        Web3ETH = webs[1];

        BribesV2Context = bribesV2Context;
        HttpClientFactory = httpClientFactory;
    }

    [Function("BribesPrisma")]
    public async Task Run(
        [TimerTrigger("0 */15 * * * *", RunOnStartup = false)] TimerInfo bribeTimer)
    {
        var lastEpochOnly = Config.GetValue<bool>("LastEpochOnly");

        await Jobs.Jobs.BribesV2.UpdateBribes(
            Logger,
            BribesV2Context,
            HttpClientFactory.CreateClient,
            Web3ETH,
            new BribesV2Factory.OptionsGetBribes(Protocol.ConvexPrisma, lastEpochOnly),
            None);
    }
}