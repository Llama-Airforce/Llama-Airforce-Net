using LanguageExt;

namespace Llama.Airforce.SeedWork.Extensions;

public static class IEnumerableExt
{
    public static Lst<T> toList<T>(this IEnumerable<T> xs) => Prelude.toList(xs);
}