# Chantier V : visualisation de données

Chantier du plan de la suite (`../Plan-Suite.md`). Il couvre le tableau 4.10 du benchmark. État au 04/10/2026. Statut : proposition.

## 0. Point de départ

<!-- généré:SCORES:4.10 -->
| Tableau | Lignes | Aujourd'hui | S1 | S2 | S3 | S4 | S5 | S6 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 4.10 Visualisation de données | 13 | 4 % | 4 % | 35 % | 35 % | 35 % | 77 % | 77 % |
<!-- fin -->

- **Aucun graphique n'est intégré.** LiveCharts2 a été choisi le 14/07/2026 pour les graphiques, les jauges et les sparklines, mais il n'apparaît que comme un libellé dans la démo. Les trois lignes sont ⚪, déléguées.
- **Cette délégation a été décidée pour une application.** Sous le principe du 04/10/2026, elle doit être rejustifiée pour la suite (Q2).
- **HDevMindMap existe.** C'est un éditeur dédié, avec trois dispositions, mais sans sérialisation ni liaison aux données.
- **Rien d'autre.** Ni carte géographique, ni diagrammes, ni TreeMap, Sunburst, Sankey ou Heatmap, ni sélection de plage.

C'est la famille la plus faible du benchmark (4 %).

## 1. Décisions proposées

- **Graphiques (Q2)** : garder LiveCharts2 (MIT, natif Avalonia, rendu Skia), sous un habillage HDev.
  - Thème clair et sombre, palette catégorielle validée pour le contraste et le daltonisme.
  - Typographie de la suite.
  - Liaison MVVM.
  - Impression par I1.
  - Il serait livré dans un paquet séparé, `HDev.UI.Charts`, pour garder la bibliothèque sans dépendance tierce.
  - **Alternative** : des graphiques maison, sur le rendu de la suite. C'est plus souverain, mais bien plus long.
- **Le reste est dessiné maison**, sur les moteurs déjà présents :
  - TreeMap, Sunburst et Sankey : des dispositions pures, testées comme celles de la carte mentale ;
  - les diagrammes : sur le moteur de la carte mentale et le rendu de graphe de L22 (PERT) ;
  - la sélection de plage : un curseur de plage (E9) posé sur un aperçu de graphique.
- **Absences proposées (Q7)** : les graphiques 3D, la carte géographique et le SVG interactif.
  - La carte pourrait aussi passer par une bibliothèque libre de cartographie. Elle dépendrait alors de tuiles réseau : c'est à trancher.

## 2. Lots

<!-- généré:LOTS:V -->
| Lot | Objet | Taille | Dépend de | Jalon | Lignes visées | Statut |
|---|---|---|---|---|---|---|
| **V0** | Prototype : graphiques sous HDevTheme | S | Q2 | S2 | — | 🔲 |
| **V1** | Graphiques, jauges, sparklines, heatmap | L | V0, I1 | S2 | 4.10 : 4 | 🔲 |
| **V2** | Diagrammes et carte mentale v2 | L | — | S5 | 4.10 : 2 | 🔲 |
| **V3** | TreeMap, Sunburst, Sankey | L | V1 | S5 | 4.10 : 3 | 🔲 |
| **V4** | Sélection de plage sur un aperçu | M | V1 | S5 | 4.10 : 1 | 🔲 |
<!-- fin -->

## 3. Détail des lots

### V0 : prototype des graphiques sous HDevTheme (S)

**Objet.** Avant d'intégrer, vérifier sur un graphique en barres, une courbe et une jauge :
- le rendu du texte (`Crisp.Snap`) ;
- la bascule de thème à chaud ;
- le contraste et la palette ;
- l'impression ;
- le poids de la dépendance.

**Acceptation.** Une note de mesure, et la réponse à Q2.

### V1 : graphiques, jauges, sparklines, heatmap (L)

**Contenu.**
- HDevChart : barres, courbes, aires, secteurs, nuages, avec légende, infobulles et axes.
- HDevGauge, HDevSparkline et HDevHeatmap.
- Liaison MVVM, thème et impression.

**Acceptation.** Planches en clair et en sombre, validation de la palette, impression PDF.

### V2 : diagrammes et carte mentale v2 (L)

**Contenu.**
- La carte mentale : sérialisation (JSON et FreeMind), liaison aux données, notes et icônes.
- Un éditeur de diagrammes : formes, connecteurs routés, dispositions automatiques (arbre, couches) ; il peut reprendre le rendu de graphe de L22.

**Acceptation.** Les dispositions sont testées en xUnit, et l'édition en headless.

### V3 : TreeMap, Sunburst, Sankey (L)

**Contenu.** Les dispositions pures et testées ; le rendu, les infobulles et la sélection.

**Acceptation.** Des tests de disposition, et des planches.

### V4 : sélection de plage sur un aperçu (M)

**Contenu.** Un aperçu miniature d'une série, avec une fenêtre déplaçable et redimensionnable, liée à l'axe d'un graphique.

**Acceptation.** Des tests d'interaction.

## 4. Projection

<!-- généré:PROJ:4.10 -->
| Fonction | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|
| Graphiques 2D | ⚪ | V1 | S2 | ✅ |
| Graphiques 3D | ❌ | Q7 | — | ⚪ proposé |
| Jauges | ⚪ | V1 | S2 | ✅ |
| Sparklines | ⚪ | V1 | S2 | ✅ |
| Carte géographique | ❌ | Q7 | — | ⚪ proposé |
| Diagrammes, logigrammes | ❌ | V2 | S5 | ✅ |
| Organigramme, carte mentale | 🟡 | V2 | S5 | ✅ |
| TreeMap | ❌ | V3 | S5 | ✅ |
| Sunburst | ❌ | V3 | S5 | ✅ |
| Sankey | ❌ | V3 | S5 | ✅ |
| Heatmap | ❌ | V1 | S2 | ✅ |
| Sélection de plage sur un aperçu | ❌ | V4 | S5 | ✅ |
| SVG interactif | ❌ | Q7 | — | ⚪ proposé |
<!-- fin -->

## 5. Risques

- **Q2 conditionne V0 et V1**, et donc le gain de S2 sur la famille la plus faible.
- **Une dépendance tierce** se maintient, se suit et peut changer de cap (Q9).
- **Les dispositions maison** demandent les mêmes tests de géométrie que celles de la carte mentale.
