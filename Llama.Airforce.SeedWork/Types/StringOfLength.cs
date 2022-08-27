namespace Llama.Airforce.SeedWork.Types;

public abstract class StringOfLength : ValueObject
{
    protected StringOfLength(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A string of length cannot be null or empty.");

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => this.Value;

    public static implicit operator string(StringOfLength s) => s.Value;

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }
}