using Llama.Airforce.Functions;
using Llama.Airforce.Jobs.Extensions;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.Web3;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Llama.Airforce.Functions;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        var config = builder.GetContext().Configuration;

        builder.Services.AddHttpClient();
        builder.Services.AddContexts(config);

        var alchemy = config.GetValue<string>("ALCHEMY");
        var web3 = new Web3(alchemy);
        builder.Services.AddSingleton<IWeb3>(web3);
    }
}