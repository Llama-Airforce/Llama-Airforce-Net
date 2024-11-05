using System;
using System.Threading.Tasks;
using Llama.Airforce.Jobs.Contracts;
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

        // Act
        var tokensMinted = await Fxn.GetMintedTokensInTimeframe(
            web3,
            startDateTime,
            endDateTime);

        // Assert
        Assert.IsTrue(tokensMinted >= 0, "Tokens minted should be non-negative");
    }
}

