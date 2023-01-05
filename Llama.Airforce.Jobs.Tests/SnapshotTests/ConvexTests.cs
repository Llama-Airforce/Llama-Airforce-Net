using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
using LanguageExt.UnsafeValueAccess;
using Llama.Airforce.Jobs.Snapshots;
using Llama.Airforce.SeedWork.Types;
using NUnit.Framework;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Tests.SnapshotTests;

public class ConvexTests
{
    [Test]
    public async Task GetProposalIds()
    {
        // Arrange
        HttpClient http() => new();

        // Act
        var data = await Convex.GetProposalIds(http)
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(data.Count > 10);
    }

    [Test]
    public async Task GetScores()
    {
        // Arrange
        var address = Address.Of("0xde1e6a7ed0ad3f61d531a8a78e83ccddbd6e0c49").ValueUnsafe();
        var block = new BigInteger(13413053);
        HttpClient http() => new();

        // Act
        var scores = await Convex.GetScores(http, List(address), block)
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.AreEqual(1, scores.Length);
        Assert.AreEqual(6014304.97940906, scores.Find(address).ValueUnsafe());
    }
}