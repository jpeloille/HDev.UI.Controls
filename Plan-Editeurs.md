# Plan : éditeurs HDev.UI (famille 4.2 du benchmark HDEV-001/26)

Plan de lots pour combler la famille « Éditeurs » du benchmark DevExpress, état au **04/10/2026**.

## 0. Point de départ

**Principe (décision du 04/10/2026).** La suite se conçoit pour elle-même.
- Critères : cohérence de l'architecture, parité DevExpress, idiomes Avalonia.
- **DevExpress fixe le niveau, pas la solution technique.** Ses capacités sont le plancher à atteindre. Quand une conception plus simple, plus pratique ou plus idiomatique existe, c'est elle qu'on retient : l'objectif est d'être aussi bon que DevExpress, et meilleur quand on le peut.
- **Les applications hôtes s'adaptent à la suite, pas l'inverse.** Leur usage actuel ne fixe aucune priorité, et la compatibilité ascendante de l'API n'est pas une contrainte.
- Chaque rupture est consignée dans une note de migration (§ 2).

**Base de code** : commit `4fc218e`. Le travail d'accessibilité et de contraste (focus visible, jetons de contraste, `HDevRadioGroup`, `HDevButtonAutomationPeer`) y est commité depuis le 04/10/2026. Le prérequis est levé.

**Mesure de départ** (`Benchmark-DevExpress.md`) :

| Tableau | Couvert | Partiel | Absent | Score |
|---|---:|---:|---:|---:|
| 4.2 Éditeurs (48 fonctions) | 17 | 3 | 28 | 39 % |
| 5.7 Socle des éditeurs (17 capacités) | 7 | 4 | 6 | 53 % |

Les tests E0 ont fait changer d'état deux lignes de 5.7, sans changer le score. « Popups qui se retournent » passe à Couvert, car tous les popups se retournent. « Masques simples » passe à Partiel, car le collage contourne le masque.

**Constats dans le code**
1. **Pas de socle unique.** Il existe trois familles d'implémentation :
   - sur `HDevEditorBase` : DateEdit, TimeEdit, SpinEdit et SearchControl ;
   - sur `Border`, à part : TextBox et TokenEdit ;
   - dessinés à la main : ComboBox (1 729 lignes), LookUpEdit et MonthCalendar.
2. **Le socle est fermé et rigide.**
   - Il n'a qu'un seul élément à droite (`SetRightElement`) et un popup protégé (`AttachPopup`).
   - Sa hauteur est figée (`MinHeight = MaxHeight = EditorHeight`), ce qui exclut le multiligne.
   - ComboBox et LookUpEdit gèrent leurs popups seuls. ComboBox a sa propre garde de fermeture, à côté de `HDevPopupDismissGuard`, et bascule elle-même son placement.
   - Hors ComboBox, les popups (DateEdit, LookUpEdit, SearchControl, TokenEdit) s'en remettent au positionneur d'Avalonia. Tous se retournent vers le haut faute de place, ce que E0 constate en headless, mais leur décalage vertical n'est pas reflété (constat E0 n° 4).
3. **Les valeurs ne sont pas liables.** Ce sont des propriétés CLR : `Value` de DateEdit, TimeEdit et SpinEdit ; `EditValue`, `SelectedIndex` et `SelectedItem` de ComboBox ; `EditValue` de LookUpEdit ; `Tokens` ; `SelectedDate`. `Text` est CLR sur le socle mais `StyledProperty` sur HDevTextBox.
4. **Deux styles d'API cohabitent.**
   - Le style DevExpress de la ComboBox : un objet `Properties` qui porte `Items`, `NullValuePrompt`, `TextEditStyle`, `AutoComplete`…, et `ReadOnly`.
   - Le style Avalonia du socle : `IsReadOnly`, `Placeholder`.
   - Les événements de valeur varient : `ValueChanged`, `EditValueChanged`, `TokensChanged`, `SelectedDateChanged`.
   - `TextChanged` est un `EventHandler` sur le socle, un `EventHandler<TextChangedEventArgs>` sur HDevTextBox.
   - Aucune liste n'accepte `ItemsSource` ni `ItemTemplate`.
5. **Les éditeurs n'étaient pas testés en interaction** avant E0. Il n'y avait qu'un test, sur la combo, et la couverture se limitait au moteur de masque (18 tests xUnit) et à la planche visuelle « Champs ». E0 a levé ce constat (§ 3).
6. **Le pont avec la grille est cassé.** Dans `HDevDataGridCell.GetEditorValue()`, seuls les contrôles Fluent sont reconnus. Un éditeur HDev posé par `EditTemplate` ne renvoie jamais sa valeur à la validation.

## 1. Décisions d'architecture (validées le 04/10/2026)

