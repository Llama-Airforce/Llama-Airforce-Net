using LanguageExt;
using LanguageExt.Common;
using Llama.Airforce.Jobs.Extensions;
using Llama.Airforce.SeedWork.Types;
using Newtonsoft.Json.Linq;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs;

public static class CoinGecko
{
    public static Func<
            Func<HttpClient>,
            Address,
            Network,
            Currency,
            DateTime,
            EitherAsync<Error, double>>
        GetPriceAtTime = fun((
            Func<HttpClient> httpFactory,
            Address address,
            Network network,
            Currency currency,
            DateTime date) =>
        TryAsync(async () =>
        {
            var target = date.ToUnixTimeSeconds();
            var from = new DateTime(date.Ticks, date.Kind).AddHours(-2).ToUnixTimeSeconds();
            var to = new DateTime(date.Ticks, date.Kind).AddHours(2).ToUnixTimeSeconds();

            using var httpClient = httpFactory();
            var url = $"https://api.coingecko.com/api/v3/coins/{network.NetworkToString()}/contract/{address}/market_chart/range?vs_currency={currency}&from={from}&to={to}";

            var resp = await httpClient.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Unable to get CoinGecko range price data for address {address} with currency {currency}, status code: {resp.StatusCode}");

            // Rate limit CoinGecko call.
            await Task.Delay(1200);

            var content = await resp.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            return Try(
                () => json["prices"]
                    .Map(x => (Time: x[0].ToObject<long>() / 1000, Price: x[1].ToObject<double>()))
                    .Aggregate(
                        (Time: long.MaxValue, Price: 0.0),
                        (acc, cur) => Math.Abs(target - acc.Time) < Math.Abs(target - cur.Time) ? acc : cur))
                .Map(x => x.Time == long.MaxValue ? throw new Exception("") : x.Price)
                .Match(
                    Succ: x => x,
                    Fail: _ => throw new Exception($"Failed to get range price for token {address} and currency {currency}"));
        })
            .ToEither());
}
