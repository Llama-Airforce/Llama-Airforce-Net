using LanguageExt;
using LanguageExt.Common;
using Llama.Airforce.Jobs.Subgraphs.Models;
using Llama.Airforce.SeedWork.Extensions;
using Newtonsoft.Json;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Subgraphs;

public class Convex
{
    public const string SUBGRAPH_URL_CONVEX = "https://api.thegraph.com/subgraphs/name/convex-community/curve-pools";

    /// <summary>
    /// Returns general Convex pool data from The Graph
    /// </summary>
    public static Func<EitherAsync<Error, Lst<Pool>>> GetPools = fun(() =>
    {
        const string Query = @"{
pools(
    where: { tvl_gt: 0, name_not: """" }
    first: 1000
    orderBy: creationDate
    orderDirection: desc
) {
    name
    tvl
    baseApr
    crvApr
    cvxApr
    extraRewardsApr
} }";

        return Subgraph.GetData(SUBGRAPH_URL_CONVEX, Query)
            .MapTry(JsonConvert.DeserializeObject<RequestPools>)
            .MapTry(data => toList(data.Data.PoolList));
    });

    /// <summary>
    /// Returns general snapshot data for a Convex pool from The Graph
    /// </summary>
    public static Func<string, EitherAsync<Error, Lst<Snapshot>>> GetDailySnapshots = fun((string pool) =>
    {
        string query = $@"{{
dailyPoolSnapshots(
    where: {{ poolName: ""{pool}"" }}
    first: 1000
    orderBy: timestamp
    orderDirection: desc
) {{
    timestamp
    tvl
    baseApr
    crvApr
    cvxApr
    extraRewardsApr
}} }}";

        return Subgraph.GetData(SUBGRAPH_URL_CONVEX, query)
            .MapTry(JsonConvert.DeserializeObject<RequestSnapshots>)
            .MapTry(data => toList(data.Data.SnapshotList));
    });
}