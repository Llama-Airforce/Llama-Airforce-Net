using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Llama.Airforce.Jobs.Subgraphs;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.SubgraphTests;

public class HiddenHandTests
{
    [Test]
    public async Task GetEpochsV1()
    {
        // Arrange
        HttpClient http() => new();
        const int version = 1;

        // Act
        var data = await HiddenHand.GetEpochs(
                http,
                version,
                new List<(int, string)>
                {
                    (1, "0xabaf9275ae0533ce991059e8b5664225bf54bae81b9305ae60b48198db180ad9")
                })
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(data.All(x => !string.IsNullOrWhiteSpace(x.SnapshotId)));
    }

    [Test]
    public async Task GetEpochsV2()
    {
        // Arrange
        HttpClient http() => new();
        const int version = 2;

        // Act
        var data = await HiddenHand.GetEpochs(
                http,
                version,
                new List<(int, string)>
                {
                    (0, "0x12ceba0ede49feef56e9f3690869536944618da9a0da3a726e2db089440dacf1")
                })
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(data.All(x => !string.IsNullOrWhiteSpace(x.SnapshotId)));
    }
}