using Llama.Airforce.SeedWork.Types;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Util;
using Nethereum.Web3;
using System.Numerics;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Contracts;

public static class Curve
{
    #region Function Messages

    [Function("rewardRate", "uint256")]
    private class RewardRateFunction : FunctionMessage { }

    [Function("get_virtual_price", "uint256")]
    private class VirtualPriceFunction : FunctionMessage { }

    [Function("get_total_weight", "uint256")]
    private class TotalWeightFunction : FunctionMessage { }

    [Function("rate", "uint256")]
    private class RateFunction : FunctionMessage { }

    [Function("price_oracle", "uint256")]
    private class PriceOracleFunction : FunctionMessage { }

    #endregion

    public static Func<IWeb3, Address, Task<BigInteger>> GetRewardRate = fun((IWeb3 web3, Address stakedAddress) => web3
        .Eth
        .GetContractQueryHandler<RewardRateFunction>()
        .QueryAsync<BigInteger>(stakedAddress, new RewardRateFunction()));

    public static Func<IWeb3, Address, Task<BigInteger>> GetVirtualPrice = fun((IWeb3 web3, Address address) => web3
        .Eth
        .GetContractQueryHandler<VirtualPriceFunction>()
        .QueryAsync<BigInteger>(address, new VirtualPriceFunction()));

    public static Func<IWeb3, Task<BigInteger>> GetTotalWeight = fun((IWeb3 web3) => web3
        .Eth
        .GetContractQueryHandler<TotalWeightFunction>()
        .QueryAsync<BigInteger>(Addresses.Curve.GaugeController, new TotalWeightFunction()));

    /// <summary>
    /// Gets the voting power of an address as a number between [0, 1].
    /// </summary>
    public static Func<IWeb3, Address, Task<double>> GetVotingPower = fun(async (IWeb3 web3, Address owner) =>
    {
        // Get the amount of veCRV the owner holds.
        var balanceOf = await ERC20.GetBalanceOf(web3, Addresses.Curve.VotingEscrow, owner);

        // Get the total amount of voting power in the gauge controller.
        var totalWeight = await GetTotalWeight(web3) / new BigInteger(Math.Pow(10, 18));

        // Calculate the relative power of the owner compared to the total supply of veCRV.
        return (double)(new BigDecimal(balanceOf) / new BigDecimal(totalWeight));
    });

    /// <summary>
    /// Gets the rate of how much CRV get emitted per second.
    /// </summary>
    public static Func<IWeb3, Task<BigInteger>> GetRate = fun((IWeb3 web3) => web3
        .Eth
        .GetContractQueryHandler<RateFunction>()
        .QueryAsync<BigInteger>(Addresses.Curve.Token, new RateFunction()));

    /// <summary>
    /// Gets the price oracle for Curve v2 pools.
    /// </summary>
    public static Func<IWeb3, Address, Task<BigInteger>> GetPriceOracle = fun((IWeb3 web3, Address lpToken) => web3
        .Eth
        .GetContractQueryHandler<PriceOracleFunction>()
        .QueryAsync<BigInteger>(lpToken, new PriceOracleFunction()));

    public class AdminTransfer
    {
        public AdminTransfer(int block, double value, string token)
        {
            Block = block;
            Value = value;
            Token = token;
        }

        public int Block { get; set; }
        public double Value { get; set; }
        public string Token { get; set; }
    }

    public class Fees
    {
        public Fees(int timestamp, double value)
        {
            TimeStamp = timestamp;
            Value = value;
        }

        public int TimeStamp { get; set; }
        public double Value { get; set; }
    }

    public class Emissions
    {
        public Emissions(int timestamp, double value, double crvAmount)
        {
            TimeStamp = timestamp;
            CrvAmount = crvAmount;
            Value = value;
        }

        public int TimeStamp { get; set; }
        public double Value { get; set; }
        public double CrvAmount { get; set; }
    }
}