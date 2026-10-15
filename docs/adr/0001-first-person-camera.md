# First-person camera

The PRD did not settle the camera. We choose **first person** for **gameplay** and for **replay / Ghost**: it is Mirror's Edge's signature and what produces the sense of speed and vertigo. Third person was ruled out despite real advantages (co-op readability, showcasing cosmetics, editor comfort) because it dilutes the "I am the flow" feeling that defines the genre.

## Considered and rejected

- **Third person**: better readability in co-op, in the editor and for cosmetics, but weakens immersion and the sense of speed.
- **Switchable camera**: doubles the load on every animation and camera system; postponed.

## Consequences

Body/hand animations, FOV, head-bob and motion effects are designed for the subjective view from the outset. Any third-person mode (spectate, editor) must be treated as a separate system, not a simple camera toggle.
