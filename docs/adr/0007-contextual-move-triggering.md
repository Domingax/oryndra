# Contextual move triggering

Traversal moves trigger from **context** — speed, direction, surface encountered — and not from a dedicated key. Only **jump** and **crouch** have a key. This is Mirror's Edge's model: the player expresses intent through movement, and the game chooses the move. The **MAG rope** is the exception: it is a **capability** (not a running reflex) and has a **dedicated key**, as in Catalyst.

## Considered and rejected

- **One key per move** (vault, wallrun, slide, balance…): rejected — cognitive load, and above all it breaks *Flow*: you think about buttons instead of the trajectory.
- **Hybrid** (contextual + an optional key to force a move): postponed, to be revisited if tuning mis-triggers proves problematic.

## Consequences

- Each move is a **data record**: probes to run, speed/angle/height thresholds, animation times, redo cooldown. Tuning those thresholds is the core of the "feel" work.
- The **main risk is mis-triggering** (a vault where you meant to run past). Thresholds are the answer.
- **Accessibility** comes from generous thresholds and input tolerance, **not** from visual aids — consistent with the "no navigation aid" choice.
- The **Training Map** must teach these automatisms, since no button reveals them.

## Evidence

Mirror's Edge (2008): `DefaultInput.ini` binds only `Jump` and `Crouch`; wallrun, vault, heave, slide, balance and zipline have **no** key and are triggered from context. Detail and values: `docs/research/mirrors-edge-1-movement.md`.
