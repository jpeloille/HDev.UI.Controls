# Chantier F : fondations transverses

Chantier du plan de la suite (`../Plan-Suite.md`). Il couvre le tableau 4.13 du benchmark et les dimensions transverses (§ 3 du HTML). État au 04/10/2026. Statut : proposition.

## 0. Point de départ

<!-- généré:SCORES:4.13 -->
| Tableau | Lignes | Aujourd'hui | S1 | S2 | S3 | S4 | S5 | S6 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 4.13 Composants transverses | 10 | 15 % | 15 % | 25 % | 45 % | 65 % | 70 % | 70 % |
<!-- fin -->

**Ce que dit le benchmark** (vérifié sur le code le 04/10/2026)
- **Liaison MVVM partielle.**
  - Sont liables : `Text` de HDevTextBox, la valeur de DateEdit (depuis E1), `IsChecked` des cases et radios, `IsOn` de l'interrupteur, `Value` du curseur et de la barre de progression, et toute l'API de HDevDataGrid.
  - Restent des propriétés CLR :
    - la source et la sélection de ListView, TabControl, ToggleButtonGroup, Chip, NavBar et TreeView ;
    - les modèles du Gantt, de l'agenda, du roster et de la carte mentale ;
    - `Html` de RichEdit et de HtmlView ;
    - le layout du docking.
  - L'arbre, le ruban, l'agenda et le Gantt se construisent en code.
- **Localisation** : aucune, les libellés français sont en dur. **Droite-à-gauche** : non.
- **Accessibilité** : focus visible au clavier, contraste testé, un seul `AutomationPeer` (HDevButton).
- **Thèmes** : Yaru clair et sombre, bascule à chaud, palettes injectables. Pas d'éditeur de thème.
- **Persistance** : docking et Gantt seulement. Ni la grille, ni un gestionnaire global.
- **Icônes** : quelques glyphes vectoriels internes, pas de banque.
- **OS** : Linux validé ; Windows et macOS possibles, mais non qualifiés.
- **Conception visuelle** : le prévisualiseur Avalonia seulement.

## 1. Décisions proposées

- **Un contrat des collections, symétrique du contrat de valeur (E1).**
  - `ItemsSource`, `ItemTemplate`, `SelectedItem` et `SelectedItems`, liables dans les deux sens.
  - Le changement de sélection est signalé par un événement routé qui porte son origine, comme `ValueChanged`.
  - Les noms sont ceux d'Avalonia (D3).
- **Les modèles se lient comme des valeurs.** Le projet du Gantt, les événements de l'agenda, le roster, la carte mentale, `Html` et le layout du docking deviennent des StyledProperty.
- **La localisation passe par les ressources .NET** (`.resx`, assemblies satellites), avec une culture commutable à chaud. Aucun libellé ne reste en dur dans les contrôles.
- **L'accessibilité repose sur un peer de base.**
  - Il est porté par `HDevControlBase` et spécialisé par contrôle.
  - Il se vérifie en headless sur l'arbre d'automation, et sur le bureau avec un lecteur d'écran.
- **Une variante de thème est une palette**, comme aujourd'hui. Le contraste élevé devient une troisième palette, et la densité un jeu de tailles.

## 2. Lots

<!-- généré:LOTS:F -->
| Lot | Objet | Taille | Dépend de | Jalon | Lignes visées | Statut |
|---|---|---|---|---|---|---|
| **F0** | Caractérisation des contrôles à collections | S | — | S1 | — | 🔲 |
| **F1** | Contrat des collections : ItemsSource, ItemTemplate, sélection liable | L | F0, E1 | S1 | — | 🔲 |
| **F2** | Modèles et documents liables | M | E1 | S1 | — | 🔲 |
| **F3** | Localisation : mécanisme de ressources, français et anglais | M | — | S2 | — | 🔲 |
| **F4** | Banque d'icônes et API d'icône commune | M | Q10 | S2 | 4.13 : 1 | 🔲 |
| **F5** | Accessibilité de toute la suite | L | — | S3 | — | 🔲 |
| **F6** | Persistance de l'espace de travail | M | G2 | S3 | 4.13 : 1 | 🔲 |
| **F7** | Comportements attachables et transitions | M | — | S4 | 4.13 : 2 | 🔲 |
| **F8** | Thèmes : contraste élevé, densité, concepteur de palettes | L | — | S5 | 4.13 : 1 | 🔲 |
| **F9** | Droite-à-gauche | L | F1 | S5 | — | 🔲 |
| **F10** | Qualification Windows et macOS | M | — | S5 | — | 🔲 |
| **F11** | Conception visuelle : données d'exemple et prévisualisation | S | F1 | S5 | — | 🔲 |
<!-- fin -->

