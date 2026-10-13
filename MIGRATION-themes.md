# Migration : thèmes HDev.UI

Note de migration à l'usage des applications. La suite se conçoit pour elle-même, et les applications s'adaptent.
- Chaque changement du thème qui touche une application est consigné ici, avec son équivalence.
- Il n'y a pas d'alias de compatibilité.

Ouverte par la mission M-04, le 13/10/2026 (lots F13, F12 en partie, début de F8).

## 1. Le catalogue

- **Six thèmes**, dans `HDevThemeDefinition.All`, dans l'ordre de présentation :

  | Clé (`Key`) | Nom affiché | Variante | Palette | Variante Avalonia |
  |---|---|---|---|---|
  | `Ubuntu26` | Ubuntu 26 | clair | `HDevPalette.Light` | `Light` |
  | `Ubuntu26Dark` | Ubuntu 26 sombre | sombre | `HDevPalette.Dark` | `Dark` |
  | `Ardoise` | Ardoise | clair | `HDevPalette.Ardoise` | `HDevThemeVariants.Ardoise` (hérite de `Light`) |
  | `Lagon` | Lagon | clair | `HDevPalette.Lagon` | `HDevThemeVariants.Lagon` (hérite de `Light`) |
  | `IndigoOr` | Indigo & Or | clair | `HDevPalette.IndigoOr` | `HDevThemeVariants.IndigoOr` (hérite de `Light`) |
  | `Cockpit` | Cockpit | sombre | `HDevPalette.Cockpit` | `HDevThemeVariants.Cockpit` (hérite de `Dark`) |

- **Appliquer un thème** : `HDevTheme.Apply(HDevThemeDefinition.Lagon)`.
  - Palette, variante et variante Avalonia changent d'un seul geste, avec un seul `VariantChanged`.
  - `HDevTheme.Theme` rend le thème actif ; `HDevThemeDefinition.FromKey(clé)` le retrouve à partir d'une préférence enregistrée.
- **Le mode historique reste** : `HDevTheme.Variant` et `HDevTheme.SetPalettes(...)` gardent leur comportement.
  - Les affecter **quitte le thème du catalogue**, même à variante égale : après `Apply(Ardoise)`, `Variant = Light` revient à la palette claire Yaru (ou à celle injectée par `SetPalettes`).
  - Une application qui injecte sa propre palette (`SetPalettes`) n'est pas concernée par le catalogue.

## 2. Ce que l'application doit faire

- **Appeler `HDevTheme.ApplyTo(this)`** dans `App.Initialize`, après le chargement du XAML.
  - C'est lui qui donne aux contrôles Fluent internes (TextBox, éditeurs de la grille, barres de défilement) l'accent du thème actif, et qui publie les pinceaux `HDev*Brush`.
  - **Retirer le bloc `SystemAccentColor*` d'`App.axaml`.** `Application.Resources` prime sur tout le reste : un accent recopié à la main fige l'accent Ubuntu (ou celui de l'application) quel que soit le thème.
  - Une application qui n'appelle pas `ApplyTo` garde l'accent Fluent par défaut, quel que soit le thème.
- **Poser la variante Avalonia du thème, pas Light/Dark** : `RequestedThemeVariant = HDevTheme.AvaloniaVariant`.
  - `HDevWindow` le fait déjà. Une fenêtre qui n'en dérive pas, et qui posait `HDevTheme.IsDark ? Dark : Light`, doit passer à `AvaloniaVariant`.
  - Sans cela, la grille reste habillée en Ubuntu 26 sous Ardoise, Lagon, Indigo & Or ou Cockpit.
- **Inclure l'habillage de la grille** comme avant : `<StyleInclude Source="avares://HDev.UI.Controls/Theme/DataGrid/Index.axaml" />`. Il contient un dictionnaire par thème.

## 3. Habillage de la grille : le thème, seule source

- L'habillage de `HDevDataGrid` (fond, en-tête, alternance, sélection, groupement, zone de dépôt) suit **le thème actif**, par les dictionnaires de `Theme/DataGrid/YaruDataGrid.axaml`.
- **Les palettes de grille posées sur une grille seule** (`Theme/DataGrid/Palettes/Lagon.axaml`, `Ardoise.axaml`, `IndigoOr.axaml`, `Cockpit.axaml`, apparues sur la branche `hdev` avec le sélecteur « Palette » de la démo) **n'existent pas sur `suite`**.
  - Équivalent : appliquer le thème du même nom ; les couleurs de grille sont identiques.
  - Aucune application ne les utilisait au 13/10/2026.
- Reste dans la grille (dépôt `HDev.UI.DataGrid`, hors de cette mission) : quelques couleurs en dur dans `HDevDataGrid.axaml`, dont le liseré de la ligne sélectionnée (bleu) et des textes gris, qui ne suivent pas encore le thème. Le texte de la zone de groupement est en anglais.

## 4. Teintes corrigées (contraste)

Texte d'état sur son fond d'état : 4,5:1 au moins dans les six thèmes (`ThemeContrastTests`).

| Thème | Jeton | Avant | Après | Contraste |
|---|---|---|---|---|
| Ubuntu 26 clair, Ardoise, Lagon | `TextWarning` | `#9C6E03` | `#8A6203` | 3,99 → 4,84:1 sur `BackgroundWarning` |
| Indigo & Or | `TextWarning` | `#8A6B1F` | `#8A6203` | 4,41 → 4,84:1 sur `BackgroundWarning` |
| Ubuntu 26 clair | `TextLink` | `#1C71D8` | `#1965C1` | 3,86 → 4,62:1 sur `BackgroundInfo` |
| Cockpit | `DangerPressed` | `#C9443D` | `#D35E58` | 3,72 → 4,68:1 sous `TextOnAccent` (sombre) |

- Une application qui dérive sa palette de `HDevPalette.Light` (`HDevPalette.Light with { … }`) hérite des nouvelles teintes d'avertissement et de lien.
- `AccentInfo` et `AccentInfoFill` d'Ubuntu 26 clair restent `#1C71D8` : seul le lien en texte s'assombrit.

## 5. Démo

- Le bouton 🌙 « Sombre » devient 🎨 « Thème » : chaque clic passe au thème suivant du catalogue.
