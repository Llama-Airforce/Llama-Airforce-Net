using LanguageExt;
using LanguageExt.Common;
using Llama.Airforce.Jobs.Extensions;
using Llama.Airforce.Jobs.Functions;
using Llama.Airforce.Jobs.Subgraphs.Models;
using Llama.Airforce.SeedWork.Extensions;
using Newtonsoft.Json;
using static LanguageExt.Prelude;
using Dom = Llama.Airforce.Domain.Models;

namespace Llama.Airforce.Jobs.Subgraphs;

public class Votium
{
    public const string SUBGRAPH_URL_VOTIUM_V2 = "https://gateway-arbitrum.network.thegraph.com/api/{GRAPH_API_KEY}/subgraphs/id/89LUfZ4XJzUXrXgRFbVBpFtc92HiEuWGHULw8HJ6EgQN";
    public const string SUBGRAPH_URL_VOTIUM_FXN = "https://gateway-arbitrum.network.thegraph.com/api/{GRAPH_API_KEY}/subgraphs/id/DUbmGMiU1wabsEzstg1QphikC8HMMwHs32VPaQ7hAjux";

    public static DateTime GetEpochDate(
        Dom.Protocol protocol,
        int round)
    {
        var epochStart = protocol switch
        {
            Dom.Protocol.ConvexCrv => 1348 * 86400 * 14 + round * 86400 * 14,
            // f(x) Protocol started at curve epoch 65.
            Dom.Protocol.ConvexFxn => 1348 * 86400 * 14 + (round + 65) * 86400 * 14
        };

        var epochDate = DateTimeExt.FromUnixTimeSeconds(epochStart);
        return epochDate;
    }

    /// <summary>
    /// Returns Votium epoch & bribe history from The Graph
    /// </summary>
    public static Func<
            Func<HttpClient>,
            string,
            Dom.Protocol,
            EitherAsync<Error, Lst<Dom.EpochV2>>>
        GetEpochsV2 = fun((
            Func<HttpClient> httpFactory,
            string graphApiKey,
            Dom.Protocol protocol) =>
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

            var url = protocol switch
            {
                Dom.Protocol.ConvexCrv => SUBGRAPH_URL_VOTIUM_V2.Replace("{GRAPH_API_KEY}", graphApiKey),
                Dom.Protocol.ConvexFxn => SUBGRAPH_URL_VOTIUM_FXN.Replace("{GRAPH_API_KEY}", graphApiKey),
                _ => throw new ArgumentOutOfRangeException("Unsupported protocol")
            };

            return HttpFunctions.PostData(httpFactory, url, JsonConvert.SerializeObject(new { query = Query }))
               .MapTry(JsonConvert.DeserializeObject<RequestEpochsVotiumV2>)
               .MapTry(data => toList(data
                   .Data
                   .EpochList
                   // Filter out epochs that haven't started yet.
                   .Where(epoch =>
                    {
                        var epochDate = GetEpochDate(protocol, epoch.Id);
                        return epochDate <= DateTime.Now;
                    })
                   .Select(epoch => (Dom.EpochV2)epoch)
                   .OrderBy(epoch => epoch.Round)));
        });
}
