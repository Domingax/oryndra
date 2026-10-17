using Godot;

using Oryndra.Core.Movement;

namespace Oryndra.Game;

/// <summary>
/// The greybox space the walking skeleton is played in, and the geometry the
/// simulation moves through.
/// </summary>
/// <remarks>
/// <para>
/// The pieces are the single source of truth for both: each one is a solid the
/// collision query is built from and a mesh the player sees, so a surface can never
/// be visible and passable at once. This is placeholder content — a Map will
/// replace it (ticket #5) — and what it must not grow is movement rules: the
/// simulation owns those, and this class only says where the walls are.
/// </para>
/// <para>
/// Traversable surfaces are drawn lighter than the solids around them. The PRD
/// makes colour the thing that carries readability rather than texture detail
/// ("Art &amp; sound direction"), so even a greybox should not make the player
/// guess which box is a step and which is a wall.
/// </para>
/// </remarks>
public sealed class Greybox
{
    /// <summary>Where the actor starts: on the floor, facing the pillar.</summary>
    public static readonly System.Numerics.Vector3 Spawn = new(0f, 0f, -6f);

    private static readonly StandardMaterial3D Walkable = new()
    {
        AlbedoColor = new Color(0.64f, 0.66f, 0.7f),
        Roughness = 1f,
    };

    private static readonly StandardMaterial3D Solid = new()
    {
        AlbedoColor = new Color(0.32f, 0.34f, 0.38f),
        Roughness = 1f,
    };

    private readonly Piece[] pieces =
    [
        new("Floor", Slab(0f, -0.5f, 0f, 60f, 1f, 60f), Traversable: true),
        new("Ledge", Slab(-10f, 0.5f, 0f, 12f, 1f, 12f), Traversable: true),
        new("Step", Slab(6f, 0.3f, 6f, 4f, 0.6f, 4f), Traversable: true),
        new("Wall", Slab(14f, 2f, 0f, 1f, 4f, 40f), Traversable: false),
        new("Pillar", Slab(0f, 1.5f, 18f, 4f, 3f, 4f), Traversable: false),
    ];

    /// <summary>Initializes a new instance of the <see cref="Greybox"/> class.</summary>
    public Greybox()
    {
        foreach (Piece piece in pieces)
        {
            Room.Add(piece.Box);
        }
    }

    /// <summary>Gets the geometry the simulation collides with.</summary>
    public BoxRoom Room { get; } = new();

    /// <summary>Builds the meshes of the pieces, ready to be added to the scene.</summary>
    /// <returns>The root of the greybox's visual nodes.</returns>
    public Node3D Visuals()
    {
        var root = new Node3D { Name = "Greybox" };

        foreach (Piece piece in pieces)
        {
            root.AddChild(Mesh(piece));
        }

        return root;
    }

    /// <summary>Builds a block centred on a point, with its full extent along each axis.</summary>
    /// <param name="x">Centre along X, in metres.</param>
    /// <param name="y">Centre along Y, in metres.</param>
    /// <param name="z">Centre along Z, in metres.</param>
    /// <param name="width">Full extent along X, in metres.</param>
    /// <param name="height">Full extent along Y, in metres.</param>
    /// <param name="depth">Full extent along Z, in metres.</param>
    /// <returns>The block to collide with and to draw.</returns>
    private static Box Slab(float x, float y, float z, float width, float height, float depth) =>
        Box.Around(
            new System.Numerics.Vector3(x, y, z),
            new System.Numerics.Vector3(width, height, depth));

    private static MeshInstance3D Mesh(Piece piece)
    {
        System.Numerics.Vector3 centre = (piece.Box.Min + piece.Box.Max) / 2f;
        System.Numerics.Vector3 size = piece.Box.Max - piece.Box.Min;

        return new MeshInstance3D
        {
            Name = piece.Name,
            Mesh = new BoxMesh { Size = size.ToGodot() },
            MaterialOverride = piece.Traversable ? Walkable : Solid,
            Position = centre.ToGodot(),
        };
    }

    /// <summary>One block of greybox: what it is called, where it is, and whether it is a floor.</summary>
    /// <param name="Name">The node name the piece is drawn under.</param>
    /// <param name="Box">The solid the simulation collides with.</param>
    /// <param name="Traversable">Whether the piece is a surface to run on rather than an obstacle.</param>
    private sealed record Piece(string Name, Box Box, bool Traversable);
}
