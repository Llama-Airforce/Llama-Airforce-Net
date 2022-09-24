using System.Threading.Tasks;
using Llama.Airforce.Jobs.Subgraphs;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.SubgraphTests;

public class ConvexTests
{
    private readonly IConfiguration Configuration;

    public ConvexTests()
    {
        // the type specified here is just so the secrets library can 
        // find the UserSecretId we added in the csproj file
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<ConvexTests>()
            .AddEnvironmentVariables();

        Configuration = builder.Build();
    }

    [Test]
    public async Task GetPools()
    {
        // Arrange
        var graphUrl = Configuration["GRAPH_CONVEX"];

        // Act
        var data = await Convex.GetPools(graphUrl)
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
        var graphUrl = Configuration["GRAPH_CONVEX"];

        // Act
        var data = await Convex.GetDailySnapshots(graphUrl, "seth")
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(data.Count > 20);
    }
}