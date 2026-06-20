using Lexify.Domain.Common;

namespace Lexify.Domain.ValueObjects;

public sealed class TestScore : IEquatable<TestScore>
{
    public double Value { get; }
    public int Correct { get; }
    public int Total { get; }

    public double Percentage => Value * 100;
    public bool Passed => Value >= 0.6;

    private TestScore(double value, int correct, int total)
    {
        Value = value;
        Correct = correct;
        Total = total;
    }

    public static TestScore From(int correct, int total)
    {
        if (total <= 0) throw new DomainException("Total questions must be greater than zero.");
        if (correct < 0 || correct > total) throw new DomainException("Correct answers must be between 0 and total.");
        return new TestScore(Math.Round((double)correct / total, 4), correct, total);
    }

    public bool Equals(TestScore? other) =>
        other is not null && Value == other.Value && Correct == other.Correct && Total == other.Total;

    public override bool Equals(object? obj) => obj is TestScore other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Value, Correct, Total);
    public override string ToString() => $"{Correct}/{Total} ({Percentage:F1}%)";
}
