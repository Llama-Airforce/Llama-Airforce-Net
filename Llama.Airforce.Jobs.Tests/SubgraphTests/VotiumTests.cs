using System.Net.Http;
using System.Threading.Tasks;
using Llama.Airforce.Jobs.Subgraphs;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.SubgraphTests;

public class VotiumTests
{
    [Test]
    public async Task GetEpochs()
    {
        // Arrange
        HttpClient http() => new();

        // Act
        var data = await Votium.GetEpochs(http)
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(data.All(x => !string.IsNullOrWhiteSpace(x.SnapshotId)));
    }
}