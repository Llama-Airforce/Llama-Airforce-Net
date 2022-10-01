using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using static LanguageExt.Prelude;

namespace Llama.Airforce.SeedWork.Types;

public sealed class Address : StringOfLength, IComparable<Address>
{
    private Address(string value)
        : base(value)
    {
    }

    public static Option<Address> Of(string value)
        => IsValid(value)
            ? Some(new Address(value.ToLowerInvariant()))
            : None;

    public static bool IsValid(string value)
        => !string.IsNullOrWhiteSpace(value)
            && value.Length == 42;

    public int CompareTo(Address other) => other.Value.CompareTo(this.Value);

    public static implicit operator Address(Option<Address> x) => x.ValueUnsafe();
}