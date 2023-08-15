using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Llama.Airforce.Jobs.Extensions;
using Llama.Airforce.Jobs.Factories;
using Llama.Airforce.Jobs.Functions;
using Llama.Airforce.SeedWork.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using NUnit.Framework;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Tests.FactoryTests;

public class BribeFactoryV2Tests
{
    private readonly IConfiguration Configuration;

    public BribeFactoryV2Tests()
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<BribeFactoryV2Tests>()
            .AddEnvironmentVariables();

        Configuration = builder.Build();
    }

    [Test]
    [TestCase(275218.498948121, 4073133.7953767194, 7405670.9515354205)]
    public async Task ProcessEpoch(
        double bribes,
        double bribed,
        double scoresTotal)
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);
        HttpClient http() => new();
        var logger = new LoggerFactory().CreateLogger("test");
        var getPrice = fun((long proposalEnd, Address tokenAddress, string token) =>
        {
            // Try to get the price at the end of the day of the deadline.
            // If that fails, try to get a fallback price.
            // If the proposal ends later than now (ongoing proposal), we take current prices.
            var time = DateTimeExt.FromUnixTimeSeconds(proposalEnd);
            time = time > DateTime.UtcNow ? DateTime.UtcNow : time;

            return PriceFunctions.GetPriceExt(
                http,
                tokenAddress,
                PriceFunctions.GetNetwork(token),
                web3,
                time,
                token);
        });

        // Act
        var bribeFunctions = BribesV2Factory.GetBribesFunctions(http);

        var proposalIds = await bribeFunctions.GetProposalIds()
            .MatchAsync(x => x, _ => throw new Exception());

        var epochs = await bribeFunctions.GetEpochs()
            .MatchAsync(x => x, _ => throw new Exception());

        var gauges = await bribeFunctions.GetGauges()
            .MatchAsync(x => x, _ => throw new Exception());

        var epoch = epochs.First();
        var dbEpoch = await BribesV2Factory.ProcessEpoch(
                logger,
                web3,
                new BribesV2Factory.OptionsProcessEpoch(
                    bribeFunctions,
                    proposalIds,
                    epoch,
                    gauges,
                    0),
                getPrice)
            .MatchAsync(x => x, _ => throw new Exception());

        // Assert
        Assert.AreEqual(bribes, dbEpoch.Bribes.Sum(bribe => bribe.AmountDollars));
        Assert.AreEqual(bribed, dbEpoch.Bribed.Sum(x => x.Value));
        Assert.AreEqual(scoresTotal, dbEpoch.ScoresTotal);
    }
}