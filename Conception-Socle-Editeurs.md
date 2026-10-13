# Conception du socle des éditeurs (lot E1)

Note de conception du 04/10/2026, pour `Plan-Editeurs.md`. Statut : **validée le 05/10/2026**, avec une restriction sur les retraits de fonctions (§ 10).
- La forme du socle (§ 5), le typage de D4 (§ 7) et le nom de la valeur (§ 6) sont validés.
- Le contrat de valeur (§ 6) est déjà en place sur l'éditeur témoin, HDevDateEdit.

## 1. Objet et critères

D1 est validée : la suite aura un socle unique pour ses éditeurs de champ. Sa forme technique se choisit au mérite. DevExpress sert de référence de capacités, pas de modèle (`Plan-Editeurs.md`, § 0).

**Les trois critères de D1**
1. Simplicité d'usage, en XAML comme en C#.
2. Ajout d'une capacité (bouton, popup, masque) sans créer de classe.
3. Réemploi tel quel dans une cellule de grille (E11).

**Contraintes de la suite**
- Le chrome est dessiné : `HDevTheme`, `Crisp.Snap`, hauteur de 28 px, halo de focus et d'erreur.
- Les popups passent par l'overlay, avec la garde de fermeture maison.
- Les idiomes Avalonia s'appliquent : StyledProperty, liaisons, événements routés.

## 2. La référence DevExpress

- **Une capacité vient d'une classe.** En WinForms, la chaîne est `BaseEdit`, puis `TextEdit`, puis `ButtonEdit`, puis `PopupBaseEdit`, puis `DateEdit`, `ComboBoxEdit`, `LookUpEdit`…
  - Ces classes sont celles du benchmark HDEV-001/26. L'ordre d'héritage vient de mémoire et reste à vérifier sur la documentation DevExpress.
  - Les boutons supposent `ButtonEdit`, et le popup `PopupBaseEdit`.
  - Un champ numérique qui veut une calculatrice devient une autre classe, `CalcEdit`.
- **Chaque éditeur a une classe jumelle de réglages** : `RepositoryItem` en WinForms, `EditSettings` en WPF. C'est elle qui sert dans les grilles, si bien que l'API existe deux fois.
- **La valeur n'est pas typée** (`EditValue`) : l'application la convertit.

## 3. Trois formes candidates

| | A. Chaîne d'héritage (DevExpress) | B. Base légère et pièces | C. Habillage par composition |
|---|---|---|---|
| Principe | Une classe par niveau de capacité, puis une par éditeur | Une base qui porte le contrat et le chrome ; boutons, popup et masque sont des pièces que tout éditeur active | Un contrôle « champ » qui entoure un éditeur intérieur quelconque |
| Bouton « Effacer » sur un champ texte | Changer de classe (`HDevButtonEdit`) | Ajouter une pièce, même classe | Ajouter une pièce au champ |
| Champ numérique avec calculatrice | Nouvelle classe (`HDevCalcEdit`) | Contenu de popup, même classe | Contenu de popup |
| Nouvel éditeur (couleur) | Une classe, placée au bon niveau de la chaîne | Une classe : type de valeur, format, pièces par défaut | Un éditeur intérieur, plus son câblage au champ |
| Grille | Classe jumelle de réglages, ou instance par cellule | Le gabarit de l'éditeur lui-même | Le champ et son intérieur |
| Valeur et liaison | Sur la classe | Sur la classe | Sur l'intérieur, à relayer par le champ : deux étages à lier |
| Verdict | Écartée | **Recommandée** | Écartée |

Esquisse du critère 2, à valider en E2 (noms provisoires) :

```xml
<!-- A : pour un bouton, il faut changer de classe -->
<h:HDevButtonEdit Text="{Binding Nom}">
  <h:HDevButtonEdit.Buttons>
    <h:HDevEditorButton Kind="Clear" />
  </h:HDevButtonEdit.Buttons>
</h:HDevButtonEdit>

<!-- B : même classe, une pièce de plus ; un popup s'ajoute de la même façon -->
<h:HDevTextBox Text="{Binding Nom}">
  <h:HDevTextBox.Buttons>
    <h:HDevEditorButton Kind="Clear" />
  </h:HDevTextBox.Buttons>
</h:HDevTextBox>

<h:HDevSpinEdit Value="{Binding Montant}">
  <h:HDevSpinEdit.DropDownContent>
    <h:HDevCalculator />
  </h:HDevSpinEdit.DropDownContent>
</h:HDevSpinEdit>
```

