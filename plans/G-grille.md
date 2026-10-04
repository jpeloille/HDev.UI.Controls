# Chantier G : grille et données

Chantier du plan de la suite (`../Plan-Suite.md`). Il couvre les tableaux 4.1 et 5.1 du benchmark. État au 04/10/2026. Statut : proposition.

**La grille fait foi dans HDev.UI.DataGrid.** Sa feuille de route, `IMPROVEMENTS.md` (§1 à §16, phases 1 à 4), reste la référence. Ce chantier ne la renumérote pas : il ordonne ses sections selon les jalons de la suite, et y ajoute ce que le benchmark et l'enquête du 04/10/2026 ont fait apparaître. Les lots G s'exécutent dans HDev.UI.DataGrid, avec ses tests.

## 0. Point de départ

<!-- généré:SCORES:4.1, 5.1 -->
| Tableau | Lignes | Aujourd'hui | S1 | S2 | S3 | S4 | S5 | S6 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 4.1 Grilles et données | 14 | 7 % | 7 % | 7 % | 39 % | 46 % | 93 % | 93 % |
| 5.1 Grille | 44 | 36 % | 36 % | 58 % | 77 % | 89 % | 98 % | 98 % |
<!-- fin -->

**Fait** : vue tableau, liaison à une collection observable, virtualisation et recyclage des lignes, tri multi-colonnes, ligne de filtre avec opérateurs, groupement multi-niveaux, résumés, édition in-place à événements annulables, sélection et navigation clavier, best-fit, gabarits de cellule, habillage clair et sombre.

**Constats de l'enquête du 04/10/2026** (lecture du code, pour E1)
- **Cellules.** Ce sont des contrôles à gabarit, sans dessin à la main. Seule la cellule en édition reçoit un éditeur, créé puis jeté.
- **Aucun contrat d'éditeur.**
  - `GetEditorValue()` reconnaît cinq contrôles Avalonia.
  - Pour tout autre éditeur, la grille réécrit la valeur d'avant l'édition, et annule ce qu'une liaison avait écrit.
- **`EditTemplate`** a pour DataContext la colonne, pas la ligne. Il ne reçoit pas de valeur initiale et ne gère ni Entrée ni Échap.
- **Validation de la cellule.**
  - La perte de focus ne valide pas la cellule, ni Tab.
  - Une propriété `DateTimeOffset` n'est jamais mise à jour.
  - Il n'y a ni `INotifyDataErrorInfo` ni `IDataErrorInfo`.
- **Risque relevé à la lecture, non testé** : après recyclage d'une cellule, une édition restée ouverte pourrait écrire dans une autre ligne.

**`IMPROVEMENTS.md` est à réaligner.** Son tableau § 0 date d'avant les lots de juillet 2026. Il donne encore l'édition pour un « stub », le groupement sans rendu, l'accès par réflexion et le filtre en `Contains` ; tout cela a été fait depuis. Une mise à jour est proposée, à faire dans HDev.UI.DataGrid.

## 1. Décisions proposées

- **Correction avant fonctions.** Le cycle d'édition se fiabilise (G0, G1) avant tout ajout.
- **Le contrat d'éditeur de cellule naît dans la grille** (`IGridCellEditor`, lot E11), avec ses tests ; les éditeurs HDev l'implémentent ensuite.
- **L'arbre et les propriétés s'appuient sur le moteur de la grille** (`IMPROVEMENTS.md` §10.2) : TreeList, grille verticale et inspecteur de propriétés, sans nouveau moteur de données.
- **Le tableau croisé dynamique** (G8) a son propre moteur d'agrégation. Il sera chiffré à part avant d'être lancé.

## 2. Lots

