using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Nethereum.Web3;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.ContractTests;

public class FXProtocolTests
{
    private readonly IConfiguration Configuration;

    public FXProtocolTests()
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<FXProtocolTests>()
            .AddEnvironmentVariables();

        Configuration = builder.Build();
    }


    [Test]
    public async Task GetMintedTokensInTimeframe()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Dates for fetching
        var startDate = "2024-11-02";
        var endDate = "2024-11-03";

        var startDateTime = DateTime.Parse(startDate).ToUniversalTime();
        var endDateTime = DateTime.Parse(endDate).ToUniversalTime();

        // Convert to Unix timestamp
        var startUnix = ((DateTimeOffset)startDateTime).ToUnixTimeSeconds();
        var endUnix = ((DateTimeOffset)endDateTime).ToUnixTimeSeconds();

        // Convert to BigInteger
        var startBigInt = new BigInteger(startUnix);
        var endBigInt = new BigInteger(endUnix);

        var contractAddress = "0x365AccFCa291e7D3914637ABf1F7635dB165Bb09"; // FXN TOKEN
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

        var contract = web3.Eth.GetContract(abi, contractAddress);
        var mintableInTimeframeFunction = contract.GetFunction("mintable_in_timeframe");

        // Act
        var tokensMinted = await mintableInTimeframeFunction.CallAsync<BigInteger>(startBigInt, endBigInt);

        // Assert
        Assert.IsTrue(tokensMinted >= 0, "Tokens minted should be non-negative");
    }
}

