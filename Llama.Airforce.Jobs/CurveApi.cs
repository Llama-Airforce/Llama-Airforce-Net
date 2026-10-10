using LanguageExt;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;
using Llama.Airforce.SeedWork.Extensions;
using Llama.Airforce.SeedWork.Types;
using Newtonsoft.Json;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs;

public static class CurveApi
{
    public class RequestGauges
    {
        [JsonProperty("data")]
        public Dictionary<string, RequestGauge> Data { get; set; }
    }

    public class RequestGauge
    {
        [JsonProperty("gauge")]
        public string Gauge { get; set; }

        [JsonProperty("rootGauge")]
        public string RootGauge { get; set; }

        [JsonProperty("shortName")]
        public string ShortName { get; set; }
    }

    public const string CURVE_API_URL = "https://api.curve.finance/api/getAllGauges";

    public static Func<
            Func<HttpClient>,
            EitherAsync<Error, Map<string, string>>>
        GetGauges = fun((
            Func<HttpClient> httpFactory) =>
        {
            return Functions
               .HttpFunctions
               .GetData(
                    httpFactory,
                    CURVE_API_URL)
               .MapTry(JsonConvert.DeserializeObject<RequestGauges>)
               .MapTry(x => x.Data.Aggregate(Map<string, string>(), (
                        acc,
                        kv) =>
                {
                    var address = Address.Of(string.IsNullOrEmpty(kv.Value.RootGauge) ? kv.Value.Gauge : kv.Value.RootGauge).ValueUnsafe();
                    var shortName = kv.Value.ShortName;

                    return acc.AddOrUpdate(address, _ => shortName, shortName);
                }));
        });
}