- **D1, un socle unique.**
  - Un socle commun, issu de `HDevEditorBase`, porte la zone de valeur (champ texte, ou présentateur personnalisé pour les jetons et les affichages non éditables), les boutons d'éditeur, le popup, le masque, la validation et l'état visuel.
  - Tous les éditeurs de champ y passent : TextBox (multiligne compris), ComboBox, LookUpEdit, DateEdit, TimeEdit, SpinEdit, SearchControl, TokenEdit.
  - Les contrôles de choix (cases, interrupteur, radios) et MonthCalendar restent des contrôles à part. Ils implémentent le même contrat de valeur (D2).
  - **Forme technique ouverte, choisie au mérite** (précision de Julien à la validation). Le `BaseEdit` de DevExpress est une référence, pas un modèle. La note de conception d'E1 compare au moins deux formes :
    - la chaîne d'héritage de DevExpress, où chaque capacité vient d'une classe (`TextEdit`, puis `ButtonEdit`, puis `PopupBaseEdit`, puis `DateEdit`) ;
    - une base légère qui porte le contrat et l'habillage, avec des pièces composables (boutons, popup, masque, validation) que chaque éditeur active sans changer de classe.
  - Critères de choix : simplicité d'usage en XAML et en C#, ajout d'une capacité sans nouvelle classe, réemploi tel quel dans la grille (E11).
- **D2, des valeurs liables d'emblée.**
  - Interface `IHDevEditor` : `EditValue` non typée, pour le code générique et la grille, avec `ValueChanging` (annulable) et `ValueChanged`, plus `IsReadOnly`, `Placeholder`, `ErrorText`, `Validate()` et `FocusEditor()`.
  - Chaque éditeur expose une `Value` typée en `StyledProperty`, `TwoWay` par défaut, avec la validation de liaison active.
  - C'est l'idiome Avalonia, et le moment le moins cher pour le poser est la refonte du socle.
- **D3, un seul nommage, celui d'Avalonia.**
  - Noms retenus : `IsReadOnly`, `Placeholder`, `ItemsSource`, `ItemTemplate`, `SelectedItem`, `Value`, `ValueChanged`.
  - L'objet `Properties` de la ComboBox disparaît : ses réglages deviennent des propriétés directes du contrôle.
  - Pas d'alias. Les ruptures vont dans la note de migration.
- **D4, les types de date et d'heure modernes**, comme DevExpress v26.1 : `DateTime`, `DateOnly`, `TimeOnly`, `DateTimeOffset` et `TimeSpan`.
  - Les valeurs à décalage s'affichent par défaut en heure locale.
  - Le fuseau d'affichage est réglable (local, UTC, ou un fuseau donné).
  - Point ouvert, réglé en E1 : le typage de `Value` (D2) quand un éditeur admet plusieurs types.

## 2. Lots

| Lot | Objet | Taille | Dépend de | Lignes 4.2 visées | Statut |
|---|---|---|---|---|---|
| **E0** | Tests de caractérisation des éditeurs | S | — | — | ✅ |
| **E1** | Conception du socle (D1), contrat `IHDevEditor` et conventions (D2, D3) | M | E0 | — (5.7) | 🟡 forme à valider |
| **E2** | Socle composite : zone de valeur, boutons, popup, multiligne | L | E1 | 6, 19 | 🔲 |
| **E3** | Portage des éditeurs existants sur le socle | L | E2 | — (5.7) | 🔲 |
| **E4** | Sélection : cases, items à gabarit, liste à cases | M | E3 | 12, 13, 22 | 🔲 |
| **E5** | Saisie typée : masques typés, formats numériques, calculatrice | M | E3 | 2, 24, 25 | 🔲 |
| **E6** | Date et heure : types modernes, durées, plages, créneaux | L | E3 | 32, 33, 34 (+ 30, 31, 35 enrichies) | 🔲 |
| **E7** | Lookups : saisie et filtre, recherche, multi-valeurs | M | E3 | 15, 16, 17 | 🔲 |
| **E8** | Validation centralisée | S | E1, E3 | — (5.7) | 🔲 |
| **E9** | Compléments : mémo, lien, couleur, chemin, curseur de plage | M | E2 | 5, 8, 27, 42, 44 | 🔲 |
| **E10** | Compléments secondaires : MRU, curseur de zoom, notation, image | M | E2 | 7, 28, 39, 40, 41 | 🔲 |
| **E11** | Pont grille (deux dépôts) : éditeurs HDev dans `HDevDataGrid` | M | E3 | — (5.7) | 🔲 |
| **E12** | Accessibilité des éditeurs (`AutomationPeer`) | M | E3 | — (5.7) | 🔲 |

