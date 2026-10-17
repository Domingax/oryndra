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
        var differences = Differences(expected, actual).ToList();
        Assert.True(
            differences.Count == 0,
            $"{because}: expected {expected} but was {actual} ({string.Join(", ", differences)})");
    }

    private static IEnumerable<string> Differences(ActorState expected, ActorState actual)
    {
        if (Bits(expected.Position.X) != Bits(actual.Position.X))
        {
            yield return $"Position.X {expected.Position.X} != {actual.Position.X}";
        }

        if (Bits(expected.Position.Y) != Bits(actual.Position.Y))
        {
            yield return $"Position.Y {expected.Position.Y} != {actual.Position.Y}";
        }

        if (Bits(expected.Position.Z) != Bits(actual.Position.Z))
        {
            yield return $"Position.Z {expected.Position.Z} != {actual.Position.Z}";
        }

        if (Bits(expected.Velocity.X) != Bits(actual.Velocity.X))
        {
            yield return $"Velocity.X {expected.Velocity.X} != {actual.Velocity.X}";
        }

        if (Bits(expected.Velocity.Y) != Bits(actual.Velocity.Y))
        {
            yield return $"Velocity.Y {expected.Velocity.Y} != {actual.Velocity.Y}";
        }

        if (Bits(expected.Velocity.Z) != Bits(actual.Velocity.Z))
        {
            yield return $"Velocity.Z {expected.Velocity.Z} != {actual.Velocity.Z}";
        }

        if (Bits(expected.Yaw) != Bits(actual.Yaw))
        {
            yield return $"Yaw {expected.Yaw} != {actual.Yaw}";
        }

        if (expected.Grounded != actual.Grounded)
        {
            yield return $"Grounded {expected.Grounded} != {actual.Grounded}";
        }
    }

    private static int Bits(float value) => BitConverter.SingleToInt32Bits(value);
}
