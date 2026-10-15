# Mirror's Edge Catalyst — vocabulaire de gameplay et de contenu

Recherche menée pour éclairer le PRD d'oryndra (voir `PRD.md` et `GLOSSARY.md`).
Le but n'est **pas** de copier le jeu, mais de nommer précisément les systèmes qu'un
jeu de parkour doit trancher — pour que `/grill-with-docs` ait des questions concrètes
à poser.

## Question posée

Quels systèmes *nommés* structurent le parkour de Mirror's Edge Catalyst, et lesquels
sont réutilisables comme vocabulaire de conception pour oryndra ?

## Cible analysée

| Élément | Valeur |
|---|---|
| Jeu | Mirror's Edge Catalyst (Windows, retail Steam) |
| Chemin | `/data/SteamLibrary/steamapps/common/Mirrors Edge Catalyst/` |
| Moteur | Frostbite (EA DICE) |
| Binaire principal | `MirrorsEdgeCatalyst.exe` — PE, x86-64, 87 786 928 octets |
| SHA-256 du binaire | `e9d76463766973964a97cd63bd64c1191a317ec6ed6753d82b36f4ba0da45136` |
| Données | 23 Go sous `Data/Win32/gameconfigurations/*installpackage/` |

## Méthode (et ce que chaque voie a donné)

Deux voies ont été tentées, avec des résultats très différents :

1. **MCP `rea` sur le binaire** — `open_binary` a réussi (provider **Ghidra 12.1.4**,
   cible PE x86-64 liée par SHA-256). L'analyse profonde a **échoué par timeout**
   (`provider_timeout`, plafond 330 s) : l'import d'un PE de 87 Mo dépasse le délai.
   L'alternative légère `inspect_binary_layout` est indisponible (le provider
   `pwntools-elf` exige `REA_PWNTOOLS_PYTHON`, non configuré) et ne vise de toute
   façon que les ELF.
   → **Conclusion : REA couvre bien le code, pas les données.** Il n'existe pas de
   provider Frostbite pour les conteneurs `.sb`/`.toc`.

2. **Lecture directe des index Frostbite** — les fichiers d'index (`layout.toc`,
   `globals.toc`, et les en-têtes `.sb`) stockent leurs chemins d'assets **en clair**,
   tandis que les charges utiles (`.cas`, `.sb`) sont compressées/chiffrées. Une
   extraction de chaînes sur les `.toc`/`.sb` délivre une liste de
   **49 081 noms d'assets distincts** qui documentent le vocabulaire du jeu.

   Reproductible :
   ```sh
   cd "/data/SteamLibrary/steamapps/common/Mirrors Edge Catalyst/Data"
   strings -n 6 Win32/*.sb Win32/*.toc Win32/levels/sp/*/*.sb Win32/levels/sp/*/*.toc \
     | grep -aoE '[a-z][a-z0-9_/.-]{5,}' | grep -E '[a-z]{4,}' | sort -u
   ```

> Les faits ci-dessous sont des **observations** issues de ces noms (autorité :
> artefact expédié). Les lectures de conception sont marquées **inférence**.

## Ce que la structure révèle

### 1. Le kit de traversée : `runnerobject`

C'est le cœur du système. Mirror's Edge Catalyst ne code pas « parkour » : il place
des **objets de coureur** typés, chacun porteur de sa propre sémantique de mouvement.
Taxonomie observée (`objects/props/runnerobject/`) :

| Objet | Occurrences | Rôle |
|---|---|---|
| `pipe_climbable` | 173 | Tuyaux escaladables (variantes `_32` → `_2048`, coudes `c45`/`c90`, `t128`) |
| `runnerboard` | 31 | Planches/rampes inclinées (`runnerboardlarge`, `runnerramp`) |
| `dasharrows` | 23 | Flèches de guidage de course, **dont `checkpointcircle`** |
| `swingpole01` / `swingpole02` | 18 | Barres d'équilibre et de balancier (`balanceandswing`) |
| `springboards` | 13 | Tremplins |
| `swingaroundpipe01` | 5 | Tuyaux de contournement en balancier |
| `ziplinebase` | 3 | Départ de tyrolienne |

