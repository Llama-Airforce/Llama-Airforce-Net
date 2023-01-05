using System;
using System.Net.Http;
using System.Threading.Tasks;
using Llama.Airforce.Jobs.Contracts;
using Microsoft.Extensions.Configuration;
using Nethereum.Web3;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.ContractTests;

public class ConvexTests
{
    private readonly IConfiguration Configuration;

    public ConvexTests()
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<ConvexTests>()
            .AddEnvironmentVariables();

        Configuration = builder.Build();
    }

    [Test]
    public async Task GetBoostedSupply()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Act
        var boostedSupply = await Convex.GetBoostedSupply(web3);

        // Assert
    }

    [Test]
    public async Task GetCvxLockedupply()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Act
        var cvxLocked = await Convex.GetCvxLocked(web3);

        // Assert
    }

    [Test]
    public async Task GetRewardRate()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Act
        var rewardData = await Convex.GetRewardRate(web3);

        // Assert
    }

    [Test]
    public async Task GetLockedApr()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);
        HttpClient http() => new();

        // Act
        var apr = Convex.GetLockedApr(http, web3);

        // Assert
        Assert.IsTrue(await apr.IsRight);
    }

    [Test]
    public async Task GetCvxCrvApr()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);
        HttpClient http() => new();

        // Act
        var apr = Convex.GetCvxCrvApr(http, web3);

        // Assert
        Assert.IsTrue(await apr.IsRight);
    }

    [Test]
    public async Task GetLockedCrv()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);
        HttpClient http() => new();

        // Act
        var lockedCrv = Convex.GetLockedCrvUsd(http, web3);

        // Assert
        Assert.IsTrue(await lockedCrv.IsRight);
    }
}