# Asset pipeline

The MVP needs no authored art: greybox Maps are built from Godot primitives (BoxMesh / CSG). Real assets arrive later, and the project is open source with content under CC BY-SA 4.0, so every asset must be either created by us or come from a compatible licence.

We choose **Blender** as the single 3D authoring tool, exporting **glTF** to Godot. Blender is effectively mandatory because the movement is animation-driven and the character needs a rig and animations, which Blender covers end to end. **Blockbench** (fast box/pixel modelling) is an optional companion, not a required tool: it overlaps with Blender for blocky pieces and does not cover character rigging, so adding it up front would only add a second toolchain for a solo dev.

## Considered and rejected

- **Blockbench alone**: excellent for blocky props and pixel textures, but not for character rigging and animation.
- **Blender + Blockbench as two mandatory tools**: the two overlap on low-poly modelling; a second toolchain is overhead a solo dev cannot afford yet.

## Consequences

- **glTF** is the interchange format between the authoring tools and Godot.
- Blender is required before any real character work, so it becomes a prerequisite for the art phase (post-MVP).
- Only self-created assets or **CC0 / CC BY** assets may be used; proprietary content (e.g. Mixamo animations) is excluded because it cannot be redistributed under the project licence.
- The stylised direction (cel shading, low-poly, flat colours, outline — ADR `0003-art-direction-cel-shading.md`) keeps modelling cost low.
- Agents may drive Blender through an MCP server; that is evaluated separately and is not a dependency of the build.
