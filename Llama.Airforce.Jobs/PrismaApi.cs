using LanguageExt;
using LanguageExt.Common;
using Llama.Airforce.SeedWork.Extensions;
using Newtonsoft.Json;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs;

public static class PrismaApi
{
    public class RequestGauges
    {
        [JsonProperty("data")]
        public RequestGauge Data { get; set; }
    }

    public class RequestGauge
    {
        [JsonProperty("receiverToWeights")]
        public Dictionary<string, RequestReceiver> Receivers { get; set; }
    }

    public class RequestReceiver
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("weights")]
        public List<RequestWeight> Weights { get; set; }
    }

    public class RequestWeight
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public const string PRISMA_API_URL = "https://api.prismafinance.com/api/v1/emissionVotes";

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
                    PRISMA_API_URL)
               .MapTry(JsonConvert.DeserializeObject<RequestGauges>)
               .MapTry(x => x.Data
                   .Receivers
                   .Values
                   .SelectMany(receiver => receiver
                       .Weights
                       .Map(weight => (receiver.Name, weight.Id, weight.Type)))
                   .Aggregate(Map<string, string>(), (
                        acc,
                        kv) =>
                    {
                        var gaugeId = kv.Id.ToString();
                        var shortName = kv.Name.ToString();

                        if (kv.Type is "debt" or "mint")
                            shortName = $"{kv.Type}-{kv.Name}";

                        shortName = shortName
                           .Replace("Wrapped ", "")
                           .Replace(" Deposit", "")
                           .Replace("Factory Crypto Pool", "");

                        if (shortName == "Curve.fi : PRISMA/ETH")
                            shortName = "Prisma PRISMA/ETH Curve";

                        return acc.AddOrUpdate(gaugeId, _ => shortName, shortName);
                    }));
        });
}