**Contraintes communes** à tout lot qui ajoute ou modifie un popup ou un contrôle dessiné :
- **Fermeture des popups.**
  - Garde `HDevPopupDismissGuard` avec désactivation débouncée, jamais le light-dismiss d'Avalonia.
  - `OverlayPopups`, et rattachement du popup à l'arbre logique.
  - Le comportement se vérifie aussi sur le bureau réel (GNOME/XWayland), pas seulement en headless.
- **Thème.** `RefreshThemeBrushes` re-brushe le popup au changement de thème.
- **Texte dessiné.** Les origines de texte dessiné passent par `Crisp.Snap`.
- **Chrome des champs.**
  - Gabarits : hauteur `Size.EditorHeight` (28) en monoligne, retrait 8, filet de 1 px, halo de focus et d'erreur.
  - Les boutons ont un anneau `HDevFocus` et un contraste d'au moins 3:1. Tout texte atteint au moins 4,5:1.
- **Ruptures d'API.** Elles sont permises (§ 0). Chaque lot qui en introduit les consigne dans `MIGRATION-editeurs.md` : ancien nom, nouveau nom, équivalence.
- **Fin de lot.**
  - Mettre à jour les états de `Benchmark-DevExpress.md` (tableaux 4.2 et 5.7).
  - Régénérer le HTML et le Word : `node ~/.claude/skills/hdev-docx-branding/exemples/benchmark-devexpress/{docx,html}.js`.

## 3. Détail des lots

### E0 : tests de caractérisation (S) ✅

**Objet.** Figer le comportement actuel des éditeurs avant la refonte du socle, pour qu'aucune régression ne passe inaperçue.

**Contenu.** Vérifications dans `tests/Interaction`, éditeur par éditeur :
- valeur et texte affiché, dans les deux sens ;
- moment où partent les événements de valeur, au code comme à l'utilisateur ;
- clavier : flèches, Entrée, Échap, F4 ;
- masque : saisie, collage, effacement ;
- popups :
  - ouverture et fermeture ;
  - retournement près du bas de la fenêtre ;
  - fermeture par clic extérieur ;
  - survie à une brève désactivation de la fenêtre ;
- changement de thème à chaud.

Les planches de `tests/Gallery`, en clair et en sombre, sont archivées comme référence visuelle.

**Acceptation.**
- Toutes les vérifications passent avant E2.
- Pendant E2 et E3, tout écart est soit corrigé, soit voulu et consigné dans la note de migration.

**Résultat (04/10/2026) : fait.**
- **58 vérifications** dans `tests/Interaction/Program.cs`, section « Éditeurs (caractérisation E0) » :

  | TextBox | DateEdit | TimeEdit | SpinEdit | ComboBox | LookUpEdit | SearchControl | TokenEdit | MonthCalendar | Thème à chaud |
  |---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
  | 5 | 12 | 5 | 5 | 10 | 7 | 4 | 6 | 3 | 1 |

- **Trois exécutions consécutives** au vert : 123 vérifications sur 123, dont 65 existantes et 58 nouvelles.
- **Adaptateurs.** Un adaptateur par éditeur est le seul point de contact avec l'API publique (valeur, texte, événements, focus, popup) et avec les champs internes.
  - En E1 à E3, une rupture d'API se corrige dans l'adaptateur et se consigne dans `MIGRATION-editeurs.md`.
  - Une attente qui change est un changement de comportement, à justifier.
- **Déterminisme.**
  - La culture fr-FR est épinglée puis restaurée, de même que la variante de thème.
  - Les compteurs d'événements sont relatifs : aucune attente ne dépend d'un constat ci-dessous.
- **Référence visuelle** : `artifacts/gallery/reference-J0/`, 9 planches en clair et en sombre.
  - Le dossier `artifacts/` est ignoré par git : la référence n'existe que sur cette machine.
  - La comparaison avant-après de E3 en dépend.
- **Hors headless.** La survie à une brève désactivation de la fenêtre (garde débouncée) ne se teste pas en headless, car Avalonia n'y offre aucun moyen de désactiver une fenêtre. Elle passe en vérification manuelle sur le bureau réel (GNOME/XWayland), comme le prévoient les contraintes communes. **À faire par Julien sur le bureau, avant E2.**
- **Piège d'Avalonia 12.1.3**, reproduit sur un `TextBox` nu.
  - Séquence : un collage, puis un texte posé par code, puis Tout sélectionner. Fin envoie alors le curseur en 0.
  - Les tests placent donc le curseur par la frappe.

**Constats E0.** Observés en headless, ils ne sont pas gravés dans les tests. Les numéros servent aux renvois du code.

