using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Llama.Airforce.Jobs.Factories;
using Microsoft.Extensions.Configuration;
using Nethereum.Web3;
using NUnit.Framework;
using static LanguageExt.Prelude;

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
        HttpClient http() => new();
        var pools = LanguageExt.List.empty<Database.Models.Convex.Pool>();
        var latestFinishedEpoch = new Database.Models.Bribes.EpochV2
        {
            Bribes = new List<Database.Models.Bribes.BribeV2>(),
            Bribed = new Dictionary<string, double>()
        };

        // Act
        var flyer = FlyerFactory.CreateFlyerConvex(web3, http, pools, List(latestFinishedEpoch));

        // Assert
        Assert.IsTrue(await flyer.IsRight);
    }

    [Test]
    public async Task CreateAuraFlyer()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);
        HttpClient http() => new();

        // Act
        var flyer = FlyerFactory.CreateFlyerAura(web3, http);

        // Assert
        Assert.IsTrue(await flyer.IsRight);
    }
}