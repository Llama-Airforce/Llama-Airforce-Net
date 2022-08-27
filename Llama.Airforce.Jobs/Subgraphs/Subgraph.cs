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
            string,
            string,
            EitherAsync<Error, string>>
        GetData = fun((
            string url,
            string query) => Functions
        .HttpFunctions
        .GetData(
            url,
            JsonConvert.SerializeObject(new
            {
                query
            })));
}