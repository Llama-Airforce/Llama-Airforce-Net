using LanguageExt;
using LanguageExt.Common;

namespace Llama.Airforce.SeedWork.Extensions;

public static class TaskExt
{
    public static EitherAsync<Error, R> ToEitherAsync<R>(this Task<R> x) =>
        EitherAsync<Error, R>.RightAsync(x);
}