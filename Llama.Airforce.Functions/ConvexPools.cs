using Llama.Airforce.Database.Contexts;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Llama.Airforce.Functions;

public class ConvexPools
{
    private readonly PoolContext PoolContext;
    private readonly PoolSnapshotsContext PoolSnapshotsContext;

    public ConvexPools(
        PoolContext poolContext,
        PoolSnapshotsContext poolSnapshotsContext)
    {
        PoolContext = poolContext;
        PoolSnapshotsContext = poolSnapshotsContext;
    }

    [FunctionName("ConvexPools")]
    public async Task Run(
        [TimerTrigger("0 0 */12 * * *", RunOnStartup = true)] TimerInfo convexPoolsTimer,
        ILogger log)
    {
        var poolsConvex = await Jobs.Jobs.ConvexPools.UpdateConvexPools(log, PoolContext);

        foreach (var pool in poolsConvex)
            await Jobs.Jobs.ConvexPools.UpdateConvexPoolSnapshots(log, PoolSnapshotsContext, pool);
    }
}