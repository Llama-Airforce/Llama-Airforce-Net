using LanguageExt;
using LanguageExt.Common;
using Llama.Airforce.Jobs.Contracts;
using Llama.Airforce.Jobs.Extensions;
using Llama.Airforce.SeedWork.Extensions;
using Llama.Airforce.SeedWork.Types;
using Nethereum.Web3;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Functions;

public static class PriceFunctions
{
    /// <summary>
    /// Fallback token address for tokens in case coingecko price fetching fails.
    /// </summary>
    public static Func<string, Option<string>> FallbackTokenAddress = fun((string token) => token switch
    {
        "LUNA" => Some("0xd2877702675e6ceb975b4a1dff9fb7baf4c91ea9"),
        "GEIST" => Some("0xd8321aa83fb0a4ecd6348d4577431310a6e0814d"),
        _ => None
    });

    /// <summary>
    /// Last attempt fallback values for tokens in case coingecko price fetching fails.
    /// </summary>
    public static Func<
            Func<HttpClient>,
            Option<IWeb3>,
            string,
            OptionAsync<double>>
        Fallback = fun((
            Func<HttpClient> httpFactory,
            Option<IWeb3> web3,
            string token) => token switch
        {
            "USDM" => SomeAsync(1.0),
            "BB-A-USD" => SomeAsync(1.0),
            "T" => web3.Match(w => GetCurveV2Price(httpFactory, w, Addresses.ERC20.T).ToOption(), () => None),
            "eCFX" => web3.Match(w => GetCurveV2Price(httpFactory, w, Addresses.ERC20.eCFX).ToOption(), () => None),
            _ => None
        });

    /// <summary>
    /// Mapping for tokens and their respective ETH LP pair for Curve V2.
    /// </summary>
    public static Func<Address, Option<Address>> CurveV2LpAddress = fun((Address token) => token.Value switch
    {
        not null when token.Equals(Addresses.ERC20.T) => Some(Addresses.CurveV2LP.TETH),
        not null when token.Equals(Addresses.ERC20.eCFX) => Some(Addresses.CurveV2LP.eCFXETH),
        _ => None
    });

    /// <summary>
    /// Mapping for tokens that are not on Ethereum by default.
    /// </summary>
    public static Func<string, Network> GetNetwork = fun((string token) => token switch
    {
        "GEIST" => Network.Fantom,
        _ => Network.Ethereum
    });

    public static Func<
            Func<HttpClient>,
            Address,
            Network,
            Option<IWeb3>,
            Option<DateTime>,
            Option<string>,
            EitherAsync<Error, double>>
        GetPriceExt = fun((
            Func<HttpClient> httpFactory,
            Address address,
            Network network,
            Option<IWeb3> web3,
            Option<DateTime> date,
            Option<string> token) =>
        {
            var date_ = date.IfNone(DateTime.UtcNow);
            var fallback = fun((Error ex) => token.Bind(FallbackTokenAddress)
                .Bind(Address.Of)
                .ToEitherAsync(ex));

            return DefiLlama.GetPrice(httpFactory, address, network, date_)
                // Try fallback address if fetching fails.
                .BindLeft(ex => fallback(ex).Bind(fb => DefiLlama.GetPrice(httpFactory, fb, network, date_)))
                // Fallback to Coingecko in case of failure with original address.
                .BindLeft(ex => CoinGecko.GetPriceAtTime(httpFactory, address, network, Currency.Usd, date_))
                // Try different address if fetching fails.
                .BindLeft(ex => fallback(ex).Bind(fb => CoinGecko
                    .GetPriceAtTime(
                        httpFactory,
                        fb,
                        network,
                        Currency.Usd,
                        date_)))
                // Last ditch effort to look at hardcoded price fallback values.
                .BindLeft(ex => token
                    .ToAsync()
                    .Bind(par(Fallback.Par(httpFactory), web3))
                    .ToEither(Error.New($"Could not find fallback dollar value for {address}\n" + ex.Message, ex)));
        });

    public static Func<
            Func<HttpClient>,
            Address,
            Network,
            Option<IWeb3>,
            EitherAsync<Error, double>>
        GetPrice = fun((
            Func<HttpClient> httpFactory,
            Address address,
            Network network,
            Option<IWeb3> web3) => GetPriceExt(httpFactory, address, network, web3, None, None));

    /// <summary>
    /// Returns the current price in dollars for a token by looking at its ETH Curve V2 LP.
    /// </summary>
    public static Func<
            Func<HttpClient>,
            IWeb3,
            Address,
            EitherAsync<Error, double>>
        GetCurveV2Price = fun((
            Func<HttpClient> httpFactory,
            IWeb3 web3,
            Address token) =>
    {
        var weth_ = GetPriceExt(httpFactory, Addresses.ERC20.WETH, Network.Ethereum, Some(web3), None, "WETH");
        var decimals_ = ERC20.GetDecimals(web3, token).ToEitherAsync();
        var lpToken_ = CurveV2LpAddress(token).ToEitherAsync(Error.New($"No Curve V2 LP found for {token}"));

        var price_ = (
            from lpToken in lpToken_
            from decimals in decimals_
            select Curve
                .GetPriceOracle(web3, lpToken)
                .DivideByDecimals(decimals)
                .ToEitherAsync())
            .Bind(x => x);

        return
            from price in price_
            from weth in weth_
            select price * weth;
    });

    public static Func<
            Func<HttpClient>,
            IWeb3,
            EitherAsync<Error, double>>
        GetAuraBalPrice = fun((
            Func<HttpClient> httpFactory,
            IWeb3 web3) =>
        {
            var bal_ = GetPrice(httpFactory, Addresses.Balancer.Token, Network.Ethereum, Some(web3));
            var weth_ = GetPrice(httpFactory, Addresses.ERC20.WETH, Network.Ethereum, Some(web3));
            var totalSupply_ = Balancer.GetTotalSupplyBPT(web3).ToEitherAsync();
            var poolTokens_ = Balancer
                .GetPoolTokens(
                    web3,
                    "5c6ee304399dbdb9c8ef030ab642b10820db8f56000200000000000000000014")
                .ToEitherAsync();

            var bpt_ =
                from bal in bal_
                from weth in weth_
                from totalSupply in totalSupply_
                from poolTokens in poolTokens_
                select (bal * poolTokens.Balances[0].DivideByDecimals(18) + weth * poolTokens.Balances[1].DivideByDecimals(18)) / totalSupply.DivideByDecimals(18);

            var discount_ = Balancer.GetDiscountAuraBal(web3).ToEitherAsync();

            return
                from bpt in bpt_
                from discount in discount_
                select bpt * discount;
        });
}