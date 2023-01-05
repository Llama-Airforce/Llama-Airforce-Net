using System.Net.Http;
using System.Threading.Tasks;
using Llama.Airforce.Jobs.Snapshots;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.SnapshotTests;

public class AuraTests
{
    [Test]
    public async Task GetProposalIds()
    {
        // Arrange
        HttpClient http() => new();

        // Act
        var data = await Aura.GetProposalIds(http)
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(data.Count > 0);
    }
}