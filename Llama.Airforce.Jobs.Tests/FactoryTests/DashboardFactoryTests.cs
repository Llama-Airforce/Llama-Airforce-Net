using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageExt;
using Llama.Airforce.Database.Models.Bribes;
using Llama.Airforce.Jobs.Factories;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.FactoryTests;

public class DashboardFactoryTests
{
    [Test]
    public async Task CreateVotiumOverview()
    {
        // Arrange
        var web3 = new Web3(Constants.ALCHEMY);
        var logger = new LoggerFactory().CreateLogger("test");
        var epochs = Lst<Epoch>.Empty;
        var latestFinishedEpoch = new Epoch
        {
            Bribes = new List<Bribe> { new() { Amount = 0.44}},
            Bribed = new Dictionary<string, double> { { "foo", 1 } }
        };

        var data = new DashboardFactory.VotiumData(
            epochs,
            latestFinishedEpoch);

        // Act
        var overview = await DashboardFactory.CreateOverviewVotium(logger, web3, data)
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(overview.RewardPerDollarBribe > 0);
    }
}