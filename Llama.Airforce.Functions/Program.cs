using Llama.Airforce.Jobs.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Nethereum.Web3;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddUserSecrets<Program>(optional: true, reloadOnChange: false);
    })
    .ConfigureServices(s =>
    {
        var config = s.BuildServiceProvider().GetService<IConfiguration>();

        s.AddHttpClient();
        s.AddContexts(config);

        var alchemy = config.GetValue<string>("ALCHEMY");
        var web3 = new Web3(alchemy);
        s.AddSingleton<IWeb3>(web3);

        // Remove annoying HTTP logging which ignores host.json.
        s.RemoveAll<IHttpMessageHandlerBuilderFilter>();
    })
    .Build();

await host.RunAsync();