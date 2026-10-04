# Benchmark HDev.UI ↔ DevExpress WinForms · WPF · VCL

Table de correspondance contrôle par contrôle, état au **04/10/2026**.

## En bref

- **Catalogue.** 166 fonctions DevExpress recensées (union WinForms, WPF et VCL v26.1). HDev en couvre **34** pour l'usage courant et **25** en partie. **107** sont absentes, dont **11** par décision. Score : **28 %**, ou **30 %** hors absences décidées.
- **Profondeur des contrôles phares**, mesurée sur les capacités que DevExpress met en avant :

  | Contrôle | Score |
  |---|---:|
  | Gantt | 57 % |
  | Socle des éditeurs | 53 % |
  | Ruban et barres | 44 % |
  | Docking | 44 % |
  | Texte riche | 39 % |
  | Agenda | 37 % |
  | Grille | 36 % |

- **Forces.**
  - Seule suite du comparatif qui tourne nativement sous Linux.
  - Aucune licence, code intégral, une seule dépendance (Avalonia, MIT).
  - Un socle complet pour une application de bureau.
  - Le Gantt calcule le chemin critique, que la VCL ne documente pas.
- **Trous structurants.**
  - Aucun graphique intégré.
  - La famille de grilles avancées manque entièrement : TreeList, PropertyGrid, Pivot, Layout Control.
  - Aucune impression, peu d'export.
  - Hors la grille et les contrôles simples, valeurs et sélections ne sont pas liables en XAML.
  - Localisation et accessibilité absentes ou embryonnaires.

## 0. Cadre

**Ce qui est comparé**

| | Version | Référence |
|---|---|---|
| HDev.UI.Controls | commit `4fc218e` (accessibilité et contraste commités le 04/10 : `HDevFocus`, `HDevRadioGroup`, jetons de contraste, `ThemeContrastTests`, `HDevButtonAutomationPeer`) | code et tests |
| HDev.UI.DataGrid | commit `7dd3ae1` | code et tests |
| DevExpress WinForms | v26.1, build 26.1.5 du 18/09/2026 | docs.devexpress.com |
| DevExpress WPF | v26.1, build 26.1.5 | docs.devexpress.com |
| DevExpress VCL | v26.1, build 26.1.4 du 07/08/2026 | docs.devexpress.com |

DevExpress annonce la v26.2 pour décembre 2026 sur les trois plateformes.

**Méthode**

- **Lignes.** Elles viennent des catalogues DevExpress (union des trois plateformes), regroupées par fonction. On ne part pas de la liste HDev : un manque reste ainsi visible.
- **Colonne HDev.** Elle est remplie depuis le code et les tests, pas depuis les README.
- **Notations des colonnes DevExpress.** « — » : pas d'équivalent sur cette plateforme. « (nv) » : nom de classe non confirmé sur une page officielle.
- **Socle Avalonia.** Quelques fonctions de base sont couvertes par Avalonia lui-même (`TextBlock`, `GridSplitter`, `ScrollViewer`, `StorageProvider`). Elles comptent ✅ et portent la mention « socle ».
- **Pondération.** Les lignes ne sont pas pondérées : « Grille » pèse autant que « Code-barres ». La profondeur des contrôles phares est donc mesurée à part (§ 4).
- **Exclus du décompte.**
  - Snap (WinForms, en maintenance) ;
  - Code Editor (exemple Monaco, non livré) ;
  - ExpressEntityMapping (doc retirée depuis v22.1) ;
  - items VCL dépréciés.

**Légende (colonne « État »)**

| | Sens |
|---|---|
| ✅ | équivalent pour l'usage courant |
| 🟡 | partiel : le contrôle existe, mais des capacités clés manquent |
| ❌ | absent |
| ⚪ | absent par décision (hors périmètre, délégué ou refusé) ; compté comme non couvert |

Score d'une famille = (✅ + ½ 🟡) / nombre de lignes.

## 1. Synthèse par famille

| Famille | Lignes | ✅ | 🟡 | ❌ | ⚪ | Score |
|---|---:|---:|---:|---:|---:|---:|
| 3.1 Grilles et données | 14 | 0 | 2 | 12 | 0 | 7 % |
| 3.2 Éditeurs | 48 | 17 | 3 | 28 | 0 | 39 % |
| 3.3 Boutons et libellés | 6 | 4 | 0 | 2 | 0 | 67 % |
| 3.4 Rubans, barres et menus | 17 | 4 | 3 | 9 | 1 | 32 % |
| 3.5 Navigation | 14 | 2 | 3 | 9 | 0 | 25 % |
| 3.6 Mise en page | 6 | 2 | 0 | 4 | 0 | 33 % |
| 3.7 Docking et documents | 5 | 0 | 2 | 2 | 1 | 20 % |
| 3.8 Fenêtres, dialogues, notifications | 13 | 4 | 2 | 7 | 0 | 38 % |
| 3.9 Planification | 10 | 0 | 4 | 5 | 1 | 20 % |
| 3.10 Visualisation de données | 13 | 0 | 1 | 9 | 3 | 4 % |
| 3.11 Documents | 6 | 1 | 1 | 1 | 3 | 25 % |
| 3.12 Rapports, impression, export | 4 | 0 | 1 | 1 | 2 | 13 % |
| 3.13 Composants transverses | 10 | 0 | 3 | 7 | 0 | 15 % |
| **Total** | **166** | **34** | **25** | **96** | **11** | **28 %** |

- Hors absences décidées (⚪ retirés du dénominateur), le score monte à **30 %**.
- Parmi les ✅, trois relèvent du socle Avalonia.

## 2. Dimensions transverses

| Dimension | WinForms | WPF | VCL | HDev.UI |
|---|---|---|---|---|
| OS | Windows seul | Windows seul ; Linux et macOS via Avalonia XPF, un produit tiers que DevExpress ne supporte pas | Windows seul ; la grille FireMonkey multiplateforme est en pause depuis 2020 | Linux (X11, XWayland), plateforme de validation ; Windows et macOS possibles via Avalonia, non qualifiés |
| Runtime | .NET 8, 9, 10 et .NET Framework 4.6.2+ ; .NET 10 minimum en v26.2 | idem ; ARM64 documenté | Delphi et C++Builder 10.4 à 13.1, Win32 et Win64 ; Arm64EC en CTP | .NET 10, C# 14, Avalonia 12.1.3 |
| Rendu | GDI+ ; DirectX en option sur une partie des contrôles | pipeline WPF (DirectX) | GDI ; GDI+ ou Direct2D sur quelques contrôles (Chart, Gantt, Tile…) | Skia via Avalonia ; contrôles dessinés en `DrawingContext`, texte calé au pixel physique |
| Thèmes | 50+ skins (WXI, Basic, The Bezier, Office 2019, High Contrast…), palettes, Skin Editor, suivi du mode sombre Windows | 30+ thèmes (Win11, Office 2019, VS2019…), Theme Designer ; Fluent Light en CTP | 55+ skins dont 8 vectoriels, Skin Editor | Yaru clair et sombre, bascule à chaud, palettes injectables par l'app hôte ; garde de contraste WCAG testée |
| Accessibilité | MSAA et UI Automation ; conformité 508, WCAG 2.0 AA et EN 301 549 revendiquée ; ACR de juin 2026 | UI Automation ; même revendication ; ACR de juin 2026 | UI Automation en développement, désactivée par défaut ; aucune conformité revendiquée | focus visible au clavier, contraste testé, un seul `AutomationPeer` (`HDevButton`) |
| Localisation | assemblies satellites ; de, es et ja livrés ; Localization Tool | idem | `TcxLocalizer` (INI, RES) | aucune : libellés français en dur |
| RTL | `RightToLeft` global | `FlowDirection`, avec des limites dans la grille | `BiDiMode`, sauf la grille | non |
| MVVM et liaison | MVVM Framework (`MVVMContext`), compatible CommunityToolkit.Mvvm | MVVM Framework (gratuit, MIT), DI en XAML, Prism 9 | contrôles data-aware `TcxDB*` sur `TDataSet` | partiel (détail sous le tableau) |
| Design-time | designers Visual Studio, smart tags, Template Kit | smart tags XAML, Template Kit, Theme Designer | intégration RAD Studio, VCL Template Kit | prévisualiseur Avalonia seulement |
| Impression et export | Printing-Exporting : PDF (PDF/UA-2), XLS(X), DOCX, RTF, HTML, images | idem | ExpressPrinting System (PDF 1.4) ; exports XLS(X), CSV et HTML de la grille | CSV (grille), PNG (Gantt, carte mentale) ; aucune impression |
| IA | AI Chat, AI Assistant, Smart Paste, recherche sémantique, Prompt to Expression (Microsoft.Extensions.AI) | idem | AI Assistant, Smart Paste ; pas d'AI Chat | aucune |
| Code source | offres DXperience et Universal seulement | idem | toutes les offres VCL | intégral |
| Licence (prix public) | 1 099,99 $ par développeur (WinForms) ; DXperience 1 699,99 $ ; Universal 2 299,99 $ ; renouvellement annuel ≈ 50 % | 1 099,99 $ (WPF) ; mêmes offres groupées | VCL 1 699,99 $, VCL+ 2 499,99 $ ; non incluse dans Universal | aucune ; le coût est le temps de développement et de maintenance |
| Dépendance éditeur | DevExpress | DevExpress | DevExpress et Embarcadero (RAD Studio) | Avalonia (MIT) |

