using System.Threading.Tasks;
using Llama.Airforce.Jobs.Contracts;
using Llama.Airforce.SeedWork.Types;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests;

public class CoinGeckoTests
{
    [Test]
    public async Task GetData()
    {
        // Arrange

        // Act
        var data = CoinGecko.GetData(Addresses.Convex.Token, Network.Ethereum);

        // Assert
        Assert.IsTrue(await data.IsRight);
    }

    [Test]
    public async Task GetMarketCap()
    {
        // Arrange
        var data = CoinGecko.GetData(Addresses.Convex.Token, Network.Ethereum);

        // Act
        var mcap = data.Bind(x => CoinGecko.GetMarketCap(x).ToAsync());

        // Assert
        Assert.IsTrue(await mcap.IsRight);
    }

    [Test]
    public async Task GetPrice()
    {
        // Arrange

        // Act
        var price = CoinGecko.GetPrice(Addresses.Convex.Token, Network.Ethereum, Currency.Usd);

        // Assert
        Assert.IsTrue(await price.IsRight);
    }

    [Test]
    public async Task GetPriceAtTime()
    {
        // Arrange

        // Act
        var price = await CoinGecko.GetPriceAtTime(
            Addresses.Convex.Token,
            Network.Ethereum,
            Currency.Usd,
            new System.DateTime(2021, 10, 19, 0, 1, 30, System.DateTimeKind.Utc))
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(price == 14.58121824457324);
    }
}