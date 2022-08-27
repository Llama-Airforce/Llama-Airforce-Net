using System.Numerics;
using LanguageExt;
using Nethereum.Util;

namespace Llama.Airforce.Jobs.Extensions;

public static class BigIntegerExt
{
    public static double DivideByDecimals(this BigInteger x, int decimals)
    {
        if (decimals < 0)
            throw new ArgumentException("decimals cannot be negative", nameof(decimals));

        return (double)(new BigDecimal(x) / new BigDecimal(Math.Pow(10, decimals)));
    }

    public static double DivideByDecimals(this BigInteger x, BigInteger decimals) =>
        x.DivideByDecimals((int)decimals);

    public static Task<double> DivideByDecimals(this Task<BigInteger> x, int decimals) =>
        x.Map(y => y.DivideByDecimals(decimals));

    public static Task<double> DivideByDecimals(this Task<BigInteger> x, BigInteger decimals) =>
        x.DivideByDecimals((int)decimals);
}