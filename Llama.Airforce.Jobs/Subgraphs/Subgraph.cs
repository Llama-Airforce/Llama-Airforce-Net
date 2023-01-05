using LanguageExt;
using LanguageExt.Common;
using Newtonsoft.Json;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Subgraphs;

public static class Subgraph
{
    /// <summary>
    /// Returns general json data from The Graph
    /// </summary>
    public static Func<
            Func<HttpClient>,
            string,
            string,
            EitherAsync<Error, string>>
        GetData = fun((
            Func<HttpClient> httpFactory,
            string url,
            string query) => Functions
        .HttpFunctions
        .GetData(
            httpFactory,
            url,
            JsonConvert.SerializeObject(new
            {
                query
            })));
}