**Inférence :** c'est exactement la notion de **Segment** du `GLOSSARY.md` d'oryndra
(« bloc de géométrie préfabriqué à connecteurs entrée/sortie »). ME Catalyst valide ce
découpage : la traversée est un **assemblage d'objets typés à dimensions normalisées**,
pas de la géométrie libre. Les suffixes de taille (`_128`, `_256`, `_416`, `_512`…)
montrent que les dimensions sont la véritable unité de design de niveau.

### 2. Le sous-arbre `gameplay/movement/`

Le mouvement a sa propre bibliothèque, organisée par **verbe** :

```
gameplay/movement/climbing/ladders      (pf_climbladder_384 / 512 / 768 / 1536)
gameplay/movement/climbing/newpipes     (pf_climbpipe_512 … 2048)
gameplay/movement/climbing/pipes        (pf_pipeclimb_1024b_noexit_startandend)
gameplay/movement/swing                 (pf_swingbarsimple01, pf_swingbalancebarflag01)
gameplay/movement/swingaround           (pf_swingaroundpipe01/02_short)
gameplay/movement/springboard           (pf_springboardgeneric, pf_springboardairduct512)
gameplay/movement/balance/pipes
```

Deux attributs sont portés par le **nom** de l'objet, pas par un flag séparé :

- `_noexit` vs `_startandend` : si on peut sortir du tuyau aux deux bouts ;
- `_groupable_gen4` vs `_nongroupable_autogen_gen4` : si l'objet peut être **mis en
  groupe** dans le pipeline d'authoring (`gen4` = génération de géométrie).

### 3. Runner Vision est une couche d'art *dupliquée*, pas un shader

Le suffixe `_mesh_runnersvision` apparaît sur une grande partie des props praticables
(par ex. `climbpipe_1024_cluster_mesh_runnersvision`, `pipe02_416_mesh_runnersvision`,
`runnerboardlarge_01_mesh_runnersvision`).

**Inférence (forte) :** l'« affordance » (ce qui est traversable se voit) n'est pas
déduite à l'exécution — elle est **autorisée en amont**, sous forme d'une variante de
maillage distincte par objet. C'est un choix coûteux en production mais parfaitement
lisible et déterministe.

Le jeu expose même Runner Vision comme un **cosmétique déverrouillable**
(`characters/customizations/projection/runvis/unlock_faith_projection_runvis`), au même
titre que `ghost`, `chaoslines`, `halftone`, `rain`, `scanline`, `streams` — les
« projections » sont des filtres visuels interchangeables. `settings/profile/…` et
`ui/art/map/progression/icons/gearbeatlink` confirment que c'est un système à part entière.

### 4. Le système de course : créé **par les joueurs**

```
gameplay/social/pf_l_createtimetrial
gameplay/social/timetrial/pf_timetrialcheckpoint
gameplay/social/timetrial/pf_timetrialmarker
```

Et le feedback associé, dans le banc sonore :
`me_sound/ui/ui_timetrial_start`, `…_checkpoints`, `…_edit_marker_01/02`,
`…_complete`, `…_complete_fail`, `…_cancel`.

**Inférence :** le chrono n'est pas un mode à part, c'est un **éditeur de parcours
in-game** : le joueur pose des marqueurs sur une carte existante, et le jeu en fait un
parcours. C'est le modèle « Beat » de Catalyst — et c'est directement le « Parcours »
du glossaire d'oryndra (« une route chronométrée tracée sur une carte »).

### 5. Le MAG rope, seule capacité *débloquable*

