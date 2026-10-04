# Chantier O : Gantt et gestion de projet, alignés sur MS Project

Chantier du plan de la suite (`../Plan-Suite.md`). Il couvre le tableau 5.5 du benchmark et les lignes du Gantt dans 4.9. État au 04/10/2026. Statut : proposition.

**Décision Q1 du 04/10/2026.** La suite se conçoit pour elle-même, et le Gantt continue de s'aligner sur MS Project. Elle porte donc le programme de la charte UGantt (`UGantt-charte.md`, § 7), avec sa cible : « MS Project moins le `.mpp`, VBA et Project Server », c'est-à-dire les catégories 1 et 2.
- **Les lots L gardent tout de la charte** : identifiants, contenu, tailles et dépendances. Ce chantier les place dans les jalons de la suite.
  - L9, l'impression, devient le lot I1 du chantier I.
  - L25, les formules, relève de la décision Q12.
- **Lots propres à ce chantier** :
  - O0, la caractérisation avant la refonte du modèle ;
  - O1, l'extraction du moteur, si Q11 est acceptée.
- **Ce qui reste à une application**, selon ma lecture de la décision, à corriger au besoin :
  - la coquille : fenêtre, ruban, docking ;
  - les fichiers et le multi-projet ;
  - les modèles de projet ;
  - les rapports et les tableaux de bord.
- **L'interop de la suite se limite à MS Project XML.** Le XER de Primavera et le mappage CSV ou Excel ne servent pas l'alignement sur MS Project.

## 0. Point de départ

<!-- généré:SCORES:4.9, 5.5 -->
| Tableau | Lignes | Aujourd'hui | S1 | S2 | S3 | S4 | S5 | S6 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 4.9 Planification | 10 | 20 % | 20 % | 20 % | 20 % | 20 % | 80 % | 90 % |
| 5.5 Gantt | 27 | 57 % | 57 % | 63 % | 74 % | 74 % | 85 % | 100 % |
<!-- fin -->

- **Fait** :
  - le versionnage du schéma (L0) et l'édition structurelle (L1) ;
  - le moteur : agrégation des tâches récapitulatives, calendrier ouvré, chemin critique ;
  - l'ordonnancement automatique, sur des dépendances FS, SS, FF et SF avec décalage ;
  - la baseline ;
  - l'édition graphique et dans la table, avec annuler et rétablir ;
  - le zoom, et plus de 2 000 tâches virtualisées ;
  - la persistance JSON versionnée ;
  - la couleur par tâche.
- **Le moteur est presque pur.** Seule la couleur par tâche (`Avalonia.Media.Color`, dans le modèle et la sérialisation) le lie à Avalonia.
- **Manquent**, d'après la check-list 5.5 :
  - la liaison par mappings ;
  - les ressources, les affectations et la feuille de ressources ;
  - les contraintes typées, les tâches fractionnées et récurrentes ;
  - les colonnes configurables ;
  - la chronologie ;
  - MS Project XML ;
  - l'impression ;
  - la mise en forme des barres ;
  - les coûts et les champs personnalisés.

## 1. Décisions proposées

- **Q11 : le moteur dans un assembly pur.**
  - Un assembly de la suite, sans dépendance Avalonia, consommé par HDevGantt et par l'agenda pour les ressources.
  - **Pour** : il se teste seul, et il s'emploie sans interface (outil en ligne de commande, serveur).
  - **Contre** : un assembly de plus à maintenir.
  - **Coût** : remplacer `Color` par une valeur neutre.
  - Si la réponse est oui, O1 se fait avant L3.
- **Q13 : un seul modèle de ressources pour l'agenda et le Gantt.**
  - L10 le définit, et l'agenda (T1) s'en sert. L10 passe donc en S5, avant T1, alors que le reste du pilier ressources reste en S6.
- **Q12 : les formules (L25).**
  - La charte ne lançait L25 que sur une demande prouvée, au nom d'un usage applicatif.
  - L'alignement sur MS Project plaide pour des champs calculés par formule.
  - À trancher : planifier L25 (XL) après L17, ou le garder sur demande.
- **L'ordre reste celui de la charte**, justifié par les dépendances :
  - L3 (durée et travail) et L4 (calendriers) sont les clés de voûte du moteur ;
  - L12 (répartition échelonnée) est le lot le plus difficile, et il conditionne le nivellement, le suivi, la valeur acquise et les vues d'usage.
