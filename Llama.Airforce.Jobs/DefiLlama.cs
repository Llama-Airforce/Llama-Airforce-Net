using LanguageExt;
using LanguageExt.Common;
using Llama.Airforce.Jobs.Extensions;
using Llama.Airforce.SeedWork.Extensions;
using Llama.Airforce.SeedWork.Types;
using Newtonsoft.Json;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs;

public static class DefiLlama
{
    public class RequestPrice
    {
        [JsonProperty("coins")]
        public Dictionary<string, Coin> Coins { get; set; }
    }

    public class Coin
    {
        [JsonProperty("decimals")]
        public int Decimals { get; set; }

        [JsonProperty("price")]
        public double Price { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
    }

    public const string DEFILLAMA_PRICES_URL = "https://coins.llama.fi/prices/historical";

    public static Func<
            Func<HttpClient>,
            Address,
            Network,
            Option<DateTime>,
            EitherAsync<Error, double>>
        GetPrice = fun((
            Func<HttpClient> httpFactory,
            Address address,
            Network network,
            Option<DateTime> date) =>
        {
            var timestamp = date.Map(x => x.ToUnixTimeSeconds()).IfNone(0);
            var coin = $"{network.NetworkToString()}:{address}";
            var url = $"{DEFILLAMA_PRICES_URL}/{timestamp}/{coin}";

            return Functions
                .HttpFunctions
                .GetData(
                    httpFactory,
                    url)
                .MapTry(JsonConvert.DeserializeObject<RequestPrice>)
                .MapTry(x => x.Coins[coin].Price);
        });
}