# ProControls — Contexte du projet

## Objectif

Créer une **bibliothèque de contrôles Avalonia UI professionnels** avec le style **Ubuntu 26.04 (Yaru / GNOME)** pour les applications métier **Synaxis** (gestion des opérations de vol) et **Ovidie** (gestion financière).

## Caractéristiques clés

### 1. Style Ubuntu 26.04 (Yaru)

- **Palette de couleurs** : Gris/blanc subtil, sobre et professionnel
- **Accents** : Orange Ubuntu (#E95420) pour les éléments interactifs et focus
- **Typographie** : Ubuntu Sans (police système), 14px body, 16px subtitle, 19px title
- **Bordures** : Fines (1px), couleurs subtiles (#E0E0E0, #CCCCCC)
- **États visuels** : Hover, pressed, focused, disabled — tous cohérents
- **Coins arrondis** : libadwaita (6px boutons, 8px popovers, 12px cartes)

### 2. Architecture technique

- **Rendu "from scratch"** : Contrôles en C# pur avec `DrawingContext`
- **Pas de templates XAML** : Contrôle total sur chaque pixel
- **Thème centralisé** : `ProTheme.cs` contient toutes les constantes (couleurs, tailles, polices)
- **Héritage minimal** : Hériter de `Control` ou `ContentControl` selon le besoin

### 3. Indépendance technologique

- **Aucune dépendance vendor** : Pas de DevExpress, Telerik, etc.
- **Souveraineté technologique** : Code source maîtrisé à 100%
- **Dépendances** : Avalonia UI 11.2.1 + projet frère `Julien.Avalonia.DataGrid` (datagrid maison, ../AvaloniaDataGrid)

### 4. État des contrôles (audit du 10/07/2026)

Légende : ✅ complet · 🟡 fonctionnel (trous ciblés) · 🟠 façade (API déclarée, cœur non câblé) · 🔲 à faire

| Contrôle | Équivalent DevExpress | État | Trous connus |
|----------|----------------------|------|--------------|
| ProButton | SimpleButton | ✅ | pas de variantes DropDown/Split |
| ProCheckBox | CheckEdit | ✅ | — (tri-state réel) |
| ProRadioButton | RadioGroup | ✅ | — (exclusivité + flèches OK) |
| ProToggleSwitch | ToggleSwitch | ✅ | pas d'animation du thumb |
| ProSlider | TrackBarControl | ✅ | horizontal seulement, pas de range |
| ProProgressBar | ProgressBar + Marquee | ✅ | — |
| ProBadge | Badge | ✅ | Info = Primary (même couleur) |
| ProTextBox | TextEdit | 🟡 | **ni masque, ni validation** (HasError = flag externe) ; pas de multiline |
| ProCard | GroupControl | 🟡 | `Elevation` déclarée jamais appliquée ; layout construit une seule fois (Title changé après coup ignoré) |
| ProWindow | XtraForm | 🟡 | chrome custom = chemin secondaire (natif par défaut, cf. X11) ; Light forcé, pas de dark ; champs restore morts |
| ProMenuBar / ProMenuItem / ProContextMenu | BarManager, PopupMenu | 🟡 | `Shortcut` décoratif (aucun raccourci réel), pas de mnémoniques Alt+lettre ni navigation clavier ; `IsEnabled` n'empêche pas le clic |
| ProRibbon | RibbonControl | 🟠 | rendu = boutons + séparateurs SEULEMENT ; `ProRibbonComboBox`/`Gallery`/`ToggleGroup`/`DialogLauncher`/onglets contextuels déclarés mais jamais rendus ; pas de backstage/QAT |
| ProComboBox | ComboBoxEdit | 🟠 | combo à **sélection seule** : pas de saisie texte (pas de TextBox interne) ; `AutoComplete`/`TextEditStyle`/`Sorted`/`ImmediatePopup`/`DrawItem` déclarés jamais lus ; `SelectAll()` vide. = ComboBoxEdit en `DisableTextEditor` |
| JDataGrid (réf. `../AvaloniaDataGrid`, source de vérité) | GridControl/GridView | 🟡 | voir section JDataGrid ci-dessous |
| ProTabControl | XtraTabControl | 🔲 | — |
| ProTreeView | (TreeList sans colonnes) | 🔲 | — |
| ProToolbar | Bars | 🔲 | — |
| ProStatusBar | RibbonStatusBar | 🔲 | — |

### 4bis. JDataGrid — état (audit du 10/07/2026)

**Fait et réel** : tri multi-colonnes (moteur), édition in-place complète (BeginEdit/Commit/Cancel, éditeurs typés, événements annulables), groupement drag & drop multi-niveaux + group summaries, footer d'agrégats (Count/Sum/Avg/Min/Max), resize/réordonnancement/auto-génération colonnes, sélection ligne single/multi + clavier vertical, virtualisation lignes + recyclage, accès propriétés par délégués compilés (chemins `A.B.C`).

**Motif récurrent : moteur riche, UI pas branchée** :
- Filtrage : moteur ~20 opérateurs + groupes AND/OR, mais filter row = `Contains` codé en dur ; bouton filtre d'en-tête sans handler
- Multi-tri : `ThenBy` OK par API, Shift+clic non câblé
- Colonnes figées : `IsFrozen` OK en XAML, mais **le bouton pin ne gèle pas** (verrouille la position)
- Sélection cellule : `SelectCell`/`SelectedCells` existent, jamais appelés (le clic sélectionne la ligne)
- Best-fit : `MeasureColumnWidth` existe, `AutoFitWidth()` = `// TODO` vide
- Types de colonnes : 10 déclarés, mais Image/ProgressBar/Hyperlink/Button/Custom rendus en texte ; `CellTemplate`/`HeaderTemplate` jamais consommés

**Absent** : validation d'édition, new-row/suppression, recherche globale, filter panel/dropdown Excel, master-detail, bandes d'en-têtes, formatage conditionnel, export csv/xlsx, impression, copier/coller, navigation clavier horizontale, virtualisation colonnes, réactivité `INotifyPropertyChanged` par item, persistance layout, dark mode.

Note : le README de AvaloniaDataGrid est marketing (tout ✅), se fier au code / à cet audit.

### 4ter. Manques vs DevExpress WinForms, priorisés pour Synaxis/Ovidie

**Critique (bloquant apps métier)** :
- ProDateEdit / ProTimeEdit (FTL, planning, échéances) — l'éditeur le plus utilisé
- ProSpinEdit / ProCalcEdit (numérique formaté — cœur d'Ovidie)
- Socle commun masques + validation (prérequis des deux ci-dessus, trou de ProTextBox)
- ProTabControl
- ProLookUpEdit / GridLookUpEdit (référentiels avions/équipages/comptes ; s'appuyer sur JDataGrid)
- ProMessageBox / ProDialog (XtraMessageBox) — trivial, utilisé partout

**Important (structure d'application)** : ProTreeView puis TreeList (arbre+colonnes), ProToolbar, ProStatusBar, ProSearchControl, MemoEdit (multiline via ProTextBox), AlertControl/Toast, WaitForm/Splash, Docking (gros chantier, poste dispatcher), NavBar/Accordion, ButtonEdit, TokenEdit.

**Gros chantiers à arbitrer** :
- Scheduler / Gantt (planning équipages/pairings Synaxis — LE contrôle DX structurant)
- Charts / Gauges / Sparkline (dashboards Ovidie) — arbitrage maison vs lib OSS (LiveCharts2/ScottPlot) au regard de la souveraineté
- PivotGrid, VerticalGrid/PropertyGrid
- RichEdit/Spreadsheet/PdfViewer/Reports : hors périmètre de la lib de contrôles, besoins applicatifs séparés

**Ordre d'attaque recommandé** :
1. Quick wins : ProMessageBox, ProStatusBar, ProToolbar, multiline ProTextBox + **assainir les façades** (câbler ou retirer les propriétés mortes de ProComboBox/ProRibbon/ProCard)
2. Éditeurs : socle masque+validation → ProDateEdit → ProSpinEdit → ProComboBox éditable/autocomplete → ProLookUpEdit
3. JDataGrid : brancher l'UI sur le moteur existant (opérateurs de filtre, Shift+clic, pin→freeze, best-fit, sélection cellule, copier/coller, export CSV)
4. Gros chantiers : ProTabControl/TreeView, puis arbitrage Scheduler/Gantt et Charts

## Palette de couleurs Yaru (claire)

```csharp
// Backgrounds
Window      = #FFFFFF
Panel       = #FFFFFF  
Toolbar     = #F3F3F3
Control     = #FFFFFF
ControlHover = #E8E8E8
ControlPressed = #DADADA

// Text
Primary     = #1E1E1E
Secondary   = #6A6A6A
Disabled    = #A0A0A0
Inverse     = #FFFFFF

// Borders
Default     = #CCCCCC
Subtle      = #E0E0E0
Focus       = #0078D4

// Accents
Primary     = #0078D4
PrimaryHover = #106EBE
Success     = #107C10
Warning     = #CA5010
Error       = #D13438
Info        = #0078D4
```

## Applications cibles

### Synaxis
- Gestion des opérations de vol pour Aircalin
- Planification équipages, pairings, FTL
- Interface professionnelle pour pilotes et dispatchers

### Ovidie
- Gestion financière personnelle/entreprise
- Tableaux de bord, graphiques, rapports
- Interface sobre et efficace

## Principes de design

1. **Clarté** — L'interface doit être immédiatement compréhensible
2. **Cohérence** — Tous les contrôles suivent les mêmes règles visuelles
3. **Performance** — Rendu optimisé, pas de surcharge visuelle
4. **Accessibilité** — Contrastes suffisants, états focus visibles
5. **Professionnalisme** — Pas d'effets "gadget", sobre et efficace