| N° | Éditeur | Constat | Suite |
|---:|---|---|---|
| 1 | ComboBox | `SelectedIndex` posé par code relève `EditValueChanged`, mais pas `SelectedIndexChanged`. | Contrat posé en E1 : un seul `ValueChanged`, au code comme à l'utilisateur. La ComboBox le reçoit en E3 |
| 2 | ComboBox | Liste ouverte, Échap après une flèche garde l'élément parcouru au lieu de rendre la valeur d'ouverture. | Tranché le 04/10/2026 : Échap rend la valeur d'ouverture (§ 6). E3 |
| 3 | SearchControl | Après le choix d'une suggestion (↓ puis Entrée), la liste reste ouverte. | E3 |
| 4 | DateEdit, LookUpEdit, SearchControl, TokenEdit | Au retournement, Avalonia ne reflète pas le décalage vertical. DateEdit, SearchControl et TokenEdit : 2 px de jour dessous, 2 px de chevauchement dessus. LookUpEdit : bordure fusionnée dessous, 1 px de jour dessus. La ComboBox, qui bascule elle-même, reste cohérente. | E2 |
| 5 | DateEdit | Tant que le texte est invalide, la valeur garde la dernière valeur valide, ce qui se défend. Mais reposer cette même valeur par code ne réaffiche pas le texte : après un collage « abc », `Value = 14/09/2026` laisse « abc » à l'écran. | **Corrigé en E1** dans la couche typée du socle (vérifié sur DateEdit) |
| 6 | DateEdit, TimeEdit, SpinEdit | Le collage contourne le masque et le filtre. DateEdit garde tels quels « 20092026 », « 2026-09-21 » ou « abc » ; TimeEdit, « 1745 » ; SpinEdit, « 12a,5 ». La valeur ne bouge pas, et seule la validation rattrape. Un collage déjà au format (« 20/09/2026 ») passe. | E2 |
| 7 | DateEdit (mineur) | Après un collage, une frappe en bloc (IME, dictée, douchette) qui remplace tout le texte laisse la sélection active : le Retour arrière suivant efface tout. Cause non isolée : le cas ne se reproduit pas sans collage, et il voisine avec le piège d'Avalonia ci-dessus. | E2 |
| 8 | TokenEdit (mineur) | Hauteur de 29 px, contre 28 pour les autres champs (`Size.EditorHeight`). | E3 |

### E1 : contrat et conventions (M)

**Objet.** Choisir la forme du socle (D1) et poser D2 et D3 avant de l'écrire.

**Contenu.**
- **Note de conception du socle** : les formes candidates de D1 essayées sur un éditeur témoin, et le choix motivé par les critères de D1.
- Interface `IHDevEditor`, événements de valeur typés (ancienne et nouvelle valeur, annulation).
- Patron unique pour les valeurs : `StyledProperty`, `TwoWay` par défaut, validation de liaison active, coercition des bornes.
- Liste des noms retenus et des noms abandonnés : elle ouvre la note de migration.
- Un seul événement de valeur, qu'elle vienne du code ou de l'utilisateur (constat E0 n° 1).
- Le texte se resynchronise quand on repose la valeur (n° 5).
- Typage de `Value` quand un éditeur admet plusieurs types (D4) : un éditeur par type, un type déduit de la liaison, ou une base générique typée.

**Acceptation.**
- La note de conception est écrite, et la forme du socle est actée avant E2.
- Un test headless lie un éditeur témoin à un ViewModel, dans les deux sens, et voit remonter une erreur `INotifyDataErrorInfo`.
- Les conventions sont décrites en tête de `MIGRATION-editeurs.md`.

**Résultat (04/10/2026) : fait, sauf la forme, à valider.**
- **Note de conception** : `Conception-Socle-Editeurs.md`.
  - Elle recommande la forme B, une base légère avec des pièces composables, plutôt que la chaîne d'héritage de DevExpress.
  - Pour D4, elle recommande un éditeur par type de valeur.
  - **Ces deux choix sont à valider avant E2** (§ 6, question 4).
- **Contrat en place.**
  - `HDevEditorContract.cs` : `IHDevEditor` et l'événement de valeur commun (`HDevEditor.ValueChangedEvent`, avec son origine).
  - Couche typée `HDevEditorBase<T>`.
  - Deux sources d'erreur, la saisie et la liaison.
  - `Validate()`.
- **Témoin porté : HDevDateEdit.**
  - Sa valeur est liable dans les deux sens par défaut.
  - Il lève `ValueChanged` et `ValueChanging`.
  - L'heure de la valeur est conservée.
  - Le constat n° 5 est corrigé.
- **Vérifications.**
  - 13 vérifications « Contrat des éditeurs (E1) », sur un formulaire en XAML compilé lié à un ViewModel `INotifyDataErrorInfo`.
  - Le banc complet passe trois fois à 136 sur 136. Les attentes E0 n'ont pas changé : seule la ligne `Validate` des adaptateurs a bougé.
  - 380 tests xUnit verts.
  - Les 18 planches sont identiques au pixel près à `reference-J0`.
