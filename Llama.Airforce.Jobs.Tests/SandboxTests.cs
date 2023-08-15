using System.Numerics;
using System.Threading.Tasks;
using Llama.Airforce.Jobs.Extensions;
using Nethereum.ABI;
using Nethereum.Hex.HexConvertors.Extensions;
using NUnit.Framework;

namespace Llama.Airforce.Jobs.Tests;

public class SandboxTests
{
    [Test]
    public async Task Sandbox()
    {
        // Arrange

        // Act
        var x = new ABIEncode()
            .GetSha3ABIEncodedPacked(3, 0)
            .ToHex()
            .Insert(0, "0x");

        var y = BigInteger.Parse("0").DivideByDecimals(18);
        ;

        // Assert
    }
}