Détail de la liaison MVVM côté HDev (relevé exhaustif des propriétés de valeur et de sélection) :
- **Liables** (propriétés Avalonia) :
  - `Text` de `HDevTextBox` ;
  - `IsChecked` de CheckBox, RadioButton et RadioCard ;
  - `IsOn` de ToggleSwitch ;
  - `Value` de Slider et de ProgressBar ;
  - toute l'API de `HDevDataGrid` (`ItemsSource`, `SelectedItem`…).
- **Propriétés CLR, donc non liables en XAML :**
  - valeurs de DateEdit, TimeEdit, SpinEdit, ComboBox, LookUpEdit, TokenEdit, SearchControl et MonthCalendar ;
  - source et sélections de ListView, TabControl, ToggleButtonGroup, Chip, NavBar et TreeView ;
  - modèles du Gantt, de l'agenda, du roster et de la carte mentale ;
  - `Html` de RichEdit et de HtmlView ;
  - layout du docking.
- L'arbre, le ruban, l'agenda et le Gantt se construisent en code.

## 3. Table de correspondance

### 3.1 Grilles et données

| Fonction | WinForms | WPF | VCL | HDev | État | Écart / remarque |
|---|---|---|---|---|---|---|
| Grille de données, vue tableau | `GridControl` + `GridView` | `GridControl` + `TableView` | `TcxGrid` + `TcxGridTableView` | `HDevDataGrid` | 🟡 | 36 % des capacités DevExpress (§ 4.1) |
| Vue à bandes (en-têtes multi-niveaux) | `BandedGridView`, `AdvBandedGridView` | `GridControlBand` | `TcxGridBandedTableView` | — | ❌ | |
| Vue fiches, cartes | `CardView`, `LayoutView` | `CardView` | `TcxGridCardView`, `TcxGridLayoutView` | — | ❌ | |
| Vue tuiles, Kanban, explorateur | `TileView`, `WinExplorerView`, `ItemsView` | — | `TcxGridWinExplorerView` | `HDevListView` | 🟡 | items riches et groupes ; ni tuiles, ni Kanban, ni gabarits |
| Graphique intégré à la grille | — | — | `TcxGridChartView` | — | ❌ | |
| Gros volumes (server mode, instant feedback, sources virtuelles) | Server Mode, Instant Feedback, `VirtualServerModeSource` | Server Mode, Instant Feedback, Virtual Sources | vues Server Mode, sources ADO, FireDAC, dbExpress | — | ❌ | données en mémoire ; seule l'UI est virtualisée |
| Master-detail | niveaux du `GridControl` | `DataControlDetailDescriptor`… | `TcxGridLevel` | — | ❌ | |
| Pagination | Pager Navigation (behavior) | `PagerControl`, `AllowPaging` | — | — | ❌ | |
| Navigateur d'enregistrements | `DataNavigator`, `ControlNavigator` | — | `TcxNavigator`, `TcxDBNavigator` | — | ❌ | |
| Constructeur de filtre | `FilterControl`, `FilterEditorControl` | `FilterEditorControl` | `TcxFilterControl` | — | ❌ | moteur ET/OU présent dans `GridFilter`, sans UI |
| Arbre multi-colonnes (TreeList) | `TreeList` | `TreeListControl` | `TcxTreeList`, `TcxDBTreeList`, `TcxVirtualTreeList` | — | ❌ | `HDevTreeView` est mono-colonne ; la table-arbre du Gantt a 4 colonnes figées |
| Grille verticale | `VGridControl` | — | `TcxVerticalGrid`, `TcxDBVerticalGrid` | — | ❌ | |
| Inspecteur de propriétés | `PropertyGridControl` | `PropertyGridControl` | `TcxRTTIInspector` | — | ❌ | |
| Tableau croisé dynamique | `PivotGridControl` | `PivotGridControl` | `TcxPivotGrid` (+ OLAP) | — | ❌ | |

### 3.2 Éditeurs

**Texte**

| Fonction | WinForms | WPF | VCL | HDev | État | Écart / remarque |
|---|---|---|---|---|---|---|
| Texte mono-ligne | `TextEdit` | `TextEdit` | `TcxTextEdit` | `HDevTextBox` | ✅ | |
| Texte masqué | `TextEdit` (`MaskSettings`) | `TextEdit` (masques) | `TcxMaskEdit` | `HDevEditorBase.Mask` | 🟡 | masque simple (0/9/L/A) sur les éditeurs du socle ; ni numérique, ni regex ; aucun champ texte masqué autonome ; le collage contourne le masque |
| Mot de passe | `TextEdit` | `PasswordBoxEdit` | `TcxTextEdit` | `HDevTextBox` (`IsPassword`) | ✅ | |
| Multi-ligne | `MemoEdit` | `TextEdit` (nv) | `TcxMemo` | `HDevTextBox` (`IsMultiline`) | ✅ | |
| Mémo en popup | `MemoExEdit` | `MemoEdit` | `TcxBlobEdit` | — | ❌ | |
| Texte avec boutons | `ButtonEdit` | `ButtonEdit` | `TcxButtonEdit` | — | ❌ | boutons-glyphes internes seulement, aucune collection publique |
| Historique des saisies (MRU) | `MRUEdit` | — | `TcxMRUEdit` | — | ❌ | |
| Lien hypertexte | `HyperLinkEdit`, `HyperlinkLabelControl` | `HyperlinkEdit` | `TcxHyperLinkEdit` | — | ❌ | |
| Recherche, auto-suggestion | `SearchControl` | `SearchControl`, `AutoSuggestEdit` | `TdxOfficeSearchBox` | `HDevSearchControl` | ✅ | suggestions par callback, croix d'effacement |
| Jetons | `TokenEdit` | `ComboBoxEdit` (mode token) | `TdxTokenEdit` | `HDevTokenEdit` | ✅ | |

**Listes et sélection**

| Fonction | WinForms | WPF | VCL | HDev | État | Écart / remarque |
|---|---|---|---|---|---|---|
| Liste déroulante | `ComboBoxEdit` | `ComboBoxEdit` | `TcxComboBox` | `HDevComboBox` | ✅ | éditable, autocomplétion ; pas de dessin personnalisé des items |
| Liste déroulante à images | `ImageComboBoxEdit` | — | `TcxImageComboBox` | — | ❌ | |
| Liste déroulante à cases | `CheckedComboBoxEdit` | `ComboBoxEdit` (mode checked) | `TcxCheckComboBox` | — | ❌ | |
| Choix de police | `FontEdit` | `FontEdit` | `TcxFontNameComboBox` | — | ❌ | |
| Lookup (popup tabulaire) | `LookUpEdit`, `GridLookUpEdit` | `LookUpEdit` | `TcxLookupComboBox`, `TcxExtLookupComboBox` | `HDevLookUpEdit` | 🟡 | popup HDevDataGrid multi-colonnes, qui se retourne vers le haut faute de place ; ni saisie, ni filtre texte |
| Lookup avec recherche | `SearchLookUpEdit` | `LookUpEdit` (mode search) | — | — | ❌ | |
| Lookup multi-valeurs | — | `LookUpEdit` (modes multi-select, token) | — | — | ❌ | |
| Lookup arborescent | `TreeListLookUpEdit` | — | — | — | ❌ | |
| Popup à contenu libre | `PopupContainerEdit` | `PopupBaseEdit` (nv) | `TcxPopupEdit` | — | ❌ | `AttachPopup` reste protégé dans le socle |
| Galerie en popup | `PopupGalleryEdit` | — | — | — | ❌ | |
| Liste | `ListBoxControl`, `ImageListBoxControl` | `ListBoxEdit` | `TcxListBox`, `TdxListViewControl` | `HDevListView` | ✅ | items riches, groupes repliables, multi-sélection, virtualisée |
| Liste à cases | `CheckedListBoxControl` | `ListBoxEdit` (mode checked) | `TcxCheckListBox`, `TcxCheckGroup` | — | ❌ | |

