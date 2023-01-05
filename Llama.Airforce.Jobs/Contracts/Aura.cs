using System.Numerics;
using LanguageExt;
using Llama.Airforce.Jobs.Extensions;
using Llama.Airforce.Jobs.Functions;
using Llama.Airforce.SeedWork.Extensions;
using Llama.Airforce.SeedWork.Types;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Web3;
using static LanguageExt.Prelude;
using Error = LanguageExt.Common.Error;

namespace Llama.Airforce.Jobs.Contracts;

public static class Aura
{
    public static int AuraDecimals = 18;
    public static int BalancerDecimals = 18;
    public static double RewardFee = 0.17;

    #region Function Messages

    [Function("rewardRate", "uint256")]
    private class RewardRateFunction : FunctionMessage { }

    #endregion

    public static Func<IWeb3, Address, Task<BigInteger>> GetRewardRate = fun((IWeb3 web3, Address stakedAddress) => web3
        .Eth
        .GetContractQueryHandler<RewardRateFunction>()
        .QueryAsync<BigInteger>(stakedAddress, new RewardRateFunction()));

    /// <summary>
    /// Returns how much AURA would be minted today given a certain amount of BAL.
    /// Copy pasted from cvx, hence the same names.
    /// </summary>
    public static Func<IWeb3, double, Task<double>> GetAuraMintAmount = fun(async (IWeb3 web3, double balEarned) =>
    {
        const int EmissionsMaxSupply = 50_000_000;
        const int InitMintAmount = 50_000_000;
        const int CliffCount = 500;

        var emissionsMinted = await ERC20
            .GetTotalSupply(web3, Addresses.Aura.Token)
            .DivideByDecimals(AuraDecimals) - InitMintAmount;

        var cliff = emissionsMinted / ((double)EmissionsMaxSupply / CliffCount);

        if (cliff < CliffCount)
        {
            var reduction = (CliffCount - cliff) * 2.5 + 700;
            var amount = balEarned * reduction / CliffCount;

            var amountTillMax = EmissionsMaxSupply - emissionsMinted;
            if (amount > amountTillMax)
                amount = amountTillMax;

            return amount;
        }

        return 0;
    });

    /// <summary>
    /// Returns the APR for single-sided staking of auraBAL.
    /// </summary>
    public static Func<
            Func<HttpClient>,
            IWeb3,
            EitherAsync<Error, double>>
        GetAuraBalApr = fun((
            Func<HttpClient> httpFactory, 
            IWeb3 web3) =>
    {
        var balPrice_ = PriceFunctions.GetPrice(httpFactory, Addresses.Balancer.Token, Network.Ethereum, Some(web3));
        var bbausdPrice_ = PriceFunctions.GetPriceExt(httpFactory, Addresses.Balancer.BBAUSDToken, Network.Ethereum, Some(web3), None, "BB-A-USD");
        var auraPrice_ = PriceFunctions.GetPrice(httpFactory, Addresses.Aura.Token, Network.Ethereum, Some(web3));

        // For some reason DefiLlama and CoinGecko can't return the price at a specific time, so we fall back to 'normal' price fetching.
        var auraBalPrice_ = PriceFunctions.GetAuraBalPrice(httpFactory, web3);

        var balRate_ = GetRewardRate(web3, Addresses.Aura.BalStaked).DivideByDecimals(AuraDecimals).ToEitherAsync();
        var bbausdRate_ = GetRewardRate(web3, Addresses.Aura.BBAUSDStaked).DivideByDecimals(AuraDecimals).ToEitherAsync();
        var supply_ = ERC20.GetTotalSupply(web3, Addresses.Aura.BalStaked).DivideByDecimals(AuraDecimals).ToEitherAsync();

        var balPerYear_ =
            from auraBalPrice in auraBalPrice_
            from rate in balRate_
            from supply in supply_
            select rate / (supply * auraBalPrice) * 86400 * 365;

        var bbausdPerYear_ =
            from auraBalPrice in auraBalPrice_
            from rate in bbausdRate_
            from supply in supply_
            select rate / (supply * auraBalPrice) * 86400 * 365;

        var auraPerYear_ = balPerYear_.MapAsync(x => GetAuraMintAmount(web3, x));

        return
            from balPrice in balPrice_
            from bbausdPrice in bbausdPrice_
            from auraPrice in auraPrice_
            from balPerYear in balPerYear_
            from bbausdPerYear in bbausdPerYear_
            from auraPerYear in auraPerYear_
            select balPerYear * balPrice + auraPerYear * auraPrice + bbausdPerYear * bbausdPrice;
    });
}