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
- **Seules les lignes marquées « E1, fait » sont faites.** Toutes les autres sont des propositions, confirmées au lot indiqué.
- **Une fonction n'est retirée que si elle a un équivalent** (décision du 05/10/2026). Une ligne « supprimé » donne donc toujours l'équivalent.

### 2.1 Socle (HDevEditorBase : DateEdit, TimeEdit, SpinEdit, SearchControl)

| Avant | Après | Lot |
|---|---|---|
| `DoValidate()` | `Validate()` | **E1, fait** |
| `ErrorText` (posé par `SetError`) | `ErrorText` calculé : erreur de saisie, sinon de liaison | **E1, fait** |
| `SetError(texte)` | conservé ; ne pose plus que l'erreur de saisie | **E1, fait** |
| `TextChanged` (`EventHandler`) | conservé | — |
| `Validating`, `HDevValidatingEventArgs` | conservés (revus en E8, validation centralisée) | E8 |
| `Mask`, `IsMaskComplete`, `Placeholder`, `IsReadOnly`, `FocusEditor()`, `SelectAll()` | conservés | — |

### 2.2 HDevDateEdit et HDevTimeEdit

| Avant | Après | Lot |
|---|---|---|
| DateEdit `Value` (propriété CLR) | `Value`, StyledProperty liable dans les deux sens | **E1, fait** |
| DateEdit `ValueChanged` (`EventHandler`) | `ValueChanged` (`EventHandler<HDevValueChangedEventArgs>`, routé, commun) | **E1, fait** |
| — | DateEdit `ValueChanging` (annulable) | **E1, fait** |
| DateEdit : l'heure de `Value` tronquée à minuit | heure conservée ; une date choisie garde l'heure de la valeur courante | **E1, fait** |
| `MinDate`, `MaxDate`, `Format` | conservés (plus clairs que `DisplayDateStart` et `CustomDateFormatString` d'Avalonia) | — |
| TimeEdit `Value` (CLR) et `ValueChanged` | comme DateEdit | E3 |

### 2.3 HDevSpinEdit

| Avant | Après | Lot |
|---|---|---|
| `Value` (`decimal`, CLR) | `Value`, StyledProperty liable | E3 |
| `Value` (`decimal`) | `decimal?` (valeur nulle permise) | E5 |
| `MinValue`, `MaxValue` | `Minimum`, `Maximum` (noms Avalonia) | E3 |
| `Increment` | conservé (nom Avalonia de `NumericUpDown`) | — |
| `Decimals` | conservé (plus simple qu'une chaîne de format) | — |
| `ValueChanged` (`EventHandler`) | `ValueChanged` commun | E3 |

### 2.4 HDevTextBox

| Avant | Après | Lot |
|---|---|---|
| `Text` | conservé : c'est la valeur de l'éditeur | — |
| `TextChanged` (`EventHandler<TextChangedEventArgs>`) | conservé ; s'y ajoute le `ValueChanged` commun | E3 |
| `HasError` (réglable, `HasError="True"`) | lecture seule ; l'erreur se pose par `SetError` ou par la validation | E3 |
| `FocusTextBox()` | `FocusEditor()` | E3 |
| `IsPassword`, `IsMultiline`, `MaxLength`, `Clear()` | conservés | — |

### 2.5 HDevComboBox

| Avant | Après | Lot |
|---|---|---|
| `Properties` (objet de réglages) | supprimé : réglages directs sur le contrôle | E3 |
| `Properties.Items` | `Items`, et `ItemsSource` pour une source liée | E3 |
| `Properties.NullText`, `Properties.NullValuePrompt` | `Placeholder` | E3 |
| `Properties.ShowNullValuePrompt` | `ShowPlaceholder`, propriété directe : conservé, et renommé comme l'invite (`Placeholder`) ; décidé le 05/10/2026 | E3 |
| `Properties.ReadOnly`, `ReadOnly` | `IsReadOnly` | E3 |
| `Properties.TextEditStyle` | `TextEditStyle`, propriété directe : conservé avec ses trois modes, car `HideTextEditor` n'a pas d'équivalent dans Avalonia, qui n'a que `IsEditable` ; décidé le 05/10/2026 | E3 |
| `Properties.AutoComplete`, `CaseSensitiveSearch`, `ImmediatePopup`, `AllowNullInput`, `DropDownRows` | conservés, en propriétés directes | E3 |
| `Properties.Sorted` | `Sorted`, propriété directe : conservé | E3 |
| `Properties.ShowDropDown` | remplacé par les boutons d'éditeur (`Buttons`) | E2 |
| `EditValue` | `Value` (et `IHDevEditor.EditValue`) | E3 |
| `OldEditValue` | supprimé : `OldValue` des arguments de `ValueChanged` | E3 |
| `EditValueChanged`, `SelectedIndexChanged`, `SelectedValueChanged` | `ValueChanged`, levé aussi pour un changement par le code (constat E0 n° 1) | E3 |
| `EditValueChanging` | `ValueChanging` | E3 |
| `IsPopupOpen` (lecture seule) | `IsDropDownOpen`, StyledProperty liable | E2 |
| `ShowPopup()`, `ClosePopup()`, `HidePopup()` | `IsDropDownOpen = true / false` | E2 |
| `Popup` (annulable), `PopupOpened` | `DropDownOpening` (annulable), `DropDownOpened` | E2 |
| `QueryCloseUp`, `CloseUp`, `PopupClosed` | `DropDownClosing` (annulable, avec acceptation de la valeur), `DropDownClosed` | E2 |
| `ButtonClick` | `ButtonClick` des boutons d'éditeur du socle | E2 |
| `DoValidate()` | `Validate()` | E3 |
| `RefreshEditValue()` | supprimé : les liaisons s'en chargent | E3 |
| `SelectedIndex`, `SelectedItem`, `Text`, `TextChanged`, `IsModified`, `CustomDisplayText`, `Reset()`, `SelectAll()`, `DeselectAll()`, `FindItem()`, `FindItemByPrefix()`, `GetItemText()` | conservés | — |

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
| `ShowPopup()`, `ClosePopup()` | `IsDropDownOpen` | E2 |
| `Columns`, `DropDownRows`, `DropDownWidth` | conservés | — |

### 2.7 HDevSearchControl et HDevTokenEdit

| Avant | Après | Lot |
|---|---|---|
| SearchControl `Text` | conservé : c'est la valeur | — |
| SearchControl `SuggestionsProvider`, `SuggestionText`, `SearchRequested`, `SuggestionChosen` | conservés | — |
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
