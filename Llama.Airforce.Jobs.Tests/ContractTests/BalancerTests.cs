using System.Threading.Tasks;
using Llama.Airforce.Jobs.Contracts;
using Microsoft.Extensions.Configuration;
using Nethereum.Web3;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.ContractTests;

public class BalancerTests
{
    private readonly IConfiguration Configuration;

    public BalancerTests()
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<BalancerTests>()
            .AddEnvironmentVariables();

        Configuration = builder.Build();
    }

    [Test]
    public async Task GetTotalWeight()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Act
        var totalWeight = await Balancer.GetTotalWeight(web3);

        // Assert
    }

    [Test]
    public async Task GetVotingPower()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Act
        var votingPower = await Balancer.GetVotingPower(web3, Addresses.Aura.VoterProxy);

        // Assert
    }

    [Test]
    public async Task GetRate()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Act
        var rate = await Balancer.GetRate(web3);

        // Assert
    }

    [Test]
    public async Task GetDiscountAuraBal()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Act
        var rate = await Balancer.GetDiscountAuraBal(web3);

        // Assert
    }
}