# C# over GDScript

The PRD proposed GDScript "(+ C# if needed)". We invert the priority: **C# first**. Main reason: a typed, well-tooled language gives a **more robust harness for agents** (compilation, refactoring, tests, static analysis) and a better performance ceiling — useful for a movement game targeting 60 FPS on a modest configuration. GDScript remains possible for small scripts, but is not the default language.

## Considered and rejected

- **GDScript**: faster iteration and gentler syntax for a solo dev, but weaker typing and less solid agent tooling.

## Consequences

The Godot project is configured in .NET (Mono) mode. The Godot/.NET base is a dependency to install and version; contributors and agents must be able to compile C#.
