# Migration : éditeurs HDev.UI

Note de migration à l'usage des applications. La suite se conçoit pour elle-même, et les applications s'adaptent (`Plan-Editeurs.md`, § 0).
- Chaque rupture d'API des éditeurs est consignée ici, lot par lot, avec son équivalence.
- Il n'y a pas d'alias de compatibilité.

Ouverte au lot E1, le 04/10/2026.

## 1. Conventions

Elles valent pour tout éditeur, dès son passage au contrat de valeur. La conception est dans `Conception-Socle-Editeurs.md`.

### 1.1 La valeur

- **Une valeur typée, liable dans les deux sens par défaut.**
  - On écrit `Value="{Binding Depart}"`, sans `Mode=TwoWay`.
  - La validation de liaison est active : une erreur `INotifyDataErrorInfo`, une erreur d'annotation ou une exception de la source s'affiche dans l'éditeur.
- **Le nom de la valeur est celui d'Avalonia quand Avalonia en a un** :

  | Éditeurs | Propriété de valeur |
  |---|---|
  | Éditeurs de texte (HDevTextBox, HDevSearchControl) | `Text` |
  | Cases, interrupteur, radios | `IsChecked` |
  | HDevMonthCalendar | `SelectedDate` |
  | Tous les autres | `Value` |

  Il n'existe jamais deux noms pour la même valeur.
- **Le code générique passe par `IHDevEditor.EditValue`**, la valeur non typée. C'est le cas de la grille et de la validation de formulaire.
- **Poser une valeur depuis le code garde la liaison.** Avalonia 12 transmet la valeur à la source, et la liaison reste en place (mesuré).
- **Reposer la valeur courante** réaffiche son texte et efface l'erreur de saisie, sans lever d'événement.
- **Bornage.** Une valeur ramenée dans ses bornes est réécrite dans la source liée (mesuré) : le ViewModel reçoit la valeur bornée.
  - On le réserve aux bornes dures d'un type, comme `Minimum` et `Maximum` d'un nombre.
  - Une date hors bornes n'est jamais bornée en silence.
    - Posée par le code, elle s'affiche telle quelle, et la validation la signale.
    - Tapée par l'utilisateur, elle n'est pas prise.
- **Une valeur posée par le code ou par la source n'est pas modifiée par l'éditeur.** Par exemple, HDevDateEdit garde l'heure d'une valeur qu'il n'affiche pas.

### 1.2 Les événements

- **`ValueChanged`** est levé pour tout changement de valeur, qu'il vienne du code, de la source ou de l'utilisateur.
  - Ses arguments : `OldValue`, `NewValue`, `Origin` (`Code` ou `User`) et `IsUserChange`.
  - C'est un événement routé, commun à toute la famille : `HDevEditor.ValueChangedEvent`. Un formulaire l'écoute d'un seul `AddHandler` :

    ```csharp
    formulaire.AddHandler(HDevEditor.ValueChangedEvent, (s, e) => { if (e.IsUserChange) Modifie = true; });
    ```

  - Ancienne et nouvelle valeur sont de type `object` ; la valeur typée se lit sur l'éditeur. Ce choix est validé depuis le 05/10/2026.
- **`ValueChanging`** est levé avant un changement venu de l'utilisateur, et lui seulement.
  - On peut l'annuler avec `e.Cancel = true` : la valeur reste, et le texte revient à la valeur courante.
  - Une valeur venue du code ou de la source ne s'annule pas : elle se valide.
- **Encapsulation.** Un éditeur ne fait pas sortir les événements de valeur de ses pièces internes (le calendrier de DateEdit, la grille de LookUpEdit). Le formulaire ne voit que ceux de l'éditeur.

### 1.3 Les erreurs

- **`ErrorText`** affiche d'abord l'erreur de saisie : texte invalide, `Validating` refusé ou `SetError`. Sinon, il affiche l'erreur de la liaison.
- **Une saisie valide efface l'erreur de saisie.** L'erreur de la liaison reste jusqu'à ce que la source la retire.
- **`SetError(texte)`** pose ou efface l'erreur de saisie ; il ne touche pas à l'erreur de liaison.

### 1.4 Le nommage (D3)

- **Noms Avalonia** : `IsReadOnly`, `Placeholder`, `ItemsSource`, `ItemTemplate`, `SelectedItem`, `Value`, `ValueChanged`, `IsDropDownOpen`, `Minimum`, `Maximum`.
- **Méthodes** : `Validate()` et `FocusEditor()`.
- **Un nom HDev est gardé quand Avalonia n'a pas d'équivalent**, ou quand le nom HDev est nettement plus simple. Exemples : `IsPassword` plutôt que `PasswordChar`, `IsMultiline` plutôt que `AcceptsReturn` et `TextWrapping`. Le cas est signalé dans la table.
- **Les réglages d'un éditeur sont des propriétés directes du contrôle**, jamais un sous-objet (`Properties`).

