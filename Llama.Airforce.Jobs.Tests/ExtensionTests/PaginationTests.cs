using System.Threading.Tasks;
using Llama.Airforce.Jobs.Extensions;
using Llama.Airforce.SeedWork.Extensions;
using NUnit.Framework;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Tests.ExtensionTests;

public class PaginationTests
{
    [Test]
    public async Task DoPagination()
    {
        // Arrange
        var f = fun((int page, int offset) => page switch
        {
            0 => List(1, 2).ToEitherAsync(),
            1 => List(3, 4).ToEitherAsync(),
            _ => List(5).ToEitherAsync()
        });

        var expected = List(1, 2, 3, 4, 5);

        // Act
        var xs = await f.Paginate(0, 2)
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.AreEqual(expected, xs);
    }

    [Test]
    public async Task DoPagination2()
    {
        // Arrange
        var f = fun((int page, int offset) => page switch
        {
            0 => List(1, 2, 3).ToEitherAsync(),
            1 => List(4, 5).ToEitherAsync(),
            _ => LanguageExt.List.empty<int>().ToEitherAsync()
        });

        var expected = List(1, 2, 3, 4, 5);

        // Act
        var xs = await f.Paginate(0, 2)
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.AreEqual(expected, xs);
    }

    [Test]
    public async Task DoPaginationEmpty()
    {
        // Arrange
        var f = fun((int page, int offset) => LanguageExt.List.empty<int>().ToEitherAsync());

        var expected = LanguageExt.List.empty<int>();

        // Act
        var xs = await f.Paginate(0, 2)
            .MatchAsync(x => x, _ => throw new System.Exception());

        // Assert
        Assert.AreEqual(expected, xs);
    }
}