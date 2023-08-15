using LanguageExt;
using LanguageExt.Common;
using Llama.Airforce.Jobs.Subgraphs.Models;
using Llama.Airforce.SeedWork.Extensions;
using Newtonsoft.Json;
using static LanguageExt.Prelude;
using Dom = Llama.Airforce.Domain.Models;

namespace Llama.Airforce.Jobs.Subgraphs;

public class Votium
{
    public const string SUBGRAPH_URL_VOTIUM = "https://api.thegraph.com/subgraphs/name/convex-community/votium-bribes";
    public const string SUBGRAPH_URL_VOTIUM_V2 = "https://api.thegraph.com/subgraphs/name/convex-community/votium-v2";

    /// <summary>
    /// Returns Votium epoch & bribe history from The Graph
    /// </summary>
    public static Func<
            Func<HttpClient>,
            EitherAsync<Error, Lst<Dom.Epoch>>>
        GetEpochs = fun((Func<HttpClient> httpFactory) =>
    {
        const string Query = @"{
epoches(
    where: { bribeCount_gt: 0, id_not: ""0xb59c1e06f38e5daaaa51e672174f6a4a65cf654d1e363ed25bf11153876fbaec"" }
    first: 1000
    orderBy: initiatedAt
    orderDirection: asc
  ) {
    id
    deadline
    initiatedAt
    bribeCount
    bribes {
        choiceIndex
        token
        amount
    }
} }";

        return Subgraph.GetData(httpFactory, SUBGRAPH_URL_VOTIUM, Query)
            .MapTry(JsonConvert.DeserializeObject<RequestEpochsVotium>)
            .MapTry(data => toList(data
                .Data
                .EpochList
                .Select(epoch => (Dom.Epoch)epoch)));
    });

        /// <summary>
    /// Returns Votium epoch & bribe history from The Graph
    /// </summary>
    public static Func<
            Func<HttpClient>,
            EitherAsync<Error, Lst<Dom.EpochV2>>>
        GetEpochsV2 = fun((Func<HttpClient> httpFactory) =>
        {
            const string Query = @"{
rounds(
    where: { bribeCount_gt: 0 }
    first: 1000
    orderBy: initiatedAt
    orderDirection: asc
) {
  id
  initiatedAt
  bribeCount
  incentives {
    gauge
    token
    amount
    maxPerVote
  }
} }";

        return Subgraph.GetData(httpFactory, SUBGRAPH_URL_VOTIUM_V2, Query)
            .MapTry(JsonConvert.DeserializeObject<RequestEpochsVotiumV2>)
            .MapTry(data => toList(data
                .Data
                .EpochList
                .Select(epoch => (Dom.EpochV2)epoch)));
    });
}