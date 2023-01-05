using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests;

public class AlchemyTests
{
    private readonly IConfiguration Configuration;

    public AlchemyTests()
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<AlchemyTests>()
            .AddEnvironmentVariables();

        Configuration = builder.Build();
    }

    [Test]
    public async Task GetCurrentBlock()
    {
        // Arrange
        var alchemy = Configuration["ALCHEMY"];
        HttpClient http() => new();

        // Act
        var data = Alchemy.GetCurrentBlock(http, alchemy);

        // Assert
        Assert.IsTrue(await data.IsRight);
    }
}