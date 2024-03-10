using LanguageExt;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;
using Llama.Airforce.SeedWork.Extensions;
using Llama.Airforce.SeedWork.Types;
using Newtonsoft.Json;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs;

public static class FxnApi
{
    public class RequestGauges
    {
        [JsonProperty("data")]
        public Dictionary<string, RequestGauge> Data { get; set; }
    }

    public class RequestGauge
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("gauge")]
        public string Gauge { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public const string FXN_API_URL = "https://api.aladdin.club/api1/get_fx_gauge_list";

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
                    FXN_API_URL)
               .MapTry(JsonConvert.DeserializeObject<RequestGauges>)
               .MapTry(x => x.Data
                   .Values
                   .Select(gauge => (gauge.Name, gauge.Gauge, gauge.Type))
                   .Aggregate(Map<string, string>(), (
                        acc,
                        kv) =>
                    {
                        var gaugeId = Address.Of(kv.Gauge).ValueUnsafe(); // Votium bribe gauge
                        var shortName = kv.Name; // Snapshot choice

                        return acc.AddOrUpdate(gaugeId, _ => shortName, shortName);
                    }));
        });
}