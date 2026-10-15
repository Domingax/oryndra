# Product Requirements Document – oryndra

## 🎯 Concept

Un jeu de **parkour 3D** nerveux, compétitif et créatif. Le joueur progresse dans des **Cartes** — faites main, communautaires, ou générées —, seul ou en coopération (jusqu'à 3), et y trace des **Parcours** chronométrés.

Deux façons de jouer, chacune **seul ou en équipe** :
- **Pour le plaisir** — découvrir les cartes communautaires, explorer, jouer entre amis.
- **Pour la compétition** — viser le meilleur temps, battre ses records et ceux des autres.

Le cœur du jeu est son **éditeur** et sa **communauté** : la communauté construit sa propre expérience à travers les cartes et leur partage. Vision : **« le Minecraft du parkour »**.

## 👥 Public cible

- Joueurs amateurs de gameplay nerveux, fun et créatif.
- Deux communautés : **compétiteurs** (temps, records) et **explorateurs** (découverte, jeu social).
- Plateforme de départ : **PC** (Windows, Linux). Mac ensuite. Consoles/mobiles seulement en cas de succès.

## 🕹 Gameplay

### Mouvement — le cœur du jeu

- **Tous les mouvements de Mirror's Edge Catalyst** sont visés :
  course / sprint, saut (dont saut long), **wallrun** (deux étages, avec apex), wallclimb, **vault** (plusieurs variantes), **relevé** (heave), **glissade** (slide), **amorti** (roll), **balance**, escalade (tuyaux/échelles), **coil**, springboard, **MAG rope** (swing / pull-up / pull-down), tyrolienne, swing.
- **Aucun combat.** Le mouvement est le seul verbe ; pas de coup, pas de takedown.
- Le **sprint est une jauge** (une réserve qui monte et descend), pas un simple booléen.
- **Architecture de mouvement data-driven et extensible** : l'ajout ou la modification d'un mouvement doit être simple. Le modèle de référence est *sonde → décision → animation* (des détecteurs géométriques bon marché alimentent un « pilote » de mouvement), et non un contrôleur monolithique — validé par le modèle de Mirror's Edge, où chaque mouvement est un enregistrement de données (sondes à lancer, modificateurs de vitesse, durées d'animation, recharge).
- **Déclenchement contextuel** : comme Mirror's Edge, seuls le saut et l'accroupi ont une touche dédiée ; wallrun, vault, relevé, glissade, balance et tyrolienne se déclenchent par le **contexte** (course, direction, surface rencontrée), pas par un bouton. Le **MAG rope** fait exception : capacité, touche dédiée.
- **Référence de tuning** : le modèle de mouvement et des valeurs réelles de **Mirror's Edge (2008)** sont documentés dans `docs/research/mirrors-edge-1-movement.md` — points de départ à re-tuner à l'échelle d'oryndra, pas à copier.

### Navigation & accessibilité

- **Aucune aide à la navigation** : ni chemin surligné, ni flèches. **Trouver son chemin est une mécanique.**
- **Tolérance d'input en socle**, non optionnelle (coyote time, marge de saisie des rebords) : sans elle, le jeu paraît cassé, il n'est pas plus difficile.
- « Accessible » signifie **mécaniques simples à comprendre**, jamais **niveaux faciles**.

### Boucle de jeu & compétition

- Un **Parcours** est une route chronométrée (checkpoints + arrivée) tracée sur une **Carte**.
- **Ghost** : le replay semi-transparent d'une course, rejoué en parallèle.
  - Au MVP : **record personnel** + **ghost de référence de l'auteur**, tous deux hors-ligne.
  - Plus tard : ghosts d'amis et des classements en ligne.
- **Progression** : tous les mouvements sont **disponibles dès le départ** (équité compétitive) ; la progression récompense la **maîtrise** (médailles par parcours, records), pas le déblocage de capacités.
- Classements en ligne, mode équipe et médailles avancées arrivent après le MVP et la coop.

### Éditeur intégré

- **Composer** des cartes à partir de **Segments** préfabriqués (accrochage, connecteurs entrée/sortie).
- Placer les **éléments de gameplay** : spawn, checkpoints, arrivée, dangers, objets mobiles.
- **Tester** la carte immédiatement, dans l'éditeur.
- **Exporter / partager** (fichier de carte ouvert, puis miroir Steam Workshop).
- La modélisation de géométrie libre (CSG) est hors périmètre initial.

