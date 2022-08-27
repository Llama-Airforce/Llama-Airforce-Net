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
        

        // Act
        var data = await Aura.GetProposalIds()
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(data.Count > 0);
    }
}