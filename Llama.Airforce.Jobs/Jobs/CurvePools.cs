using LanguageExt;
using Llama.Airforce.Database.Contexts;
using Llama.Airforce.Jobs.Contracts;
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
        CurvePoolContext,
        Task<Lst<CurvePool>>>
    UpdateCurvePools = fun((
        ILogger logger,
        CurvePoolContext poolContext) =>
    Subgraphs.Curve
        .GetPools()
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
            string,
            CurvePoolSnapshotsContext,
            CurvePool,
            Task<Option<Db.Curve.CurvePoolSnapshots>>>
        UpdateCurvePoolSnapshots = fun((
            ILogger logger,
            string alchemyEndpoint,
            CurvePoolSnapshotsContext context,
            CurvePool pool) =>
        Subgraphs.Curve
            .GetPoolSnapshots(pool.Name)
            .MatchAsync(
                async snapshot =>
                {
                    // We're not tracking fees until roughly a week before gauge is assigned.
                    var firstBlock = snapshot
                        .FeeSnapshotList
                        .Min(x => x.Block) - 6200 * 7;

                    var transfers_ = Alchemy.GetTransfers(alchemyEndpoint, pool);
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

    public static Func<
            ILogger,
            CurvePoolRatiosContext,
            Db.Curve.CurvePoolSnapshots,
            Task>
        UpdateCurvePoolRatios = fun(async (
            ILogger logger,
            CurvePoolRatiosContext context,
            Db.Curve.CurvePoolSnapshots snapshot) =>
        {
            // we only store ratios for the last 6 months
            var ratioCutOffDate = DateTimeOffset.Now.AddMonths(-6).ToUnixTimeSeconds();
            var aggregatedFees = snapshot
                .FeeSnapshots
                .GroupBy(x => x.TimeStamp)
                .Select(x => new Curve.Fees(
                    x.Key,
                    x.Sum(xa => xa.Value)))
                .Where(x => x.TimeStamp > ratioCutOffDate)
                .ToList();

            var aggregatedEmissions = snapshot
                .EmissionSnapshots
                .GroupBy(x => x.TimeStamp)
                .Select(x => new Curve.Emissions(
                    x.Key,
                    x.Sum(xa => xa.Value),
                    x.Sum(xa => xa.CrvAmount)))
                .Where(x => x.TimeStamp > ratioCutOffDate)
                .ToList();

            var ratios = aggregatedEmissions.Map(emissions =>
            {
                var fees = Optional(aggregatedFees.Find(fees => fees.TimeStamp == emissions.TimeStamp));
                var ratio = emissions.Value == 0
                    ? 0
                    : fees.Map(f => f.Value / emissions.Value).IfNone(0);

                return new Db.Curve.PoolRatio
                {
                    TimeStamp = emissions.TimeStamp,
                    Ratio = ratio
                };
            });

            var ratioDb = new Db.Curve.CurvePoolRatios
            {
                Name = snapshot.Name.Replace("/", "-"), // Replace / with - for CosmosDB.
                Ratios = ratios.ToList()
            };

            await context.UpsertAsync(ratioDb);
            logger.LogInformation($"Updated Curve pool ratios: {snapshot.Name}");

            return Unit.Default;
        });
}