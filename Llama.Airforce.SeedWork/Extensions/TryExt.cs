using LanguageExt;
using static LanguageExt.Prelude;

namespace Llama.Airforce.SeedWork.Extensions;

public static class TryExt
{
    public static T ValueOr<T>(this Try<T> x, T valueOr) => x.Match(y => y, _ => valueOr);

    public static Try<T> BindFail<T>(this Try<T> x, Func<Exception, Try<T>> f) => x.BiBind(
        Succ: Try,
        Fail: f);
}