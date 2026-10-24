# DESIGN — charte de HDev.UI.Controls

Source de vérité du rendu visuel de la suite : ce que veulent dire les jetons, quand utiliser
quelle variante, et les règles que les tests (`ThemeContrastTests`, `BorderInputTests`,
`ThemeScaleTests`, `SelectionTokensTests`, `CategoricalPaletteTests`) garantissent.
Le code des jetons est dans `HDev.UI.Controls/Theme/HDevTheme.cs` ; la planche visuelle
(`dotnet run --project tests/Gallery -- <libellé>`) les rend en clair et en sombre.

Langage : Yaru / libadwaita (Ubuntu), police Inter embarquée.

## 1. Principes

1. **Une seule source de jetons.** Aucune couleur, taille ou rayon littéral dans un contrôle :
   tout passe par `HDevTheme`. Une palette hôte (`HDevPalette.Light with { … }`) rebrande la suite.
2. **Le contraste est une règle, pas un goût.** Texte ≥ 4,5:1, indicateurs et filets de champ ≥ 3:1,
   mesurés par `HDevTheme.Contrast` et vérifiés par les tests.
3. **La mesure égale le dessin.** Un contrôle occupe exactement ce qu'il dessine ; il se centre
   dans une rangée plus haute au lieu de s'étirer.
4. **Un langage par état.** Une famille pour « choix on/off », une pour « sélection dans une
   collection », un jeu « désactivé », un focus.

## 2. Jetons

