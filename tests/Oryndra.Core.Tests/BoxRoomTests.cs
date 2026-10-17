using System.Numerics;

using Oryndra.Core.Movement;

using Oryndra.Core.Tests.Support;

namespace Oryndra.Core.Tests;

/// <summary>
/// The block geometry itself: the pieces the movement tests and the greybox build
/// their rooms out of.
/// </summary>
/// <remarks>
/// The resolver's contract is narrow on purpose: it moves a body that starts
/// outside solid geometry, and it only ever *removes* motion. The cases below pin
/// both halves, because a play test found what breaking the first one looks like —
/// a body thrown onto a wall it was standing beside, and across the level from a
/// step it was sliding along.
/// </remarks>
public sealed class BoxRoomTests
{
    [Fact]
    public void MoveBody_WhenTheBodyStandsAgainstAWall_LeavesItOnTheFloor()
    {
        var body = MovementTuning.Default.Body;
        var room = BoxRoom.Room();

        // The body stands on the floor with its side against a wall, a hair inside
        // it: the tick that stopped it left it there, because the face it stopped at
        // is not exactly representable.
        var moved = room.MoveBody(new Vector3(7.600001f, 0f, 0f), new Vector3(0f, -0.0067f, 0f), body);

        Nearly.Equal(0f, moved.Position.Y, "the body stays on the floor");
        Assert.True(moved.Grounded, "the floor under it is what stopped the move");
    }

    [Fact]
    public void MoveBody_WhenTheBodyFallsBesideAWall_IsNotGrounded()
    {
        var body = MovementTuning.Default.Body;
        var room = BoxRoom.Room();

        var moved = room.MoveBody(new Vector3(7.600001f, 2.2f, 0f), new Vector3(0f, -0.1f, 0f), body);

        Nearly.Equal(2.1f, moved.Position.Y, "a fall past a wall is still a fall");
        Assert.False(moved.Grounded, "nothing is under a body in the air");
    }

    [Fact]
    public void MoveBody_WhenTheBodySlidesAlongABlockFace_OnlyRemovesMotion()
    {
        var body = MovementTuning.Default.Body;
        var room = new BoxRoom()
            .Add(Box.FromCorners(new Vector3(-30f, -1f, -30f), new Vector3(30f, 0f, 30f)))
            .Add(Box.FromCorners(new Vector3(4f, 0f, 4f), new Vector3(8f, 0.6f, 8f)));

        // Standing on the floor against the step's +X face, inside its Z range,
        // sliding along it. The body's volume is a hair inside the step, which used
        // to resolve it against the step's *far* Z face, three metres away.
        var moved = room.MoveBody(new Vector3(8.4f, 0f, 5.4f), new Vector3(0f, -0.0067f, -0.011f), body);

        Nearly.Equal(5.389f, moved.Position.Z, "the slide is not a throw", tolerance: 1e-3f);
        Nearly.Equal(0f, moved.Position.Y, "the body stays on the floor");
        Nearly.Equal(0.011f, MathF.Abs(moved.Motion.Z), "the resolver removes motion, it does not add it", tolerance: 1e-3f);
    }

    [Fact]
    public void MoveBody_OverManyMoves_NeverMovesTheBodyFurtherThanItWasAsked()
    {
        var body = MovementTuning.Default.Body;
        var room = FuzzRooms.WithAStep();
        var random = new Random(20261017);

        for (var sample = 0; sample < 20_000; sample++)
        {
            var position = new Vector3(
                (random.NextSingle() * 20f) - 10f,
                random.NextSingle() * 2f,
                (random.NextSingle() * 20f) - 10f);
            var delta = new Vector3(
                random.NextSingle() - 0.5f,
                random.NextSingle() - 0.5f,
                random.NextSingle() - 0.5f) * 0.4f;

            var moved = room.MoveBody(position, delta, body);

            Assert.True(
                MathF.Abs(moved.Motion.X) <= MathF.Abs(delta.X) + 1e-4f
                && MathF.Abs(moved.Motion.Y) <= MathF.Abs(delta.Y) + 1e-4f
                && MathF.Abs(moved.Motion.Z) <= MathF.Abs(delta.Z) + 1e-4f,
                $"sample {sample}: asked {delta} at {position} and was moved {moved.Motion}");
        }
    }

    [Fact]
    public void OuterBounds_OfARoomWithNoBlocks_Throws()
    {
        var room = new BoxRoom();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => room.OuterBounds);

        Assert.Contains("at least one block", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Overlaps_WithABodyRestingOnTheFloor_IsFalse()
    {
        // Touching faces do not share volume: a body standing on a floor is
        // outside it, which is what lets the actor stand still and still collide.
        BoxRoom room = new BoxRoom().Add(Box.FromCorners(new Vector3(-1f, -1f, -1f), new Vector3(1f, 0f, 1f)));

        Assert.False(room.Overlaps(Vector3.Zero, MovementTuning.Default.Body));
    }

    [Fact]
    public void Around_CentresABlockOnTheGivenPoint()
    {
        var box = Box.Around(new Vector3(1f, 2f, 3f), new Vector3(2f, 4f, 6f));

        Assert.Equal(Box.FromCorners(new Vector3(0f, 0f, 0f), new Vector3(2f, 4f, 6f)), box);
    }

    [Fact]
    public void OuterBounds_OfSeveralBlocks_ContainsThemAll()
    {
        BoxRoom room = new BoxRoom()
            .Add(Box.FromCorners(new Vector3(-1f, -1f, -1f), new Vector3(1f, 1f, 1f)))
            .Add(Box.FromCorners(new Vector3(0f, 0f, 0f), new Vector3(4f, 4f, 4f)));

        Assert.Equal(Box.FromCorners(new Vector3(-1f, -1f, -1f), new Vector3(4f, 4f, 4f)), room.OuterBounds);
    }

    [Fact]
    public void Contains_WithABlockInsideTheTolerance_IsTrue()
    {
        var bounds = Box.FromCorners(Vector3.Zero, Vector3.One);
        var reaching = Box.FromCorners(new Vector3(-0.1f, 0f, 0f), new Vector3(0.5f, 0.5f, 0.5f));

        Assert.False(bounds.Contains(reaching));
        Assert.True(bounds.Contains(reaching, tolerance: 0.2f));
    }
}
