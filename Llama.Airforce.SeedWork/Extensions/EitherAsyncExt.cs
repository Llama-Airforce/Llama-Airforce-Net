using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Llama.Airforce.SeedWork.Extensions;

public static class EitherAsyncExt
{
    /// <summary>
    /// Applies a mapping function and wraps it in a Try
    /// </summary>
    public static EitherAsync<Error, Ret> MapTry<R, Ret>(this EitherAsync<Error, R> x, Func<R, Ret> f) =>
        x.MapTry(f, ex => ex.Message);

    /// <summary>
    /// Applies a mapping function and wraps it in a Try
    /// </summary>
    public static EitherAsync<Error, Ret> MapTry<R, Ret>(
            this EitherAsync<Error, R> x,
            Func<R, Ret> f,
            Func<Exception, string> onFail) =>
        x.Bind(y => Try(() => f(y))
            .ToAsync()
            .ToEither(ex => Error.New(onFail(ex), ex)));
}