**Numérique**

| Fonction | WinForms | WPF | VCL | HDev | État | Écart / remarque |
|---|---|---|---|---|---|---|
| Compteur | `SpinEdit` | `SpinEdit` | `TcxSpinEdit` | `HDevSpinEdit` | ✅ | |
| Calculatrice | `CalcEdit` | `PopupCalcEdit`, `Calculator` | `TcxCalcEdit` | — | ❌ | au backlog |
| Monétaire | `TextEdit` (masque numérique) | `TextEdit` (masque numérique) | `TcxCurrencyEdit` | `HDevSpinEdit` (décimales) | 🟡 | pas de format monétaire |
| Curseur | `TrackBarControl` | `TrackBarEdit` | `TcxTrackBar` | `HDevSlider` | ✅ | horizontal seulement |
| Curseur de plage | `RangeTrackBarControl` | `TrackBarEdit` (mode range) | `TdxRangeTrackBar` | — | ❌ | |
| Curseur de zoom | `ZoomTrackBarControl` | `TrackBarEdit` (mode zoom) | `TdxZoomTrackBar` | — | ❌ | |
| Roues tactiles | — | `DateEdit` (mode picker) | `TdxWheelPicker`, `TdxDateTimeWheelPicker`, `TdxNumericWheelPicker` | — | ❌ | |

**Date et heure**

| Fonction | WinForms | WPF | VCL | HDev | État | Écart / remarque |
|---|---|---|---|---|---|---|
| Date | `DateEdit` | `DateEdit` | `TcxDateEdit` | `HDevDateEdit` | ✅ | masque, calendrier natif, bornes |
| Heure | `TimeEdit` | `TimePicker` | `TcxTimeEdit` | `HDevTimeEdit` | ✅ | |
| Durée | `TimeSpanEdit` | `TextEdit` (masque TimeSpan) | — | — | ❌ | |
| Date avec décalage horaire | `DateTimeOffsetEdit` | — | — | — | ❌ | |
| Plage de dates | `CalendarControl` (plages) | `DateRangeControl` | — | — | ❌ | |
| Calendrier intégré | `CalendarControl` | `DateNavigator` | `TcxDateNavigator` | `HDevMonthCalendar` | ✅ | sélection simple ; ni plages, ni multi-sélection, ni numéros de semaine |

**Choix**

| Fonction | WinForms | WPF | VCL | HDev | État | Écart / remarque |
|---|---|---|---|---|---|---|
| Case à cocher | `CheckEdit` | `CheckEdit` | `TcxCheckBox` | `HDevCheckBox` | ✅ | tri-état |
| Interrupteur | `ToggleSwitch` | `ToggleSwitchEdit` | `TdxToggleSwitch` | `HDevToggleSwitch` | ✅ | |
| Groupe radio | `RadioGroup` | `ListBoxEdit` (mode radio) | `TcxRadioGroup` | `HDevRadioButton`, `HDevRadioGroup` | ✅ | exclusivité et flèches partagées avec `HDevRadioCard` |
| Notation | `RatingControl` | `RatingEdit` | `TdxRatingControl` | — | ❌ | |

**Images, couleurs, fichiers**

| Fonction | WinForms | WPF | VCL | HDev | État | Écart / remarque |
|---|---|---|---|---|---|---|
| Image | `PictureEdit` | `ImageEdit` | `TcxImage` | — | ❌ | `HDevAvatar` ne fait que la photo ronde |
| Image en popup | `ImageEdit` | `PopupImageEdit` | `TcxBlobEdit` | — | ❌ | |
| Couleur | `ColorEdit`, `ColorPickEdit` | `ColorEdit`, `PopupColorEdit` | `TdxColorEdit`, `TcxColorComboBox`, `TdxColorGallery` | — | ❌ | |
| Pinceau (dégradés) | — | `BrushEdit`, `PopupBrushEdit` | — | — | ❌ | |
| Chemin de fichier ou de dossier | behaviors FilePath, OpenFile | `BrowsePathEdit` | `TcxShellComboBox` | — | ❌ | |

**Indicateurs et divers**

| Fonction | WinForms | WPF | VCL | HDev | État | Écart / remarque |
|---|---|---|---|---|---|---|
| Progression | `ProgressBarControl` | `ProgressBarEdit` | `TcxProgressBar` | `HDevProgressBar` | ✅ | 4 variantes de couleur |
| Progression indéterminée | `MarqueeProgressBarControl` | `ProgressBarEdit` (marquee) | — | `HDevProgressBar` | ✅ | |
| Code-barres | `BarCodeControl` | `BarCodeEdit` | `TdxBarCode` | — | ❌ | |
| Webcam | `CameraControl` | `CameraControl` | `TdxCameraControl` | — | ❌ | |

### 3.3 Boutons et libellés

| Fonction | WinForms | WPF | VCL | HDev | État | Écart / remarque |
|---|---|---|---|---|---|---|
| Bouton | `SimpleButton` | `SimpleButton` | `TcxButton` | `HDevButton` | ✅ | 6 variantes (Primary, Secondary, Ghost, Danger, Outline, OutlineAccent), 3 tailles |
| Bouton à icône seule | `SimpleButton` | `SimpleButton` | `TcxButton` | `HDevIconButton` | ✅ | |
| Bouton à menu | `DropDownButton` | `DropDownButton` | `TcxButton` (nv) | — | ❌ | seul le ruban a des boutons déroulants |
| Bouton partagé (split) | `DropDownButton` (nv) | `SplitButton` | `TcxButton` (nv) | — | ❌ | |
| Bouton bascule, groupe segmenté | `CheckButton` (GroupIndex) | — (nv) | `TcxButton` (nv) | `HDevToggleButtonGroup`, `HDevChip` | ✅ | |
| Libellé formaté | `LabelControl` (HTML) | — | `TcxLabel`, `TdxFormattedLabel` (BBCode) | `TextBlock` Avalonia | ✅ socle | |

### 3.4 Rubans, barres et menus

| Fonction | WinForms | WPF | VCL | HDev | État | Écart / remarque |
|---|---|---|---|---|---|---|
| Ruban | `RibbonControl` | `RibbonControl` | `TdxRibbon` | `HDevRibbon` | 🟡 | voir § 4.2 |
| Onglets contextuels | `RibbonPageCategory` | `RibbonPageCategory` | `TdxRibbonContext` | — | ❌ | `IsContextual` déclaré, jamais rendu |
| Menu application, Backstage | `ApplicationMenu`, `BackstageViewControl` | `ApplicationMenu`, `BackstageViewControl` | `TdxBarApplicationMenu`, `TdxRibbonBackstageView` | — | ❌ | |
| Barre d'accès rapide (QAT) | intégrée au `RibbonControl` | intégrée au `RibbonControl` | intégrée au `TdxRibbon` | — | ❌ | |
| Galeries | `RibbonGalleryBarItem`, `GalleryControl` | `RibbonGalleryBarItem`, `GalleryControl` | `TdxRibbonGalleryItem`, `TdxRibbonDropDownGallery`, `TdxGalleryControl` | — | ❌ | |
| Mini-barre contextuelle | `RibbonMiniToolbar` | — | `TdxRibbonMiniToolbar` | — | ❌ | |
| Recherche de commandes | recherche du `RibbonControl` | recherche de `ThemedWindow` | `TdxRibbonCaptionAreaSearchToolbar` | — | ❌ | |
| Barres d'outils | `BarManager` + `Bar` | `BarManager`, `ToolBarControl` | `TdxBarManager` + `TdxBar` | `HDevToolbar` | 🟡 | boutons, bascules, séparateurs ; ni débordement, ni déroulants, ni barres flottantes |
| Éditeurs dans les barres | `BarEditItem` | `BarEditItem` | `TcxBarEditItem` | — | ❌ | |
| Menu principal | menu du `BarManager` | `MainMenuControl` | menu du `TdxBarManager` | `HDevMenuBar` | ✅ | Alt+lettre, navigation clavier complète, raccourcis réels |
| Menu contextuel | `PopupMenu` | `PopupMenu` | `TdxBarPopupMenu`, `TdxRibbonPopupMenu` | `HDevContextMenu` | ✅ | sous-menus, éléments cochables, icônes, raccourcis |
| Menu radial | `RadialMenu` | `RadialContextMenu` | `TdxRibbonRadialMenu` | — | ❌ | |
| Popup de contrôles | `PopupControlContainer` | `PopupControlContainer` | — | — | ❌ | |
| Barre d'état | `RibbonStatusBar`, barre d'état du `BarManager` | `RibbonStatusBarControl`, `StatusBarControl` | `TdxRibbonStatusBar`, `TdxStatusBar` | `HDevStatusBar` | ✅ | |
| Fusion MDI des barres | fusion du `RibbonControl` et du `BarManager` | fusion MDI | `Merge`, `Unmerge` | — | ⚪ | MDI refusé (incompatible Wayland) |
| Infobulles riches | `ToolTipController`, `SuperToolTip` (nv) | `SuperTip` | `TdxScreenTip` | `ToolTip` Avalonia | 🟡 | texte seul ; pas de modèle titre, description et pied |
| Accélérateurs clavier | raccourcis du `BarManager` | `KeyGesture` des items (nv) | raccourcis du `TdxBarManager` | `HDevShortcutManager` | ✅ | |

