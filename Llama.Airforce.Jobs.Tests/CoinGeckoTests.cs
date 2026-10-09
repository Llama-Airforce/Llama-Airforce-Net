using System.Net.Http;
using System.Threading.Tasks;
using Llama.Airforce.Jobs.Contracts;
using Llama.Airforce.SeedWork.Types;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests;

public class CoinGeckoTests
{
    [Test]
    public async Task GetPriceAtTime()
    {
        // Arrange
        HttpClient http() => new();

        // Act
        var price = await CoinGecko.GetPriceAtTime(
            http,
            Addresses.Convex.Token,
            Network.Ethereum,
            Currency.Usd,
            new System.DateTime(2021, 10, 19, 0, 1, 30, System.DateTimeKind.Utc))
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(price == 14.58121824457324);
    }
}
