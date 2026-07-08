# ProControls — Contexte du projet

## Objectif

Créer une **bibliothèque de contrôles Avalonia UI professionnels** avec le style **Visual Studio 2022** pour les applications métier **Synaxis** (gestion des opérations de vol) et **Ovidie** (gestion financière).

## Caractéristiques clés

### 1. Style Visual Studio 2022

- **Palette de couleurs** : Gris/blanc subtil, sobre et professionnel
- **Accents** : Bleu (#0078D4) pour les éléments interactifs et focus
- **Typographie** : Segoe UI, tailles cohérentes (12px body, 14px subtitle, 18px title)
- **Bordures** : Fines (1px), couleurs subtiles (#E0E0E0, #CCCCCC)
- **États visuels** : Hover, pressed, focused, disabled — tous cohérents
- **Coins arrondis** : Subtils (2-4px) pour un look moderne mais professionnel

### 2. Architecture technique

- **Rendu "from scratch"** : Contrôles en C# pur avec `DrawingContext`
- **Pas de templates XAML** : Contrôle total sur chaque pixel
- **Thème centralisé** : `VS2022Theme.cs` contient toutes les constantes (couleurs, tailles, polices)
- **Héritage minimal** : Hériter de `Control` ou `ContentControl` selon le besoin

### 3. Indépendance technologique

- **Aucune dépendance vendor** : Pas de DevExpress, Telerik, etc.
- **Souveraineté technologique** : Code source maîtrisé à 100%
- **Dépendances** : Avalonia UI 11.2.1 + projet frère `Julien.Avalonia.DataGrid` (datagrid maison, ../AvaloniaDataGrid)

### 4. Contrôles cibles

| Contrôle | Description | Priorité |
|----------|-------------|----------|
| ProButton | Boutons avec variantes (Primary, Secondary, Ghost, Danger) et tailles (S, M, L) | ✅ Fait |
| ProTextBox | Champ texte avec placeholder, password, erreur, readonly | ✅ Fait |
| ProCheckBox | Case à cocher tri-state | ✅ Fait |
| ProRadioButton | Bouton radio avec groupes | ✅ Fait |
| ProToggleSwitch | Interrupteur on/off | ✅ Fait |
| ProSlider | Curseur de valeur | ✅ Fait |
| ProProgressBar | Barre de progression (déterminée/indéterminée) | ✅ Fait |
| ProBadge | Étiquette de statut colorée | ✅ Fait |
| ProCard | Conteneur avec titre et bordure | ✅ Fait |
| ProWindow | Fenêtre custom avec chrome personnalisé | ✅ Fait |
| ProMenuBar | Barre de menu horizontale | ✅ Fait |
| ProMenuItem | Élément de menu avec sous-menus | ✅ Fait |
| ProContextMenu | Menu contextuel (clic droit) | ✅ Fait |
| ProRibbon | Ruban style Office avec onglets et groupes | ✅ Fait |
| ProComboBox | Liste déroulante | 🔲 À faire |
| ProTabControl | Onglets de navigation | 🔲 À faire |
| ProTreeView | Arborescence | 🔲 À faire |
| JDataGrid (réf.) | Grille de données : référence au projet `Julien.Avalonia.DataGrid` (source de vérité) + skin VS2022 (`Theme/DataGrid/VS2022DataGrid.axaml`) | ✅ Référencé |
| ProToolbar | Barre d'outils | 🔲 À faire |
| ProStatusBar | Barre de statut | 🔲 À faire |

## Palette de couleurs VS2022

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
