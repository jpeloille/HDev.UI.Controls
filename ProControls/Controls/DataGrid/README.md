# ProDataGrid

Grille de données professionnelle pour Avalonia UI avec style Visual Studio 2022.

## Caractéristiques

- ✅ **Tri** : Tri simple et multi-colonnes
- ✅ **Filtrage** : Ligne de filtrage intégrée
- ✅ **Groupage** : Panneau de groupage avec glisser-déposer
- ✅ **Colonnes gelées** : Support des colonnes gelées à gauche
- ✅ **Virtualisation** : Performance optimisée pour grandes listes
- ✅ **Sélection** : Simple, multiple et par cellule
- ✅ **Redimensionnement** : Redimensionnement des colonnes
- ✅ **Réorganisation** : Réorganisation des colonnes par glisser-déposer
- ✅ **Édition** : Édition de cellules (en cours d'implémentation)

## Utilisation de base

### XAML

```xml
<Window xmlns:datagrid="clr-namespace:ProControls.Controls.DataGrid;assembly=ProControls"
        xmlns:models="clr-namespace:ProControls.Models.DataGrid;assembly=ProControls">

    <datagrid:ProDataGrid x:Name="MyDataGrid"
                         ItemsSource="{Binding Employees}"
                         ShowFilterRow="True"
                         ShowGroupPanel="True"
                         AllowSorting="True"
                         Height="400">
        <datagrid:ProDataGrid.Columns>
            <models:GridColumn FieldName="Id" Header="ID" Width="80" />
            <models:GridColumn FieldName="FirstName" Header="Prénom" Width="150" />
            <models:GridColumn FieldName="LastName" Header="Nom" Width="150" />
            <models:GridColumn FieldName="Email" Header="Email" Width="250" />
            <models:GridColumn FieldName="Salary" Header="Salaire" Width="100" FormatString="C0" />
            <models:GridColumn FieldName="HireDate" Header="Date d'embauche" Width="120" ColumnType="DateTime" />
            <models:GridColumn FieldName="IsActive" Header="Actif" Width="80" ColumnType="Boolean" />
        </datagrid:ProDataGrid.Columns>
    </datagrid:ProDataGrid>

</Window>
```

### Code-behind

```csharp
using ProControls.Controls.DataGrid;
using System.Collections.ObjectModel;

public class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; }
    public bool IsActive { get; set; }
}

// Dans votre fenêtre
public ObservableCollection<Employee> Employees { get; set; }

public MainWindow()
{
    Employees = new ObservableCollection<Employee>
    {
        new Employee { Id = 1, FirstName = "Jean", LastName = "Martin", Email = "jean.martin@example.com", Salary = 45000, HireDate = DateTime.Now.AddYears(-2), IsActive = true },
        // ...
    };

    InitializeComponent();

    var dataGrid = this.FindControl<ProDataGrid>("MyDataGrid");
    dataGrid.ItemsSource = Employees;
}
```

## Propriétés principales

| Propriété | Type | Description |
|-----------|------|-------------|
| `ItemsSource` | IEnumerable | Source de données |
| `Columns` | GridColumnCollection | Collection de colonnes |
| `ShowFilterRow` | bool | Afficher la ligne de filtrage |
| `ShowGroupPanel` | bool | Afficher le panneau de groupage |
| `AllowSorting` | bool | Autoriser le tri |
| `AllowEditing` | bool | Autoriser l'édition |
| `SelectionMode` | SelectionMode | Mode de sélection (Single, Multiple, Cell) |
| `SelectedItem` | object | Élément sélectionné |
| `SelectedItems` | IList | Éléments sélectionnés |
| `AlternateRowBackground` | IBrush | Couleur de fond des lignes alternées |
| `SelectedRowBackground` | IBrush | Couleur de fond de la ligne sélectionnée |
| `HoverRowBackground` | IBrush | Couleur de fond au survol |

## Types de colonnes

### Texte (par défaut)

```xml
<models:GridColumn FieldName="Name" Header="Nom" Width="150" />
```

### Numérique avec format

```xml
<models:GridColumn FieldName="Salary" Header="Salaire" Width="100" FormatString="C0" />
<models:GridColumn FieldName="Quantity" Header="Quantité" Width="80" FormatString="N0" />
```

### Date/Heure

```xml
<models:GridColumn FieldName="HireDate" Header="Date d'embauche" Width="120"
                  ColumnType="DateTime" FormatString="dd/MM/yyyy" />
```

### Booléen

```xml
<models:GridColumn FieldName="IsActive" Header="Actif" Width="80"
                  ColumnType="Boolean" />
```

## Événements

| Événement | Description |
|-----------|-------------|
| `SelectionChanged` | Déclenché quand la sélection change |
| `SelectionChangedCommand` | Commande ICommand pour la sélection |

## Styles et personnalisation

Les couleurs suivent le thème VS2022 par défaut :

- **Header background** : `#EEEEF2`
- **Header border** : `#CCCEDB`
- **Row alternate** : `#FAFAFA`
- **Row selected** : `#C9DEF5`
- **Row hover** : `#E5F1FB`
- **Grid lines** : `#E5E5E5`
- **Cell selected** : `#A6C8E6`
- **Focus border** : `#0078D4`

Pour personnaliser, modifiez les ressources dans `ProControls/Theme/DataGrid/ProDataGrid.axaml`.

## Exemple complet

Voir `ProControls.Demo/MainWindow.axaml` pour un exemple complet avec :
- 100 employés générés
- Tri multi-colonnes
- Filtrage par texte
- Groupage par département
- Colonnes de différents types

## Limitations actuelles

- L'édition inline est partiellement implémentée
- Pas encore de support pour les templates de cellules personnalisés
- Export Excel/CSV/PDF à venir

## Dépendances

- Avalonia 11.2.1
- CommunityToolkit.Mvvm 8.3.2
