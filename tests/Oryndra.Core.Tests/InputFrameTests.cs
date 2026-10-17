using System.Numerics;

using Oryndra.Core.Movement;

using Oryndra.Core.Tests.Support;

namespace Oryndra.Core.Tests;

/// <summary>
/// The input frame's own arithmetic: which way a stick points once a yaw turns it.
/// </summary>
/// <remarks>
/// The lateral sign is the one the engine layer cannot check for itself and the
/// one a first-person view feels immediately: get it wrong and pressing right
/// strafes left. The expectation below is the cross product with up — the view's
/// right — rather than a hand-written vector per yaw.
/// </remarks>
public sealed class InputFrameTests
{
    /// <summary>A quarter turn and an odd angle, so a sign error that only shows at some yaws is caught.</summary>
    public static TheoryData<float> Yaws => [0f, MathF.PI / 2f, MathF.PI, 3f * MathF.PI / 2f, 1.234f];

    [Theory]
    [MemberData(nameof(Yaws))]
    public void WishDirection_WithTheStickForward_PointsAlongTheFacing(float yaw)
    {
        var direction = Frame(right: 0f, forward: 1f, yaw).WishDirection;
        var facing = new Vector3(MathF.Sin(yaw), 0f, MathF.Cos(yaw));

        Nearly.Equal(0f, (direction - facing).Length(), "the actor faces +Z at yaw 0 and turns towards +X");
    }

    [Theory]
    [MemberData(nameof(Yaws))]
    public void WishDirection_WithTheStickRight_PointsToTheRightOfTheView(float yaw)
    {
        var facing = Frame(right: 0f, forward: 1f, yaw).WishDirection;
        var right = Frame(right: 1f, forward: 0f, yaw).WishDirection;

        Nearly.Equal(1f, right.Length(), "a unit stick asks for a unit direction");
        Nearly.Equal(0f, (right - Vector3.Cross(facing, Vector3.UnitY)).Length(), "the view's right");
    }

    [Fact]
    public void WishDirection_WithTheStickAtRest_AsksForNothing()
    {
        var frame = new InputFrame { MoveAxis = Vector2.Zero, Yaw = 1.1f };

        Assert.Equal(Vector3.Zero, frame.WishDirection);
    }

    private static InputFrame Frame(float right, float forward, float yaw) =>
        new() { MoveAxis = new Vector2(right, forward), Yaw = yaw };
}
