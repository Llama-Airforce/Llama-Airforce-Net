﻿using System.Net.Http;
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
        HttpClient http() => new();

        // Act
        var data = await Convex.GetPools(http, graphUrl)
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
        HttpClient http() => new();

        // Act
        var data = await Convex.GetDailySnapshots(http, graphUrl, "seth")
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(data.Count > 20);
    }
}