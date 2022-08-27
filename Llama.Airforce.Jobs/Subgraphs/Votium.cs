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

    /// <summary>
    /// Returns Votium epoch & bribe history from The Graph
    /// </summary>
    public static Func<EitherAsync<Error, Lst<Dom.Epoch>>> GetEpochs = fun(() =>
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

        return Subgraph.GetData(SUBGRAPH_URL_VOTIUM, Query)
            .MapTry(JsonConvert.DeserializeObject<RequestEpochsVotium>)
            .MapTry(data => toList(data
                .Data
                .EpochList
                .Select(epoch => (Dom.Epoch)epoch)));
    });
}