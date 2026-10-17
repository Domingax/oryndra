using Oryndra.Core.Movement;

namespace Oryndra.Core.Tests.Support;

/// <summary>Compares simulation states down to the raw bits.</summary>
/// <remarks>
/// Plain equality would hide two classes of regression: a NaN (never equal to
/// itself) and a sign flip on zero (equal, but a different value downstream).
/// </remarks>
internal static class ActorStateAssertions
{
    /// <summary>Asserts that two states are bit-for-bit the same state.</summary>
    /// <param name="expected">The state the run should have reproduced.</param>
    /// <param name="actual">The state the run produced.</param>
    /// <param name="because">What the comparison is about, for the failure message.</param>
    public static void AssertBitIdentical(ActorState expected, ActorState actual, string because)
    {
        var differences = new List<string>();

        Compare(differences, "Position.X", expected.Position.X, actual.Position.X);
        Compare(differences, "Position.Y", expected.Position.Y, actual.Position.Y);
        Compare(differences, "Position.Z", expected.Position.Z, actual.Position.Z);
        Compare(differences, "Velocity.X", expected.Velocity.X, actual.Velocity.X);
        Compare(differences, "Velocity.Y", expected.Velocity.Y, actual.Velocity.Y);
        Compare(differences, "Velocity.Z", expected.Velocity.Z, actual.Velocity.Z);
        Compare(differences, "Yaw", expected.Yaw, actual.Yaw);
        Compare(differences, "Grounded", expected.Grounded, actual.Grounded);

        Assert.True(
            differences.Count == 0,
            $"{because}: expected {expected} but was {actual} ({string.Join(", ", differences)})");
    }

    private static void Compare(List<string> differences, string name, float expected, float actual)
    {
        if (BitConverter.SingleToInt32Bits(expected) != BitConverter.SingleToInt32Bits(actual))
        {
            differences.Add($"{name} {expected} != {actual}");
        }
    }

    private static void Compare<T>(List<string> differences, string name, T expected, T actual)
        where T : IEquatable<T>
    {
        if (!expected.Equals(actual))
        {
            differences.Add($"{name} {expected} != {actual}");
        }
    }
}