<!-- généré:LOTS:G -->
| Lot | Objet | Taille | Dépend de | Jalon | Lignes visées | Statut |
|---|---|---|---|---|---|---|
| **G0** | Caractérisation de l'édition dans la grille | S | — | S1 | — | 🔲 |
| **G1** | Cycle d'édition fiable | M | G0 | S1 | — | 🔲 |
| **G2** | Grille professionnelle : recherche, filtres, validation, ajout, layout | L | G1 | S2 | 5.1 : 8 | 🔲 |
| **G3** | Profondeur : filtres avancés, plages, presse-papiers, mise en forme | L | G2 | S3 | 4.1 : 2 ; 5.1 : 9 | 🔲 |
| **G4** | Arbre et propriétés : TreeList, grille verticale, inspecteur | L | G2 | S3 | 4.1 : 3 | 🔲 |
| **G5** | Finition : formulaire d'édition, glisser, fusion, colonnes calculées | L | G3, M1 | S4 | 4.1 : 1 ; 5.1 : 5 | 🔲 |
| **G6** | Volumes et hiérarchies : sources serveur, pagination, master-detail | XL | G2 | S5 | 4.1 : 3 ; 5.1 : 3 | 🔲 |
| **G7** | Vues alternatives : bandes, fiches, tuiles, Kanban | L | G2 | S5 | 4.1 : 3 ; 5.1 : 1 | 🔲 |
| **G8** | Tableau croisé dynamique | XL | G2 | S5 | 4.1 : 1 | 🔲 |
| **G9** | Lookup arborescent | S | G4, E7 | S5 | 4.2 : 1 | 🔲 |
<!-- fin -->

Correspondance avec `IMPROVEMENTS.md` :

| Lot | Sections |
|---|---|
| G1 | §2.2 cycle d'édition, §2.4 conversion typée |
| G2 | §2.3 validation, §2.5 ajout et suppression, §5.2 filtre style Excel, §5.4 panneau du filtre, §6.1 recherche, §7.1 sélecteur de colonnes, §7.7 menu d'en-tête, §13.1 layout ; écoute `INotifyPropertyChanged` par item |
| G3 | §3.6 intervalles, §4.1 agrégat personnalisé, §5.3 constructeur de filtre, §7.3 figées à droite, §8.1 plages, §8.3 et §8.4 presse-papiers, §11 mise en forme conditionnelle, §15.1 accessibilité |
| G4 | §10.2 arbre ; grille verticale et inspecteur |
| G5 | §1.5 hauteur variable, §7.6 fusion, §7.8 colonnes calculées, §14.1 glisser-déposer ; formulaire d'édition, navigateur |
| G6 | §9 volumes et pagination, §10.1 master-detail |
| G7 | §7.2 bandes ; fiches, tuiles, Kanban |
| G8 | tableau croisé (hors `IMPROVEMENTS.md`) |
| I3 (chantier I) | §12 export et impression |

## 3. Détail des lots

### G0 : caractérisation de l'édition (S)

**Objet.** Écrire en headless, avant toute correction, les constats de l'enquête :
- la réécriture de la valeur ;
- le DataContext d'`EditTemplate` ;
- l'écriture après recyclage ;
- `DateTimeOffset` ;
- la perte de focus et Tab.

