# oryndra

Jeu de parkour 3D inspiré de Mirror's Edge : espaces chronométrés, cartes communautaires, jeu seul ou en coopération.

## Contenu & compétition

**Carte** :
L'environnement jouable d'oryndra, produit par l'éditeur ou par génération. Une carte héberge un ou plusieurs parcours.
_Avoid_: map, niveau, level, monde

**Parcours** :
Une route chronométrée (checkpoints + arrivée) tracée sur une carte ; l'unité de comparaison de temps.
_Avoid_: course, tracé, route

**Segment** :
Un bloc de géométrie préfabriqué à connecteurs entrée/sortie, servant à composer les cartes (éditeur) et à générer les parcours (génération procédurale).
_Avoid_: tile, module, prefab, bloc

**Ghost** :
Le replay semi-transparent d'un parcours, rejoué en parallèle de la tentative du joueur.
_Avoid_: fantôme, replay, spectre

## Mouvement

**Flow** :
L'état de course continue où la vitesse se maintient par l'enchaînement des mouvements, sans temps mort.
_Avoid_: fluidité, rythme

**Relevé** (heave) :
La traction qui hisse le joueur par-dessus un rebord. À ne pas confondre avec `Mantle`, qui désigne une API graphique et non un mouvement de parkour.
_Avoid_: mantle

**Coil** :
Une impulsion verticale déclenchée contre un support, qui propulse le joueur vers le haut.
_Avoid_: ressort, spring

**MAG rope** :
Un grappin permettant de se balancer (swing) ou de se tracter (pull-up / pull-down) vers un point d'ancrage.
_Avoid_: grappin, corde
