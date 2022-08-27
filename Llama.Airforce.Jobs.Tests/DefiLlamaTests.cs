using Llama.Airforce.Jobs.Contracts;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Llama.Airforce.Jobs.Tests;

public class DefiLlamaTests
{
    [Test]
    public async Task GetPrice()
    {
        // Arrange

        // Act
        var price = await DefiLlama.GetPrice(
            Addresses.Convex.Token,
            Network.Ethereum,
            new System.DateTime(2021, 10, 19, 0, 1, 30, System.DateTimeKind.Utc))
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(price == 14.58121824457324);
    }
}