Chaque constat est soit gravé (s'il est voulu), soit consigné comme défaut, sur le modèle d'E0.

**Acceptation.** Les vérifications passent trois fois de suite, et les défauts sont listés.

### G1 : cycle d'édition fiable (M)

**Contenu.**
- La lecture et l'écriture de la valeur passent par un contrat. Un éditeur non reconnu ne réécrase plus la valeur.
- `EditTemplate` reçoit la ligne en DataContext, une valeur initiale, et la gestion d'Entrée et d'Échap.
- L'écriture vise l'item en édition, pas la ligne recyclée.
- La conversion est typée et tient compte de la culture ; `DateTimeOffset` est pris en charge.
- La perte de focus et Tab valident la cellule.

**Acceptation.** Les défauts de G0 sont corrigés, et leurs tests passent au vert.

### G2 : grille professionnelle (L)

**Contenu.**
- La recherche globale, avec surlignage et navigation.
- Le filtre déroulant style Excel, et le panneau du filtre actif.
- La validation `INotifyDataErrorInfo`, qui bloque le commit, et la ligne d'ajout et la suppression.
- La mise à jour à chaque changement de propriété d'un item.
- Le sélecteur de colonnes et le menu d'en-tête.
- La sauvegarde et la restauration du layout.

**Acceptation.** Des tests headless pour chaque fonction, et des planches mises à jour.

### G3 : profondeur (L)

**Contenu.**
- Le constructeur de filtre, sur le moteur existant.
- Le groupement par intervalles et les résumés personnalisés.
- La sélection de plages, et le copier-coller en TSV et HTML.
- Les colonnes figées à droite, et les lignes épinglées.
- La mise en forme conditionnelle : règles, barres de données, échelles, icônes.
- L'accessibilité de la grille.

**Acceptation.**
- Des tests du moteur pour les règles et les intervalles.
- Des tests headless pour l'interaction.
- Un test sur l'arbre d'automation.
- La check-list 5.1 dépasse 75 % : la ligne « Grille de données, vue tableau » de 4.1 passe à ✅ (règle des contrôles phares, `../Plan-Suite.md`, § 6).

### G4 : arbre et propriétés (L)

**Contenu.**
- TreeList : une hiérarchie parent-enfant dans la grille, repliable, triable et filtrable.
- La grille verticale et l'inspecteur de propriétés, avec les catégories, les éditeurs par type (E11) et la recherche.

**Acceptation.** Des tests headless, et des planches.

### G5 : finition (L)

**Contenu.**
- Le formulaire d'édition d'une ligne, sur le Layout Control (M1).
- Le glisser-déposer de lignes.
- La fusion de cellules.
- La hauteur automatique, le retour à la ligne et l'aperçu de ligne.
- Les colonnes non liées ou calculées.
- Le navigateur d'enregistrements.

**Acceptation.** Des tests headless et des planches.

### G6 : volumes et hiérarchies (XL)

**Contenu.**
- Les sources serveur et virtuelles, où tri, filtre et groupe sont délégués au backend.
- Le chargement progressif et la pagination.
- Le master-detail.

**Acceptation.** Des bancs de mesure sur un million de lignes virtuelles. Le master-detail est testé en headless.

### G7 : vues alternatives (L)

**Contenu.** Les en-têtes à bandes, la vue fiches et cartes, et la vue tuiles et Kanban, avec gabarits.

**Acceptation.** Des planches et des tests headless.

### G8 : tableau croisé dynamique (XL)

**Contenu.**
- Un moteur d'agrégation multidimensionnel.
- Des champs en lignes, en colonnes et en valeurs, des totaux et des filtres.
- Un export via I2.

**Acceptation.** Les tests du moteur passent, et les performances sont mesurées.

### G9 : lookup arborescent (S)

**Contenu.** Le popup de lookup (E7) affiche une TreeList (G4). C'est la ligne « Lookup arborescent » de 4.2, renvoyée au plan grille par `Plan-Editeurs.md`.

**Acceptation.** Des tests d'interaction et une planche.

## 4. Projection

### 4.1 Grilles et données

<!-- généré:PROJ:4.1 -->
| Fonction | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|
| Grille de données, vue tableau | 🟡 | G3 | S3 | ✅ |
| Vue à bandes (en-têtes multi-niveaux) | ❌ | G7 | S5 | ✅ |
| Vue fiches, cartes | ❌ | G7 | S5 | ✅ |
| Vue tuiles, Kanban, explorateur | 🟡 | G7 | S5 | ✅ |
| Graphique intégré à la grille | ❌ | Q7 | — | ⚪ proposé |
| Gros volumes (server mode, instant feedback, sources virtuelles) | ❌ | G6 | S5 | ✅ |
| Master-detail | ❌ | G6 | S5 | ✅ |
| Pagination | ❌ | G6 | S5 | ✅ |
| Navigateur d'enregistrements | ❌ | G5 | S4 | ✅ |
| Constructeur de filtre | ❌ | G3 | S3 | ✅ |
| Arbre multi-colonnes (TreeList) | ❌ | G4 | S3 | ✅ |
| Grille verticale | ❌ | G4 | S3 | ✅ |
| Inspecteur de propriétés | ❌ | G4 | S3 | ✅ |
| Tableau croisé dynamique | ❌ | G8 | S5 | ✅ |
<!-- fin -->

### 5.1 Grille

<!-- généré:PROJ:5.1 -->
| Capacité | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|
| Vue tableau, colonnes générées automatiquement | ✅ | — | — | inchangé |
| Vues alternatives (bandes, cartes, tuiles, explorateur) | ❌ | G7 | S5 | ✅ |
| Liaison à une collection observable | ✅ | — | — | inchangé |
| Mise à jour quand une propriété d'un item change | ❌ | G2 | S2 | ✅ |
| Gros volumes côté serveur | ❌ | G6 | S5 | ✅ |
| Virtualisation des lignes, recyclage des cellules | ✅ | — | — | inchangé |
| Tri multi-colonnes | ✅ | — | — | inchangé |
| Ligne de filtre, opérateurs par type | ✅ | — | — | inchangé |
| Filtre déroulant style Excel | ❌ | G2 | S2 | ✅ |
| Constructeur de filtre (ET/OU) | ❌ | G3 | S3 | ✅ |
| Panneau du filtre actif | ❌ | G2 | S2 | ✅ |
| Recherche globale (Find Panel, Search Panel) | ❌ | G2 | S2 | ✅ |
| Groupement multi-niveaux par glisser-déposer | ✅ | — | — | inchangé |
| Groupement par intervalles ou personnalisé | ❌ | G3 | S3 | ✅ |
| Résumés de pied et de groupe | ✅ | — | — | inchangé |
| Résumés personnalisés | ❌ | G3 | S3 | ✅ |
| Master-detail | ❌ | G6 | S5 | ✅ |
| Édition in-place, événements annulables | ✅ | G1 | — | inchangé |
| Éditeurs in-place communs à la suite | 🟡 | E11 | S3 | ✅ |
| Formulaire d'édition (Edit Form) | ❌ | G5 | S4 | ✅ |
| Ligne d'ajout, suppression | ❌ | G2 | S2 | ✅ |
| Validation | ❌ | G2 | S2 | ✅ |
| Sélection multiple, cellule courante | ✅ | — | — | inchangé |
| Sélection de plages rectangulaires de cellules | ❌ | G3 | S3 | ✅ |
| Navigation clavier complète | ✅ | — | — | inchangé |
| Copier | 🟡 | G3 | S3 | ✅ |
| Coller | ❌ | G3 | S3 | ✅ |
| Glisser-déposer de lignes | ❌ | G5 | S4 | ✅ |
| Colonnes figées | 🟡 | G3 | S3 | ✅ |
| Redimensionnement, réordonnancement, best-fit, verrouillage | ✅ | — | — | inchangé |
| Sélecteur de colonnes, menu d'en-tête | ❌ | G2 | S2 | ✅ |
| Formatage conditionnel | ❌ | G3 | S3 | ✅ |
| Fusion de cellules | ❌ | G5 | S4 | ✅ |
| Hauteur de ligne automatique, retour à la ligne, aperçu de ligne | ❌ | G5 | S4 | ✅ |
| Colonnes non liées ou calculées | ❌ | G5 | S4 | ✅ |
| Gabarits de cellule | ✅ | — | — | inchangé |
| Export | 🟡 | I3 | S2 | ✅ |
| Impression, aperçu | ❌ | I3 | S2 | ✅ |
| Sauvegarde et restauration du layout | ❌ | G2 | S2 | ✅ |
| Pagination | ❌ | G6 | S5 | ✅ |
| Habillage clair et sombre | ✅ | — | — | inchangé |
| Indicateur et numéros de ligne, états « vide » et « chargement » | ✅ | — | — | inchangé |
| Accessibilité (UI Automation) | ❌ | G3 | S3 | ✅ |
| IA (Smart Paste, recherche sémantique) | ❌ | Q6 | — | ⚪ proposé |
<!-- fin -->

### Lignes d'autres tableaux

<!-- généré:AUTRES:G:4.1, 5.1 -->
| Tableau | Ligne | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|---|
| 4.2 | Lookup arborescent | ❌ | G9 | S5 | ✅ |
<!-- fin -->

Le graphique intégré à la grille, une fonction VCL seulement, est proposé en absence par décision (Q7). Une grille et un graphique liés à la même source (V1) couvrent l'usage.

## 5. Risques

- **Deux dépôts.** Chaque lot entre d'abord dans HDev.UI.DataGrid, puis la suite le reprend. `IMPROVEMENTS.md` doit suivre.
- **Le recyclage des cellules** se combine mal avec l'édition et la sélection de cellule. G0 doit le couvrir avant toute autre fonction.
- **G6 et G8 sont de taille XL.** Ils seront chiffrés de nouveau quand G2 sera fait.
