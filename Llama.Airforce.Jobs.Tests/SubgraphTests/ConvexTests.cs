using System.Threading.Tasks;
using Llama.Airforce.Jobs.Subgraphs;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.SubgraphTests;

public class ConvexTests
{
    [Test]
    public async Task GetPools()
    {
        // Arrange

        // Act
        var data = await Convex.GetPools()
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(data.Count > 20);
        Assert.IsTrue(data.All(x => !string.IsNullOrWhiteSpace(x.Name)));
        Assert.IsTrue(data.All(x => x.Tvl > 0));
    }

    [Test]
    public async Task GetSnapshots()
    {
        // Arrange

        // Act
        var data = await Convex.GetDailySnapshots("seth")
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(data.Count > 20);
    }
}