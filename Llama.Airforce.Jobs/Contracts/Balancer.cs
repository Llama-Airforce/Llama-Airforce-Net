using System.Numerics;
using Llama.Airforce.SeedWork.Types;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Util;
using Nethereum.Web3;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Contracts;

public static class Balancer
{
    #region Function Messages

    [Function("get_total_weight", "uint256")]
    private class TotalWeightFunction : FunctionMessage { }

    [Function("rate", "uint256")]
    private class RateFunction : FunctionMessage { }

    [Function("totalSupply", "uint256")]
    private class TotalSupplyFunction : FunctionMessage { }

    [Function("getPoolTokens")]
    private class GetPoolTokensFunction : FunctionMessage
    {
        [Parameter("bytes32", 1)]
        public byte[] PoolId { get; set; }
    }

    [FunctionOutput]
    public class GetPoolTokensOutput : IFunctionOutputDTO
    {
        [Parameter("address[]", "tokens", 1)]
        public List<string> Tokens { get; set; }

        [Parameter("uint256[]", "balances", 2)]
        public List<BigInteger> Balances { get; set; }

        [Parameter("uint256", "lastChangeBlock", 3)]
        public BigInteger LastChangeBlock { get; set; }
    }

    public class BatchSwapStep
    {
        [Parameter("bytes32", "poolId", 1)]
        public byte[] PoolId { get; set; }

        [Parameter("uint256", "assetInIndex", 2)]
        public BigInteger AssetInIndex { get; set; }

        [Parameter("uint256", "assetOutIndex", 3)]
        public BigInteger AssetOutIndex { get; set; }

        [Parameter("uint256", "amount", 4)]
        public BigInteger Amount { get; set; }

        [Parameter("bytes", "userData", 5)]
        public byte[] UserData { get; set; }
    }

    public class FundManagement
    {
        [Parameter("address", "sender", 1)]
        public string Sender { get; set; }

        [Parameter("bool", "fromInternalBalance", 2)]
        public bool FromInternalBalance { get; set; }
        [Parameter("address", "recipient", 3)]

        public string Recipient { get; set; }

        [Parameter("bool", "toInternalBalance", 4)]
        public bool ToInternalBalance { get; set; }
    }

    [Function("queryBatchSwap", "int256[]")]
    public class QueryBatchSwapFunction : FunctionMessage
    {
        [Parameter("uint8", "kind", 1)]
        public byte Kind { get; set; }

        [Parameter("tuple[]", "swaps", 2)]
        public List<BatchSwapStep> Swaps { get; set; }

        [Parameter("address[]", "assets", 3)]
        public List<string> Assets { get; set; }

        [Parameter("tuple", "funds", 4)]
        public FundManagement Funds { get; set; }
    }

    #endregion

    public static Func<IWeb3, Task<BigInteger>> GetTotalWeight = fun((IWeb3 web3) => web3
        .Eth
        .GetContractQueryHandler<TotalWeightFunction>()
        .QueryAsync<BigInteger>(Addresses.Balancer.GaugeController, new TotalWeightFunction()));

    /// <summary>
    /// Gets the voting power of an address as a number between [0, 1].
    /// </summary>
    public static Func<IWeb3, Address, Task<double>> GetVotingPower = fun(async (IWeb3 web3, Address owner) =>
    {
        // Get the amount of veBAL the owner holds.
        var balanceOf = await ERC20.GetBalanceOf(web3, Addresses.Balancer.VotingEscrow, owner);

        // Get the total amount of voting power in the gauge controller.
        var totalWeight = await GetTotalWeight(web3);

        // Calculate the relative power of the owner compared to the total supply of veBAL.
        return (double)(new BigDecimal(balanceOf) / new BigDecimal(totalWeight));
    });

    /// <summary>
    /// Gets the rate of how much BAL get emitted per second.
    /// </summary>
    public static Func<IWeb3, Task<BigInteger>> GetRate = fun((IWeb3 web3) => web3
        .Eth
        .GetContractQueryHandler<RateFunction>()
        .QueryAsync<BigInteger>(Addresses.Balancer.TokenAdmin, new RateFunction()));

    /// <summary>
    /// Gets the total supply of BPT.
    /// </summary>
    public static Func<IWeb3, Task<BigInteger>> GetTotalSupplyBPT = fun((IWeb3 web3) => web3
        .Eth
        .GetContractQueryHandler<TotalSupplyFunction>()
        .QueryAsync<BigInteger>(Addresses.Balancer.BPT, new TotalSupplyFunction()));

    /// <summary>
    /// Gets the pool info for a given poolId in the Balancer vault.
    /// </summary>
    public static Func<IWeb3, string, Task<GetPoolTokensOutput>> GetPoolTokens = fun((IWeb3 web3, string poolId) => web3
        .Eth
        .GetContractQueryHandler<GetPoolTokensFunction>()
        .QueryDeserializingToObjectAsync<GetPoolTokensOutput>(
            new GetPoolTokensFunction()
            {
                PoolId = Convert.FromHexString(poolId)
            },
            Addresses.Balancer.Vault));

    /// <summary>
    /// Gets the discount for trading on the auraBAL/BPT pool.
    /// </summary>
    public static Func<IWeb3, Task<double>> GetDiscountAuraBal = fun(async (IWeb3 web3) =>
    {
        var swap = await web3
            .Eth
            .GetContractQueryHandler<QueryBatchSwapFunction>()
            .QueryAsync<List<BigInteger>>(Addresses.Balancer.Vault, new QueryBatchSwapFunction
            {
                Kind = 0,
                Swaps = new List<BatchSwapStep>
                {
                    new()
                    {
                        PoolId = Convert.FromHexString(
                            "3dd0843a028c86e0b760b1a76929d1c5ef93a2dd000200000000000000000249"),
                        Amount = BigInteger.Parse("1000000000000000000000"),
                        AssetInIndex = 1,
                        AssetOutIndex = 0,
                        UserData = new byte[] { 0 }
                    }
                },
                Assets = new List<string>
                {
                    Addresses.Balancer.BPT,
                    Addresses.AuraBal.Token
                },
                Funds = new FundManagement
                {
                    Sender = "0x2251AF9804d0A1A04e8e0e7A1FBB83F4D7423f9e",
                    FromInternalBalance = false,
                    Recipient = "0x2251AF9804d0A1A04e8e0e7A1FBB83F4D7423f9e",
                    ToInternalBalance = false
                }
            });

        var discount = (double)(new BigDecimal(swap[0]) * -1 / new BigDecimal(swap[1]));

        return discount;
    });
}