Les lots sans ligne visée servent les dimensions transverses (§ 3 du HTML), qui ne sont pas notées.

## 3. Détail des lots

### F0 : caractérisation des contrôles à collections (S)

**Objet.** Figer le comportement de ListView, TreeView, TabControl, NavBar, ToggleButtonGroup, Chip et Accordion avant F1 :
- la sélection, à la souris, au clavier et multiple ;
- les événements ;
- l'ajout et le retrait d'items ;
- les groupes ;
- le glisser-déposer.

La méthode est celle d'E0 : des adaptateurs, et des constats laissés hors des attentes.

**Acceptation.** Les vérifications headless passent trois fois de suite.

### F1 : contrat des collections (L)

**Contenu.**
- `ItemsSource`, `ItemTemplate`, `SelectedItem`, `SelectedItems`, et `SelectionChanged` routé avec son origine. La liaison est bidirectionnelle par défaut.
- Contrôles concernés : ListView, TabControl, ToggleButtonGroup, groupe de Chip, NavBar, Accordion, et TreeView avec une source hiérarchique (les enfants viennent d'un gabarit).
- L'arbre et la barre de navigation se construisent en XAML.

**Acceptation.**
- Chaque contrôle est lié en XAML compilé à un ViewModel, dans les deux sens.
- Les tests F0 restent verts.
- Les ruptures sont consignées dans `MIGRATION-collections.md`.

### F2 : modèles et documents liables (M)

**Contenu.**
- Deviennent des StyledProperty :
  - le projet du Gantt ;
  - les événements de l'agenda ;
  - le modèle du roster ;
  - la racine de la carte mentale ;
  - `Html` de RichEdit et de HtmlView ;
  - le layout du docking.
- `Html` se lie dans les deux sens, pour l'édition.

**Acceptation.** Chaque modèle se lie en XAML compilé. Le Gantt et l'agenda se construisent sans code.

### F3 : localisation (M)

**Contenu.**
- Tous les libellés intégrés passent en ressources : menus des contrôles, messages de validation, filtres de la grille, boutons des boîtes de dialogue, noms de mois.
- Livraison en français et en anglais.
- Culture commutable à chaud, à la manière du thème.

**Acceptation.**
- Aucun libellé en dur, vérifié par un test qui parcourt les sources.
- Planches en français et en anglais.

### F4 : banque d'icônes (M)

**Contenu.**
- Un jeu d'icônes vectorielles, dont la source se décide en Q10.
- Une API commune (nom, taille, couleur qui suit le thème), consommée par les boutons, le ruban, les barres, les menus, l'arbre et les éditeurs.
- Les glyphes internes (`HDevEditorIcon`) y migrent.

**Acceptation.** Une planche « icônes » en clair et en sombre. Le contraste des icônes atteint 3:1.

### F5 : accessibilité de la suite (L)

**Contenu.**
- Un peer de base dans `HDevControlBase` : rôle, nom accessible tiré du libellé ou de l'infobulle, états.
- Des peers spécialisés pour :
  - les contrôles à collections (sélection) ;
  - le ruban et les menus (invocation, expansion) ;
  - les contrôles dessinés, comme le Gantt et l'agenda (navigation de base).
- Les éditeurs relèvent d'E12, la grille de G3.

**Acceptation.** Des tests headless sur l'arbre d'automation, et une vérification avec Orca sur le bureau réel.

### F6 : persistance de l'espace de travail (M)

**Contenu.** Un gestionnaire enregistre et restaure, dans un seul document versionné :
- le layout de la grille (G2) ;
- le docking ;
- l'état des fenêtres ;
- les colonnes du Gantt (L2) ;
- les préférences de vue de l'agenda.

**Acceptation.**
- L'aller-retour est testé.
- Une version de schéma future est refusée, selon la même règle que le JSON du Gantt.

### F7 : comportements attachables et transitions (M)

**Contenu.**
- Des comportements attachables en XAML, sans sous-classer : presse-papiers, persistance, défilement automatique, validation.
- Des transitions de pages habillées sur le `TransitioningContentControl` d'Avalonia (glissement, fondu). N4 s'en sert.

**Acceptation.**
- Les comportements sont démontrés et testés en headless.
- Les transitions se désactivent quand le système demande moins d'animations.

### F8 : thèmes (L)

**Contenu.**
- Une palette à contraste élevé.
- Une densité compacte, qui réduit hauteurs et retraits.
- Un outil de conception de palettes qui vérifie le contraste pendant la saisie.

**Acceptation.**
- `ThemeContrastTests` s'étend aux trois palettes.
- Des planches existent pour chaque variante et chaque densité.

### F9 : droite-à-gauche (L)

**Contenu.** Tous les contrôles dessinés respectent `FlowDirection` : dispositions, chevrons et ordre des colonnes passent en miroir.

**Acceptation.** Des planches dans une langue écrite de droite à gauche, et des tests de géométrie en miroir.

### F10 : qualification Windows et macOS (M)

**Contenu.**
- Le banc headless et les planches tournent aussi sous Windows et macOS.
- On vérifie, plateforme par plateforme, les popups, le rendu du texte et les raccourcis.

**Acceptation.** Le banc est vert sur les trois plateformes, et les écarts sont consignés.

### F11 : conception visuelle (S)

**Contenu.**
- Des données d'exemple pour le prévisualiseur (`Design.DataContext`).
- Les contrôles dessinés sont rendus en conception.

**Acceptation.** Chaque contrôle s'affiche dans le prévisualiseur de l'IDE.

## 4. Projection

<!-- généré:PROJ:4.13 -->
| Fonction | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|
| Thèmes | 🟡 | F8 | S5 | ✅ |
| Validation centralisée | 🟡 | E8 | S3 | ✅ |
| Dépôt d'éditeurs partagé (in-place) | ❌ | E11 | S3 | ✅ |
| Persistance de layout | 🟡 | F6 | S3 | ✅ |
| Behaviors | ❌ | F7 | S4 | ✅ |
| Gabarits d'UI HTML-CSS | ❌ | Q7 | — | ⚪ proposé |
| Transitions animées | ❌ | F7 | S4 | ✅ |
| Banque d'icônes | ❌ | F4 | S2 | ✅ |
| Chat IA | ❌ | Q6 | — | ⚪ proposé |
| Extensions IA (assistant, Smart Paste…) | ❌ | Q6 | — | ⚪ proposé |
<!-- fin -->

- La validation centralisée et le dépôt d'éditeurs relèvent du chantier E (E8, E11).
- Les deux lignes IA attendent la décision Q6.
- Les gabarits d'UI HTML-CSS sont proposés en absence par décision (Q7) : l'idiome Avalonia, c'est le gabarit XAML.

La projection des dimensions transverses est dans `../Plan-Suite.md`, § 6.

## 5. Risques

- **F1 touche sept contrôles.** Il faut les passer un par un, derrière F0.
- **Une localisation tardive coûte cher**, car chaque nouveau contrôle ajoute des libellés en dur. D'où F3 au plus tôt, en S2.
- **L'accessibilité sous Linux** passe par la prise en charge d'AT-SPI dans Avalonia. Elle se vérifie sur le bureau réel, pas seulement en headless.
