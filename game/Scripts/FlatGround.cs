using System.Numerics;

using Oryndra.Core.Movement;

namespace Oryndra.Game;

/// <summary>
/// The collision seam backed by a flat floor at y = 0.
/// </summary>
/// <remarks>
/// A placeholder until the greybox exists, so that the project boots with a
/// simulation that can actually stand up. Note what does not change when it is
/// replaced: the seam itself. That is the point of keeping the simulation
/// engine-agnostic.
/// </remarks>
public sealed class FlatGround : ICollisionQuery
{
    /// <inheritdoc />
    public BodyMoveResult MoveBody(Vector3 position, Vector3 delta, BodyShape body)
    {
        var target = position + delta;
        var grounded = false;

        if (target.Y < 0f)
        {
            target = new Vector3(target.X, 0f, target.Z);
            grounded = true;
        }

        return new BodyMoveResult(target, target - position, grounded);
    }
}
