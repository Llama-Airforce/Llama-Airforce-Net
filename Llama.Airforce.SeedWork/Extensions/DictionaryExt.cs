namespace Llama.Airforce.SeedWork.Extensions;

public static class DictionaryExt
{
    public static IReadOnlyDictionary<T, U> AsReadOnly<T, U>(this Dictionary<T, U> x)
        where T : notnull => x;
}