### 3.5 Navigation

| Fonction | WinForms | WPF | VCL | HDev | État | Écart / remarque |
|---|---|---|---|---|---|---|
| Onglets | `XtraTabControl` | `DXTabControl` | `TcxPageControl`, `TcxTabControl` | `HDevTabControl` | ✅ | fermeture annulable, clavier ; ni débordement, ni glisser |
| Onglets dans la barre de titre | `TabForm` | `DXTabbedWindow` (outdated) | — | — | ❌ | |
| Pages avec transitions | `NavigationFrame`, `TabPane` | `NavigationFrame` | — | — | ❌ | |
| Accordéon, barre latérale à groupes | `AccordionControl`, `NavBarControl`, `NavigationPane` | `AccordionControl`, `NavBarControl` | `TdxNavBar` (vues Accordion, Side Bar, Navigation Pane, Explorer Bar) | `HDevAccordion` | 🟡 | sections à contenu hébergé, Single/Multiple ; ni sous-niveaux, ni recherche, ni mode replié |
| Barre de modules (Outlook) | `OfficeNavigationBar` | `OfficeNavigationBar` | `TdxNavBarOfficeNavigationBar` | `HDevNavBar` | ✅ | rail vertical icône + libellé + badge, mode compact |
| Menu hamburger | `AccordionControl` (vue HamburgerMenu) | `HamburgerMenu` | `TdxNavBar` (vue Hamburger) | `HDevNavBar` (compact) | 🟡 | pas de mode overlay |
| Tuiles | `TileControl` | `TileLayoutControl` | `TdxTileControl` | — | ❌ | |
| Barre et menu de tuiles | `TileBar`, `TileNavPane` | `TileBar`, `TileNavPane` | `TdxTileBar` | — | ❌ | |
| Arbre | `TreeList` (style TreeView) | `TreeViewControl` | `TdxTreeViewControl` | `HDevTreeView` | 🟡 | non virtualisé ; ni cases, ni édition, ni `ItemsSource` |
| Fil d'Ariane | `BreadCrumbEdit` | `BreadcrumbControl` | `TdxBreadcrumbEdit` | — | ❌ | |
| Étapes d'un processus | `StepProgressBar` | `StepProgressBar` | — | — | ❌ | |
| Assistant | `WizardControl` (nv) | `Wizard` | `TdxWizardControl` | — | ❌ | |
| Boîte à outils | `ToolboxControl` | — | — | — | ❌ | |
| Diaporama, carrousel | `ImageSlider` | `CarouselPanel`, `Book` | — | — | ❌ | |

### 3.6 Mise en page

| Fonction | WinForms | WPF | VCL | HDev | État | Écart / remarque |
|---|---|---|---|---|---|---|
| Mise en page de formulaire | `LayoutControl` | `LayoutControl` | `TdxLayoutControl` | — | ❌ | `Grid` Avalonia, sans alignement automatique ni personnalisation à l'exécution |
| Formulaire généré depuis les données | `DataLayoutControl` | `DataLayoutControl` | — | — | ❌ | |
| Groupe titré, panneau | `GroupControl`, `PanelControl`, `RoundedSkinPanel` | `GroupBox` | `TcxGroupBox`, `TdxCheckGroupBox`, `TdxPanel` | `HDevCard` | ✅ | titre, élévation, rayon 12 |
| Conteneurs de base (pile, tableau, défilement, séparateur) | `StackPanel`, `TablePanel`, `XtraScrollableControl`, `SplitContainerControl` | `FlowLayoutControl`, `DockLayoutControl`, `ScrollBox`, `DXExpander` | `TcxSplitter` | `StackPanel`, `Grid`, `ScrollViewer`, `GridSplitter`, `Expander` (Avalonia) | ✅ socle | non habillés par HDev |
| Panneau latéral redimensionnable | `SidePanel` | — | `TdxPanel` | — | ❌ | |
| Panneau ou dialogue volant | `FlyoutPanel`, `FlyoutDialog` | `FlyoutControl`, `Flyout` | `TdxCalloutPopup` | — | ❌ | seuls les déroulants internes ont un popup HDev |

### 3.7 Docking et documents

| Fonction | WinForms | WPF | VCL | HDev | État | Écart / remarque |
|---|---|---|---|---|---|---|
| Panneaux ancrables | `DockManager` | `DockLayoutManager` | `TdxDockingManager` | `HDevDockManager` | 🟡 | phase 1 ; voir § 4.3 |
| Documents à onglets | `DocumentManager` (`TabbedView`) | `DocumentGroup` (`MDIStyle` Tabbed) | `TdxTabbedMDIManager` | région centre de `HDevDockManager`, `HDevTabControl` | 🟡 | équivalent SDI ; pas de groupes de documents côte à côte au glisser |
| MDI natif (fenêtres enfants) | `NativeMdiView` | `MDIStyle` MDI | — | — | ⚪ | refusé : étranger aux conventions Linux, incompatible Wayland |
| UI « Modern » (modules plein écran) | `WindowsUIView` | `PageView`, `SlideView`, `FlipView`, `NavigationFrame` | — | — | ❌ | |
| Tableau de bord à widgets | `WidgetView` | — | — | — | ❌ | |

### 3.8 Fenêtres, dialogues, notifications

| Fonction | WinForms | WPF | VCL | HDev | État | Écart / remarque |
|---|---|---|---|---|---|---|
| Fenêtre thémée | `XtraForm` | `ThemedWindow` | `TdxForm`, `TdxSkinForm` | `HDevWindow` | 🟡 | décorations natives GNOME par défaut (choix Wayland) ; chrome maison secondaire |
| Ruban ou commandes dans la barre de titre | `RibbonForm`, `ToolbarForm` | `ThemedWindow` (`WindowKind`) | `TdxRibbonForm` | — | ❌ | contraire au choix du chrome natif |
| Fenêtre Fluent | `FluentDesignForm` | `ThemedWindow` (acrylique) | `TdxFluentDesignForm` | — | ❌ | |
| Boîte de message | `XtraMessageBox` | `ThemedMessageBox` | `TdxMessageDialogForm` | `HDevMessageBox` | ✅ | 4 jeux de boutons, 4 icônes, `ShowAsync` |
| Boîte de saisie | `XtraInputBox` | — | — | `HDevInputBox` | ✅ | |
| Dialogue qui héberge une vue | `XtraDialog` | `DialogService`, `ThemedWindow.ShowDialog` | — | — | ❌ | |
| Dialogues de fichiers | `XtraOpenFileDialog`, `XtraSaveFileDialog`, `XtraFolderBrowserDialog` | `DXOpenFileDialog`, `DXSaveFileDialog`, `DXFolderBrowserDialog` | `TdxOpenFileDialog`, `TdxSaveFileDialog`, `TcxShellBrowserDialog` | `StorageProvider` Avalonia (sélecteur natif du bureau) | ✅ socle | |
| Explorateur de fichiers embarqué | `FileExplorerAssistant` | — | `TdxShellListView`, `TdxShellTreeView`, `TdxShellBreadcrumbEdit` | — | ❌ | |
| Alertes dans l'application | `AlertControl` | `NotificationService` (personnalisées) | `TdxAlertWindowManager` | `HDevToast` | ✅ | empilées, auto-fermeture, clic actionnable |
| Notifications système | `ToastNotificationsManager` | `NotificationService` (natives) | — | — | ❌ | pas de notification du bureau (D-Bus, portail) |
| Écran de démarrage, attente | `SplashScreenManager`, `ProgressPanel` | `SplashScreenManager`, `LoadingDecorator`, `WaitIndicator` | `TdxSplashFormManager` (CTP) | — | ❌ | seul `HDevDataGrid` a un voile de chargement |
| Badges et guides posés sur un contrôle | `AdornerUIManager` (`Badge`, `Guide`) | `Badge` | `TdxUIAdornerManager` (`TdxBadge`, `TdxGuide`) | `HDevBadge` | 🟡 | badge autonome ; ni couche d'adornement, ni guides |
| Barre des tâches, zone de notification | `TaskbarAssistant` | `TaskbarButtonService`, `NotifyIconService` | — | — | ❌ | |