- **Note de migration ouverte** : `MIGRATION-editeurs.md`. Elle contient les conventions, la table complète des noms et les ruptures d'E1.
- **Prototype des mécanismes**, hors de la solution : `tests/Prototypes/SocleE1`. Il fonde les décisions de forme et de typage.

### E2 : socle composite (L)

**Objet.** Construire le socle commun de D1.

**Contenu.**
- **Zone de valeur** : le champ texte par défaut, ou un présentateur fourni par l'éditeur (jetons, texte non éditable de la combo).
- **Multiligne** : la hauteur n'est fixée qu'en monoligne.
- **Boutons d'éditeur.**
  - Collection `Buttons` avec `ButtonClick`, des types (déroulant, effacer, points de suspension, recherche, plus et moins, personnalisé) et une infobulle.
  - Glyphes ajoutés à `HDevEditorIcon` : effacer, points de suspension, loupe, horloge, calculatrice, lien, dossier, pastille.
- **Popup public.**
  - Contenu, ouverture, fermeture, événements d'ouverture et de fermeture annulables.
  - Retournement vers le haut faute de place, par le positionneur d'Avalonia, avec un décalage reflété (constat E0 n° 4). Le basculement manuel de la ComboBox devient inutile.
  - Clavier : F4 et Alt+↓ ouvrent, Échap ferme.
  - Une seule garde de fermeture pour toute la suite.
- **Masque et validation** hébergés par le socle, avec l'affichage d'erreur existant (filet, halo, infobulle).
  - Le collage passe par le masque (constat E0 n° 6).
  - Une saisie sous masque réduit la sélection au curseur (n° 7).
- **Nouveaux contrôles** : **HDevButtonEdit** (ligne 6) et **HDevPopupContainerEdit** (ligne 19).

**Acceptation.**
- Tests d'Interaction sur les boutons, le popup (y compris le retournement mesuré en géométrie), le collage sous masque et le multiligne.
- Planche « Champs » en clair et en sombre.

### E3 : portage des éditeurs existants (L)

**Objet.** Faire passer tous les éditeurs existants sur le socle et supprimer les implémentations parallèles.

**Contenu.**
- **HDevTextBox** : monoligne, multiligne, mot de passe.
- **HDevComboBox.**
  - `ItemsSource`, `ItemTemplate`, `SelectedItem` et `Value`.
  - Modes éditables et autocomplétion, repris de `Properties`, qui disparaît.
  - Le popup de liste passe sur le socle.
  - Échap dans la liste ouverte rend la valeur d'ouverture (décision du 04/10/2026, constat E0 n° 2), vérifié par un test d'Interaction.
- **HDevLookUpEdit** : le popup de grille passe sur le socle.
- **DateEdit, TimeEdit, SpinEdit, SearchControl** : ils adoptent le contrat et des valeurs liables. La liste de SearchControl se ferme après le choix d'une suggestion (constat E0 n° 3).
- **HDevTokenEdit** : les jetons deviennent la zone de valeur du socle, à la hauteur du gabarit (n° 8).
- **HDevMonthCalendar** : `SelectedDate` devient liable ; le contrôle reste hors socle.
- Le code de rendu propre à la ComboBox et au LookUpEdit fermés est supprimé.

**Acceptation.**
- Les tests E0 restent verts, ou leurs écarts voulus sont consignés.
- Chaque éditeur a son test de liaison TwoWay.
- Les planches avant et après sont comparées.
- 5.7 « Valeur liable » et « Socle commun » passent à **Couvert**.

### E4 : sélection (M)

**Objet.** Sélection multiple et items riches.

**Contenu.**
- **HDevCheckedComboBox** (ligne 13) : popup à cases, « Tout sélectionner », texte résumé (« Alice, Bob, +3 »), valeur sous forme de liste.
- **HDevCheckedListBox** (ligne 22), qui partage la liste à cases.
- **Items à icône ou couleur** (ligne 12) : par `ItemTemplate`, gratuit une fois E3 fait.

**Acceptation.**
- Tests d'Interaction : souris, Espace, « Tout sélectionner », résumé.
- Planche mise à jour.

### E5 : saisie typée (M)

**Objet.** Saisir des nombres, des montants et des codes formatés.

**Contenu.**
- **Masques typés dans `HDevMaskEngine`** : numérique (culture, décimales, bornes), date et heure, durée, et un sous-ensemble de regex pour les codes (ligne 2).
- **Formats de SpinEdit** (ligne 25) :
  - séparateur de milliers, monnaie et pourcentage ;
  - une forme d'affichage et une forme d'édition ;
  - une valeur nulle permise.
