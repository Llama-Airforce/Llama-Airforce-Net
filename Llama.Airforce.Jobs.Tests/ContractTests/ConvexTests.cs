using System.Threading.Tasks;
using Llama.Airforce.Jobs.Contracts;
using Nethereum.Web3;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.ContractTests;

public class ConvexTests
{
    [Test]
    public async Task GetBoostedSupply()
    {
        // Arrange
        var web3 = new Web3(Constants.ALCHEMY);

        // Act
        var boostedSupply = await Convex.GetBoostedSupply(web3);

        // Assert
    }

    [Test]
    public async Task GetCvxLockedupply()
    {
        // Arrange
        var web3 = new Web3(Constants.ALCHEMY);

        // Act
        var cvxLocked = await Convex.GetCvxLocked(web3);

        // Assert
    }

    [Test]
    public async Task GetRewardRate()
    {
        // Arrange
        var web3 = new Web3(Constants.ALCHEMY);

        // Act
        var rewardData = await Convex.GetRewardRate(web3);

        // Assert
    }

    [Test]
    public async Task GetLockedApr()
    {
        // Arrange
        var web3 = new Web3(Constants.ALCHEMY);

        // Act
        var apr = Convex.GetLockedApr(web3);

        // Assert
        Assert.IsTrue(await apr.IsRight);
    }

    [Test]
    public async Task GetCvxCrvApr()
    {
        // Arrange
        var web3 = new Web3(Constants.ALCHEMY);

        // Act
        var apr = Convex.GetCvxCrvApr(web3);

        // Assert
        Assert.IsTrue(await apr.IsRight);
    }

    [Test]
    public async Task GetLockedCrv()
    {
        // Arrange
        var web3 = new Web3(Constants.ALCHEMY);

        // Act
        var lockedCrv = Convex.GetLockedCrvUsd(web3);

        // Assert
        Assert.IsTrue(await lockedCrv.IsRight);
    }
}