using System.Collections.Generic;
using System.Threading.Tasks;
using Llama.Airforce.Jobs.Factories;
using Nethereum.Web3;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.FactoryTests;

public class FlyerFactoryTests
{
    [Test]
    public async Task CreateConvexFlyer()
    {
        // Arrange
        var web3 = new Web3(Constants.ALCHEMY);
        var pools = LanguageExt.List.empty<Database.Models.Convex.Pool>();
        var latestFinishedEpoch = new Database.Models.Bribes.Epoch
        {
            Bribes = new List<Database.Models.Bribes.Bribe>(),
            Bribed = new Dictionary<string, double>()
        };

        // Act
        var flyer = FlyerFactory.CreateFlyerConvex(web3, pools, latestFinishedEpoch);

        // Assert
        Assert.IsTrue(await flyer.IsRight);
    }

    [Test]
    public async Task CreateAuraFlyer()
    {
        // Arrange
        var web3 = new Web3(Constants.ALCHEMY);

        // Act
        var flyer = FlyerFactory.CreateFlyerAura(web3);

        // Assert
        Assert.IsTrue(await flyer.IsRight);
    }
}