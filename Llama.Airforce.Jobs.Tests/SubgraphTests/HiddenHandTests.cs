using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Llama.Airforce.Jobs.Subgraphs;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.SubgraphTests;

public class HiddenHandTests
{
    [Test]
    public async Task GetEpochs()
    {
        // Arrange
        HttpClient http() => new();

        // Act
        var data = await HiddenHand.GetEpochs(http,new List<(int, string)> { (1, "0xabaf9275ae0533ce991059e8b5664225bf54bae81b9305ae60b48198db180ad9") })
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(data.All(x => !string.IsNullOrWhiteSpace(x.SnapshotId)));
    }
}