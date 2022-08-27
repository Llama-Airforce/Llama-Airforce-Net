using LanguageExt;
using System.Globalization;
using System.Numerics;
using static LanguageExt.Prelude;

namespace Llama.Airforce.SeedWork.Extensions;

public static class StringExt
{
    public static Try<int> ParseHexInt(this Func<string> f) => Try(() => int.Parse(
        f().Remove(0, 2),
        NumberStyles.HexNumber,
        CultureInfo.InvariantCulture));

    public static Try<double> ParseDouble(this Func<string> f) => Try(() => double.Parse(
        f(),
        CultureInfo.InvariantCulture));

    public static Try<double> ParseHexDouble(this Func<string> f) => Try(() => (double)BigInteger.Parse(
        f().Remove(0, 2).Insert(0, "0"),
        NumberStyles.HexNumber,
        CultureInfo.InvariantCulture));
}