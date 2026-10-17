using System.Numerics;

using Oryndra.Core.Movement;

namespace Oryndra.Core.Tests;

/// <summary>
/// The block geometry itself: the pieces the movement tests and the greybox build
/// their rooms out of.
/// </summary>
public sealed class BoxRoomTests
{
    [Fact]
    public void OuterBounds_OfARoomWithNoBlocks_Throws()
    {
        var room = new BoxRoom();

        var error = Assert.Throws<InvalidOperationException>(() => room.OuterBounds);

        Assert.Contains("at least one block", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Overlaps_WithABodyRestingOnTheFloor_IsFalse()
    {
        // Touching faces do not share volume: a body standing on a floor is
        // outside it, which is what lets the actor stand still and still collide.
        var room = new BoxRoom().Add(Box.FromCorners(new Vector3(-1f, -1f, -1f), new Vector3(1f, 0f, 1f)));

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
        var room = new BoxRoom()
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
