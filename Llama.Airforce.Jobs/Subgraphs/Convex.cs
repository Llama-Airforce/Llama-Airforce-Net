using LanguageExt;
using LanguageExt.Common;
using Llama.Airforce.Jobs.Subgraphs.Models;
using Llama.Airforce.SeedWork.Extensions;
using Newtonsoft.Json;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Subgraphs;

public class Convex
{
    /// <summary>
    /// Returns general Convex pool data from The Graph
    /// </summary>
    public static Func<
            Func<HttpClient>,
            string,
            EitherAsync<Error, Lst<Pool>>>
        GetPools = fun((
            Func<HttpClient> httpFactory,
            string graphUrl) =>
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

            return Subgraph.GetData(httpFactory, graphUrl, Query)
                .MapTry(JsonConvert.DeserializeObject<RequestPools>)
                .MapTry(data => toList(data.Data.PoolList));
        });

    /// <summary>
    /// Returns general snapshot data for a Convex pool from The Graph
    /// </summary>
    public static Func<
            Func<HttpClient>,
            string,
            string,
            EitherAsync<Error, Lst<Snapshot>>>
        GetDailySnapshots = fun((
            Func<HttpClient> httpFactory,
            string graphUrl,
            string pool) =>
        {
            var query = $@"{{
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

            return Subgraph.GetData(httpFactory, graphUrl, query)
                .MapTry(JsonConvert.DeserializeObject<RequestSnapshots>)
                .MapTry(data => toList(data.Data.SnapshotList));
        });
}