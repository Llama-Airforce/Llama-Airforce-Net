using System.Net.Http;
using System.Threading.Tasks;
using LanguageExt.UnsafeValueAccess;
using Llama.Airforce.SeedWork.Types;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests;

public class CurveApiTests
{
    [Test]
    public async Task GetGauges()
    {
        // Arrange
        HttpClient http() => new();

        // Act
        var gauges = await CurveApi
           .GetGauges(http)
           .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.IsTrue(gauges
           .Find(Address.Of("0x4792b8845e4d7e18e104b535d81b6904d72915a4")
               .ValueUnsafe())
           .ValueUnsafe().ShortName == "MIM+FRAXBP (0xb3bC…)");
    }
}