using System.Numerics;

using Oryndra.Core.Movement;

namespace Oryndra.Core.Tests.Support;

/// <summary>Rooms the fuzz harness drives the simulation through.</summary>
/// <remarks>
/// A room of bare walls only ever asks the collision query about flat surfaces.
/// The geometry a player actually trips over is a block *inside* the room — a step
/// whose side they run into, whose top they land on — so the fuzz room carries one.
/// </remarks>
internal static class FuzzRooms
{
    /// <summary>Builds a room with a low step in the middle of it.</summary>
    /// <returns>A room whose floor top sits at y = 0 and whose step top sits at y = 0.6.</returns>
    public static BoxRoom WithAStep() =>
        BoxRoom.Room().Add(Box.Around(new Vector3(5f, 0.3f, 0f), new Vector3(4f, 0.6f, 4f)));
}