- **HDevCalcEdit** (ligne 24) : calculatrice en popup, sur un moteur pur (priorités, pourcentage, effacement) et pilotable au clavier.

**Acceptation.**
- Tests xUnit : le moteur de masque passe de 18 à environ 60 tests, plus le moteur de calcul.
- Tests d'Interaction : saisie, collage et effacement sous masque.
- 5.7 « Masques typés » passe à **Couvert**.

### E6 : date et heure (L)

**Objet.** Couvrir tous les types temporels de .NET.

**Contenu.**
- **Types** (D4) : valeurs `DateOnly`, `TimeOnly` et `DateTimeOffset` (ligne 33), avec un fuseau d'affichage réglable.
- **HDevTimeSpanEdit** (ligne 32) : formats h:mm (au-delà de 24 h compris) et heures décimales.
- **HDevDateRangeEdit** (ligne 34), avec une sélection de plage et des numéros de semaine ISO dans `HDevMonthCalendar`, plus des préréglages : semaine, mois, mois précédent.
- **HDevTimeEdit** : une liste de créneaux en popup, avec un pas réglable.
- **Raccourcis de date** : T pour aujourd'hui, ↑ et ↓ pour un jour.

**Acceptation.**
- Tests xUnit : allers-retours de valeurs (changement d'heure compris), analyse des durées, préréglages, créneaux.
- Tests d'Interaction : sélection de plage, choix d'un créneau.
- Planche mise à jour.

### E7 : lookups (M)

**Objet.** Des lookups qu'on peut taper et filtrer.

**Contenu.**
- **HDevLookUpEdit** (ligne 15) : saisie avec filtre incrémental, bouton effacer, clavier complet.
- **Mode recherche** (ligne 16) : champ de recherche dans le popup, qui filtre la grille par son moteur `GridFilter`.
- **Lookup multi-valeurs** (ligne 17) : `HDevTokenEdit` alimenté par une source de lookup.
- **Lookup arborescent** (ligne 18) : **reporté au plan grille**, car il dépend d'un TreeList.

**Acceptation.** Tests d'Interaction (frappe, filtre, Entrée, Échap, retournement), planche mise à jour.

### E8 : validation centralisée (S)

**Objet.** Valider tout un formulaire d'un coup.

**Contenu.**
- **`HDevValidation`**, l'équivalent de `DXValidationProvider` :
  - règles Requis, Bornes, Regex et Personnalisée ;
  - `ValidateAll(racine)`, qui met le focus sur le premier champ fautif.
- Erreurs de règle et erreurs de liaison passent par le même affichage.

**Acceptation.**
- Tests xUnit du moteur de règles.
- Test headless sur un formulaire invalide.
- 5.7 « Validation centralisée » passe à **Couvert**.

### E9 : compléments (M)

**Objet.** Combler les éditeurs d'usage courant restants.

**Contenu.**
- **HDevMemoEdit** (ligne 5) : mémo en popup.
- **HDevHyperLinkEdit** (ligne 8) : ouverture par le lanceur du système.
- **HDevColorEdit** (ligne 42) : palette de la charte, couleurs récentes, hexadécimal.
- **HDevBrowsePathEdit** (ligne 44) : sélecteur natif du bureau via `StorageProvider`.
- **HDevRangeSlider** (ligne 27).

**Acceptation.** Pour chaque contrôle : tests d'Interaction et planche en clair et en sombre.

### E10 : compléments secondaires (M)

**Objet.** Finir la famille.

**Contenu.**
- **HDevMRUEdit** (ligne 7) : historique des saisies.
- **Curseur de zoom** (ligne 28) : `HDevSlider` avec boutons plus et moins.
- **HDevRatingControl** (ligne 39).
- **HDevPictureEdit** (ligne 40) et **image en popup** (ligne 41) : chargement par `StorageProvider`, effacement, recadrage simple.

**Acceptation.** Pour chaque contrôle : tests d'Interaction et planche.

### E11 : pont grille (M, deux dépôts)

**Objet.** Le même éditeur en autonome et dans une cellule de grille, pilier du modèle DevExpress.

**Contenu.**
- **HDev.UI.DataGrid**, qui fait foi :
  - contrat `IGridCellEditor` : valeur, focus, touches réservées tant qu'un popup est ouvert ;
  - `GetEditorValue()` et l'initialisation de l'éditeur passent par ce contrat ;
  - tests headless dans ce dépôt.
- **HDev.UI.Controls** :
  - les éditeurs implémentent le contrat ;
  - préréglages `HDevGridEditors` (texte, date, heure, nombre, combo, lookup, case).

**Acceptation.**
- Dans une cellule :
  - Entrée valide ;
  - Échap annule et restaure ;
  - Tab passe à la cellule suivante ;
  - Échap ferme d'abord un popup ouvert.
- 5.7 « Même éditeur en autonome et in-place » passe à **Couvert**, de même que la ligne correspondante de la check-list grille.

### E12 : accessibilité (M)

**Objet.** Exposer les éditeurs aux technologies d'assistance.

**Contenu.**
- Généraliser le premier `AutomationPeer` (`HDevButtonAutomationPeer`, commit `4fc218e`) : des `AutomationPeer` portés par le socle, exposant la valeur (`IValueProvider`), l'ouverture et la fermeture des popups (`IExpandCollapseProvider`) et l'état des cases et interrupteurs.
- Le nom accessible vient du libellé ou de l'invite.

**Acceptation.**
- Tests headless sur l'arbre d'automation : nom, valeur, état.
- 5.7 « Accessibilité » passe à **Couvert**.

## 4. Projection

### Les 48 lignes de 4.2

| # | Fonction | Aujourd'hui | Lot | Cible |
|---:|---|---|---|---|
| 1 | Texte mono-ligne | Couvert | E3 | Couvert |
| 2 | Texte masqué | Partiel | E5 | Couvert |
| 3 | Mot de passe | Couvert | E3 | Couvert |
| 4 | Multi-ligne | Couvert | E3 | Couvert |
| 5 | Mémo en popup | Absent | E9 | Couvert |
| 6 | Texte avec boutons | Absent | E2 | Couvert |
| 7 | Historique MRU | Absent | E10 | Couvert |
| 8 | Lien hypertexte | Absent | E9 | Couvert |
| 9 | Recherche, auto-suggestion | Couvert | E3 | Couvert |
| 10 | Jetons | Couvert | E3 | Couvert |
| 11 | Liste déroulante | Couvert | E3 | Couvert |
| 12 | Liste déroulante à images | Absent | E4 | Couvert |
| 13 | Liste déroulante à cases | Absent | E4 | Couvert |
| 14 | Choix de police | Absent | — | Décision (à acter) |
| 15 | Lookup | Partiel | E7 | Couvert |
| 16 | Lookup avec recherche | Absent | E7 | Couvert |
| 17 | Lookup multi-valeurs | Absent | E7 | Couvert |
| 18 | Lookup arborescent | Absent | plan grille | Absent (reporté) |
| 19 | Popup à contenu libre | Absent | E2 | Couvert |
| 20 | Galerie en popup | Absent | — | Décision (à acter) |
| 21 | Liste | Couvert | — | Couvert |
| 22 | Liste à cases | Absent | E4 | Couvert |
| 23 | Compteur | Couvert | E3 | Couvert |
| 24 | Calculatrice | Absent | E5 | Couvert |
| 25 | Monétaire | Partiel | E5 | Couvert |
| 26 | Curseur | Couvert | — | Couvert |
| 27 | Curseur de plage | Absent | E9 | Couvert |
| 28 | Curseur de zoom | Absent | E10 | Couvert |
| 29 | Roues tactiles | Absent | — | Décision (à acter) |
| 30 | Date | Couvert | E6 | Couvert |
| 31 | Heure | Couvert | E6 | Couvert |
| 32 | Durée | Absent | E6 | Couvert |
| 33 | Date avec décalage horaire | Absent | E6 | Couvert |
| 34 | Plage de dates | Absent | E6 | Couvert |
| 35 | Calendrier intégré | Couvert | E6 | Couvert |
| 36 | Case à cocher | Couvert | — | Couvert |
| 37 | Interrupteur | Couvert | — | Couvert |
| 38 | Groupe radio | Couvert | — | Couvert |
| 39 | Notation | Absent | E10 | Couvert |
| 40 | Image | Absent | E10 | Couvert |
| 41 | Image en popup | Absent | E10 | Couvert |
| 42 | Couleur | Absent | E9 | Couvert |
| 43 | Pinceau (dégradés) | Absent | — | Décision (à acter) |
| 44 | Chemin de fichier ou de dossier | Absent | E9 | Couvert |
| 45 | Progression | Couvert | — | Couvert |
| 46 | Progression indéterminée | Couvert | — | Couvert |
| 47 | Code-barres | Absent | — | Décision (à acter) |
| 48 | Webcam | Absent | — | Décision (à acter) |

**Score projeté de 4.2.** Il se calcule comme dans le benchmark : (Couvert + ½ Partiel) / 48. Une absence décidée reste comptée comme non couverte.

| Après | Couvert | Partiel | Absent | Score |
|---|---:|---:|---:|---:|
| Aujourd'hui | 17 | 3 | 28 | 39 % |
| J1 (E0 à E3) | 19 | 3 | 26 | 43 % |
| J2 (+ E4 à E7) | 31 | 0 | 17 | 65 % |
| J3 (+ E8, E11, E12) | 31 | 0 | 17 | 65 % |
| J4 (+ E9, E10) | **41** | 0 | 7 | **85 %** |

Les 7 absences restantes : 6 proposées en « Décision », et le lookup arborescent reporté au plan grille.

### Les 17 capacités de 5.7

| Capacité | Aujourd'hui | Lot | Cible |
|---|---|---|---|
| Valeur liable en XAML | Partiel | E3 | Couvert |
| Masques simples | Partiel | E2 | Couvert |
| Masques numériques, date-heure, regex | Partiel | E5 | Couvert |
| Validation annulable | Couvert | — | Couvert |
| Validation centralisée, `IDataErrorInfo` | Absent | E8 | Couvert |
| Texte d'invite | Couvert | — | Couvert |
| Lecture seule | Couvert | — | Couvert |
| Boutons d'éditeur configurables | Absent | E2 | Couvert |
| Popups qui se retournent | Couvert | E2 (décalage) | Couvert |
| Autocomplétion | Couvert | — | Couvert |
| Même éditeur en autonome et in-place | Absent | E11 | Couvert |
| Socle commun à tous les éditeurs | Partiel | E3 | Couvert |
| Thème à chaud | Couvert | — | Couvert |
| États visuels, halo de focus | Couvert | — | Couvert |
| Libellé flottant | Absent | — | Décision (à acter : étranger au style Yaru) |
| Accessibilité | Absent | E12 | Couvert |
| IA | Absent | — | Décision (à acter) |

Score projeté de 5.7, jalon par jalon :

| Après | Score |
|---|---:|
| Aujourd'hui | 53 % |
| J1 | 68 % |
| J2 | 71 % |
| J3 | **88 %** (15 sur 17) |
| J4 | 88 % |

## 5. Séquence recommandée

| Jalon | Contenu | Ce qu'il apporte |
|---|---|---|
| **J0** ✅ | E0 | Le comportement actuel des éditeurs verrouillé avant la refonte : 58 vérifications, 8 constats |
| **J1** | E1 → E2 → E3 | Contrat, socle unique, éditeurs portés et liables. Tout le reste en dépend. 4.2 : 43 % ; 5.7 : 68 % |
| **J2** | E4, E5, E6, E7 (dans l'ordre voulu, une fois E3 fait) | Les familles de saisie : sélection, saisie typée, temps, lookups. 4.2 : 65 % |
| **J3** | E8, E11, E12 | L'intégration et la qualité en profondeur : validation de formulaire, éditeurs dans la grille, accessibilité. 5.7 : 88 % |
| **J4** | E9, E10 | La largeur du catalogue. 4.2 : 85 % |

L'intégration et la qualité (J3) passent avant les compléments (J4). Ce sont des capacités transverses, qui valent pour chaque éditeur, alors que les compléments n'ajoutent que des contrôles.

## 6. Questions à trancher

1. **Décisions D1 à D4** (§ 1). **Validées le 04/10/2026.** Pour D1, le socle unique est acquis ; sa forme technique se choisit au mérite, dans la note de conception d'E1.
2. **Acter les absences par décision** :
   - en 4.2 : police, galerie en popup, roues tactiles, pinceau, code-barres, webcam ;
   - en 5.7 : libellé flottant et IA.

   Le benchmark serait mis à jour en conséquence.
3. **Échap dans la liste ouverte de la ComboBox** (constat E0 n° 2). **Tranché le 04/10/2026 : Échap rend la valeur d'ouverture.**
4. **Conception du socle** (`Conception-Socle-Editeurs.md`, § 10) :
   - la forme B, une base légère avec des pièces composables ;
   - pour D4, un éditeur par type de valeur ;
   - le nom de la valeur : celui d'Avalonia quand il existe (`Text`, `IsChecked`, `SelectedDate`), `Value` sinon ;
   - les suppressions de fonctions proposées dans `MIGRATION-editeurs.md`, qui ne sont pas des renommages.

## 7. Risques

- **Refonte du socle et portage de la ComboBox** (1 729 lignes). Plusieurs comportements ont été durement acquis : garde des popups sous GNOME/XWayland, retournement, autocomplétion. La parade :
  - poser les tests E0 avant ;
  - porter éditeur par éditeur ;
  - comparer les planches visuelles avant et après.
- **Popups sous GNOME / XWayland.** Le bureau de Julien rebondit en activation, ce qui a déjà donné des popups invisibles ou fermés trop tôt. Tout popup se vérifie sur le bureau réel.
- **Ruptures d'API.** Elles sont assumées (§ 0). Le risque est de les disperser : la note de migration les regroupe, lot par lot.
- **Deux dépôts pour E11.** `HDev.UI.DataGrid` fait foi : le contrat y entre d'abord, avec ses tests, avant les préréglages côté Controls.