## 2. Table des noms

« Conservé » signifie que le nom ne change pas. Le lot indique quand la rupture a lieu.
- **Seules les lignes marquées « E1, fait » ou « E2, fait » sont faites.** Toutes les autres sont des propositions, confirmées au lot indiqué.
- **Une fonction n'est retirée que si elle a un équivalent** (décision du 05/10/2026). Une ligne « supprimé » donne donc toujours l'équivalent.

### 2.1 Socle (HDevEditorBase : DateEdit, TimeEdit, SpinEdit, SearchControl, ButtonEdit, DropDownEdit)

| Avant | Après | Lot |
|---|---|---|
| `DoValidate()` | `Validate()` | **E1, fait** |
| `ErrorText` (posé par `SetError`) | `ErrorText` calculé : erreur de saisie, sinon de liaison | **E1, fait** |
| `SetError(texte)` | conservé ; ne pose plus que l'erreur de saisie | **E1, fait** |
| `TextChanged` (`EventHandler`) | `EventHandler<TextChangedEventArgs>`, le type d'Avalonia (QX-08 c) : `(s, e) => …` compile sans changement | **E3, fait** |
| `Validating`, `HDevValidatingEventArgs` | conservés (revus en E8, validation centralisée) | E8 |
| `Mask`, `IsMaskComplete`, `Placeholder`, `IsReadOnly`, `FocusEditor()`, `SelectAll()` | conservés | — |
| `IHDevEditor.Placeholder` | quitte `IHDevEditor` pour `IHDevFieldEditor : IHDevEditor`, l'interface des éditeurs de champ (QX-05, nom validé par QX-08 a) ; tout éditeur du socle l'implémente, les contrôles de choix et le calendrier non | **E3, fait** |
| `HDevEditorButton.Icon` lu pour `Custom` seulement | lu pour tout type : posé, il remplace le glyphe du type (QX-08 f) | **E3, fait** |
| `HDevEditorBase<T>.Value` (abstrait, public) | retiré ; chaque éditeur déclare sa valeur sous le nom D3 (`Value`, ou `Text` pour un éditeur de texte) ; le code générique passe par `IHDevEditor.EditValue` | **E2, fait** |
| `Text` (CLR) | conservé, surchargeable : un éditeur de texte le porte par sa StyledProperty liable (HDevButtonEdit) | **E2, fait** |
| — | `IsMultiline` : la hauteur n'est fixée qu'en monoligne | **E2, fait** |
| — | `Buttons` (`HDevEditorButton` : `Kind`, `Ellipsis` par défaut, et `Icon`, `None` par défaut, lu pour `Custom` seulement ; `Command`, `CommandParameter`, et `ToolTip.Tip`, `IsVisible`, `IsEnabled` d'Avalonia ; 26 px de large, ne prend pas le focus) et `HDevEditorButton.Click` (événement routé qui remonte, `RoutedEventArgs` : le bouton sur `Source`, `Handled` empêche l'action par défaut) | **E2, fait** |
| — | `IsDropDownOpen` (liable dans les deux sens), `DropDownContent`, `DropDownOpening` (`CancelEventArgs`), `DropDownOpened`, `DropDownClosing` (`HDevDropDownClosingEventArgs` : `Cancel`, `CloseReason`, `IsValueAccepted`), `DropDownClosed` (`HDevDropDownClosedEventArgs` : `CloseReason`, `IsValueAccepted`) ; `HDevDropDownCloseReason` : `Accepted`, `Canceled`, `LightDismiss` | **E2, fait** |
| `SetRightElement` (protégé, un seul élément utile) | conservé ; les éléments s'alignent de gauche à droite, avant les `Buttons` | **E2, fait** |
| — | `SetValuePresenter(Control?)` (protégé) : un présentateur à la place du champ texte | **E2, fait** |
| `HDevEditorIcon` | conservé ; s'y ajoutent `Clear`, `Ellipsis`, `Search`, `Clock`, `Calculator`, `Link`, `Folder`, `Swatch`, `Plus`, `Minus` | **E2, fait** |

### 2.2 HDevDateEdit et HDevTimeEdit