- **La rupture du modèle se consigne.**
  - L3 change le modèle des tâches : schéma v2 et migration des fichiers v1.
  - La liaison du projet (F2, S1) n'en dépend pas.
  - Les ruptures d'API vont dans `MIGRATION-gantt.md`.

## 2. Lots

<!-- généré:LOTS:O -->
| Lot | Objet | Taille | Dépend de | Jalon | Lignes visées | Statut |
|---|---|---|---|---|---|---|
| **L0** | Versionnage du schéma JSON | S | — | S1 | — | ✅ |
| **L1** | Édition structurelle de la table | M | — | S1 | — | ✅ |
| **O0** | Caractérisation du Gantt avant la refonte du modèle | S | L1 | S2 | — | 🔲 |
| **O1** | Extraction du moteur dans un assembly pur (si Q11) | M | O0, Q11 | S2 | — | 🔲 |
| **L2** | Modèle de colonnes configurable | M | L1 | S2 | 5.5 : 1 | 🔲 |
| **L3** | Durée, travail et types de tâches ; schéma v2 | XL | O0 | S2 | — | 🔲 |
| **L4** | Calendriers multiples et granularité horaire | XL | L3 | S2 | — | 🔲 |
| **L5** | Contraintes, échéances et inspecteur de dates | L | L3, L4 | S3 | 5.5 : 1 | 🔲 |
| **L6** | Passe avant et arrière complète, marges, chemins critiques multiples | M | L3, L4 | S3 | — | 🔲 |
| **L7** | Tâches fractionnées, récurrentes et inactives | M | L4 | S3 | 5.5 : 2 | 🔲 |
| **L8** | Baselines multiples et plans intermédiaires | S | L3 | S3 | — | 🔲 |
| **L10** | Modèle de ressources partagé et feuille de ressources | L | L4 | S5 | 4.9 : 1 ; 5.5 : 1 | 🔲 |
| **L11** | Affectations | L | L10, L3 | S6 | 5.5 : 1 | 🔲 |
| **L12** | Moteur de répartition échelonnée et contours de charge | XL | L11 | S6 | — | 🔲 |
| **L13** | Surutilisation et nivellement | XL | L12 | S6 | — | 🔲 |
| **L14** | Coûts : tables de taux, coûts fixes, budget | M | L11 | S6 | — | 🔲 |
| **L15** | Suivi avancé : réel, restant, date d'état | M | L12 | S6 | — | 🔲 |
| **L16** | Valeur acquise (EVM) | M | L12, L14, L8 | S6 | — | 🔲 |
| **L17** | Système de champs natifs et personnalisés | L | L2 | S6 | 5.5 : 1 | 🔲 |
| **L18** | Vues d'usage éditables et histogramme de ressources | L | L12, L17 | S6 | — | 🔲 |
| **L19** | Filtres, groupes et tris | L | L17 | S6 | — | 🔲 |
| **L20** | Tables et vues composables | L | L17, L19 | S6 | — | 🔲 |
| **L21** | Mise en forme des barres et couche d'adornement | L | L2 | S5 | 4.9 : 1 ; 5.5 : 1 | 🔲 |
| **L22** | Vue réseau PERT | M | — | S5 | — | 🔲 |
| **L23** | Vues calendrier, chronologie et planificateur d'équipe | L | L11 | S6 | 4.9 : 1 ; 5.5 : 1 | 🔲 |
| **L24** | Interop MS Project XML | L | L3, L10 | S6 | 5.5 : 1 | 🔲 |
<!-- fin -->

Placement dans les jalons de la suite :
- le jalon 1 de la charte (L2, L3, L4) tombe sur S2 ;
- le jalon 2 (L5 à L8) tombe sur S3 ;
- le modèle de ressources (L10), la mise en forme (L21) et le PERT (L22) tombent sur S5 ;
- les jalons 3 et 4 tombent sur S6.

## 3. Détail des lots

### O0 : caractérisation (S)

**Objet.** Figer le Gantt avant L3 :
- l'édition graphique et dans la table, et l'édition structurelle ;
- l'ordonnancement, le chemin critique et la baseline ;
- annuler et rétablir ;
- la sérialisation, sur des fichiers v1 de référence.

La méthode est celle d'E0.

**Acceptation.** Le banc passe trois fois de suite, et les fichiers v1 sont conservés pour tester la migration vers v2.

### O1 : extraction du moteur (M), si Q11

**Contenu.**
- Le modèle, l'ordonnancement, la structure, l'historique, la sérialisation et l'axe de temps passent dans un assembly sans Avalonia.
- La couleur devient une valeur neutre.
- HDevGantt consomme ce moteur.