**Pourquoi écarter C.** La valeur vit dans l'éditeur intérieur, et le champ doit la relayer : deux étages à lier, à valider et à brancher sur la grille. Pour l'usage courant, c'est moins simple que A et que B.

## 4. Essais et mesures

Ces essais ont été faits sur un prototype jetable, hors de la solution : Avalonia 12.1.3, headless, XAML compilé. Pour les reproduire : `dotnet run --project tests/Prototypes/SocleE1`.
- **Les pièces de B se déclarent en XAML compilé.**
  - Une collection `Buttons` accepte des éléments liables : la visibilité d'un bouton suit une propriété du ViewModel.
  - Un contenu de popup déclaré en XAML reçoit le DataContext dès l'ouverture.
- **La valeur typée se lie dans les deux sens sans `Mode`.**
  - `SetCurrentValue` garde la liaison et met la source à jour.
  - `SetValue` depuis le code aussi : Avalonia 12 transmet la valeur, et la liaison survit.
- **Deux règles de l'analyseur Avalonia.**
  - Un propriétaire générique de propriété est déconseillé (AVP1002).
  - Un enregistrement hors initialiseur statique est signalé (AVP1001).
  - D'où le patron retenu : la propriété est déclarée par la classe concrète, et la logique vit dans une couche générique. L'analyseur reste muet.
- **Un événement routé commun fonctionne**, avec un propriétaire hors de toute hiérarchie. Un conteneur le reçoit, origine comprise.
- **Imbrication.** L'événement d'une pièce interne, comme un éditeur dans un popup, traverse l'éditeur hôte puis atteint la fenêtre.
  - D'où la règle d'encapsulation, posée par un gestionnaire de classe.
  - Mesuré : le formulaire ne voit que les changements de l'hôte.
- **Bornage.** Une valeur bornée est réécrite dans la source : le ViewModel pose 150, l'éditeur borne à 100, le ViewModel reçoit 100.
- **D4.** Une valeur non typée, liée à un `DateOnly?` vide au départ, produit un `DateTime` que la liaison refuse : « Could not convert ». La saisie est perdue.

**Non essayé**
- La forme A n'a pas été prototypée : c'est le modèle connu de DevExpress.
- Pour B, seule la déclaration des pièces en XAML est prouvée. Leur rendu, leur clavier et leurs popups sont le travail d'E2.

## 5. Recommandation : la forme B

- **Le socle**
  - `HDevEditorBase` porte le chrome, la zone de valeur, le masque, la validation et les deux sources d'erreur.
  - `HDevEditorBase<T>` porte la valeur typée, ses événements et la liaison. Cette couche est déjà en place (§ 6).
- **Les pièces, à construire en E2**
  - `Buttons` : des boutons d'éditeur liables (type, infobulle, visibilité, commande), sur n'importe quel éditeur.
  - Le popup : son contenu, `IsDropDownOpen` (liable) et des événements d'ouverture et de fermeture annulables.
    - Une seule garde de fermeture pour toute la suite.
    - Le retournement d'Avalonia, avec un décalage reflété (constat E0 n° 4).
  - Le masque, qui filtre aussi le collage (constat E0 n° 6).
- **Un éditeur, une classe.** Elle fixe le type de valeur, le format, l'analyse du texte et ses pièces par défaut. Par exemple, DateEdit, c'est un bouton calendrier et un popup calendrier.
- **Lignes 6 et 19 de 4.2.** Le champ à boutons (ButtonEdit) et le popup à contenu libre (PopupContainerEdit) deviennent des configurations. Une classe nommée reste possible, pour la découverte.
- **Grille.**
  - La colonne porte le gabarit d'un éditeur, sa vraie classe.
  - La grille lit et écrit par `IHDevEditor.EditValue`, et suit `ValueChanged`.
  - Un popup ouvert garde ses touches en les marquant traitées.
  - Il n'y a pas de classe jumelle.

