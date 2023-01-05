using System.Net.Http;
using System.Threading.Tasks;
using Llama.Airforce.Database.Contexts;
using Llama.Airforce.Domain.Models;
using Llama.Airforce.Jobs.Factories;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Functions;

public class Bribes
{
    private readonly IConfiguration Config;
    private readonly IWeb3 Web3;
    private readonly BribesContext BribesContext;
    private readonly IHttpClientFactory HttpClientFactory;

    public Bribes(
        IConfiguration config,
        IWeb3 web3,
        BribesContext bribesContext,
        IHttpClientFactory httpClientFactory)
    {
        Config = config;
        Web3 = web3;
        BribesContext = bribesContext;
        HttpClientFactory = httpClientFactory;
    }

    [FunctionName("Bribes")]
    public async Task Run(
        [TimerTrigger("0 */15 * * * *", RunOnStartup = false)] TimerInfo bribeTimer,
        ILogger log)
    {
        var lastEpochOnly = Config.GetValue<bool>("LastEpochOnly");

        await Jobs.Jobs.Bribes.UpdateBribes(
            log,
            BribesContext,
            HttpClientFactory.CreateClient,
            Web3,
            new BribesFactory.OptionsGetBribes(
                Platform.Votium,
                Protocol.ConvexCrv,
                lastEpochOnly),
            None);

        await Jobs.Jobs.Bribes.UpdateBribes(
            log,
            BribesContext,
            HttpClientFactory.CreateClient,
            Web3,
            new BribesFactory.OptionsGetBribes(
                Platform.HiddenHand,
                Protocol.AuraBal,
                lastEpochOnly),
            None);
    }
}