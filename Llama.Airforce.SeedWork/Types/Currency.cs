using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using static LanguageExt.Prelude;

namespace Llama.Airforce.SeedWork.Types;

public sealed class Currency : StringMax
{
    public static Currency Usd = Of("usd");

    private Currency(string value)
        : base(value)
    {
    }

    public static new Option<Currency> Of(string value)
        => IsValid(value)
            ? Some(new Currency(value.ToLower()))
            : None;

    public static new bool IsValid(string value) => StringMax.IsValid(value);

    public override string ToString() => this.Value;

    public static implicit operator Currency(Option<Currency> x) => x.ValueUnsafe();
}