### Couleurs
| Famille | Jetons | Usage |
|---|---|---|
| `Background.*` | Window, Panel, PanelAlt, Toolbar, StatusBar, Control (+Hover/Pressed), Input | Surfaces ; `Input` est le fond des champs |
| `Background.Selection` / `SelectionInactive` | teinte (#FBDDD2 / #683B2B en sombre) et gris | Sélection de collection ; le texte garde sa couleur |
| `Background.TextSelection` | teinte | Sélection de texte dans un champ (texte en `Text.Primary`) |
| `Text.*` | Primary, Secondary, Tertiary, Placeholder, OnAccent, Link, OnWarning | `OnAccent` n'est lisible que sur `Accent.Fill` |
| `Border.*` | Default, Subtle, Strong, Hover, **Input / InputHover** (≥ 3:1), Focused, FocusOuter | `Default` = filet décoratif ; `Input` = filet d'un champ ou d'une case |
| `Accent.Primary` | orange Yaru #E95420 | **Indicateurs sans texte** : focus, coches, progression, curseurs |
| `Accent.Fill` / `FillHover` / `FillPressed` | #C64614… | **Tout fond portant du texte** (bouton Primary, segment choisi, jour choisi, badge) |
| `Accent.SuccessFill` / `InfoFill`, `Danger.*` | fonds de statut | Badges et boutons à texte blanc |
| `Disabled.*` | Background, Border, Text | Un seul jeu « désactivé » (les anciens jetons sont `[Obsolete]`) |
| `Categorical(i)` | 8 teintes, ≥ 3:1 sur le panneau | Données : avatars, branches de carte mentale, séries |

Les jetons optionnels (`AccentFill`, `BorderInput`, `TextSelection`…) sont **dérivés** de la palette
active quand elle ne les fournit pas : un hôte à accent teal garde son teal.
Utiliser `HDevTheme.OnColor(fond)` pour le texte posé sur une couleur arbitraire, et
`EnsureContrast(couleur, fond, ratio)` pour garantir un ratio.

### Typographie
`FontSizeMicro` 11 (plancher) · `Caption` 12 · `Dense` 13 · `Body` 14 · `Subtitle` 16 · `Title` 19 · `Header` 24.
Aucune taille en dessous de 11. Graisses via `Typography.Weight*`.

### Géométrie
- Rayons : `XS` 3 (cases, barres) · `Row` 4 (survols, lignes) · `Small` 6 (boutons, champs) · `Medium` 8 (popovers) · `Large` 12 (cartes).
- Hauteurs de contrôle : 28 / 34 / 42 (`ControlHeightSmall/Medium/Large`) ; `EditorHeight` = `RowHeight` = 28 ;
  `MenuItemHeight` 32 ; `BarHeight` 34 ; `StatusBarHeight` 26 ; `TabHeight` 28 ; `TabHeightCompact` 24 (Dock).
- Traits : `StrokeHairline` 1 · `StrokeGlyph` 1,2 · `StrokeEmphasis` 2.
- Icônes : `IconSmall` 12 · `IconMedium` 16 · `IconLarge` 20.

## 3. États

### Deux familles de « coché »
- **Choix on/off** (case, radio, switch, segment) : accent plein ; `Accent.Fill` s'il y a du texte.
- **Sélection dans une collection** (ListView, TreeView, Gantt, Roster, NavBar, Chip cochée, jetons,
  bascules de Toolbar et de Ribbon) : `HDevTheme.SelectionFill(actif)` — teinte, grise quand le
  contrôle n'a plus le focus ; une barre accent de 3 px marque l'élément courant.

### Désactivé
`Disabled.*` pour les contrôles dessinés ; les composites (onglets, NavBar, accordéon, segments,
progression, badge) s'atténuent à 50 % (`HDevDisabled`).

### Lecture seule
Fond `PanelAlt`, pas de halo : un champ en lecture seule n'est ni grisé comme un désactivé ni
éditable.

### Focus
Visible **au clavier seulement** (Tab, flèches), jamais au clic. Anneau de `FocusRingWidth` 2 px,
décalé de `FocusRingOffset` 1 px, couleur `Border.FocusOuter` pleine. Les champs gardent un halo
(`HDevInputChrome`) ; les éléments de collection un anneau intérieur (`HDevFocus.DrawRing(inside)`).

### Boutons : quelle variante pour quoi
| Variante | Rôle |
|---|---|
| `Primary` | L'action principale de l'écran (une seule) |
| `Secondary` | Action courante, fond gris |
| `Outline` | « Annuler », actions de pied de dialogue |
| `OutlineAccent` | Action secondaire mise en avant (« Aperçu ») |
| `Ghost` | Barres d'outils, actions discrètes (sans filet) |
| `Danger` | Action destructive |

Pied de dialogue (convention GNOME) : Annuler en `Outline` à gauche, l'action en `Primary` à droite.
`HDevIconButton` : bouton carré à icône, mêmes variantes ; nommer l'action par une info-bulle.

## 4. Glyphes
`HDevIcon` : jeu vectoriel unique (26 glyphes : chevrons, Close, Check, Search, Plus, Minus, More,
Info, Warning, Error, Question, Trash, Pencil, Link, Indent, Outdent, Recurrence, Image, Pin,
ArrowUp/Down, Calendar), trait 1,2 px, teinté par le contrôle. **Pas d'emoji ni de caractère Unicode
comme icône** dans la bibliothèque. Emplacements : `HDevIconView`, `HDevIconButton.Icon`, et
`VectorIcon` sur les éléments de menu, barre d'outils, ruban, rail, onglets et chips.

## 5. Mode sombre
Chaque contrôle dessiné s'abonne à la bascule (`HDevTheme.Track`). `HDevTheme.ApplyTo(application)`
publie la palette en ressources (`HDev*Brush`, `SystemAccentColor*`) et l'info-bulle HDev.
`HDevHtmlView` rend sur une surface « papier » claire en sombre (`PaperInDarkMode`, par défaut) :
un mail aux couleurs inline reste lisible. Les popovers partagent `HDevTheme.PopoverShadow`
(renforcée en sombre).

## 6. Hors périmètre actuel (backlog P2)
Noms accessibles (AutomationPeers, AT-SPI), chiffres tabulaires, barres de défilement unifiées et
états vides, animations, allocations par image (Gantt, Scheduler, ListView), lignes du DataGrid.
