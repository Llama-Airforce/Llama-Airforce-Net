using System.Threading.Tasks;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests;

public class AlchemyTests
{
    [Test]
    public async Task GetCurrentBlock()
    {
        // Arrange

        // Act
        var data = Alchemy.GetCurrentBlock(Constants.ALCHEMY);

        // Assert
        Assert.IsTrue(await data.IsRight);
    }
}