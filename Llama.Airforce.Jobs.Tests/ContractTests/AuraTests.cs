using System.Threading.Tasks;
using Llama.Airforce.Jobs.Contracts;
using Nethereum.Web3;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.ContractTests;

public class AuraTests
{
    [Test]
    public async Task GetAuraMintAmount()
    {
        // Arrange
        var web3 = new Web3(Constants.ALCHEMY);

        // Act
        var auraMinted = await Aura.GetAuraMintAmount(web3, 1);

        // Assert
        Assert.IsTrue(auraMinted > 0);
    }
}