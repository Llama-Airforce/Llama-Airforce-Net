using System.Threading.Tasks;
using LanguageExt.UnsafeValueAccess;
using Llama.Airforce.Jobs.Contracts;
using Llama.Airforce.SeedWork.Types;
using Microsoft.Extensions.Configuration;
using Nethereum.Web3;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests.ContractTests;

public class ERC20Tests
{
    private readonly IConfiguration Configuration;

    public ERC20Tests()
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<ERC20Tests>()
            .AddEnvironmentVariables();

        Configuration = builder.Build();
    }

    [Test]
    public async Task GetSymbol()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Act
        var symbol = await ERC20.GetSymbol(web3, Address.Of("0xBC6DA0FE9aD5f3b0d58160288917AA56653660E9").ValueUnsafe());

        // Assert
        Assert.AreEqual("alUSD", symbol);
    }

    [Test]
    public async Task GetTotalSupply()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Act
        var totalSupply = await ERC20.GetTotalSupply(web3, Addresses.Convex.Token);

        // Assert
    }

    [Test]
    public async Task GetDecimals()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Act
        var decimals = await ERC20.GetDecimals(web3, Addresses.Convex.Token);

        // Assert
    }

    [Test]
    public async Task GetBalanceOf()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        var web3 = new Web3(alchemy);

        // Act
        var balanceOf = await ERC20.GetBalanceOf(
            web3,
            Addresses.Curve.VotingEscrow,
            Addresses.Convex.VoterProxy);

        // Assert
    }
}