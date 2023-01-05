using LanguageExt;
using LanguageExt.Common;
using Llama.Airforce.Jobs.Subgraphs.Models;
using Llama.Airforce.SeedWork.Extensions;
using Newtonsoft.Json;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Subgraphs;

public class Curve
{
    public const string SUBGRAPH_URL_CURVE = "https://api.thegraph.com/subgraphs/name/convex-community/crv-emissions";

    /// <summary>
    /// Returns general Curve pool data from The Graph
    /// </summary>
    public static Func<
            Func<HttpClient>,
            EitherAsync<Error, Lst<CurvePool>>>
        GetPools = fun((
            Func<HttpClient> httpFactory) =>
    {
        const string Query = @"
        {
            pools(
                where: { tvl_gt: 0, name_not: """" }
                orderBy: tvl
                orderDirection: desc
                first: 1000) 
            {
                name
                isV2
                swap
                coins
                lpToken
                assetType
                tvl
            }
        }";

        return Subgraph.GetData(httpFactory, SUBGRAPH_URL_CURVE, Query)
            .MapTry(JsonConvert.DeserializeObject<RequestCurvePools>)
            .MapTry(data => toList(data.Data.CurvePoolList));
    });


    /// <summary>
    /// Returns fees and emissions snapshots of Curve pools from The Graph
    /// </summary>
    public static Func<
            Func<HttpClient>,
            string,
            EitherAsync<Error, CurvePoolSnapshot>>
        GetPoolSnapshots = fun((
            Func<HttpClient> httpFactory,
            string pool) =>
    {
        string query = $@"
        {{
            pools(
                where: {{ name: ""{pool}"" }}
                first: 1000) 
            {{
                name
                assetType
                snapshots {{
                    fees
                    poolTokenPrice
                    block
                    timestamp
                }}           
                emissions {{
                    value
                    crvAmount
                    timestamp
                }}
            }}
        }}";

        return Subgraph.GetData(httpFactory, SUBGRAPH_URL_CURVE, query)
            .MapTry(JsonConvert.DeserializeObject<RequestCurvePoolSnapshots>)
            .MapTry(data => data.Data.CurveSnapshotList.Single());
    });

    /// <summary>
    /// Returns fees and emissions snapshots of Curve pools from The Graph
    /// </summary>
    public static Func<
            Func<HttpClient>,
            EitherAsync<Error, Lst<CurvePoolSnapshot>>>
        GetPoolSnapshotsForRatio = fun((
            Func<HttpClient> httpFactory) =>
    {
        string query = $@"
        {{
            pools(first: 1000)
            {{
                name

                snapshots(
                    first: 26,
                    orderBy: timestamp,
                    orderDirection: desc
                ) {{
                    timestamp
                    fees
                }}        

                emissions(
                    first: 26,
                    orderBy: timestamp,
                    orderDirection: desc
                ) {{
                    timestamp
                    value
                }}
            }}
        }}";

        return Subgraph.GetData(httpFactory, SUBGRAPH_URL_CURVE, query)
            .MapTry(JsonConvert.DeserializeObject<RequestCurvePoolSnapshots>)
            .MapTry(data => toList(data.Data.CurveSnapshotList));
    });
}
