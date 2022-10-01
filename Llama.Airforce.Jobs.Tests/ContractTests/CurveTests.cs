using System.Threading.Tasks;
using Llama.Airforce.Jobs.Contracts;
using Llama.Airforce.SeedWork.Types;
using Microsoft.Extensions.Configuration;
using Nethereum.Web3;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.ContractTests;

public class CurveTests
{
    private readonly IConfiguration Configuration;

    public CurveTests()
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<ConvexTests>()
            .AddEnvironmentVariables();

        Configuration = builder.Build();
    }

    [Test]
    public async Task GetRewardRate()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Act
        var boostedSupply = await Curve.GetRewardRate(web3, Addresses.Curve.Staked);

        // Assert
    }

    [Test]
    public async Task GetVirtualPrice()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Act
        var rewardData = await Curve.GetVirtualPrice(web3, Addresses.Curve.CurveSwap);

        // Assert
    }

    [Test]
    public async Task GetTotalWeight()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Act
        var totalWeight = await Curve.GetTotalWeight(web3);

        // Assert
    }

    [Test]
    public async Task GetVotingPower()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Act
        var votingPower = await Curve.GetVotingPower(web3, Addresses.Convex.VoterProxy);

        // Assert
    }

    [Test]
    public async Task GetRate()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Act
        var rate = await Curve.GetRate(web3);

        // Assert
    }

    [Test]
    public async Task GetPriceOracle()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);
        var teth = Address.Of("0x752eBeb79963cf0732E9c0fec72a49FD1DEfAEAC");

        // Act
        var price = await Curve.GetPriceOracle(web3, teth);

        // Assert
    }
}