**Acceptation.** Les tests xUnit du moteur tournent sans Avalonia, et les tests O0 restent verts.

### Lots de la charte (L2 à L24)

Le contenu fait foi dans `UGantt-charte.md`, § 7. Ne figurent ici que les précisions propres à la suite.
- **L2** : les colonnes configurées s'enregistrent avec l'espace de travail (F6).
- **L3, L4** : schéma v2, migration des fichiers v1, et allers-retours testés.
- **L5** : les contraintes typées (ligne 5.5).
- **L7** : les tâches fractionnées et récurrentes (lignes 5.5).
- **L10** : le modèle de ressources partagé (Q13) et la feuille de ressources (lignes 4.9 et 5.5).
- **L11** : les affectations (ligne « Ressources et affectations »).
- **L14 et L17** : les coûts et le système de champs (ligne « Coûts, champs personnalisés », S6).
- **L21** : la mise en forme des barres (ligne « Gabarits ou dessin personnalisé des barres »).
- **L22** : un rendu de graphe, que les diagrammes (V2) peuvent reprendre.
- **L23** : les vues calendrier, chronologie et planificateur d'équipe, dans le périmètre complet de la charte (lignes « Chronologie »).
- **L24** : MS Project XML seulement.

## 4. Projection

### 5.5 Gantt

<!-- généré:PROJ:5.5 -->
| Capacité | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|
| Table-arbre et diagramme | ✅ | — | — | inchangé |
| Tâches récapitulatives, jalons | ✅ | — | — | inchangé |
| Dépendances FS, SS, FF, SF avec décalage | ✅ | — | — | inchangé |
| Ordonnancement automatique | ✅ | — | — | inchangé |
| Calendrier ouvré (week-ends, jours fériés) | ✅ | — | — | inchangé |
| Chemin critique | ✅ | — | — | inchangé |
| Baseline | ✅ | — | — | inchangé |
| Édition graphique (déplacer, redimensionner, avancement, liens) | ✅ | — | — | inchangé |
| Édition in-place de la table | ✅ | — | — | inchangé |
| Édition structurelle (insérer, supprimer, indenter, déplacer) | ✅ | — | — | inchangé |
| Annuler, rétablir | ✅ | — | — | inchangé |
| Zoom de l'échelle de temps | ✅ | — | — | inchangé |
| Gros volumes | ✅ | — | — | inchangé |
| Couleur par tâche | ✅ | — | — | inchangé |
| Persistance | ✅ | — | — | inchangé |
| Liaison de données (mappings) | ❌ | T5 | S5 | ✅ |
| Ressources et affectations | ❌ | L11 | S6 | ✅ |
| Feuille de ressources | ❌ | L10 | S5 | ✅ |
| Contraintes typées (8 types) | ❌ | L5 | S3 | ✅ |
| Tâches fractionnées | ❌ | L7 | S3 | ✅ |
| Tâches récurrentes | ❌ | L7 | S3 | ✅ |
| Colonnes configurables | ❌ | L2 | S2 | ✅ |
| Vue chronologie | ❌ | L23 | S6 | ✅ |
| Import et export MS Project XML | ❌ | L24 | S6 | ✅ |
| Impression, export | 🟡 | I3 | S2 | ✅ |
| Gabarits ou dessin personnalisé des barres | ❌ | L21 | S5 | ✅ |
| Coûts, champs personnalisés | ❌ | L17 | S6 | ✅ |
<!-- fin -->

### Lignes d'autres tableaux

<!-- généré:AUTRES:O:5.5 -->
| Tableau | Ligne | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|---|
| 4.9 | Gantt de projet | 🟡 | L21 | S5 | ✅ |
| 4.9 | Feuille de ressources | ❌ | L10 | S5 | ✅ |
| 4.9 | Chronologie du projet | ❌ | L23 | S6 | ✅ |
<!-- fin -->

La ligne « Gantt de projet » (4.9) passe à ✅ en S5, quand la check-list 5.5 atteint 85 %. C'est la règle des contrôles phares (`../Plan-Suite.md`, § 6).

## 5. Risques

- **Taille.** Quatre lots sont de taille XL : L3, L4, L12 et L13. S6 porte l'essentiel du programme.
- **L3 est une refonte cassante** du modèle et du format. O0 et les fichiers témoins doivent passer d'abord.
- **L12 est « le boss du programme »** selon la charte. Il conditionne quatre autres lots.
- **La fidélité de MS Project XML** se mesure sur un corpus de fichiers témoins.