### 3.9 Planification

| Fonction | WinForms | WPF | VCL | HDev | État | Écart / remarque |
|---|---|---|---|---|---|---|
| Agenda | `SchedulerControl` | `SchedulerControl` | `TcxScheduler` | `HDevScheduler` | 🟡 | 4 vues, récurrences de base, glisser ; voir § 4.4 |
| Vue chronologique par ressources | `TimelineView` | `TimelineView` | `TcxSchedulerTimeGridView` | `HDevRoster` | 🟡 | contrôle séparé, sans lien avec l'agenda |
| Vue année | `YearView` | — | `TcxSchedulerYearView` | — | ❌ | |
| Vue agenda (liste) | `AgendaView` | `AgendaView`, `ListView` | `TcxSchedulerAgendaView` | — | ❌ | |
| Vue Gantt de l'agenda | `GanttView` | — | `TcxSchedulerGanttView` | — | ❌ | |
| Navigateur de dates | `DateNavigator` | `DateNavigationPanel` | `TcxDateNavigator` | `HDevMonthCalendar` | 🟡 | à coupler à la main avec l'agenda |
| Arbre ou filtre de ressources | `ResourcesTree`, `ResourcesCheckedListBoxControl`, `ResourcesComboBoxEdit` | `ResourceTreeControl` | navigateur de ressources | — | ❌ | |
| Gantt de projet | `GanttControl` | `GanttControl` | `TdxGanttControl` | `HDevGantt` | 🟡 | voir § 4.5 |
| Feuille de ressources | — | `GanttResourceControl` | `TdxGanttControlResourceSheetView` | — | ⚪ | renvoyée à UGantt.Core (décision du 14/07/2026) |
| Chronologie du projet | Timeline du `GanttControl` (nv) | — | `TdxGanttControlTimelineView` | — | ❌ | lot L23 |

### 3.10 Visualisation de données

| Fonction | WinForms | WPF | VCL | HDev | État | Écart / remarque |
|---|---|---|---|---|---|---|
| Graphiques 2D | `ChartControl` | `ChartControl` | `TdxChartControl` | — | ⚪ | délégué à LiveCharts2 (14/07/2026), pas encore intégré |
| Graphiques 3D | `ChartControl` (vues 3D) | `Chart3DControl` | — | — | ❌ | |
| Jauges | `GaugeControl` | `CircularGaugeControl`, `LinearGaugeControl`, `DigitalGaugeControl`, `StateIndicatorControl` | `TdxGaugeControl` | — | ⚪ | même décision |
| Sparklines | `SparklineEdit` | `SparklineEdit` | `TdxSparklineEdit` | — | ⚪ | même décision |
| Carte géographique | `MapControl` | `MapControl` | `TdxMapControl` | — | ❌ | |
| Diagrammes, logigrammes | `DiagramControl` | `DiagramControl`, `DiagramDesignerControl` | `TdxFlowChart` | — | ❌ | |
| Organigramme, carte mentale | `DiagramControl` (`ApplyMindMapTreeLayout`, `DiagramOrgChartController`) | `DiagramControl` (`ApplyMindMapTreeLayout`) | `TdxOrgChart` | `HDevMindMap` | 🟡 | éditeur dédié, 3 dispositions ; ni sérialisation, ni liaison aux données |
| TreeMap | `TreeMapControl` | `TreeMapControl` | — | — | ❌ | |
| Sunburst | `SunburstControl` | `SunburstControl` | — | — | ❌ | |
| Sankey | `SankeyDiagramControl` | `SankeyDiagramControl` | — | — | ❌ | |
| Heatmap | `HeatmapControl` | `HeatmapControl` | — | — | ❌ | |
| Sélection de plage sur un aperçu | `RangeControl` | `RangeControl` | `TdxRangeControl` | — | ❌ | |
| SVG interactif | `SvgImageBox` | — | — | — | ❌ | |

### 3.11 Documents

| Fonction | WinForms | WPF | VCL | HDev | État | Écart / remarque |
|---|---|---|---|---|---|---|
| Traitement de texte | `RichEditControl` | `RichEditControl` | `TdxRichEditControl` | `HDevRichEdit` | 🟡 | voir § 4.6 |
| Mémo riche léger | `RepositoryItemRichTextEdit` (in-place) | — | `TcxRichEdit` | `HDevRichEdit` | ✅ | HTML au lieu de RTF |
| Tableur | `SpreadsheetControl` | `SpreadsheetControl` | `TdxSpreadSheet` | — | ⚪ | hors périmètre |
| Visionneuse PDF | `PdfViewer` | `PdfViewerControl` | `TdxPDFViewer` | — | ⚪ | hors périmètre |
| Correcteur orthographique | `SpellChecker` | `SpellChecker` | `TdxSpellChecker` | — | ❌ | |
| API de documents (Word, Excel, PDF) | Office File API (offre Universal) | Office File API | — | — | ⚪ | non visuelle, hors périmètre |

### 3.12 Rapports, impression, export

| Fonction | WinForms | WPF | VCL | HDev | État | Écart / remarque |
|---|---|---|---|---|---|---|
| Impression et aperçu de tout contrôle | `PrintingSystem`, `PrintableComponentLink`, `DocumentViewer` | `DocumentPreviewControl`, `PrintableControlLink` | `TdxComponentPrinter` + report links | — | ❌ | lot L9 prévu (impression paginée et PDF pour toute la suite) |
| Export de données | PDF, XLS(X), DOCX, CSV, HTML, images | idem | PDF 1.4, XLS(X), CSV, HTML | CSV (grille), PNG (Gantt, carte mentale) | 🟡 | ni XLSX ni PDF |
| Concepteur de rapports | `XtraReport` + designer utilisateur | `ReportDesigner` | `TdxReport` (designer web, WebView2) | — | ⚪ | hors périmètre |
| Tableaux de bord BI | `DashboardDesigner`, `DashboardViewer` (nv, offre Universal) | `DashboardControl` (visionneuse) | `TdxDashboardControl` | — | ⚪ | hors périmètre |

### 3.13 Composants transverses

| Fonction | WinForms | WPF | VCL | HDev | État | Écart / remarque |
|---|---|---|---|---|---|---|
| Thèmes | 50+ skins, Skin Editor | 30+ thèmes, Theme Designer | 55+ skins, Skin Editor | `HDevTheme` | 🟡 | 2 variantes (Yaru clair et sombre), palettes injectables ; pas d'éditeur de thème |
| Validation centralisée | `DXErrorProvider`, `DXValidationProvider` | `Validate`, `IDataErrorInfo`, `INotifyDataErrorInfo` | `ValidationOptions` | `HDevEditorBase.Validating` | 🟡 | par éditeur ; ni fournisseur central, ni `INotifyDataErrorInfo` |
| Dépôt d'éditeurs partagé (in-place) | `RepositoryItem*`, `PersistentRepository` | `*EditSettings` | `TcxEditRepository` | — | ❌ | la grille instancie des éditeurs Fluent |
| Persistance de layout | `WorkspaceManager`, `SaveLayoutTo…` | `WorkspaceManager`, `LayoutSerializationService` | `TcxPropertiesStore` | `HDevDockManager.SaveLayout`, JSON du Gantt | 🟡 | ni grille, ni gestionnaire global |
| Behaviors | `BehaviorManager` | behaviors MVVM | — | — | ❌ | |
| Gabarits d'UI HTML-CSS | `HtmlContentControl` + gabarits | — | BBCode (`TdxFormattedLabel`) | — | ❌ | `HDevHtmlView` affiche des documents, pas des gabarits d'UI |
| Transitions animées | `TransitionManager` | `WorkspaceManager` (transitions) | — | — | ❌ | |
| Banque d'icônes | `SvgImageCollection`, icônes DevExpress | icônes DevExpress (Fluent en v26.2) | `TcxImageList` | glyphes vectoriels internes | ❌ | |
| Chat IA | `AIChatControl` | `AIChatControl` | — | — | ❌ | |
| Extensions IA (assistant, Smart Paste…) | AI Assistant, Smart Paste, recherche sémantique… | idem | AI Assistant, Smart Paste | — | ❌ | |

