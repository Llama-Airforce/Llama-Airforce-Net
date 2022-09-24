using System.Collections.Generic;
using System.Threading.Tasks;
using Llama.Airforce.Jobs.Factories;
using Microsoft.Extensions.Configuration;
using Nethereum.Web3;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.FactoryTests;

public class FlyerFactoryTests
{
    private readonly IConfiguration Configuration;

    public FlyerFactoryTests()
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<FlyerFactoryTests>()
            .AddEnvironmentVariables();

        Configuration = builder.Build();
    }

    [Test]
    public async Task CreateConvexFlyer()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);
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
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Act
        var flyer = FlyerFactory.CreateFlyerAura(web3);

        // Assert
        Assert.IsTrue(await flyer.IsRight);
    }
}