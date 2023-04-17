using Llama.Airforce.Database.Contexts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Llama.Airforce.Functions;

public class ConvexPools
{
    private readonly ILogger Logger;
    private readonly IConfiguration Config;
    private readonly PoolContext PoolContext;
    private readonly PoolSnapshotsContext PoolSnapshotsContext;
    private readonly IHttpClientFactory HttpClientFactory;

    public ConvexPools(
        ILoggerFactory loggerFactory,
        IConfiguration config,
        PoolContext poolContext,
        PoolSnapshotsContext poolSnapshotsContext,
        IHttpClientFactory httpClientFactory)
    {
        Logger = loggerFactory.CreateLogger<ConvexPools>();
        Config = config;
        PoolContext = poolContext;
        PoolSnapshotsContext = poolSnapshotsContext;
        HttpClientFactory = httpClientFactory;
    }

    [Function("ConvexPools")]
    public async Task Run(
        [TimerTrigger("0 0 */12 * * *", RunOnStartup = false)] TimerInfo convexPoolsTimer)
    {
        var graphUrl = Config.GetValue<string>("GRAPH_CONVEX");

        var poolsConvex = await Jobs.Jobs.ConvexPools.UpdateConvexPools(
            Logger,
            HttpClientFactory.CreateClient,
            graphUrl,
            PoolContext);

        foreach (var pool in poolsConvex)
            await Jobs.Jobs.ConvexPools.UpdateConvexPoolSnapshots(
                Logger,
                HttpClientFactory.CreateClient,
                graphUrl,
                PoolSnapshotsContext,
                pool);
    }
}