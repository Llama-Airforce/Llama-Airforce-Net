using System.Net.Http;
using System.Threading.Tasks;
using Llama.Airforce.Jobs.Contracts;
using Llama.Airforce.Jobs.Functions;
using Microsoft.Extensions.Configuration;
using Nethereum.Web3;
using NUnit.Framework;
using static LanguageExt.Prelude;

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
    public async Task GetPricesdFXS()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);
        HttpClient http() => new();

        // Act
        var price = await PriceFunctions.GetCurveV1Price(http, web3, Addresses.ERC20.sdFXS, Addresses.ERC20.FXS, true)
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(price > 0.01);
    }

    [Test]
    public async Task GetPriceT()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);
        HttpClient http() => new();

        // Act
        var price = await PriceFunctions.GetCurveV2Price(http, web3, Addresses.ERC20.T, None)
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(price > 0.01);
    }

    [Test]
    public async Task GetPriceAuraBal()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);
        HttpClient http() => new();

        // Act
        var price = await PriceFunctions.GetAuraBalPrice(http, web3)
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(price > 0.01);
    }
}