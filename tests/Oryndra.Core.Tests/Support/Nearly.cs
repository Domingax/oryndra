namespace Oryndra.Core.Tests.Support;

/// <summary>Assertions the movement tests need and xUnit does not provide.</summary>
internal static class Nearly
{
    /// <summary>Asserts that <paramref name="actual"/> is within <paramref name="tolerance"/> of <paramref name="expected"/>.</summary>
    /// <param name="expected">The value the run should have produced.</param>
    /// <param name="actual">The value the run produced.</param>
    /// <param name="because">What the comparison is about, for the failure message.</param>
    /// <param name="tolerance">How far apart the two values may be.</param>
    public static void Equal(float expected, float actual, string? because = null, float tolerance = 1e-4f)
    {
        var message = $"expected {expected} ± {tolerance} but was {actual}";
        if (because is not null)
        {
            message += $" ({because})";
        }

        Assert.True(MathF.Abs(expected - actual) <= tolerance, message);
    }
}
