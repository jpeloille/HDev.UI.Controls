# ProControls — Contexte du projet

## Objectif

Créer une **bibliothèque de contrôles Avalonia UI professionnels** avec le style **Ubuntu 26.04 (Yaru / GNOME)** pour les applications métier **Synaxis** (gestion des opérations de vol) et **Ovidie** (gestion financière).

## Caractéristiques clés

### 1. Style Ubuntu 26.04 (Yaru)

- **Palette de couleurs** : Gris/blanc subtil, sobre et professionnel
- **Accents** : Orange Ubuntu (#E95420) pour les éléments interactifs et focus
- **Typographie** : **Inter embarquée** (`fonts:Inter#Inter`, nécessite `.WithInterFont()` dans l'app hôte), 14px body, 16px subtitle, 19px title. Décision du 14/07/2026 : les fontes Ubuntu du système sont devenues variables et rasterisent sales dans Skia (pas de hinting FreeType) ; Inter rend net et identique sur toute machine (A/B validé). Fallback Ubuntu Sans. Rendu subpixel LCD actif au niveau ProWindow (sûr depuis OverlayPopups=true : surface fenêtre opaque) + origines de texte snappées au pixel (Crisp.Snap) partout. **Snapping au PIXEL PHYSIQUE (14/07/2026)** : `Crisp.Snap` arrondit désormais dans l'espace device (`round(p*scale)/scale`), pas en unités logiques — indispensable dès que le scaling est fractionnaire (125/150/175 %, typique Wayland) où 1 unité logique ≠ 1 pixel (sinon glyphes flous). Le `RenderScaling` (uniforme par fenêtre) est figé une fois en tête de chaque `Render` via `Crisp.BeginFrame(this)` (récupéré proprement par `visual.GetVisualRoot()?.RenderScaling`, API publique Avalonia 11.2.1) ; les ~57 sites d'appel `Crisp.Snap(...)` restent inchangés. Fallback 1.0 si non attaché = ancien comportement, sans régression. **Point à re-auditer à la migration Avalonia 12** (le contrat GetVisualRoot/IRenderRoot.RenderScaling peut bouger).
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
| ProTextBox | TextEdit | 🟡 | **ni masque, ni validation** (HasError = flag externe). Multiline OK (`IsMultiline`, lot 1) |
| ProCard | GroupControl | ✅ | `Elevation` appliquée + `Title`/`HasBorder` réactifs (lot 1) |
| ProWindow | XtraForm | 🟡 | chrome custom = chemin secondaire (natif par défaut, cf. X11) ; Light forcé, pas de dark ; champs restore morts |
| ProMenuBar / ProMenuItem / ProContextMenu | BarManager, PopupMenu | ✅ | raccourcis RÉELS (ProShortcutManager, 14/07), Alt+lettre ouvre les menus, navigation ↑↓/Enter/←→/Échap (sous-menus compris), `IsEnabled` bloque le clic |
| ProRibbon | RibbonControl | 🟡 | rendu = boutons + séparateurs + dialog launcher (lot 1). Classes inertes (ComboBox/Gallery/ToggleGroup) retirées ; `AccentColor`/`IsContextual` toujours non exploités ; pas de backstage/QAT |
| ProComboBox | ComboBoxEdit | ✅ | éditable (lot 2) : `TextEditStyle` Standard/DisableTextEditor/HideTextEditor, `AutoComplete` Suggest/Append/SuggestAppend, `ImmediatePopup`, `SelectAll`, texte libre accepté. `DrawItem` (owner-draw) toujours absent |
| ProEditorBase (socle) | BaseEdit/RepositoryItem | ✅ | masque simple (0/9/L/A + littéraux, suppression = troncature alignée), `Validating` annulable au blur, erreur bordure rouge + tooltip (lot 2) |
| ProDateEdit / ProTimeEdit | DateEdit / TimeEdit | ✅ | masque format, calendrier popup (recréé à chaque ouverture — état périmé sinon), MinDate/MaxDate, spin ±min/±h (lot 2) |
| ProSpinEdit | SpinEdit | ✅ | Increment/bornes/décimales, spin+molette+flèches, filtre numérique culture (lot 2). CalcEdit (calculette popup) → backlog |
| ProLookUpEdit | LookUpEdit/GridLookUpEdit | 🟡 | dropdown JDataGrid multi-colonnes, DisplayMember/ValueMember, Suppr efface (lot 2). Pas de saisie/filtre texte ni flip haut/bas |
| ProToolbar | Bars | 🟡 | boutons icône/texte, toggles, séparateurs (lot 1). Pas d'overflow, pas de drag |
| ProStatusBar | RibbonStatusBar | ✅ | panneaux gauche/droite, séparateurs, panneaux cliquables (lot 1) |
| ProMessageBox | XtraMessageBox | ✅ | modale OK/OKCancel/YesNo/YesNoCancel, icônes, résultat typé, ShowAsync + raccourcis (lot 1) |
| JDataGrid (réf. `../AvaloniaDataGrid`, source de vérité) | GridControl/GridView | 🟡 | voir section JDataGrid ci-dessous |
| ProTabControl | XtraTabControl | ✅ | onglets pilule, contenu hébergé, fermeture optionnelle (TabClosing annulable, clic milieu), clavier ←→ (lot 4a). Pas d'overflow d'onglets |
| ProTreeView | TreeView | ✅ | expand/collapse, sélection, clavier complet (↑↓←→ Enter Home/End), événements, ExpandAll/CollapseAll (lot 4a). Non virtualisé, pas de checkboxes/édition |

### 4bis. JDataGrid — état (audit du 10/07/2026)

**Fait et réel** : tri multi-colonnes (moteur), édition in-place complète (BeginEdit/Commit/Cancel, éditeurs typés, événements annulables), groupement drag & drop multi-niveaux + group summaries, footer d'agrégats (Count/Sum/Avg/Min/Max), resize/réordonnancement/auto-génération colonnes, sélection ligne single/multi + clavier vertical, virtualisation lignes + recyclage, accès propriétés par délégués compilés (chemins `A.B.C`).

**~~Motif récurrent : moteur riche, UI pas branchée~~ → résorbé au lot 3 (14/07/2026, commit f430933)** :
- ~~Filtrage `Contains` en dur~~ → sélecteur d'opérateur par type de colonne dans la filter row ; le bouton filtre d'en-tête reste sans handler (redondant avec la filter row)
- ~~Shift+clic non câblé~~ → multi-tri OK
- ~~Pin ≠ freeze~~ → le pin gèle réellement
- ~~SelectCell jamais appelé~~ → mode Cell câblé (clic + ←→↑↓/Tab) ; états non réappliqués au recyclage (scroll) — connu
- ~~AutoFitWidth TODO~~ → best-fit au double-clic (en-tête + 100 lignes)
- Types de colonnes : toujours 10 déclarés, Image/ProgressBar/Hyperlink/Button/Custom rendus en texte ; `CellTemplate`/`HeaderTemplate` jamais consommés
- Nouveau : Ctrl+C (TSV), `ToCsv`/`ExportCsvAsync` (vue courante)

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
1. ~~Quick wins : ProMessageBox, ProStatusBar, ProToolbar, multiline ProTextBox + assainir les façades~~ ✅ **Fait le 10/07/2026** (validé visuellement)
2. ~~Éditeurs : socle masque+validation → ProDateEdit → ProSpinEdit → ProComboBox éditable/autocomplete → ProLookUpEdit~~ ✅ **Fait le 14/07/2026** (benchmark masque 15/15, corrigé et validé visuellement ; restes : CalcEdit, saisie/filtre texte du LookUp, flip haut/bas des popups éditeurs)
3. ~~JDataGrid : brancher l'UI sur le moteur existant (opérateurs de filtre, Shift+clic, pin→freeze, best-fit, sélection cellule, copier/coller, export CSV)~~ ✅ **Fait le 14/07/2026** (commit AvaloniaDataGrid f430933 ; restes : coller, réapplication des états cellule au recyclage, vitrine du mode Cell)
4. Gros chantiers :
   - ~~ProTabControl/TreeView~~ ✅ **Lot 4a fait le 14/07/2026** (validé visuellement)
   - **Arbitrages actés (14/07/2026)** :
     - **ProGantt maison, spécialisé GESTION DE PROJET** (tâches WBS, jalons, dépendances FS/SS/FF/SF, avancement, chemin critique CPM) — PAS un planning de ressources. Architecture : moteur d'ordonnancement pur testé (roll-up, calendrier ouvré, propagation, CPM) + UI custom-rendered (table arbre à gauche façon MS Project, timeline à droite). Phase 1 = afficher, phase 2 = ordonnancer/interagir, phase 3 = baseline/export.
     - **ProGantt phase 1 ✅ faite le 14/07/2026** (validée) : moteur GanttProject/GanttTask/GanttCalendar/GanttTimeAxis (24 tests), contrôle virtualisé lignes×temps (>2000 tâches fluide), table arbre + timeline, barres/avancement, crochets récapitulatifs, jalons losanges, flèches de dépendances routage MS Project (L + S à rebours), ombrage chômés, now-line, zoom Ctrl+molette centré, splitter, tooltips. Calibrage : dates posées par l'app, calendrier ouvré v1, cible >2000 tâches.
     - **ProGantt phase 2a ✅ faite le 14/07/2026** (validée) : GanttScheduler — ordonnancement auto (`Project.AutoSchedule`, défaut off ; tri topologique Kahn, calage FS/SS/FF/SF + lag en jours ouvrés, durée préservée, `IsManuallyScheduled` exclut), chemin critique par dates (marge totale ouvrée `TotalFloatDays`, `IsCritical`, récapitulatives héritent), cycles refusés par `DependsOn` (`WouldCreateCycle`). 19 tests. UI : `HighlightCriticalPath` (barres rouges) + toggles démo.
     - **ProGantt phase 2b ✅ faite le 14/07/2026** (validée) : interactions souris — déplacement (fantôme pointillé + étiquette, snap jour ouvré, durée ouvrée préservée), resize par les bords, poignée d'avancement (triangle sous la barre, snap 5 %), création de liens (connecteur + ligne élastique verte/rouge, cycle refusé), Échap annule, `IsReadOnly`. Contrat « le contrôle affiche, le métier décide » : `TaskDatesChanging`/`ProgressChanging`/`LinkCreating` annulables (+ événements Changed/Created). Aucune mutation pendant le drag (commit au relâcher, un seul Recalculate).
     - **ProGantt phase 3 ✅ faite le 14/07/2026** (validée, dont l'export hors écran) : baseline (`SetBaseline`/`ClearBaseline` instantané des dates effectives, barres grises prévues + carrés creux jalons, écart ouvré dans le tooltip, toggle `ShowBaseline`), édition in-place de la table (double-clic/F2 → ProTextBox/ProDateEdit/ProSpinEdit en dogfooding, Enter/Échap, mêmes événements annulables que le drag), clic droit → ProContextMenu (suppression de liens via `LinkRemoving`/`Removed` annulables, bascule ordonnancement manuel), `ZoomToFit()` + `ExportPng(path, entireProject)` (rendu hors écran du projet complet). **ProGantt terminé sur les 3 phases prévues.** Prochain différenciant : intégration LiveCharts2 (Ovidie).
     - **Charts Ovidie : LiveCharts2** (MIT, Avalonia natif, forkable) avec habillage ProTheme — pas de développement maison.

**Tests unitaires du socle (14/07/2026)** : `ProControls.Tests` (ProMaskEngine + moteurs Gantt/Document/Scheduler/MindMap, 178 tests) et `AvaloniaDataGrid/tests` (GridFilter/GridDataSource/PropertyAccessor/FormatValue, 61 tests) — `dotnet test` sur chaque projet. À maintenir : toute évolution d'un moteur pur passe par là.

## ProMindMap — carte mentale de brainstorming ✅ fait le 14/07/2026 (validé)

Hors cible Outlook — demande directe. Calibrage : brainstorming libre (édition = cœur), milliers de nœuds (culling viewport + cache de mesure de texte), 3 layouts.
- **Moteur pur testé (14 tests)** : hauteurs/largeurs de sous-arbres bottom-up, empilement sans chevauchement, **Radial** (branches équilibrées gauche/droite par hauteurs), **TreeRight** (organigramme couché), **TreeDown** (organigramme vertical, frères horizontaux centrés sous le parent), repli exclut les descendants, mesureur injecté.
- **Contrôle** : pilules arrondies (racine accent, couleurs de branche héritées, palette 8 teintes), connecteurs Bézier (horizontaux ou verticaux selon le mode, épaisseur décroissante), pan (drag du fond), zoom Ctrl+molette centré, culling.
- **Grammaire d'édition** (souris seule, clavier seul, ou mixte) : Tab = enfant, Enter = frère (édition immédiate ProTextBox in-place), Suppr, F2/double-clic, Espace = repli ; **pastille plier/déplier cliquable** sur le connecteur (compte de descendants repliés) ; badges de coin du nœud sélectionné : **« + » bas-extérieur** (ajout) et **« × » haut-extérieur** (suppression, racine exclue) — hors du couloir des connecteurs ; **clic droit = ProContextMenu** complet ; drag d'un nœud = re-parentage (élastique vert/rouge, descendants interdits, `NodeReparenting` annulable) ; navigation flèches transposée par mode ; undo/redo instantanés ; `ZoomToFit`/`CenterOnRoot`/`ExportPng` (hors écran). Dark mode suivi.
- Restes v2 : sérialisation (JSON + FreeMind .mm), notes/icônes par nœud, liens transverses, multi-sélection.

## Cible « Outlook natif Linux » — cartographie des manques (stade projet, 14/07/2026)

Étude de faisabilité : recréer un Outlook (shell + Courrier + Calendrier + Contacts + Tâches) 100 % natif avec la suite. **Aucun développement lancé** — cartographie de référence.

**Réutilisable tel quel** : Ribbon (basique), menus/contextuels, toolbar, statusbar, TabControl, TreeView (volet dossiers), JDataGrid (tâches, vues tabulaires), toute la famille éditeurs (formulaires contact/RDV), MessageBox, badges, ProWindow.

**🔴 Les 4 chantiers structurants** :
1. ~~**Rendu HTML des mails**~~ ✅ **v1 faite le 14/07/2026** (« les deux monstres », validée) : architecture « un socle, deux têtes » — ProDocument (modèle riche) + HtmlParser (sous-ensemble sanitisé PAR CONSTRUCTION : script/iframe/handlers éliminés, aucun réseau, images data: décodées / distantes via ImageResolver applicatif) + DocLayoutEngine (TextLayout Avalonia, tables simples, listes, citations, code, images, virtualisation) + **ProHtmlView** (liens cliquables, re-wrap). Restes v2 : images inline dans le flux texte (v1 = blocs), CSS étendu, sélection/copie dans le viewer.
2. ~~**ProRichEdit**~~ ✅ **v1 faite le 14/07/2026** (validée) : DocEditor (cœur pur testé — positions, insertion/suppression, découpe de runs pour style par plage, scission/fusion de paragraphes, undo/redo par instantanés à frappes regroupées, liens normalisés en runs LinkHref) + HtmlSerializer (HTML échappé en sortie) + **ProRichEdit** (caret clignotant par TextLayout, sélection souris/clavier, Ctrl+B/I/U/Z/Y/A/C/X/V, Enter/Backspace, titres, barre d'outils). Restes v2 : insertion d'images/tables/liens, collage HTML riche, Home/End niveau ligne complet.
3. ~~**ProScheduler**~~ ✅ **v1 faite le 14/07/2026** (validée) : moteur pur testé (expansion des récurrences None/Daily/Weekly-jours/Monthly + Interval/Until, couloirs de chevauchement greedy par clusters, plages de vues) + contrôle — vues Jour/Semaine ouvrée/Semaine (grille horaire scrollable, heures non ouvrées et week-ends ombrés, bandeau toute-la-journée, ligne « maintenant », événements côte à côte) et Mois (chips + « +n autres », aujourd'hui en pastille, molette = mois). Drag déplacement + resize (fantôme, snap 15 min, `EventChanging` annulable, récurrents non déplaçables v1), double-clic créneau → `TimeSlotDoubleClicked`, navigation ◀ Aujourd'hui ▶. Restes v2 : édition/détachement d'occurrences récurrentes, drag multi-jours, catégories/légendes, fuseaux.
4. ~~**ProListView**~~ ✅ **v1 faite le 14/07/2026** (validée) : liste virtualisée à items riches — ItemAdapter structuré (Titre/Aperçu/Trailing/Icône initiales/Badge/Emphasized = non-lu gras + barre accent), GroupSelector → en-têtes repliables avec compte, actions au survol (la date s'efface), sélection Single/Multi (Ctrl/Shift/Ctrl+A), clavier complet, ObservableCollection réactive, ellipsis. Démo « Boîte » = mini-Outlook 2 volets (ProListView + ProHtmlView, ouvrir = lu, suppression réactive). Restes v2 : drag & drop vers dossiers, hauteur d'item variable, avatars image.

~~**🟠 Nécessaires, taille moyenne**~~ ✅ **Faits le 14/07/2026** (validés) :
- **ProNavBar** : rail vertical de modules (icône+label+badge compteur, mode compact, sélection)
- **ProSearchControl** : sur ProEditorBase — suggestions en popup (callback app), ↑↓/Enter, croix d'effacement, `SearchRequested`/`SuggestionChosen`
- **ProTokenEdit** : chips avec ✕ + saisie inline, suggestions par callback, `TokenValidator` pour le texte libre, Enter/;/Tab valide, Backspace à vide retire le dernier
- **ProToast** : `ProToast.Show(fenêtre, titre, message, icône, durée, onClick)` — empilés bas-droite via OverlayLayer, auto-fermeture, clic actionnable
- **ProMonthCalendar** : calendrier mensuel natif (◀ ▶, molette, pastille aujourd'hui, `BoldDates` = jours à événements, bornes, clavier) — **remplace le Calendar Fluent dans ProDateEdit** (souveraineté)
- **Drag & drop applicatif** : ProListView source (`EnableDragItems`, seuil 5 px, DataObject `procontrols-item`), ProTreeView cible (`AllowDropItems`, surlignage du nœud, `ItemDropped`)
- Démo « Boîte » = shell Outlook 5 colonnes (rail + dossiers + recherche + liste + lecture, glisser mail→dossier, toast à l'arrivée, badge non-lus vivant) ; TokenEdit dans Composer ; MonthCalendar couplé à l'Agenda.

**🟡 Dettes existantes qui deviennent bloquantes** :
- ~~Raccourcis clavier réels~~ ✅ **Fait le 14/07/2026** : `ProShortcutManager` par fenêtre (KeyGesture.Parse des `Shortcut`, dispatch en bulle = le contrôle focalisé garde la priorité), enregistrement auto ProMenuBar (récursif, `RefreshShortcuts()`) + `ProRibbonButton.Shortcut`, **Alt+lettre** ouvre les menus, **navigation clavier complète** dans les menus ouverts (↑↓ saute séparateurs/désactivés, Enter, ←→ entre menus et sous-menus, Échap), `IsEnabled` bloque réellement le clic. API publique pour les raccourcis applicatifs.
- ~~Dark mode ProTheme~~ ✅ **Fait le 14/07/2026** (validé) : `ProTheme` à palettes commutables — l'API historique (`ProTheme.Background.Panel`…) est INTACTE, les tokens délèguent à la palette active (`ProPalette.Light`/`Dark`, 56 couleurs) ; `ProTheme.Variant` + `VariantChanged`. Propagation : ProWindow suit (RequestedThemeVariant pour les Fluent internes + invalidation récursive), 6 contrôles à brushes construits s'abonnent (Card, TextBox, EditorBase, TokenEdit, HtmlView/RichEdit = re-layout), dropdowns re-brushés (Combo/Date/Search/Token/LookUp via `RefreshThemeBrushes()` virtuel du socle), JDataGrid via ThemeDictionaries du skin Yaru (+ extraction des couleurs en dur du thème de base, commit AvaloniaDataGrid e04094c). Toggle 🌙 démo. Une 3e variante = une palette de plus.
- Restantes : ribbon avancé (backstage/galeries/QAT/onglets contextuels), rendu des colonnes Image/Bouton du JDataGrid + master-detail, impression (inexistante partout).
- **Migration Avalonia 12 (ajoutée au backlog le 14/07/2026)** : la suite est sur **11.2.1** ; NuGet au 14/07/2026 : ligne 11.x maintenue jusqu'à **11.3.18**, majeure actuelle **12.1.0**. Plan en deux temps : (1) bump faible risque **11.2.1 → 11.3.18** (même majeure — bump des deux projets + Avalonia.Fonts.Inter, build, 239 tests, tour de démo complet avec attention aux popups overlay et au rendu texte) ; (2) **migration 12.x = chantier dédié** : lire le guide de migration officiel, brancher, compiler, auditer nos points de contact bas niveau (OverlayPopups X11, TextLayout/ValueSpan, RenderOptions subpixel, OverlayLayer des toasts, DragDrop, RenderTargetBitmap hors écran, **snapping DPI = `GetVisualRoot()?.RenderScaling` dans Crisp.BeginFrame**). Ne pas mélanger les deux étapes.
- **ProDock (docking de panneaux, ajouté au backlog le 14/07/2026)** : panneaux ancrables/détachables/empilables façon IDE (VS Code/Rider) — cible : postes denses type dispatcher Synaxis. Décision d'architecture actée : **PAS de MDI** (étranger aux conventions Linux, structurellement incompatible Wayland — pas de positionnement global des fenêtres par l'app) ; le modèle Linux est SDI + onglets (ProTabControl ✅) + splits (GridSplitter) + **docking** (le seul morceau manquant). Périmètre envisagé : zones d'ancrage (gauche/droite/bas + centre document), drag d'un panneau avec aperçu des cibles, empilement en onglets, redimensionnement, panneaux épinglés/auto-masqués, sérialisation du layout. Gros chantier — à lancer quand un écran Synaxis le réclame.

**⚪ Hors contrôles (l'autre moitié de l'iceberg, pour mémoire)** : IMAP/SMTP (ou EWS/Graph), stockage local + indexation de recherche, iCal/récurrences, vCard.

**Volume estimé** : ~6 contrôles nouveaux + 4 moyens + 2 monstres (HTML, RichEdit) dépassant à eux seuls les lots 1-4. **Ordre naturel si lancement** : ProListView → NavBar/Search/TokenEdit → ProScheduler → RichEdit → rendu HTML (décision HTML en dernier).

**⚑ 14/07/2026 : LES 4 CHANTIERS STRUCTURANTS SONT FAITS (v1 validées)** — rendu HTML (ProHtmlView), ProRichEdit, ProListView, ProScheduler. Restent les moyens (ProNavBar, ProSearchControl, ProTokenEdit, ProAlert/toasts, ProMonthCalendar, drag & drop applicatif) et les dettes (raccourcis clavier réels, ribbon avancé, impression, dark mode) — puis le backend (IMAP/stockage/indexation) qui est l'autre moitié du produit.

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
