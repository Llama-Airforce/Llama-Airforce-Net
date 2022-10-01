using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using static LanguageExt.Prelude;

namespace Llama.Airforce.SeedWork.Types;

public class StringMax : StringOfLength
{
    protected StringMax(string value) : base(value)
    {

    }

    public static Option<StringMax> Of(string value)
        => IsValid(value)
            ? Some(new StringMax(value))
            : None;

    public static bool IsValid(string value)
        => !string.IsNullOrWhiteSpace(value);

    public static implicit operator StringMax(Option<StringMax> x) => x.ValueUnsafe();
}