using System.Numerics;
using System.Threading.Tasks;
using Llama.Airforce.Jobs.Contracts;
using Microsoft.Extensions.Configuration;
using Nethereum.Web3;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.ContractTests;

public class ConvexL2VotingTests
{
    private readonly IConfiguration Configuration;

    public ConvexL2VotingTests()
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<ConvexL2VotingTests>()
            .AddEnvironmentVariables();

        Configuration = builder.Build();
    }

    [Test]
    public async Task GetGaugeTotal()
    {
        // Arrange
        var web3 = new Web3("https://zkevm-rpc.com");

        // Act
        var gaugeTotal = await ConvexL2Voting.GaugeTotal(web3, 3, "0xc2075702490F0426E84E00d8B328119027813AC5");

        // Assert
        Assert.IsTrue(gaugeTotal == BigInteger.Parse("3328514392483745005228779"));
    }

    [Test]
    public async Task GetProposal()
    {
        // Arrange
        var web3 = new Web3("https://zkevm-rpc.com");

        // Act
        var proposal = await ConvexL2Voting.GetProposal(web3, 3);

        // Assert
        Assert.IsTrue(proposal.StartTime == BigInteger.Parse("1692230459"));
        Assert.IsTrue(proposal.EndTime == BigInteger.Parse("1692662459"));
    }
}