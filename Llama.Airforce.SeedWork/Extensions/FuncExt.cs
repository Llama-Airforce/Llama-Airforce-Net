namespace Llama.Airforce.SeedWork.Extensions;

public static class FuncExt
{
    public static Func<R> Par<T, R>(this Func<T, R> func, T t) => () => func(t);

    public static Func<T2, R> Par<T1, T2, R>(this Func<T1, T2, R> func, T1 t1)
        => t2 => func(t1, t2);

    public static Func<T2, T3, R> Par<T1, T2, T3, R>(this Func<T1, T2, T3, R> func, T1 t1)
        => (t2, t3) => func(t1, t2, t3);

    public static Func<T2, T3, T4, R> Par<T1, T2, T3, T4, R>(this Func<T1, T2, T3, T4, R> func, T1 t1)
        => (t2, t3, t4) => func(t1, t2, t3, t4);

    public static Func<T2, T3, T4, T5, R> Par<T1, T2, T3, T4, T5, R>(this Func<T1, T2, T3, T4, T5, R> func, T1 t1)
        => (t2, t3, t4, t5) => func(t1, t2, t3, t4, t5);

    public static Func<T2, T3, T4, T5, T6, R> Par<T1, T2, T3, T4, T5, T6, R>(this Func<T1, T2, T3, T4, T5, T6, R> func,
        T1 t1)
        => (t2, t3, t4, t5, t6) => func(t1, t2, t3, t4, t5, t6);

    public static Func<T2, T3, T4, T5, T6, T7, R> Par<T1, T2, T3, T4, T5, T6, T7, R>(
        this Func<T1, T2, T3, T4, T5, T6, T7, R> func, T1 t1)
        => (t2, t3, t4, t5, t6, t7) => func(t1, t2, t3, t4, t5, t6, t7);

    public static Func<T2, T3, T4, T5, T6, T7, T8, R> Par<T1, T2, T3, T4, T5, T6, T7, T8, R>(
        this Func<T1, T2, T3, T4, T5, T6, T7, T8, R> func, T1 t1)
        => (t2, t3, t4, t5, t6, t7, t8) => func(t1, t2, t3, t4, t5, t6, t7, t8);

    public static Func<T2, T3, T4, T5, T6, T7, T8, T9, R> Par<T1, T2, T3, T4, T5, T6, T7, T8, T9, R>(
        this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, R> func, T1 t1)
        => (t2, t3, t4, t5, t6, t7, t8, t9) => func(t1, t2, t3, t4, t5, t6, t7, t8, t9);

    public static Func<T2, T3, T4, T5, T6, T7, T8, T9, T10, R> Par<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, R>(
        this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, R> func, T1 t1)
        => (t2, t3, t4, t5, t6, t7, t8, t9, t10) => func(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);

    public static Func<T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, R>
        Par<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, R>(
            this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, R> func, T1 t1)
        => (t2, t3, t4, t5, t6, t7, t8, t9, t10, t11) => func(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);

    public static Func<T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, R> Par<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10,
        T11, T12, R>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, R> func, T1 t1)
        => (t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12) => func(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);

    public static Func<T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, R> Par<T1, T2, T3, T4, T5, T6, T7, T8, T9,
        T10, T11, T12, T13, R>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, R> func, T1 t1)
        => (t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13) =>
            func(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);

    public static R Pipe<T, R>(this T @this, Func<T, R> f) => f(@this);
}
