using System.Numerics;
using Llama.Airforce.SeedWork.Types;
using Nethereum.Util;
using Nethereum.Web3;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Contracts;

public static class Fxn
{
    /// <summary>
    /// Gets the amount of FXN tokens emitted in a certain timerange.
    /// </summary>
    public static Func<
            IWeb3,
            DateTime,
            DateTime,
            Task<BigInteger>>
        GetMintedTokensInTimeframe = fun((
            IWeb3 web3,
            DateTime start,
            DateTime end) =>
    {
        // Convert to Unix timestamp
        var startUnix = ((DateTimeOffset)start).ToUnixTimeSeconds();
        var endUnix = ((DateTimeOffset)end).ToUnixTimeSeconds();

        // Convert to BigInteger
        var startBigInt = new BigInteger(startUnix);
        var endBigInt = new BigInteger(endUnix);

        var abi = @"[
            {
                ""stateMutability"": ""view"",
                ""type"": ""function"",
                ""name"": ""mintable_in_timeframe"",
                ""inputs"": [
                    { ""name"": ""start"", ""type"": ""uint256"" },
                    { ""name"": ""end"", ""type"": ""uint256"" }
                ],
                ""outputs"": [
                    { ""name"": """", ""type"": ""uint256"" }
                ]
            }
        ]";

        var contract = web3.Eth.GetContract(abi, Addresses.Fxn.Token);
        var mintableInTimeframeFunction = contract.GetFunction("mintable_in_timeframe");

        return mintableInTimeframeFunction.CallAsync<BigInteger>(startBigInt, endBigInt);
    });

    /// <summary>
    /// Gets the voting power of an address as a number between [0, 1].
    /// </summary>
    public static Func<IWeb3, Address, Task<double>> GetVotingPower = fun(async (IWeb3 web3, Address owner) =>
    {
        // Get the amount of veFXN the owner holds.
        var balanceOf = await ERC20.GetBalanceOf(web3, Addresses.Fxn.Locker, owner);

        // Get the total amount of voting power in the gauge controller.
        var totalWeight = await ERC20.GetTotalSupply(web3, Addresses.Fxn.Locker);

        // Calculate the relative power of the owner compared to the total supply of veFXN.
        return (double)(new BigDecimal(balanceOf) / new BigDecimal(totalWeight));
    });
}