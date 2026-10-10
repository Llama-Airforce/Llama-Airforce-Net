namespace Llama.Airforce.SeedWork.Extensions;

public static class FuncExt
{
    public static Func<R> Par<T, R>(this Func<T, R> func, T t) => () => func(t);

    public static Func<T2, R> Par<T1, T2, R>(this Func<T1, T2, R> func, T1 t1)
        => t2 => func(t1, t2);

    public static Func<T2, T3, R> Par<T1, T2, T3, R>(this Func<T1, T2, T3, R> func, T1 t1)
        => (t2, t3) => func(t1, t2, t3);
}
