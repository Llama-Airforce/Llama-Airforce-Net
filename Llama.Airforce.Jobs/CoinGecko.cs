using LanguageExt;
using LanguageExt.Common;
using Llama.Airforce.Jobs.Extensions;
using Llama.Airforce.SeedWork.Types;
using Newtonsoft.Json.Linq;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs;

public static class CoinGecko
{

    /// <summary>
    /// Returns general market data from Coingecko for a given token address.
    /// </summary>
    public static Func<
            Address,
            Network,
            EitherAsync<Error, JObject>>
        GetData = fun((
            Address address,
            Network network) =>
        TryAsync(async () =>
        {
            using var httpClient = new HttpClient();
            var url = $"https://api.coingecko.com/api/v3/coins/{network.NetworkToString()}/contract/{address}?tickers=false&community_data=false&developer_data=false";

            var resp = await httpClient.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Unable to get CoinGecko data for address {address}, status code: {resp.StatusCode}");

            var content = await resp.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            // Rate limit CoinGecko call.
            await Task.Delay(1200);

            return json;
        })
            .ToEither());

    /// <summary>
    /// Returns market cap related data for a given piece of Coingecko data.
    /// </summary>
    public static Func<
            JObject,
            Either<Error, (double MCap, double FDV)>>
        GetMarketCap = fun((
            JObject data) =>
        {
            var mcap_ = Try(() => double.Parse(
                    (string)data["market_data"]["market_cap"]["usd"],
                    System.Globalization.CultureInfo.InvariantCulture))
                .ToEither(ex => Error.New("Failed to get market cap", ex));

            var fdv_ = Try(() => double.Parse(
                    (string)data["market_data"]["fully_diluted_valuation"]["usd"],
                    System.Globalization.CultureInfo.InvariantCulture))
                .ToEither(ex => Error.New("Failed to get fully diluted market valuation", ex));

            return
                from mcap in mcap_
                from fdv in fdv_
                select (mcap, fdv);
        });


    public static Func<
            Address,
            Network,
            Currency,
            EitherAsync<Error, double>>
        GetPrice = fun((
            Address address,
            Network network,
            Currency currency) =>
        TryAsync(async () =>
        {
            using var httpClient = new HttpClient();
            var url = $"https://api.coingecko.com/api/v3/simple/token_price/{network.NetworkToString()}?contract_addresses={address}&vs_currencies={currency}";

            var resp = await httpClient.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Unable to get CoinGecko price data for address {address} with currency {currency}, status code: {resp.StatusCode}");

            var content = await resp.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            // Rate limit CoinGecko call.
            await Task.Delay(600);

            return Try(() => double.Parse(
                    (string)json[address.ToString()][currency.ToString()],
                    System.Globalization.CultureInfo.InvariantCulture))
                .Match(
                    Succ: x => x,
                    Fail: _ => throw new Exception($"Failed to get price for token {address} and currency {currency}"));
        })
            .ToEither());

    public static Func<
            Address,
            Network,
            Currency,
            DateTime,
            EitherAsync<Error, double>>
        GetPriceAtTime = fun((
            Address address,
            Network network,
            Currency currency,
            DateTime date) =>
        TryAsync(async () =>
        {
            var target = date.ToUnixTimeSeconds();
            var from = new DateTime(date.Ticks, date.Kind).AddHours(-2).ToUnixTimeSeconds();
            var to = new DateTime(date.Ticks, date.Kind).AddHours(2).ToUnixTimeSeconds();

            using var httpClient = new HttpClient();
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