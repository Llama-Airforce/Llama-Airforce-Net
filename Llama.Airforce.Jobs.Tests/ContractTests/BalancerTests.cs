using System.Threading.Tasks;
using Llama.Airforce.Jobs.Contracts;
using Nethereum.Web3;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.ContractTests;

public class BalancerTests
{
    [Test]
    public async Task GetTotalWeight()
    {
        // Arrange
        var web3 = new Web3(Constants.ALCHEMY);

        // Act
        var totalWeight = await Balancer.GetTotalWeight(web3);

        // Assert
    }

    [Test]
    public async Task GetVotingPower()
    {
        // Arrange
        var web3 = new Web3(Constants.ALCHEMY);

        // Act
        var votingPower = await Balancer.GetVotingPower(web3, Addresses.Aura.VoterProxy);

        // Assert
    }

    [Test]
    public async Task GetRate()
    {
        // Arrange
        var web3 = new Web3(Constants.ALCHEMY);

        // Act
        var rate = await Balancer.GetRate(web3);

        // Assert
    }

    [Test]
    public async Task GetDiscountAuraBal()
    {
        // Arrange
        var web3 = new Web3(Constants.ALCHEMY);

        // Act
        var rate = await Balancer.GetDiscountAuraBal(web3);

        // Assert
    }
}