# oryndra

A 3D parkour game inspired by Mirror's Edge: timed spaces, community Maps, played solo or co-op.

## Content & competition

**Map**:
The playable environment of oryndra, produced by the editor or by generation. A Map hosts one or more Courses.
_Avoid_: level, world, stage

**Course**:
A timed route (checkpoints + goal) laid out on a Map; the unit of time comparison.
_Avoid_: track, path, race

**Segment**:
A prefabricated geometry block with entry/exit connectors, used to compose Maps (editor) and to generate Courses (procedural generation).
_Avoid_: tile, module, prefab, block

**Ghost**:
The semi-transparent replay of a Course, played back alongside the player's attempt.
_Avoid_: replay, spectre, phantom

## Movement

**Flow**:
The state of continuous running where speed is maintained by chaining moves without dead time.
_Avoid_: smoothness, rhythm

**Heave**:
The pull-up that hoists the player over a ledge. Not to be confused with `Mantle`, which names a graphics API and not a parkour move.
_Avoid_: mantle, climb-up

**Coil**:
A vertical impulse triggered against a support, launching the player upward.
_Avoid_: spring, bounce

**MAG rope**:
A grappling hook used to swing or to pull (pull-up / pull-down) toward an anchor point.
_Avoid_: grapple, rope

**Gait**:
The speed band the actor moves in — idle, walk, run or sprint — decided each tick from how far the player pushes the stick and what the sprint reserve allows. Presentation, animation and sound read it rather than inferring a band from the velocity.
_Avoid_: speed, pace, mode

**Sprint reserve**:
The meter that builds while the actor runs on the ground and drains while it sprints; a full reserve is what turns a run into a sprint. A reserve rather than a held button, so that speed rewards clean running.
_Avoid_: stamina, sprint bar, sprint button

**Input tolerance**:
The invisible margin that keeps a precise player from feeling the game is broken when a press just misses a ledge or the ground. Always on, and never a difficulty setting.
_Avoid_: forgiveness, assist, difficulty

**Coyote time**:
The part of the input tolerance that lets a jump pressed just after leaving the ground still count. Spent by the jump that uses it, so one press is one jump.
_Avoid_: jump buffer, grace period
