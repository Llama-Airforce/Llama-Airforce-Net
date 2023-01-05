using System.Net.Http;
using System.Threading.Tasks;
using Llama.Airforce.Jobs.Snapshots;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.SnapshotTests;

public class SnapshotTests
{
    [Test]
    public async Task GetProposal()
    {
        // Arrange
        var id = "QmaS9vd1vJKQNBYX4KWQ3nppsTT3QSL3nkz5ZYSwEJk6hZ";
        HttpClient http() => new();

        // Act
        var data = await Snapshot.GetProposal(http, id)
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(data.Choices.Count == 52);
        Assert.IsTrue(data.Start == 1634169673);
        Assert.IsTrue(data.End == 1634601673);
    }

    [Test]
    public async Task GetNumChoices()
    {
        // Arrange
        var id = "0xabaf9275ae0533ce991059e8b5664225bf54bae81b9305ae60b48198db180ad9";
        HttpClient http() => new();

        // Act
        var numChoices = await Snapshot.GetNumChoices(http, id)
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.AreEqual(79, numChoices);
    }

    [Test]
    public async Task GetVotes()
    {
        // Arrange
        var id = "QmaS9vd1vJKQNBYX4KWQ3nppsTT3QSL3nkz5ZYSwEJk6hZ";
        HttpClient http() => new();

        // Act
        var votes = await Snapshot.GetVotes(http, id)
            .MatchAsync(x => x, _ => throw new System.Exception());
        var votium = votes.First();

        // Assert
        Assert.IsTrue(votes.Count > 10);
        Assert.AreEqual("QmR1BM9aQwvgLtv74priD62q8JgbB5N2h7o65HL9MoRMXL", votium.Id);
        Assert.AreEqual("0xde1E6A7ED0ad3F61D531a8a78E83CcDdbd6E0c49", votium.Voter);

        Assert.AreEqual(12, votium.Choices.Count);
        Assert.AreEqual(430, votium.Choices["1"]);
        Assert.AreEqual(47, votium.Choices["7"]);
        Assert.AreEqual(2174, votium.Choices["15"]);
        Assert.AreEqual(33, votium.Choices["17"]);
        Assert.AreEqual(90, votium.Choices["23"]);
        Assert.AreEqual(3550, votium.Choices["33"]);
        Assert.AreEqual(1040, votium.Choices["37"]);
        Assert.AreEqual(213, votium.Choices["41"]);
        Assert.AreEqual(1007, votium.Choices["42"]);
        Assert.AreEqual(1048, votium.Choices["49"]);
        Assert.AreEqual(15, votium.Choices["51"]);
        Assert.AreEqual(351, votium.Choices["52"]);
    }
}