using LanguageExt;
using LanguageExt.Common;

namespace Llama.Airforce.SeedWork.Extensions;

public static class LstExt
{
    public static EitherAsync<Error, Lst<R>> ToEitherAsync<R>(this Lst<R> x) =>
        EitherAsync<Error, Lst<R>>.Right(x);
}