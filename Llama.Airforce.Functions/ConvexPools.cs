using Llama.Airforce.Database.Contexts;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Llama.Airforce.Functions;

public class ConvexPools
{
    private readonly IConfiguration Config;
    private readonly PoolContext PoolContext;
    private readonly PoolSnapshotsContext PoolSnapshotsContext;

    public ConvexPools(
        IConfiguration config,
        PoolContext poolContext,
        PoolSnapshotsContext poolSnapshotsContext)
    {
        Config = config;
        PoolContext = poolContext;
        PoolSnapshotsContext = poolSnapshotsContext;
    }

    [FunctionName("ConvexPools")]
    public async Task Run(
        [TimerTrigger("0 0 */12 * * *", RunOnStartup = false)] TimerInfo convexPoolsTimer,
        ILogger log)
    {
        var graphUrl = Config.GetValue<string>("GRAPH_CONVEX");

        var poolsConvex = await Jobs.Jobs.ConvexPools.UpdateConvexPools(
            log,
            graphUrl,
            PoolContext);

        foreach (var pool in poolsConvex)
            await Jobs.Jobs.ConvexPools.UpdateConvexPoolSnapshots(
                log,
                graphUrl,
                PoolSnapshotsContext,
                pool);
    }
}