## 4. Profondeur des contrôles phares

Chaque check-list reprend les capacités que DevExpress met en avant pour le contrôle, en faisant l'union des trois plateformes. Une remarque signale les capacités qu'une seule plateforme propose.

Le score mesure l'étendue couverte, pas l'effort restant. Un seul item DevExpress, comme le formatage conditionnel, recouvre à lui seul des dizaines de règles.

| Contrôle phare | Équivalents DevExpress | Capacités | ✅ | 🟡 | ❌ | ⚪ | Score |
|---|---|---:|---:|---:|---:|---:|---:|
| Grille (`HDevDataGrid`) | `GridControl`, `TcxGrid` | 44 | 14 | 4 | 26 | 0 | 36 % |
| Ruban, barres, menus | `RibbonControl` + `BarManager`, `TdxRibbon` + `TdxBarManager` | 26 | 11 | 1 | 13 | 1 | 44 % |
| Docking (`HDevDockManager`) | `DockManager`, `DockLayoutManager`, `TdxDockingManager` | 16 | 7 | 0 | 7 | 2 | 44 % |
| Agenda (`HDevScheduler` + `HDevRoster`) | `SchedulerControl`, `TcxScheduler` | 23 | 7 | 3 | 13 | 0 | 37 % |
| Gantt (`HDevGantt`) | `GanttControl`, `TdxGanttControl` | 27 | 15 | 1 | 8 | 3 | 57 % |
| Texte riche (`HDevRichEdit`) | `RichEditControl`, `TdxRichEditControl` | 23 | 7 | 4 | 12 | 0 | 39 % |
| Socle des éditeurs | `BaseEdit` + `RepositoryItem`, `EditSettings`, `TcxEditRepository` | 17 | 7 | 4 | 6 | 0 | 53 % |

### 4.1 Grille : `HDevDataGrid` ↔ `GridControl` / `TcxGrid`

| Capacité DevExpress | HDev | Remarque |
|---|---|---|
| Vue tableau, colonnes générées automatiquement | ✅ | |
| Vues alternatives (bandes, cartes, tuiles, explorateur) | ❌ | |
| Liaison à une collection observable | ✅ | ajouts et suppressions suivis |
| Mise à jour quand une propriété d'un item change | ❌ | pas d'écoute `INotifyPropertyChanged` par item |
| Gros volumes côté serveur | ❌ | |
| Virtualisation des lignes, recyclage des cellules | ✅ | |
| Tri multi-colonnes | ✅ | Maj+clic |
| Ligne de filtre, opérateurs par type | ✅ | texte, nombre, date, préréglages Aujourd'hui, Semaine, Mois, Année |
| Filtre déroulant style Excel | ❌ | |
| Constructeur de filtre (ET/OU) | ❌ | moteur présent, sans UI |
| Panneau du filtre actif | ❌ | |
| Recherche globale (Find Panel, Search Panel) | ❌ | |
| Groupement multi-niveaux par glisser-déposer | ✅ | |
| Groupement par intervalles ou personnalisé | ❌ | |
| Résumés de pied et de groupe | ✅ | Count, Sum, Avg, Min, Max |
| Résumés personnalisés | ❌ | |
| Master-detail | ❌ | |
| Édition in-place, événements annulables | ✅ | |
| Éditeurs in-place communs à la suite | 🟡 | éditeurs Fluent par défaut ; `EditTemplate` permet de brancher un éditeur HDev à la main |
| Formulaire d'édition (Edit Form) | ❌ | |
| Ligne d'ajout, suppression | ❌ | |
| Validation | ❌ | ni `IDataErrorInfo`, ni `INotifyDataErrorInfo` |
| Sélection multiple, cellule courante | ✅ | modes None, Single, Multiple, Cell |
| Sélection de plages rectangulaires de cellules | ❌ | plages de lignes seulement |
| Navigation clavier complète | ✅ | flèches, Tab, Pg↑/Pg↓, Début/Fin, Ctrl+A, F2, Entrée, Échap |
| Copier | 🟡 | texte TSV seulement ; DevExpress copie aussi en HTML, RTF et XLS |
| Coller | ❌ | |
| Glisser-déposer de lignes | ❌ | |
| Colonnes figées | 🟡 | à gauche seulement ; DevExpress fige aussi à droite et épingle des lignes |
| Redimensionnement, réordonnancement, best-fit, verrouillage | ✅ | |
| Sélecteur de colonnes, menu d'en-tête | ❌ | |
| Formatage conditionnel | ❌ | |
| Fusion de cellules | ❌ | |
| Hauteur de ligne automatique, retour à la ligne, aperçu de ligne | ❌ | |
| Colonnes non liées ou calculées | ❌ | |
| Gabarits de cellule | ✅ | `CellTemplate` ; `HeaderTemplate` ignoré |
| Export | 🟡 | CSV seulement ; ni XLSX ni PDF |
| Impression, aperçu | ❌ | |
| Sauvegarde et restauration du layout | ❌ | |
| Pagination | ❌ | WPF : `AllowPaging` ; WinForms : behavior Pager Navigation |
| Habillage clair et sombre | ✅ | skin Yaru |
| Indicateur et numéros de ligne, états « vide » et « chargement » | ✅ | |
| Accessibilité (UI Automation) | ❌ | |
| IA (Smart Paste, recherche sémantique) | ❌ | |

### 4.2 Ruban, barres et menus

| Capacité DevExpress | HDev | Remarque |
|---|---|---|
| Onglets et groupes | ✅ | |
| Boutons grands et petits | ✅ | |
| Boutons déroulants | ✅ | menu `HDevContextMenu` |
| Boutons bascule | ✅ | |
| Lanceur de dialogue de groupe | ✅ | |
| Repli du ruban | ✅ | `IsCollapsed` |
| Onglets contextuels | ❌ | `IsContextual` et `AccentColor` déclarés, jamais rendus |
| Key tips (Alt) | ❌ | Alt+lettre existe dans la barre de menus |
| Menu application, Backstage | ❌ | |
| Barre d'accès rapide | ❌ | |
| Galeries | ❌ | |
| Éditeurs dans le ruban et les barres | ❌ | |
| Ruban simplifié, groupes adaptatifs | ❌ | |
| Mini-barre, menu radial | ❌ | |
| Recherche de commandes | ❌ | |
| Personnalisation par l'utilisateur | ❌ | |
| Fusion MDI | ⚪ | MDI refusé |
| Modèle de commandes partagé entre ruban, barres et menus | ❌ | chaque contrôle a ses propres éléments |
| Infobulles riches | 🟡 | texte seul |
| Accélérateurs clavier | ✅ | `HDevShortcutManager` |
| Barre de menus, navigation clavier complète | ✅ | |
| Menus contextuels (sous-menus, cochables) | ✅ | |
| Barres d'outils | ✅ | |
| Débordement, barres flottantes ou ancrables | ❌ | |
| Barre d'état à panneaux | ✅ | |
| Construction déclarative (`ItemsSource`) | ❌ | construction en code |

### 4.3 Docking : `HDevDockManager`

| Capacité DevExpress | HDev | Remarque |
|---|---|---|
| Ancrage aux bords, région centrale de documents | ✅ | |
| Groupes d'onglets | ✅ | |
| Splitters à proportions | ✅ | |
| Fermeture d'un panneau | ✅ | clic milieu compris |
| Sauvegarde et restauration du layout | ✅ | topologie et Id ; le contenu est réassocié par l'app |
| API de manipulation en code | ✅ | |
| Glisser un panneau avec indicateurs d'ancrage | ❌ | phase 2 |
| Réordonnancement des onglets | ❌ | phase 2 |
| Panneaux flottants | ❌ | phase 3, sous forme de fenêtre SDI détachée |
| Auto-masquage, épinglage | ❌ | phase 3 |
| Boutons d'en-tête (maximiser, personnalisés) | ❌ | |
| Panneaux fermés restaurables | ❌ | |
| Sélecteur de documents (Ctrl+Tab) | ❌ | |
| MDI (onglets ou fenêtres) | ⚪ | refusé ; la région centrale en tient lieu |
| Fusion des barres et du ruban | ⚪ | |
| Habillage clair et sombre | ✅ | |