### Génération procédurale

- Objectif (phase Alpha) : **assemblage de Segments préfabriqués** avec connecteurs, garantissant un parcours jouable — **pas** de génération de géométrie libre.
- Elle réutilise **le même vocabulaire que l'éditeur** (un seul système de Segments).

### Coopération (jusqu'à 3) — teamplay

- La coop n'est **pas** une course parallèle : elle repose sur des **mécaniques dédiées** où les joueurs s'entraident pour franchir des obstacles **infranchissables seul** (courte échelle/boost, contrepoids, lancer, slingshot, échelle humaine, rattrapage, relais).
- Chaque mécanique est **réciproque** (celui qui aide doit pouvoir progresser à son tour).
- Les parcours qui en dépendent sont **identifiés comme coop** ; une variante solo est fournie quand c'est possible.
- Détail des mécaniques **à prototyper** ; la coop arrive **après** le moveset complet et le PCG.

## 🎨 Direction artistique & sonore

- **Rendu stylisé (cell-shading)** : simple, intemporel, compatible configs modestes.
- La **couleur et le contour** portent la lisibilité des surfaces praticables (ce qui se grimpe, ce qui se traverse) — pas le détail des textures.
- Univers **modulable** selon les cartes, **sans narration imposée**.
- **Son** :
  - Feedback SFX **par mouvement et par matériau** (sprint, wallrun, glisse, réception).
  - **Musique adaptative légère** pilotée par le *flow* (l'intensité monte avec la vitesse et l'enchaînement).
  - Un **mixer « parcours »** qui met le rythme en avant.
  - Effets « fun » discrets.

## ⚙️ Technique

- **Moteur : Godot 4.x**, langage **C# (.NET) en priorité** (GDScript possible pour de petits scripts).
- Cibles : **Windows + Linux** d'abord, Mac ensuite.
- **60 FPS minimum** en 1080p sur config modeste.
- **Architecture modulaire**, le gameplay core d'abord ; le multijoueur (coop P2P / hébergement local) ensuite.
- **Formats ouverts** : les cartes sont des fichiers redistribuables (voir la licence).

## 📦 Production & Business

- Développement **solo indé**, périmètre maîtrisé, priorité au plaisir de mouvement.
- **Open source**, aucune ambition de revenu :
  - **Code : GPL-3.0** ; **contenu (assets + cartes) : CC BY-SA 4.0**.
  - Une **charte du hub** (conditions d'utilisation, pas une licence) garde les cartes publiées **libres et gratuites** ; retrait si mise derrière un péage.
- **Distribution gratuite** : itch.io, Steam (+ Steam Workshop). Les cartes vivent dans un **dépôt ouvert** (fichiers versionnés) avec **miroir Workshop** pour la découverte.
- **Pas de monétisation** ; d'éventuels cosmétiques seraient des **déblocages gratuits / récompenses communautaires**.

### Roadmap

1. **MVP** — 1-2 cartes faites main + **carte d'entraînement** (tutoriel) ; **socle de mouvement** (course/sprint, saut, wallrun, vault, glissade, relevé, amorti) ; boucle **time-trial avec ghost** (record perso + ghost de l'auteur, hors-ligne) ; **éditeur minimal** (poser des Segments + éléments de gameplay + tester) ; **solo** uniquement.
2. **Alpha** — **moveset complet** (MAG rope, coil, tyrolienne, balance, escalade, springboard…) et **génération procédurale** (assemblage de Segments).
3. **Beta** — **coop jusqu'à 3** (teamplay) et **partage des cartes** (dépôt ouvert + Workshop).
4. **Release** — classements en ligne, mode équipe, modération/curation, polish visuel et sonore, extensions de plateformes.

## 🚀 Vision

Un jeu de parkour simple à comprendre mais profond et fun, où la communauté construit sa propre expérience par l'éditeur et le partage. Un jeu qui vit autant par le **challenge compétitif** que par le **plaisir créatif et social** — le Minecraft du parkour.

---

## 📎 Décisions & vocabulaire

- Décisions d'architecture et de produit : `docs/adr/`.
- Vocabulaire du projet : `GLOSSARY.md`.
- Recherche : `docs/research/` — inventaire des **systèmes** de Mirror's Edge Catalyst, et **modèle de mouvement** de Mirror's Edge (2008) (machine d'état `TdMove_*` + valeurs de tuning réelles).