**Ce qui la rend meilleure que la référence**
- **Capacités par pièces.** Un bouton ou un popup s'ajoute à n'importe quel éditeur sans changer de classe. Chez DevExpress, il faut un `ButtonEdit` ou un `PopupBaseEdit`.
- **Une seule API par éditeur**, autonome comme dans une grille. DevExpress double chaque éditeur d'une classe de réglages.
- **Des valeurs typées**, y compris `DateOnly` et `DateTimeOffset`, sans conversion ni piège du null (§ 7).
- **Un seul événement routé pour toute la famille**, qui porte son origine (code ou utilisateur). Un formulaire suit tout d'un `AddHandler`. La comparaison avec DevExpress n'est pas sourcée dans le benchmark : à vérifier.
- **Deux sources d'erreur qui ne s'écrasent pas** (mesuré, § 6).

**Coûts et risques**
- **Les pièces sont à construire en E2.** Il faudra régler le clavier, le focus entre le champ et le popup, et le rendu dessiné des boutons.
- **Le contenu d'un popup** reçoit son DataContext ; les styles restent à vérifier.

## 6. Le contrat de valeur (fait en E1)

Le contrat est dans `HDevEditorContract.cs` (`IHDevEditor`, événements) et dans `HDevEditorBase.cs` (couche typée, erreurs). L'éditeur témoin est HDevDateEdit.

- **`IHDevEditor`**, tel que D2 l'a validé : `EditValue`, `ValueChanging`, `ValueChanged`, `IsReadOnly`, `Placeholder`, `ErrorText`, `Validate()` et `FocusEditor()`.
- **Le patron de la valeur**, une déclaration par éditeur :

  ```csharp
  public static readonly StyledProperty<DateTime?> ValueProperty =
      AvaloniaProperty.Register<HDevDateEdit, DateTime?>(nameof(Value),
          defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);
  protected override StyledProperty<DateTime?> ValuePropertyCore => ValueProperty;
  public DateTime? Value { get => GetValue(ValueProperty); set => SetValueFromCode(value); }
  ```

  Depuis E2 (décision du 05/10/2026), la couche typée ne déclare plus de `Value` public : l'éditeur concret déclare sa propriété sous le nom D3, `Value`, ou `Text` pour un éditeur de texte (HDevButtonEdit surcharge le `Text` du socle). Il n'y a ainsi qu'un nom par valeur.

  La couche typée fait le reste :
  - `CommitUserValue` pour les changements de l'utilisateur ;
  - le texte resynchronisé ;
  - l'événement levé, avec son origine ;
  - l'erreur de liaison.
- **Les événements.**
  - `ValueChanged` est routé et commun (`HDevEditor.ValueChangedEvent`). Il est levé pour le code comme pour l'utilisateur, ce qui règle le constat E0 n° 1 pour les éditeurs portés.
  - `ValueChanging` est annulable, et réservé aux changements de l'utilisateur. Une valeur du code ou de la source ne s'annule pas : elle se valide.
- **Les erreurs.** Il y a deux sources, gardées à part : la saisie et la liaison.
  - L'affichage montre la saisie d'abord.
  - Une saisie valide n'efface que l'erreur de saisie. L'erreur du ViewModel reste jusqu'à ce que la source la retire (vérifié).
- **Le texte.** Reposer la valeur courante réaffiche son texte (constat E0 n° 5).
- **Ce que l'éditeur ne fait jamais : modifier une valeur qui vient de la source.** HDevDateEdit garde l'heure qu'il n'affiche pas, et une date choisie garde l'heure de la valeur courante. Le bornage, qui réécrit la source, est réservé aux bornes dures d'un type.
- **Encapsulation.** Les événements de valeur des pièces internes s'arrêtent à l'éditeur.
- **Le nom de la valeur suit D3** : celui d'Avalonia quand il existe (`Text`, `IsChecked`, `SelectedDate`), `Value` sinon. Il n'y a jamais deux noms pour une même valeur.

**Deux écarts à D2**
- **Arguments non typés : écart validé le 05/10/2026.** D2 annonçait des événements de valeur typés. Or ancienne et nouvelle valeur sont de type `object`, parce qu'un seul événement sert toute la famille. C'est ce qui permet l'écoute d'un formulaire en un seul `AddHandler`. La valeur typée se lit sur l'éditeur (`Value`).
- **`Placeholder` dans `IHDevEditor`.** Il n'a pas de sens pour une case ou un interrupteur, qui doivent pourtant suivre le contrat (D1). C'est à trancher en E3 : soit une invite vide, soit une interface séparée pour les champs.
  - **Tranché le 13/10/2026 (QX-05)** : `Placeholder` sort de `IHDevEditor` et passe dans une interface séparée, propre aux éditeurs à zone de texte (nom fixé en E3). Les contrôles de choix (case, bascule, groupe radio) n'en portent pas ; la ComboBox garde son invite, comme celle d'Avalonia.

