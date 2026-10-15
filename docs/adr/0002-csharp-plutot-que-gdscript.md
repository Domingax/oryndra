# C# plutôt que GDScript

Le PRD proposait GDScript « (+ C# si besoin) ». On inverse la priorité : **C# d'abord**. Raison principale : un langage typé et outillé offre un **harnais plus robuste pour les agents** (compilation, refactoring, tests, analyse statique) et un meilleur plafond de performance — utile pour un jeu de mouvement visant 60 FPS sur config modeste. GDScript reste possible pour de petits scripts, mais n'est pas le langage par défaut.

## Options considérées et écartées

- **GDScript** : itération plus rapide, syntaxe plus douce pour un dev solo, mais typage plus faible et outillage agent moins solide.

## Conséquences

Le projet Godot est configuré en mode .NET (Mono). Le socle Godot/.NET est une dépendance à installer et à versionner ; les contributions et les agents doivent savoir compiler du C#.
