using System.Net.Http;
using System.Threading.Tasks;
using Llama.Airforce.Jobs.Snapshots;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.SnapshotTests;

public class AuraTests
{
    [Test]
    public async Task GetProposalIdsV1()
    {
        // Arrange
        HttpClient http() => new();
        const int version = 1;

        // Act
        var data = await Aura.GetProposalIds(http, version)
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(data.Count > 0);
    }

    [Test]
    public async Task GetProposalIdsV2()
    {
        // Arrange
        HttpClient http() => new();
        const int version = 2;

        // Act
        var data = await Aura.GetProposalIds(http, version)
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(data.Count > 0);
    }
}