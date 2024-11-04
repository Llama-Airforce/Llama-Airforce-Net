using System;
using System.Numerics;
using System.Threading.Tasks;
using Llama.Airforce.Jobs.Contracts;
using Llama.Airforce.SeedWork.Types;
using Microsoft.Extensions.Configuration;
using Nethereum.Web3;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.ContractTests
{
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
            string startDate = "2024-11-02";
            string endDate = "2024-11-03";

            DateTime startDateTime = DateTime.Parse(startDate).ToUniversalTime();
            DateTime endDateTime = DateTime.Parse(endDate).ToUniversalTime();

            // Convert to Unix timestamp
            long startUnix = ((DateTimeOffset)startDateTime).ToUnixTimeSeconds();
            long endUnix = ((DateTimeOffset)endDateTime).ToUnixTimeSeconds();

            // Convert to BigInteger
            BigInteger startBigInt = new BigInteger(startUnix);
            BigInteger endBigInt = new BigInteger(endUnix);

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
            BigInteger tokensMinted = await mintableInTimeframeFunction.CallAsync<BigInteger>(startBigInt, endBigInt);

            // Assert
            Assert.IsTrue(tokensMinted >= 0, "Tokens minted should be non-negative");
        }
    }
}