### 4.4 Agenda : `HDevScheduler` (+ `HDevRoster`)

| Capacité DevExpress | HDev | Remarque |
|---|---|---|
| Vue jour | ✅ | |
| Vues semaine ouvrée et semaine | ✅ | |
| Vue mois | ✅ | |
| Vue timeline | 🟡 | `HDevRoster`, contrôle séparé |
| Vue année | ❌ | |
| Vue agenda (liste) | ❌ | |
| Vue Gantt de l'agenda | ❌ | |
| Ressources, groupement par ressource | ❌ | |
| Récurrences | 🟡 | quotidienne, hebdomadaire par jours, mensuelle, intervalle, fin ; ni annuelle, ni exceptions |
| Modification d'une seule occurrence | ❌ | les occurrences récurrentes ne se déplacent pas |
| Bandeau « toute la journée » | ✅ | |
| Ligne « maintenant », heures non ouvrées ombrées | ✅ | |
| Glisser et redimensionner, événements annulables | ✅ | pas de glisser sur plusieurs jours |
| Formulaire d'édition de rendez-vous | ❌ | `TimeSlotDoubleClicked` laissé à l'app |
| Rappels | ❌ | |
| Fuseaux horaires | ❌ | |
| Import et export iCalendar | ❌ | |
| Synchronisation Google ou Microsoft | ❌ | |
| Libellés, statuts, catégories | ❌ | |
| Navigateur de dates couplé | 🟡 | `HDevMonthCalendar`, couplage à la main |
| Liaison par mappings | ❌ | modèle `ScheduleEvent` imposé |
| Lecture seule | ✅ | `IsReadOnly` |
| Impression | ❌ | |

### 4.5 Gantt : `HDevGantt`

| Capacité DevExpress | HDev | Remarque |
|---|---|---|
| Table-arbre et diagramme | ✅ | |
| Tâches récapitulatives, jalons | ✅ | |
| Dépendances FS, SS, FF, SF avec décalage | ✅ | |
| Ordonnancement automatique | ✅ | planification manuelle possible par tâche |
| Calendrier ouvré (week-ends, jours fériés) | ✅ | |
| Chemin critique | ✅ | absent de la doc VCL |
| Baseline | ✅ | |
| Édition graphique (déplacer, redimensionner, avancement, liens) | ✅ | |
| Édition in-place de la table | ✅ | |
| Édition structurelle (insérer, supprimer, indenter, déplacer) | ✅ | |
| Annuler, rétablir | ✅ | |
| Zoom de l'échelle de temps | ✅ | |
| Gros volumes | ✅ | plus de 2 000 tâches, virtualisé |
| Couleur par tâche | ✅ | |
| Persistance | ✅ | JSON versionné ; DevExpress passe par la liaison de données |
| Liaison de données (mappings) | ❌ | |
| Ressources et affectations | ⚪ | renvoyées à UGantt.Core |
| Feuille de ressources | ⚪ | idem |
| Contraintes typées (8 types) | ❌ | VCL |
| Tâches fractionnées | ❌ | WinForms |
| Tâches récurrentes | ❌ | VCL |
| Colonnes configurables | ❌ | lot L2 |
| Vue chronologie | ❌ | lot L23 |
| Import et export MS Project XML | ❌ | VCL ; prévu côté UGantt |
| Impression, export | 🟡 | PNG seulement |
| Gabarits ou dessin personnalisé des barres | ❌ | lot L21 |
| Coûts, champs personnalisés | ⚪ | renvoyés à UGantt.Core |

### 4.6 Texte riche : `HDevRichEdit`

| Capacité DevExpress | HDev | Remarque |
|---|---|---|
| Mise en forme des caractères | ✅ | gras, italique, souligné, couleur, surlignage, taille |
| Titres, styles de paragraphe | 🟡 | titres ; pas de styles nommés |
| Listes | 🟡 | puces et numéros ; pas de listes multi-niveaux |
| Alignement, justification | ✅ | |
| Liens | ✅ | Ctrl+K |
| Images | 🟡 | en blocs ; ni dans le fil du texte, ni flottantes |
| Tableaux | ❌ | affichés par `HDevHtmlView`, pas éditables |
| Annuler, rétablir | ✅ | |
| Presse-papiers riche | ✅ | HTML |
| Import et export HTML | ✅ | et contrat `storage-v1` |
| DOCX, RTF, ODT | ❌ | |
| Export PDF | ❌ | |
| Mise en page (sections, marges, colonnes, en-têtes et pieds de page) | ❌ | |
| Vue page, impression | ❌ | |
| Champs, publipostage | ❌ | |
| Commentaires, suivi des modifications | ❌ | |
| Rechercher, remplacer | ❌ | |
| Orthographe | ❌ | |
| Protection du document | ❌ | |
| Règle, tabulations | ❌ | |
| Barre d'outils ou ruban intégré | 🟡 | barre d'outils simple |
| Raccourcis clavier | ✅ | |
| IA (assistant) | ❌ | |

### 4.7 Socle des éditeurs

