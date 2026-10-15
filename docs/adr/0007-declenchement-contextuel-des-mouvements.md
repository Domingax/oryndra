# Déclenchement contextuel des mouvements

Les mouvements de traversée se déclenchent par le **contexte** — vitesse, direction, surface rencontrée — et non par une touche dédiée. Seuls le **saut** et l'**accroupi** ont une touche. C'est le modèle de Mirror's Edge : le joueur exprime son intention par le mouvement, et le jeu choisit le geste. Les gestes « capacitaires » qui ne sont pas des réflexes de course (typiquement le **MAG rope**) pourront avoir une touche dédiée.

## Options considérées et écartées

- **Une touche par mouvement** (vault, wallrun, glissade, balance…) : écarté — charge cognitive, et surtout ça casse le *flow* : on pense aux boutons au lieu de la trajectoire.
- **Hybride** (contextuel + touche optionnelle pour forcer un geste) : reporté, à réévaluer si le réglage des faux déclenchements pose problème.

## Conséquences

- Chaque mouvement est un **enregistrement de données** : sondes à lancer, seuils de vitesse/angle/hauteur, durées d'animation, cooldown de reprise. Le réglage de ces seuils est le cœur du travail de « feel ».
- Le **risque principal est le faux déclenchement** (un vault là où l'on voulait passer). Les seuils sont la réponse.
- L'**accessibilité** passe par des seuils généreux et une tolérance d'input, **pas** par des aides visuelles — cohérent avec le choix « pas d'aide à la navigation ».
- La **carte d'entraînement** doit enseigner ces automatismes, puisqu'aucun bouton ne les révèle.

## Preuve

Mirror's Edge (2008) : `DefaultInput.ini` ne lie que `Jump` et `Crouch` ; wallrun, vault, relevé, glissade, balance et tyrolienne n'ont **aucune** touche et sont déclenchés par le contexte. Détail et valeurs : `docs/research/mirrors-edge-1-movement.md`.
