using LanguageExt;
using Llama.Airforce.Database.Contexts;
using Llama.Airforce.Jobs.Subgraphs.Models;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;
using Convex = Llama.Airforce.Jobs.Subgraphs.Convex;
using Db = Llama.Airforce.Database.Models;

namespace Llama.Airforce.Jobs.Jobs;

public class ConvexPools
{
    public static Func<
        ILogger,
        string,
        PoolContext,
        Task<Lst<Pool>>>
    UpdateConvexPools = fun((
        ILogger logger,
        string graphUrl,
        PoolContext poolContext) =>
    Convex
        .GetPools(graphUrl)
        .MatchAsync(
            RightAsync: async pools =>
            {
                // Update pool.
                foreach (var pool in pools)
                {
                    var poolDb = new Db.Convex.Pool
                    {
                        Name = pool.Name.Replace("/", "-"), // Replace / with - for CosmosDB.
                        Tvl = pool.Tvl,
                        BaseApr = pool.BaseApr,
                        CrvApr = pool.CrvApr,
                        CvxApr = pool.CvxApr,
                        ExtraRewardsApr = pool.ExtraRewardsApr,
                    };

                    await poolContext.UpsertAsync(poolDb);
                    logger.LogInformation($"Updated Convex Pool: {pool.Name}");
                }

                return pools;
            },
            Left: ex =>
            {
                logger.LogError($"Failed to update Convex pools: {ex}");
                return LanguageExt.List.empty<Pool>();
            }));

    public static Func<
            ILogger,
            string,
            PoolSnapshotsContext,
            Pool,
            Task>
        UpdateConvexPoolSnapshots = fun((
            ILogger logger,
            string graphUrl,
            PoolSnapshotsContext context,
            Pool pool) =>
        Convex
            .GetDailySnapshots(graphUrl, pool.Name)
            .MatchAsync(
                RightAsync: async snapshots =>
                {
                    var poolSnapshotsDb = new Db.Convex.PoolSnapshots
                    {
                        Name = pool.Name.Replace("/", "-"), // Replace / with - for CosmosDB.,
                        Snapshots = snapshots
                            .Map(s => new Db.Convex.PoolSnapshotData()
                            {
                                TimeStamp = s.TimeStamp,
                                Tvl = s.Tvl,
                                BaseApr = s.BaseApr,
                                CrvApr = s.CrvApr,
                                CvxApr = s.CvxApr,
                                ExtraRewardsApr = s.ExtraRewardsApr,
                            })
                            .ToList()
                    };

                    await context.UpsertAsync(poolSnapshotsDb);
                    logger.LogInformation($"Updated pool snapshots: {pool.Name}");

                    return Unit.Default;
                },
                Left: ex =>
                {
                    logger.LogError($"Failed to update pools snapshots: {ex}");
                    return Unit.Default;
                }));
}