| Capacité DevExpress | HDev | Remarque |
|---|---|---|
| Valeur liable en XAML (`EditValue`) | 🟡 | oui pour TextBox, CheckBox, RadioButton, ToggleSwitch, Slider ; non pour DateEdit, TimeEdit, SpinEdit, ComboBox, LookUpEdit, TokenEdit, SearchControl, MonthCalendar (propriétés CLR) |
| Masques simples | 🟡 | 0, 9, L, A et littéraux ; le collage contourne le masque, seule la validation le rattrape (test d'interaction) |
| Masques numériques, date-heure, regex | 🟡 | DateEdit et SpinEdit formatent eux-mêmes ; pas de regex |
| Validation annulable, texte et rendu d'erreur | ✅ | |
| Validation centralisée, `IDataErrorInfo` | ❌ | |
| Texte d'invite | ✅ | |
| Lecture seule | ✅ | |
| Boutons d'éditeur configurables | ❌ | |
| Popups qui se retournent vers le haut | ✅ | tous (test d'interaction) ; la ComboBox bascule elle-même, les autres s'en remettent à Avalonia, qui ne reflète pas leur décalage de 1 à 2 px |
| Autocomplétion | ✅ | ComboBox : Suggest, Append |
| Même éditeur en autonome et in-place (grille, arbre, barres) | ❌ | |
| Socle commun à tous les éditeurs | 🟡 | `HDevEditorBase` porte DateEdit, TimeEdit, SpinEdit et SearchControl ; TextBox, ComboBox, LookUpEdit et TokenEdit sont à part |
| Thème clair et sombre à chaud | ✅ | |
| États visuels cohérents, halo de focus | ✅ | |
| Libellé flottant | ❌ | WinForms |
| Accessibilité (UI Automation) | ❌ | |
| IA (autocomplétion, assistant) | ❌ | |

## 5. Lecture

### Forces

1. **Linux natif.** C'est la seule suite du comparatif qui tourne nativement sous Linux. WinForms et VCL sont limités à Windows. WPF n'y arrive que par Avalonia XPF, que DevExpress ne supporte pas.
2. **Souveraineté et coût.** Aucune licence, code intégral, une seule dépendance (Avalonia, MIT).
   - En face, une licence DevExpress coûte de 1 100 à 2 500 $ par développeur la première année. Elle donne un usage perpétuel et 12 mois de mises à jour.
   - Rester à jour coûte ensuite environ la moitié chaque année. Sans renouvellement, on garde la dernière version obtenue.
   - Côté .NET, les sources sont réservées aux offres DXperience et Universal.
3. **Socle d'application de bureau.** Ces briques suffisent à bâtir un client de type Outlook (démo « Boîte ») :
   - les éditeurs du quotidien ;
   - les menus avec de vrais raccourcis ;
   - les onglets et les toasts ;
   - les listes riches ;
   - l'agenda ;
   - le texte riche en HTML.
4. **Gantt.** C'est le contrôle le plus abouti (57 %) : chemin critique, baseline, édition structurelle, annuler/rétablir, persistance versionnée.
5. **Qualité vérifiable.**
   - Moteurs purs testés : environ 340 méthodes xUnit sur les deux dépôts.
   - 65 vérifications d'interaction headless.
   - Planches PNG en clair et en sombre.
   - Contraste WCAG garanti par des tests (`ThemeContrastTests`).
6. **Pièces propres.** Le roster, la carte mentale dédiée et le contrat de stockage HTML (annexe A).

### Écarts structurants

Classés par ampleur de l'écart de parité (score de la famille, § 1), puis par portée dans l'architecture de la suite.

1. **Visualisation de données (4 %).** Rien n'est intégré. LiveCharts2 a été choisi le 14/07/2026 pour les graphiques, les jauges et les sparklines, mais n'apparaît que comme libellé dans la démo. Carte, diagrammes, TreeMap, Sunburst, Sankey et Heatmap n'ont pas d'équivalent.
2. **Grilles et données (7 %).** La grille ne couvre que 36 % de ses capacités :
   - recherche globale ;
   - filtre style Excel ;
   - validation et ligne d'ajout ;
   - persistance du layout ;
   - export XLSX ;
   - formatage conditionnel ;
   - master-detail.

   TreeList, PropertyGrid (ou grille verticale) et Pivot manquent tous.
3. **Impression et export (13 %).** Chaque contrôle DevExpress s'imprime et s'exporte (PDF, XLSX, DOCX). HDev n'exporte que du CSV et du PNG. Le lot L9 (impression paginée et PDF) est prévu mais pas lancé.
4. **Fondations transverses (15 %).**
   - Les valeurs et sélections ne sont pas liables en XAML, hors la grille et les contrôles simples.
   - Il n'y a ni dépôt d'éditeurs partagé, ni validation centralisée.
   - Il n'y a ni localisation ni RTL, et l'accessibilité ne fait que commencer (focus visible, contraste, un `AutomationPeer`). WinForms et WPF revendiquent la conformité 508 et WCAG.
5. **Planification et docking (20 %).** L'agenda n'a ni ressources, ni rappels, ni iCalendar. Le docking attend ses phases 2 et 3 (glisser, flottant, auto-masquage).
6. **Navigation et mise en page (25 % et 33 %).** Le Layout Control, qui aligne les formulaires, manque, comme les tuiles, le fil d'Ariane et l'assistant.
7. **Ruban (32 % pour les rubans et barres).** Il manque Backstage, barre d'accès rapide, galeries et onglets contextuels.

### Pistes

Ordonnées par dépendances d'architecture, puis par gain de parité :

1. **Socle des éditeurs** : contrat commun, socle unique, valeurs liables (jalon J1 de `Plan-Editeurs.md`). Les formulaires, la validation et l'édition dans la grille en dépendent.
2. **Lot L9, impression paginée et PDF.** Il est transverse : toute la suite en profite, le Gantt en premier.
3. **Grille.** D'abord recherche globale, filtre style Excel, validation et ligne d'ajout, persistance du layout, export XLSX. Ensuite TreeList et PropertyGrid, sur le moteur de HDevDataGrid.
4. **Visualisation.** Intégrer LiveCharts2 sous l'habillage HDevTheme (graphiques, jauges, sparklines). La décision est prise, rien n'est intégré.
5. **Layout Control.** La mise en page de formulaire, sur le socle des éditeurs.

**Option non souveraine à connaître.** Selon DevExpress, ses bibliothèques sans interface (Office File API, moteur de Reports) tournent sous Linux et macOS, et la doc cite Avalonia. Elles couvriraient l'export XLSX, PDF et DOCX, mais au prix d'une licence DevExpress (offre Universal pour l'Office File API).

## Annexe A — Ce que HDev apporte en propre

**A.1 Sans équivalent dédié dans les inventaires v26.1**

| Contrôle HDev | Rôle | Le plus proche chez DevExpress |
|---|---|---|
| `HDevAvatar` | initiales ou photo ronde, pastille de présence, couleur stable par nom | `PictureEdit`, `ImageEdit` ; aucun avatar dédié trouvé |
| `HDevRadioCard` | choix exclusif en carte (titre + sous-titre) | `RadioGroup` ; aucune carte-radio dédiée trouvée |
| `HDevHtmlView` | rendu HTML sanitisé (mails) : ni script ni accès réseau ; liens, sélection et copie | `RichEditControl` en lecture seule (import HTML) ; `HtmlContentControl` compose des UI, pas des documents |
| Contrat `storage-v1` | HTML TipTap comme format de stockage, parseur strict à diagnostics, point fixe testé | — |

**A.2 Spécialisations d'une fonction DevExpress (comptées dans la table)**

| Contrôle HDev | Ligne de la table | Différence |
|---|---|---|
| `HDevRoster` | Vue chronologique par ressources | forme « roster » propre (bande d'amplitude, barre, blocs ; voies de jours ou de ressources), indépendante d'un modèle de rendez-vous |
| `HDevMindMap` | Organigramme, carte mentale | éditeur dédié (Tab et Entrée, re-parentage au glisser, 3 dispositions) ; DevExpress applique une disposition « mind map » à un éditeur de diagrammes généraliste |
| `HDevChip`, `HDevToggleButtonGroup` | Bouton bascule, groupe segmenté | pilules cochables ou fermables avec badge ; segments dans une pilule |

## Annexe B — Sources (consultées le 04/10/2026)

- WinForms :
  - [arbre des contrôles v26.1](https://docs.devexpress.com/WindowsForms/7874/winforms-controls)
  - [contrôles inclus](https://docs.devexpress.com/WindowsForms/401381/controls-and-libraries/editors-and-simple-controls/included-controls-and-components)
  - [notes de version v26.1](https://docs.devexpress.com/WindowsForms/405256/whats-new/v26-1-release-notes)
  - [page produit et prix](https://www.devexpress.com/products/net/controls/winforms/)
- WPF :
  - [arbre des contrôles](https://docs.devexpress.com/WPF/7875/controls-and-libraries)
  - [éditeurs inclus](https://docs.devexpress.com/WPF/6933/controls-and-libraries/data-editors/included-components)
  - [notes de version v26.1](https://docs.devexpress.com/WPF/405496/whats-new/v26-1-release-notes)
  - [page produit et prix](https://www.devexpress.com/products/net/controls/wpf/)
- VCL :
  - [documentation](https://docs.devexpress.com/VCL/)
  - [liste des contrôles](https://docs.devexpress.com/VCL/401349/vcl-controls)
  - [notes de version v26.1](https://docs.devexpress.com/VCL/405314/whats-new/latest-release-notes)
  - [page produit et prix](https://www.devexpress.com/products/vcl/)
- Versions et licences :
  - [versions.xml](https://www.devexpress.com/support/versions.xml)
  - [FAQ de licence](https://www.devexpress.com/support/eulas/)
- Linux :
  - [WinForms et .NET](https://docs.devexpress.com/WindowsForms/401191/dotnet-core-support)
  - [WPF et .NET](https://docs.devexpress.com/WPF/401165/dotnet-core-support)
  - [Avalonia XPF + DevExpress (tiers)](https://avaloniaui.net/xpf/packages/devexpress)
- Accessibilité :
  - [WinForms](https://docs.devexpress.com/WindowsForms/404293/build-an-application/accessibility)
  - [WPF](https://docs.devexpress.com/WPF/403061/common-concepts/accessibility-support)
  - [VCL](https://docs.devexpress.com/VCL/405004/ExpressCrossPlatformLibrary/accessibility-support)
- Diagram, disposition « mind map » :
  - [WinForms](https://docs.devexpress.com/WindowsForms/116904/controls-and-libraries/diagrams/automatic-layout)
  - [WPF](https://docs.devexpress.com/WPF/116572/controls-and-libraries/diagram-control/automatic-layout)
- HDev : code des deux dépôts, tests xUnit et headless, `HDev.UI.Controls/context.md` (audit du 10/07/2026 et mises à jour), `UGantt-charte.md`.

## Annexe C — Points non vérifiés

- **Classes DevExpress marquées (nv) dans la table :**
  - variantes de `TcxButton` ;
  - `WizardControl`, `SuperToolTip`, `DashboardDesigner` / `DashboardViewer` (WinForms) ;
  - `PopupBaseEdit` et la gestion du multi-ligne par `TextEdit` (WPF) ;
  - Timeline du `GanttControl` WinForms.
- **Gantt VCL.** La doc VCL ne mentionne ni chemin critique, ni liaison `TDataSet`, ni impression. Ces fonctions sont peut-être seulement non documentées.
- **Gantt WinForms.** Semaine de travail, annuler/rétablir et filtrage ne sont pas confirmés.
- **Chiffres marketing.** « 190+ contrôles » et « 60+ types de graphiques » n'ont pas été recomptés.
- **Prix.** Ce sont les prix publics de devexpress.com au 04/10/2026, hors remises.
- **Côté HDev.** L'état vient du code et des tests. Le rendu n'a pas été revalidé visuellement pour ce benchmark.
