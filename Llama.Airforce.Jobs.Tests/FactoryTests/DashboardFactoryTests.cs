﻿using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using LanguageExt;
using Llama.Airforce.Database.Models.Bribes;
using Llama.Airforce.Jobs.Factories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.FactoryTests;

public class DashboardFactoryTests
{
    private readonly IConfiguration Configuration;

    public DashboardFactoryTests()
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<DashboardFactoryTests>()
            .AddEnvironmentVariables();

        Configuration = builder.Build();
    }

    [Test]
    public async Task CreateVotiumOverview()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);
        HttpClient http() => new();
        var logger = new LoggerFactory().CreateLogger("test");
        var epochs = Lst<EpochV2>.Empty;
        var latestFinishedEpoch = new EpochV2
        {
            Bribes = new List<BribeV2> { new() { Amount = 0.44}},
            Bribed = new Dictionary<string, double> { { "foo", 1 } },
            ScoresTotal = 1
        };

        var dataV1 = new DashboardFactory.VotiumDataV1(
            new Lst<Epoch>());

        var dataV2 = new DashboardFactory.VotiumDataV2(
            epochs,
            latestFinishedEpoch);

        // Act
        var overview = await DashboardFactory.CreateOverviewVotium(logger, web3, http, dataV1, dataV2)
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(overview.RewardPerDollarBribe > 0);
    }

    [Test]
    public async Task CreateVotiumOverview_FailsWithZeroScoresTotal()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);
        HttpClient http() => new();
        var logger = new LoggerFactory().CreateLogger("test");
        var epochs = Lst<EpochV2>.Empty;
        var latestFinishedEpoch = new EpochV2
        {
            Bribes = new List<BribeV2> { new() { Amount = 0.44 } },
            Bribed = new Dictionary<string, double> { { "foo", 1 } },
            ScoresTotal = 0
        };

        var dataV1 = new DashboardFactory.VotiumDataV1(
            new Lst<Epoch>());

        var dataV2 = new DashboardFactory.VotiumDataV2(
            epochs,
            latestFinishedEpoch);

        // Act
        var overview = await DashboardFactory.CreateOverviewVotium(logger, web3, http, dataV1, dataV2);

        // Assert
        Assert.IsTrue(overview.IsLeft && overview.LeftToList().First().Message == "Total scores is zero");
    }
}