## 7. D4 : typer une valeur qui admet plusieurs types

| Option | Pour | Contre | Verdict |
|---|---|---|---|
| **Un éditeur par type** (HDevDateEdit pour `DateTime?`, HDevDateOnlyEdit, HDevDateTimeOffsetEdit ; HDevTimeEdit pour `TimeSpan?`, HDevTimeOnlyEdit), sur une implémentation générique commune | Typage à la compilation, aucune conversion, pas de piège du null ; la grille choisit l'éditeur d'après le type de la propriété | Plus de classes, mais minces | **Recommandée** |
| Type déduit de la liaison (valeur `object`) | Une seule classe | Piège mesuré : une source vide reçoit un `DateTime` refusé, et la saisie est perdue | Écartée |
| Type déclaré (propriété `ValueType`) | Une seule classe ; le cas du null est couvert | Ne marche que si l'on pense à la poser ; valeur non typée en C# | Écartée |
| Classe générique qui porte la propriété (`HDevDateEdit<T>`) | Une seule classe générique | Déconseillée par Avalonia (AVP1002) ; pas d'usage direct en XAML | Écartée |

## 8. La grille aujourd'hui (éléments pour E11)

Ces constats viennent d'une lecture du code de HDev.UI.DataGrid, qui fait foi pour la grille. Ils seront corrigés là-bas.
- **Les cellules sont des contrôles à gabarit.** Seule la cellule en édition reçoit un éditeur, créé puis jeté.
- **Il n'existe aucun contrat d'éditeur.**
  - `GetEditorValue()` teste cinq contrôles Avalonia.
  - Pour tout autre éditeur, la grille réécrit la valeur d'avant l'édition, et annule donc ce qu'une liaison avait écrit.
- **`EditTemplate`** a pour DataContext la colonne, pas la ligne. Il ne reçoit pas de valeur initiale et ne gère ni Entrée ni Échap.
- **Validation et validation de saisie.**
  - La grille n'a ni `INotifyDataErrorInfo` ni `IDataErrorInfo`.
  - La perte de focus ne valide pas la cellule, et Tab non plus.
  - Une propriété `DateTimeOffset` n'est jamais mise à jour.
- **Risque relevé à la lecture, non testé** : après recyclage d'une cellule, une édition restée ouverte pourrait écrire dans une autre ligne.
- **Conséquence pour E11.** Le lot commence par le contrat `IGridCellEditor` dans HDev.UI.DataGrid, avec ses tests. La forme B s'y branche sans classe jumelle.

## 9. Ce qui reste incertain

- La forme A n'a pas été prototypée. La comparaison repose sur le modèle connu de DevExpress et sur des esquisses d'usage.
- Le clavier et le focus des pièces de B, dans un formulaire comme dans une grille, sont à concevoir en E2.
- Le maintien d'une liaison bidirectionnelle sous `SetValue` est mesuré sur Avalonia 12.1.3. Le test « Contrat des éditeurs (E1) » le surveille.
- Le risque de recyclage de la grille vient de la lecture du code, sans test.

## 10. Décisions (validées le 05/10/2026)

1. **La forme B** pour le socle (§ 5) : validée.
2. **Un éditeur par type** pour D4 (§ 7) : validé.
3. **Le nom de la valeur** : celui d'Avalonia quand il existe (`Text`, `IsChecked`, `SelectedDate`), `Value` sinon (§ 6). Validé.
4. **Les retraits de fonctions : seulement quand elles ont un équivalent.**
   - **Retirées**, avec leur équivalent :
     - `OldEditValue` → `OldValue` dans les arguments de `ValueChanged` ;
     - `RefreshEditValue()` → les liaisons s'en chargent ;
     - `ShowPopup()`, `ClosePopup()` et `HidePopup()` (ComboBox et LookUpEdit) → la propriété `IsDropDownOpen`.
   - **Conservées**, en propriétés directes du contrôle, puisque l'objet `Properties` disparaît (D3) :
     - `ShowNullValuePrompt`, renommée `ShowPlaceholder` comme l'invite ;
     - `TextEditStyle`, avec ses trois modes, dont `HideTextEditor` ;
     - `Sorted`.
