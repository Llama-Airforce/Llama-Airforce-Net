using LanguageExt;
using Llama.Airforce.Database.Contexts;
using Llama.Airforce.Jobs.Subgraphs.Models;
using Llama.Airforce.SeedWork.Extensions;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;
using Db = Llama.Airforce.Database.Models;

namespace Llama.Airforce.Jobs.Jobs;

public class CurvePools
{
    public static Func<
        ILogger,
        Func<HttpClient>,
        CurvePoolContext,
        Task<Lst<CurvePool>>>
    UpdateCurvePools = fun((
        ILogger logger,
        Func<HttpClient> httpFactory,
        CurvePoolContext poolContext) =>
    Subgraphs.Curve
        .GetPools(httpFactory)
        .MatchAsync(
            RightAsync: async pools =>
            {
                // Update pool.
                foreach (var pool in pools)
                {
                    var poolDb = new Db.Curve.Pool
                    {
                        Name = pool.Name.Replace("/", "-"), // Replace / with - for CosmosDB.
                        Tvl = pool.Tvl,
                        Swap = pool.Swap,
                        LpToken = pool.LpToken,
                        IsV2 = pool.IsV2,
                        AssetType = pool.AssetType,
                        CoinList = pool.CoinList
                    };

                    await poolContext.UpsertAsync(poolDb);
                    logger.LogInformation($"Updated Curve Pool: {pool.Name}");
                }

                return pools;
            },
            Left: ex =>
            {
                logger.LogError($"Failed to update Curve pools: {ex}");
                return LanguageExt.List.empty<CurvePool>();
            }));

    public static Func<
            ILogger,
            Func<HttpClient>,
            string,
            CurvePoolSnapshotsContext,
            CurvePool,
            Task<Option<Db.Curve.CurvePoolSnapshots>>>
        UpdateCurvePoolSnapshots = fun((
            ILogger logger,
            Func<HttpClient> httpFactory,
            string alchemyEndpoint,
            CurvePoolSnapshotsContext context,
            CurvePool pool) =>
        Subgraphs.Curve
            .GetPoolSnapshots(httpFactory, pool.Name)
            .MatchAsync(
                async snapshot =>
                {
                    // We're not tracking fees until roughly a week before gauge is assigned.
                    var firstBlock = snapshot
                        .FeeSnapshotList
                        .Min(x => x.Block) - 6200 * 7;

                    var transfers_ = Alchemy.GetTransfers(httpFactory, alchemyEndpoint, pool);
                    var feeSnapshots_ = pool.IsV2
                        ? snapshot.FeeSnapshotList
                            .Map(s => new Db.Curve.FeeSnapshot
                            {
                                TimeStamp = s.TimeStamp,
                                Value = s.Fees
                            })
                            .toList()
                            .ToEitherAsync()
                        : transfers_.Map(transfers => transfers
                            .Where(transfer => transfer.Block >= firstBlock)
                            .Map(transfer => Alchemy.GetFeesFromTransfer(
                                snapshot.FeeSnapshotList,
                                transfer))
                            .Somes()
                            .Map(fees => new Db.Curve.FeeSnapshot
                            {
                                Value = fees.Value * 2, // Admin + LP Fees
                                TimeStamp = fees.TimeStamp
                            })
                            .toList());

                    var poolSnapshotsDb_ =
                        from feeSnapshots in feeSnapshots_
                        select new Db.Curve.CurvePoolSnapshots
                        {
                            Name = pool.Name.Replace("/", "-"), // Replace / with - for CosmosDB.,
                            FeeSnapshots = feeSnapshots.ToList(),
                            EmissionSnapshots = snapshot.EmissionSnapshotList
                                .Map(s => new Db.Curve.EmissionSnapshot
                                {
                                    TimeStamp = s.TimeStamp,
                                    Value = s.Value,
                                    CrvAmount = s.CrvAmount
                                })
                                .ToList()
                        };

                    return await poolSnapshotsDb_.MatchAsync(
                        RightAsync: async poolSnapshotsDb =>
                        {
                            await context.UpsertAsync(poolSnapshotsDb);
                            logger.LogInformation($"Updated Curve pool snapshots: {pool.Name}");
                            return Some(poolSnapshotsDb);
                        },
                        Left: ex =>
                        {
                            logger.LogError($"Failed to generate Curve pools snapshots: {ex}");
                            return None;
                        });
                },
                Left: ex =>
                {
                    logger.LogError($"Failed to update Curve pools snapshots: {ex}");
                    return None;
                }));
}