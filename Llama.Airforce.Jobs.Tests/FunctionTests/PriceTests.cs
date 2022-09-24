using System.Threading.Tasks;
using Llama.Airforce.Jobs.Contracts;
using Llama.Airforce.Jobs.Functions;
using Microsoft.Extensions.Configuration;
using Nethereum.Web3;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.FunctionTests;

public class PriceTests
{
    private readonly IConfiguration Configuration;

    public PriceTests()
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<PriceTests>()
            .AddEnvironmentVariables();

        Configuration = builder.Build();
    }

    [Test]
    public async Task GetPriceT()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Act
        var price = await PriceFunctions.GetCurveV2Price(web3, Addresses.ERC20.T)
            .MatchAsync(x => x, _ => throw new System.Exception()); ;

        // Assert
        Assert.IsTrue(price > 0.01);
    }

    [Test]
    public async Task GetPriceAuraBal()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Act
        var price = await PriceFunctions.GetAuraBalPrice(web3)
            .MatchAsync(x => x, _ => throw new System.Exception()); ;

        // Assert
        Assert.IsTrue(price > 0.01);
    }
}