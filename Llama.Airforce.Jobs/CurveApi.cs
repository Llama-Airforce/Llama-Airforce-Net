﻿using LanguageExt;
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

        [JsonProperty("shortName")]
        public string ShortName { get; set; }
    }

    public record Gauge(
        Address Address,
        string ShortName);

    public const string CURVE_API_URL = "https://api.curve.finance/api/getAllGauges";

    public static Func<
            Func<HttpClient>,
            EitherAsync<Error, Map<Address, Gauge>>>
        GetGauges = fun((
            Func<HttpClient> httpFactory) =>
        {
            return Functions
               .HttpFunctions
               .GetData(
                    httpFactory,
                    CURVE_API_URL)
               .MapTry(JsonConvert.DeserializeObject<RequestGauges>)
               .MapTry(x => x.Data.Aggregate(Map<Address, Gauge>(), (
                        acc,
                        kv) =>
                {
                    var address = Address.Of(kv.Value.Gauge).ValueUnsafe();
                    var gauge = new Gauge(address, kv.Value.ShortName);

                    return acc.AddOrUpdate(address, _ => gauge, gauge);
                }));
        });

    /// <summary>
    /// Maps the Map<Address, Gauge> to a Map<string, string>
    /// </summary>
    public static Func<
            Func<HttpClient>,
            EitherAsync<Error, Map<string, string>>>
        GetGaugesGaugeToShortName = fun((
            Func<HttpClient> httpFactory) =>
        {
            return GetGauges(httpFactory)
               .Map(gauges =>
                {
                    var newMap = Map<string, string>();

                    foreach (var (address, gauge) in gauges)
                        newMap = newMap.AddOrUpdate(address, _ => gauge.ShortName, gauge.ShortName);

                    return newMap;
                });
        });
}