using Llama.Airforce.Database.Contexts;
using Llama.Airforce.Jobs.Factories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Functions;

public class BribesCurveV3
{
    private readonly ILogger Logger;
    private readonly IConfiguration Config;

    private readonly IWeb3 Web3ETH;
    private readonly IWeb3 Web3ZKEVM;

    private readonly BribesV3Context BribesV3Context;
    private readonly IHttpClientFactory HttpClientFactory;

    public BribesCurveV3(
        ILoggerFactory loggerFactory,
        IConfiguration config,
        IEnumerable<IWeb3> web3,
        BribesV3Context bribesV3Context,
        IHttpClientFactory httpClientFactory)
    {
        Logger = loggerFactory.CreateLogger<BribesCurveV3>();
        Config = config;

        var webs = web3.ToArray();
        Web3ZKEVM = webs[0];
        Web3ETH = webs[1];

        BribesV3Context = bribesV3Context;
        HttpClientFactory = httpClientFactory;
    }

    [Function("BribesCurveV3")]
    public async Task Run(
        [TimerTrigger("0 */15 * * * *", RunOnStartup = false)] TimerInfo bribeTimer)
    {
        var lastEpochOnly = Config.GetValue<bool>("LastEpochOnly");

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
}