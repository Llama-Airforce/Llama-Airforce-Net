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
        x.Bind(y => Try(() => f(y))
            .ToAsync()
            .ToEither(ex => Error.New(ex.Message, ex)));
}