| Avant | Après | Lot |
|---|---|---|
| DateEdit `Value` (propriété CLR) | `Value`, StyledProperty liable dans les deux sens | **E1, fait** |
| DateEdit `ValueChanged` (`EventHandler`) | `ValueChanged` (`EventHandler<HDevValueChangedEventArgs>`, routé, commun) | **E1, fait** |
| — | DateEdit `ValueChanging` (annulable) | **E1, fait** |
| DateEdit : l'heure de `Value` tronquée à minuit | heure conservée ; une date choisie garde l'heure de la valeur courante | **E1, fait** |
| `MinDate`, `MaxDate`, `Format` | conservés (plus clairs que `DisplayDateStart` et `CustomDateFormatString` d'Avalonia) | — |
| TimeEdit `Value` (CLR) et `ValueChanged` | comme DateEdit : `Value` liable dans les deux sens, `ValueChanged` commun, `ValueChanging` | **E3, fait** |

### 2.3 HDevSpinEdit

| Avant | Après | Lot |
|---|---|---|
| `Value` (`decimal`, CLR) | `Value`, StyledProperty liable, bornée par coercition (la source liée reçoit la borne) | **E3, fait** |
| `Value` (`decimal`) | `decimal?` (valeur nulle permise) | E5 |
| `MinValue`, `MaxValue` | `Minimum`, `Maximum` (noms Avalonia), StyledProperty ; les changer ramène la valeur | **E3, fait** |
| `Increment` | conservé (nom Avalonia de `NumericUpDown`) | — |
| `Decimals` | conservé (plus simple qu'une chaîne de format) | — |
| `ValueChanged` (`EventHandler`) | `ValueChanged` commun ; s'y ajoute `ValueChanging` | **E3, fait** |

### 2.4 HDevTextBox

| Avant | Après | Lot |
|---|---|---|
| `Text` | conservé : c'est la valeur de l'éditeur, StyledProperty liable dans les deux sens, avec la validation de liaison | **E3, fait** |
| `TextChanged` (`EventHandler<TextChangedEventArgs>`) | conservé, c'est celui du socle ; s'y ajoutent le `ValueChanged` commun et `ValueChanging` | **E3, fait** |
| `HasError` (réglable, `HasError="True"`) | lecture seule ; l'erreur se pose par `SetError` ou par la validation | **E3, fait** |
| `FocusTextBox()` | `FocusEditor()` | **E3, fait** |
| `IsPassword`, `IsMultiline`, `MaxLength`, `Clear()` | conservés ; `IsMultiline` est celui du socle | — |
| `HDevTextBox : Border` | `HDevTextBox : HDevEditorBase<string?>` : masque, `Buttons`, popup, `Validate()`, `Validating` du socle | **E3, fait** |

### 2.5 HDevComboBox

| Avant | Après | Lot |
|---|---|---|
| `Properties` (objet de réglages, `HDevComboBoxProperties`) | supprimé : réglages directs sur le contrôle | **E3, fait** |
| `Properties.Items` | `Items` ; `ItemsSource` (liable) pour une source liée, qui prend le pas sur `Items`, comme dans `ItemsControl` | **E3, fait** |
| — | `ItemTemplate` : gabarit d'un élément, dans la liste et dans le champ non éditable | **E3, fait** |
| `Properties.NullText`, `Properties.NullValuePrompt` | `Placeholder` (`IHDevFieldEditor`) | **E3, fait** |
| `Properties.ShowNullValuePrompt` (`ShowNullValuePromptOptions` : `EmptyValue`, `EditorFocused`, `EditorReadOnly`) | `ShowPlaceholder` (`HDevPlaceholderVisibility` : `WhenEditable`, `WhenFocused`, `Always`), QX-08 d | **E3, fait** |
| `Properties.ReadOnly`, `ReadOnly` | `IsReadOnly` | **E3, fait** |
| `Properties.TextEditStyle` (`TextEditStyles` : `Standard`, `DisableTextEditor`, `HideTextEditor`) | `TextEditStyle` (`HDevTextEditStyle` : `Editable`, `SelectOnly` par défaut, `NoText`), QX-08 e | **E3, fait** |
| `Properties.ImmediatePopup` | `OpenDropDownOnInput`, QX-08 e | **E3, fait** |
| `Properties.AllowNullInput` | `AllowNull`, QX-08 e | **E3, fait** |
| `Properties.AutoComplete` (`AutoCompleteMode`), `CaseSensitiveSearch`, `DropDownRows`, `Sorted` | conservés, en propriétés directes | **E3, fait** |
| `Properties.ShowDropDown` | remplacé par les boutons d'éditeur : un bouton `DropDown` par défaut, dans `Buttons` | **E3, fait** |
| `EditValue` | `Value`, liable dans les deux sens (et `IHDevEditor.EditValue`) | **E3, fait** |
| `SelectedItem` (CLR, la valeur même en saisie libre) | StyledProperty liable ; l'élément de la liste, nul pour un texte libre ; le poser pose la valeur | **E3, fait** |
| `OldEditValue` | supprimé : `OldValue` des arguments de `ValueChanged` | **E3, fait** |
| `EditValueChanged`, `SelectedIndexChanged`, `SelectedValueChanged` | `ValueChanged`, levé aussi pour un changement par le code (constat E0 n° 1) | **E3, fait** |
| `EditValueChanging` (`HDevEditValueChangingEventArgs`, `NewValue` modifiable) | `ValueChanging` (`HDevValueChangingEventArgs`, annulable) | **E3, fait** |
| `IsPopupOpen` (lecture seule) | `IsDropDownOpen`, StyledProperty liable | **E3, fait** |
| `ShowPopup()`, `ClosePopup()`, `HidePopup()` | `IsDropDownOpen = true / false` | **E3, fait** |
| `Popup` (annulable, `HDevPopupEventArgs`), `PopupOpened` | `DropDownOpening` (annulable), `DropDownOpened` | **E3, fait** |
| `QueryCloseUp`, `CloseUp` (`HDevCloseUpEventArgs`), `PopupClosed` | `DropDownClosing` (annulable, avec acceptation de la valeur), `DropDownClosed` | **E3, fait** |
| `ButtonClick` | `HDevEditorButton.Click`, routé, des boutons d'éditeur ; un clic sur la valeur ouvre la liste sans le lever | **E3, fait** |
| `DoValidate()` | `Validate()` | **E3, fait** |
| `RefreshEditValue()` | supprimé : les liaisons s'en chargent | **E3, fait** |
| `IsEnabledProperty` (redéclarée par `new`) | supprimée : celle d'Avalonia | **E3, fait** |
| `HDevComboBox : Control` (dessinée) | `HDevComboBox : HDevEditorBase<object?>` : champ texte en mode `Editable`, présentateur sinon, popup du socle | **E3, fait** |
| `SelectedIndex`, `Text`, `TextChanged`, `IsModified`, `CustomDisplayText`, `Reset()`, `SelectAll()`, `DeselectAll()`, `FindItem()`, `FindItemByPrefix()`, `GetItemText()` | conservés | — |

### 2.6 HDevLookUpEdit

| Avant | Après | Lot |
|---|---|---|
| `DataSource` | `ItemsSource` | E3 |
| `DisplayMember` | `DisplayMemberBinding` (liaison compilée possible) | E3 |
| `ValueMember` | `SelectedValueBinding` | E3 |
| `EditValue` | `Value` | E3 |
| `SelectedRow` | `SelectedItem` | E3 |
| `NullText` | `Placeholder` | E3 |
| `ReadOnly` | `IsReadOnly` | E3 |
| `EditValueChanged` | `ValueChanged` | E3 |
| `ShowPopup()`, `ClosePopup()` | `IsDropDownOpen` | E3 (pièce du socle : E2, fait) |
| `Columns`, `DropDownRows`, `DropDownWidth` | conservés | — |

### 2.7 HDevSearchControl et HDevTokenEdit

| Avant | Après | Lot |
|---|---|---|
| SearchControl `Text` | conservé : c'est la valeur, désormais StyledProperty liable dans les deux sens, avec `ValueChanged` commun et `ValueChanging` | **E3, fait** |
| SearchControl `SuggestionsProvider`, `SuggestionText`, `SearchRequested`, `SuggestionChosen` | conservés | — |
| SearchControl : liste de suggestions dans son propre popup, croix d'effacement interne | popup du socle (`IsDropDownOpen`, `DropDownOpening`…) ; bouton d'éditeur `Clear` dans `Buttons` | **E3, fait** |
| TokenEdit `Tokens` | `Value` : la collection de jetons, liable | E3 |
| TokenEdit `TokensChanged` | `ValueChanged` | E3 |
| TokenEdit `TokenText`, `TokenValidator`, `SuggestionsProvider`, `Placeholder` | conservés | — |

### 2.8 Contrôles de choix et calendrier

| Avant | Après | Lot |
|---|---|---|
| CheckBox `IsChecked`, RadioButton `IsChecked`, `GroupName`, `IsThreeState` | conservés, liables dans les deux sens par défaut | E3 |
| ToggleSwitch `IsOn` | `IsChecked` (nom Avalonia) | E3 |
| `CheckedChanged`, `Toggled` | `ValueChanged` | E3 |
| `Label`, `OnLabel`, `OffLabel` | conservés (texte dessiné, plus simple qu'un `Content`) | — |
| MonthCalendar `SelectedDate` | conservé, liable | E3 |
| MonthCalendar `SelectedDateChanged` | `ValueChanged` | E3 |
| Slider `Value`, `Minimum`, `Maximum` | conservés, liables | E3 |
| Slider `ValueChanged` (`EventHandler<double>`) | `ValueChanged` commun | E3 |

## 3. Ruptures, lot par lot

Les renvois « E3 (pièce du socle : E2, fait) » de la table : la pièce existe sur le socle depuis E2 ; la ComboBox et le LookUpEdit, hors socle, la reçoivent à leur portage (E3), qui retire alors leur ancienne API.

### E1 (04/10/2026)

1. **`DoValidate()` devient `Validate()`** sur le socle : DateEdit, TimeEdit, SpinEdit et SearchControl.
2. **`ErrorText` est calculé** : l'erreur de saisie d'abord, sinon celle de la liaison. `SetError` ne pose plus que l'erreur de saisie.
3. **HDevDateEdit.`Value`** est une StyledProperty liable dans les deux sens, avec la validation de liaison.
4. **HDevDateEdit.`ValueChanged`** passe de `EventHandler` à `EventHandler<HDevValueChangedEventArgs>`, routé et commun. Un abonnement `(s, e) => …` compile sans changement.
5. **HDevDateEdit.`ValueChanging`** est nouveau. Il est annulable et n'est levé que pour les changements de l'utilisateur.
6. **HDevDateEdit ne tronque plus l'heure.**
   - Avant : `Value = 14/09/2026 10:30` relisait `14/09/2026 00:00`.
   - Maintenant : la valeur garde son heure, qui n'est pas affichée. Une date choisie par l'utilisateur garde l'heure de la valeur courante, ou minuit si elle était vide.
7. **Reposer la valeur courante réaffiche son texte** et efface l'erreur de saisie (constat E0 n° 5).
8. **Encapsulation des événements de valeur** des pièces internes, sur tout le socle.

Le Gantt, qui édite ses dates avec HDevDateEdit, n'est pas touché par le point 6 : son modèle ramène toujours le début et la fin des tâches à minuit (`GanttModel.cs`).

### E2 (05/10/2026)

**Ruptures**
1. **`HDevEditorBase<T>` ne déclare plus `public abstract T Value`** (rupture d'E1, décision du 05/10/2026). Chaque éditeur déclare sa propriété publique de valeur sous le nom D3 : `Value` pour HDevDateEdit (inchangé pour ses appelants), `Text` pour HDevButtonEdit. Le code qui lisait `Value` sur la base générique passe par `IHDevEditor.EditValue`. Restent sur la couche typée : `ValuePropertyCore` (protégé), `CommitUserValue`, `SetValueFromCode`, `EditValue`, les événements et les erreurs.
2. **`Text` du socle devient surchargeable** (`virtual`). Sans effet pour les appelants ; un éditeur de texte le porte par sa StyledProperty, sans masquer le `Text` du socle.
3. **Le collage passe par le masque et les filtres** (constat E0 n° 6). Il entre en entier ou pas du tout, comme une frappe en bloc ; en monoligne, seule la première ligne compte.
   - « 20092026 » donne « 20/09/2026 » ; « 2026-09-21 » et « abc » sont refusés par DateEdit ; « 1745 » donne « 17:45 » dans TimeEdit ; SpinEdit refuse « 12a,5 ».
   - **Écart voulu d'un test E1.** « reposer la valeur courante par code réaffiche son texte » fabriquait un texte invalide en collant « abc », désormais refusé. Il passe par une saisie incomplète (« 15 ») ; l'intention (constat E0 n° 5) est inchangée.
4. **Une saisie sous masque réduit la sélection au curseur** (constat E0 n° 7) : après un collage, une frappe en bloc puis Retour arrière n'efface plus qu'un caractère.
5. **Les popups de DateEdit, SearchControl, TokenEdit et LookUpEdit gardent leur écart au champ quand ils se retournent** (constat E0 n° 4) : 2 px de jour dessous comme dessus, ou la bordure fusionnée (−1 px) du LookUpEdit. Retourné, un popup est donc 4 px (LookUpEdit : 2 px) plus haut qu'avant.
6. **Une seule garde de fermeture** (`HDevPopupDismissGuard`) pour toute la suite : HDevComboBox et les menus (HDevMenuPopup) quittent leur copie.
   - **La désactivation de la fenêtre ne ferme plus un popup dessiné dans la fenêtre** (popups overlay : `X11PlatformOptions.OverlayPopups`, le cas de la démo et des applications sous Linux), quelle que soit sa durée : Alt+Tab, focus pris par une autre fenêtre (décision du 05/10/2026, option (b) de QX-02).
     - Avant : le popup se fermait si la fenêtre restait inactive plus de 250 ms. Sur le bureau réel, aucun popup ne survivait à une désactivation ; le headless le reproduisait à 400 ms et à 1,5 s.
     - Il se ferme comme avant par un clic dans la fenêtre hors de lui, Échap, un choix ou le code.
   - **Un popup natif** (fenêtre séparée, sur d'autres plateformes ou sans overlay) garde la fermeture après 250 ms de fenêtre inactive : il ne flotte jamais au-dessus des autres applications.
   - La garde le détecte à l'exécution, popup par popup, d'après l'hôte de son contenu : hôte overlay dans la fenêtre, ou racine de popup native. Aucune option publique.
   - Vérifié en headless, qui a des popups overlay ; le chemin natif y est couvert en substituant la détection. Vérifié aussi sur le bureau réel (GNOME/XWayland, 13/10/2026).
   - **Échap ferme aussi les menus hors de la barre** : un menu contextuel (`HDevContextMenu`) ou un `HDevMenuPopup` ouvert par le code. Il ne se fermait jusqu'ici qu'au clic, et un menu qui survit à la désactivation doit se fermer au clavier. Comme dans `HDevMenuBar`, Échap ferme toute la chaîne, sous-menus compris. L'API ne change pas.
7. **HDevDateEdit passe sur le popup du socle.** Il reçoit `IsDropDownOpen`, `DropDownOpening`, `DropDownOpened`, `DropDownClosing` et `DropDownClosed` ; Alt+↑ ferme désormais aussi le calendrier. Son bouton calendrier reste interne jusqu'au portage des boutons d'éditeur (E3).
8. **Les boutons-glyphes des éditeurs (`HDevEditorGlyphButton`) sont cliquables sur toute leur surface**, et plus seulement sur les traits de leur glyphe : le test de clic d'Avalonia suit ce qui est dessiné.
9. **Le bouton calendrier de HDevDateEdit et un bouton `DropDown` donnent le focus au champ** avant d'ouvrir ou de fermer le popup, comme un clic sur la zone de valeur. Les boutons ne prenant pas le focus, un popup ouvert d'un clic sur eux, le focus ailleurs, ne recevait pas Échap (constat du bureau réel, QX-02).

**Ajouts**
- Sur tout éditeur du socle : `IsMultiline`, `Buttons` et `HDevEditorButton.Click`, le popup public (`IsDropDownOpen`, `DropDownContent` et ses quatre événements).
  - Clavier du popup : F4 et Alt+↓ ouvrent ou ferment, Alt+↑ et Échap ferment, depuis le champ comme depuis le contenu ; Entrée dans le contenu ferme en acceptant (`CloseReason = Accepted`), sauf dans un texte multiligne.
  - `HDevEditorButton.Click` est routé et remonte, comme `Button.Click` d'Avalonia : un formulaire écoute tous les boutons d'éditeur d'un seul `AddHandler(HDevEditorButton.ClickEvent, …)`, le bouton se lit sur `Source`.
  - Un bouton `DropDown` bascule le popup ; `Clear` vide la valeur, sauf si `Click` est marqué traité (`Handled`, et jamais en lecture seule) ; les autres types ne font que lever `Click`. La commande du bouton s'exécute après `Click`, dans tous les cas.
  - `IsValueAccepted` dit si le choix fait dans le popup vaut : vrai par défaut pour `Accepted` et `LightDismiss` (un clic extérieur garde le choix), faux pour `Canceled` (Échap) ; `DropDownClosing` peut le changer.
- **HDevButtonEdit** : champ texte à boutons ; `Text` liable dans les deux sens ; un bouton `Ellipsis` par défaut.
- **HDevDropDownEdit** : `Value` (`object?`) liable, non éditable, affichée par `ValueTemplate`, à défaut par `ToString()`, sinon l'invite ; un bouton `DropDown` par défaut ; un clic sur le champ ouvre.
  - Le contenu se lie à `DropDownValue`, la valeur en cours : popup fermé, elle suit `Value` ; elle en repart à l'ouverture ; `Value` ne bouge pas tant que le popup est ouvert, et le champ l'affiche.
  - À la fermeture, si `IsValueAccepted`, la valeur en cours est prise comme un changement de l'utilisateur (`ValueChanging` annulable, puis `ValueChanged`, `Origin = User`) ; sinon, ou si le changement est annulé, elle revient à `Value`. Le popup se ferme dans tous les cas.

**Décisions de nommage du 05/10/2026** (ordre : conventions d'Avalonia, puis typologie HDev) : `ButtonClick` devient `HDevEditorButton.Click` routé ; `Reason` devient `CloseReason` ; `Accept`, `Cancel`, `Outside` deviennent `Accepted`, `Canceled`, `LightDismiss` ; `AcceptValue` devient `IsValueAccepted` ; HDevPopupContainerEdit devient **HDevDropDownEdit**. Ces noms n'ont jamais été livrés : ce ne sont pas des ruptures pour les applications.

### E3 (en cours, mission M-05)

**Ruptures**
1. **Le texte des éditeurs du socle suit son origine.** Avalonia lève `TextChanged` au tour suivant du répartiteur. Le socle relève donc l'origine de chaque changement (la valeur ou la saisie) quand il a lieu, et la rend au `TextChanged` qui suit. Un texte réécrit depuis la valeur ne passe plus pour une saisie : il ne rouvre plus une liste de suggestions (constat E0 n° 3, APP-09). L'API ne change pas.
2. **HDevSpinEdit** passe sur le contrat.
   - `Value` est une StyledProperty liable dans les deux sens. Elle est bornée par coercition : une valeur hors bornes venue du code ou de la source est ramenée à la borne, et la source liée reçoit la borne.
   - `MinValue` et `MaxValue` deviennent `Minimum` et `Maximum`, des StyledProperty. Les changer ramène la valeur dans le nouvel intervalle.
   - `ValueChanged` passe de `EventHandler` au `ValueChanged` commun ; un abonnement `(s, e) => …` compile sans changement. Les flèches, la molette et les boutons lèvent un changement d'origine Utilisateur, précédé de `ValueChanging`.
3. **HDevTimeEdit** passe sur le contrat, comme HDevDateEdit.
   - `Value` est une StyledProperty liable dans les deux sens, avec `ValueChanged` commun et `ValueChanging`.
   - C'est une heure du jour. Une durée de 24 h ou plus, ou négative, est ramenée dans [00:00, 24:00), comme avant, mais ce bornage atteint désormais la source liée.
   - **Les secondes d'une valeur posée par le code sont gardées**, sans être affichées : `Value = 17:05:42` relit `17:05:42`. Avant, elles étaient tronquées (`17:05`). Une heure tapée a 0 seconde.

4. **HDevSearchControl** passe sur le contrat.
   - `Text` est sa valeur (D3) : une StyledProperty liable dans les deux sens, avec `ValueChanged` commun et `ValueChanging`. La frappe lève un changement d'origine Utilisateur à chaque caractère, comme HDevButtonEdit.
   - La croix d'effacement devient un bouton d'éditeur `Clear` dans `Buttons` : l'application peut le retirer ou le compléter.
   - La liste de suggestions passe dans le popup du socle : `IsDropDownOpen` et les événements du popup valent pour elle. F4 et Alt+↓ ouvrent les suggestions du texte courant, s'il y en a. Son habillage est celui du socle : 4 px de retrait et des coins de 8 px, au lieu de 0 et 6.
   - Le focus qui quitte le champ, ailleurs que vers la liste, ferme les suggestions. Avant, elles restaient ouvertes.
5. **Une valeur posée par le code n'est plus réécrite par l'éditeur après coup.** Le texte réécrit depuis la valeur repassait pour une saisie au tour suivant (point 1). HDevSpinEdit à 0 décimale arrondissait ainsi `Value = 1234,5` en 1235, et effaçait l'erreur posée juste après par `SetError`. C'est corrigé ; la planche « Champs » montre désormais l'état Erreur de HDevSpinEdit.

6. **`IHDevEditor` perd `Placeholder`**, qui passe dans `IHDevFieldEditor : IHDevEditor` (QX-05, QX-08 a). Le code qui lisait l'invite par `IHDevEditor` passe par `IHDevFieldEditor`. Tous les éditeurs du socle l'implémentent ; les cases, la bascule, les radios, le curseur et le calendrier implémentent `IHDevEditor` seul.
7. **Le `TextChanged` du socle devient `EventHandler<TextChangedEventArgs>`**, le type d'Avalonia (QX-08 c). Un abonnement `(s, e) => …` ou un groupe de méthodes `(object?, EventArgs)` compile sans changement ; seul un lambda aux paramètres typés `EventArgs` est à retoucher.
8. **HDevTextBox passe sur le socle** (`HDevEditorBase<string?>`).
   - `Text` est une StyledProperty liable dans les deux sens, avec la validation de liaison ; la frappe lève `ValueChanged` (origine Utilisateur) à chaque caractère, précédé de `ValueChanging`.
   - `HasError` devient lecture seule : `HasError="True"` en XAML ne compile plus. L'erreur se pose par `SetError("…")`, ou vient de la liaison. La démo est adaptée.
   - `FocusTextBox()` devient `FocusEditor()`.
   - Il reçoit les pièces du socle : `Buttons`, popup, masque, `Validate()` et `Validating`.
   - Aucun changement de géométrie. Le rendu du texte diffère de quelques pixels d'antialiasing, sans déplacement : le champ interne n'est plus l'enfant direct de l'hôte.
9. **Le bouton calendrier de HDevDateEdit est un bouton d'éditeur** : `Kind = DropDown`, `Icon = Calendar`, le premier de `Buttons` (QX-08 f). L'application peut le retirer ou en ajouter d'autres. Son comportement ne change pas.
10. **`HDevEditorButton.Icon` vaut pour tout type** : posé, il remplace le glyphe du type ; `None` (défaut) garde celui du type.

11. **HDevComboBox passe sur le socle** (`HDevEditorBase<object?>`). La table 2.5 donne chaque nom.
   - Le rendu dessiné du champ fermé est supprimé. En mode `Editable`, la zone de valeur est le champ texte du socle ; sinon, un présentateur affiche l'élément par `ItemTemplate`, à défaut son texte, ou l'invite.
   - L'allure est gardée : gris des boutons en mode non éditable (survol, pression, filet accent liste ouverte), fond de champ en mode éditable, fond grisé en lecture seule. Le chevron est désormais un bouton d'éditeur de 26 px : il est 2 px plus à gauche qu'avant, aligné sur ceux des autres champs. Il ne se retourne plus quand la liste est ouverte. Le rendu du texte diffère de quelques pixels d'antialiasing, sans déplacement.
   - La combo reçoit l'état d'erreur du socle (`SetError`, validation de liaison), qu'elle n'avait pas.
   - La liste passe dans le popup du socle (garde, retournement par le positionneur, thème), avec l'habillage d'avant : accolée au champ, bordure fusionnée, coins du bas seuls, largeur du champ. La bascule manuelle vers le haut disparaît.
   - **Clavier, liste ouverte : les flèches, Début et Fin déplacent la surbrillance sans changer la valeur.** Entrée, Espace ou un clic acceptent l'élément ; **Échap rend la valeur d'ouverture** (constat E0 n° 2, décision du 04/10/2026) ; un clic extérieur garde l'élément mis en surbrillance au clavier. Avant, chaque flèche changeait la valeur, liste ouverte. Liste fermée, rien ne change : ↑ et ↓ changent la valeur, une frappe sélectionne par préfixe.
   - `SelectedIndex` ou `Value` posés par le code lèvent `ValueChanged` (origine Code), une fois (constat E0 n° 1).
   - Mode éditable : un clic extérieur valide aussi la saisie, comme le départ du focus (APP-10). Avant, il fermait la liste sans valider.
   - Liste non éditable : le focus qui part ailleurs que vers la liste la ferme.
12. **Types retirés avec l'objet `Properties`** : `HDevComboBoxProperties`, `ShowNullValuePromptOptions`, `TextEditStyles`, `HDevPopupEventArgs`, `HDevCloseUpEventArgs`, `HDevQueryCloseUpEventArgs`, `HDevEditValueChangingEventArgs`. Leurs équivalents sont dans la table 2.5. `HDevComboBoxItemCollection`, `AutoCompleteMode` et `HDevCustomDisplayTextEventArgs` restent.

**Corrections des applications** (`Plan-Apports.md`)
- **APP-09.** La liste d'une ComboBox éditable à suggestion ne se rouvre plus après un choix. Une frappe rapide est complétée : la complétion se fait à chaque frappe, avant le champ, et une lettre qui remplace le suffixe proposé n'est plus prise pour un effacement.
- **APP-10.** Le départ du focus valide la saisie d'une ComboBox éditable, liste ouverte ou non, et ferme la liste.
- **APP-11.** `SuggestionText` de SearchControl et `TokenText` de TokenEdit ne reçoivent plus d'élément nul quand la liste recycle ses lignes.
- **Constat E0 n° 3.** La liste de SearchControl se ferme après le choix d'une suggestion.

