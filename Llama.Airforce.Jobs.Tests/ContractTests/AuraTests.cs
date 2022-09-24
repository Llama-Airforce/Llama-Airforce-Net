using System.Threading.Tasks;
using Llama.Airforce.Jobs.Contracts;
using Microsoft.Extensions.Configuration;
using Nethereum.Web3;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.ContractTests;

public class AuraTests
{
    private readonly IConfiguration Configuration;

    public AuraTests()
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<AuraTests>()
            .AddEnvironmentVariables();

        Configuration = builder.Build();
    }

    [Test]
    public async Task GetAuraMintAmount()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Act
        var auraMinted = await Aura.GetAuraMintAmount(web3, 1);

        // Assert
        Assert.IsTrue(auraMinted > 0);
    }
}