`characters/faith/armgear/magrope/` porte `magrope_pickup_1_skin`,
`magrope_pickup_2_skin`, `magrope_pickup_3_skin` — **trois paliers d'amélioration** —
plus `magrope_skin` (cosmétique), `magrope_ske` (squelette d'animation) et
`animations/prefabs/misc/pf_lookatmagrope`.

**Inférence :** le grappin est traité comme une **pièce d'équipement sur un membre**
(`armgear`), avec un axe de progression gameplay (paliers) et un axe cosmétique
(skins), séparés. C'est un modèle réutilisable pour la progression d'oryndra.

### 6. Le découpage de niveaux : les calques `_flow`

Les niveaux déclarent des couches nommées `*_flow` : `backinthegame_flow`,
`belikewater_flow`, `billboard_flow`, `securityhubdowntown1_flow`… À côté, un
`*approach_flow` par niveau (l'approche du point d'entrée).

> ⚠️ **Collision de vocabulaire à trancher** — `GLOSSARY.md` définit **Flow** comme le
> mouvement continu qui maintient la vitesse. Frostbite appelle `_flow` un **calque de
> contenu de niveau**. Deux sens pour un mot dans le même projet : à résoudre via
> `/domain-modeling`.

### 7. Familles de contenu au-delà du mouvement

`gameplay/` couvre aussi `ai` (451), `cityalert` (392), `collectibles` (284),
`mission` (114), `progression` (95), `social` (69), `logic` (70), `zipline_01` (58),
`rvo` (52 — *reciprocal velocity obstacles*, l'évitement de foule).

**Inférence :** `cityalert` (la « Gride » qui poursuit le joueur) et `rvo` sont des
systèmes à part entière dès la première version du jeu — la pression/poursuite n'est
pas un ajout cosmétique, c'est un système de gameplay de premier rang.

## Ce que ça change pour oryndra (inférences, à valider en grill)

1. **Le « Segment » est la bonne abstraction — mais il lui faut une dimension.** Chez
   ME Catalyst, la taille est dans le nom (`_512`, `_1536`). Un `Segment` qui ne
   déclare pas sa dimension ne pourra ni se générer ni se *snapper*.
2. **L'affordance est un coût de production explicite.** `_mesh_runnersvision` revient
   à doubler le maillage des surfaces praticables. Le cell-shading (ADR-0003) promet de
   payer ça par la couleur et le contour — c'est l'hypothèse à tester en prototype.
3. **Le chrono est un éditeur, pas un mode.** Poser les poursuites 2 joueurs/course
   comme un mode séparé, c'est rater ce que fait le genre.
4. **Progression et cosmétique sont deux axes orthogonaux** (paliers vs skins) — utile
   pour un modèle économique « Early Access + cosmétiques ».
5. **Un `Parcours` a besoin d'un support d'autorité** sur une `Carte` (marqueurs
   persistés), ce qui est une décision d'architecture, pas d'UI.

## Limites et inconnues résiduelles

- **Aucune constante de réglage n'a été extraite.** Vitesse, hauteur de saut, durées de
  wallrun, coyote-time : ces valeurs vivent dans les `.cas`/`.sb` compressés, hors de
  portée d'une lecture d'index. **Non résolu, et probablement non résolvable** sans
  décompresser le conteneur Frostbite.
- **Le code du mouvement n'a pas été lu.** Ghidra a expiré (`provider_timeout`, 330 s)
  sur `binary_overview`. Remède si l'on veut y revenir : relancer l'analyse Ghidra en
  tâche de fond et n'interroger qu'ensuite, ou fournir un `.idb` déjà analysé.
- Aucun PDB n'a été trouvé du côté Ghidra (`Skipping PDB processing: failed to locate PDB
  file in configured locations`) ; les noms de fonctions dans un binaire retail seront rares.
- `inspect_binary_layout` indisponible (`REA_PWNTOOLS_PYTHON` absent).
- Les noms d'assets sont des **noms de production** : ils indiquent l'existence d'un
  système, pas son comportement exact. Toute affirmation de « comment ça marche »
  ci-dessus est une inférence, pas une observation.
- Aucun asset n'est réutilisable : ces fichiers sont couverts par le droit d'auteur
  (déjà acté dans `docs/adr/0003-direction-artistique-cell-shading.md`).

## Index des preuves

| Source | Nature | Contenu |
|---|---|---|
| `open_binary` sur `MirrorsEdgeCatalyst.exe` | artefact expédié, observé | PE x86-64, SHA-256 `e9d76463…5136`, provider Ghidra 12.1.4 |
| `binary_overview` (tentative) | échec | `provider_timeout`, 330 s, analyse Ghidra inachevée |
| `bin`/`.toc`/`.sb` de `Data/` | artefact expédié, observé | 49 081 noms d'assets ; taxonomies ci-dessus |
| `Data/Win32/globals.toc`, `layout.toc` | artefact expédié, observé | liste des superbundles et bundles en clair |
