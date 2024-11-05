using System.Numerics;
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
}