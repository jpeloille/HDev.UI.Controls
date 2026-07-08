using Avalonia;
using Avalonia.Media;
using ProControls.Theme;
using System.Collections.ObjectModel;

namespace ProControls.Controls;

/// <summary>
/// Onglet du ruban contenant des groupes
/// </summary>
public class ProRibbonTab
{
    public string Header { get; set; } = "";
    public ObservableCollection<ProRibbonGroup> Groups { get; } = new();
    
    /// <summary>
    /// Couleur d'accent optionnelle pour cet onglet (pour onglets contextuels)
    /// </summary>
    public Color? AccentColor { get; set; }
    
    /// <summary>
    /// Indique si c'est un onglet contextuel (apparaît selon le contexte)
    /// </summary>
    public bool IsContextual { get; set; }
    
    public ProRibbonTab() { }
    
    public ProRibbonTab(string header)
    {
        Header = header;
    }
    
    public ProRibbonTab AddGroup(ProRibbonGroup group)
    {
        Groups.Add(group);
        return this;
    }
    
    public ProRibbonTab AddGroup(string header, Action<ProRibbonGroup> configure)
    {
        var group = new ProRibbonGroup(header);
        configure(group);
        Groups.Add(group);
        return this;
    }
}

/// <summary>
/// Groupe de contrôles dans un onglet du ruban
/// </summary>
public class ProRibbonGroup
{
    public string Header { get; set; } = "";
    public ObservableCollection<ProRibbonItem> Items { get; } = new();
    
    /// <summary>
    /// Bouton de dialogue en bas à droite du groupe (optionnel)
    /// </summary>
    public Action? DialogLauncher { get; set; }
    
    public ProRibbonGroup() { }
    
    public ProRibbonGroup(string header)
    {
        Header = header;
    }
    
    public ProRibbonGroup AddButton(ProRibbonButton button)
    {
        Items.Add(button);
        return this;
    }
    
    public ProRibbonGroup AddLargeButton(string label, string icon, Action? onClick = null)
    {
        Items.Add(new ProRibbonButton
        {
            Label = label,
            Icon = icon,
            IsLarge = true,
            OnClick = onClick
        });
        return this;
    }
    
    public ProRibbonGroup AddSmallButton(string label, string? icon = null, Action? onClick = null)
    {
        Items.Add(new ProRibbonButton
        {
            Label = label,
            Icon = icon,
            IsLarge = false,
            OnClick = onClick
        });
        return this;
    }
    
    public ProRibbonGroup AddDropdownButton(string label, string icon, bool isLarge, Action<ProContextMenu>? buildMenu = null)
    {
        var btn = new ProRibbonButton
        {
            Label = label,
            Icon = icon,
            IsLarge = isLarge,
            HasDropdown = true
        };
        
        if (buildMenu != null)
        {
            var menu = new ProContextMenu();
            buildMenu(menu);
            btn.DropdownMenu = menu;
        }
        
        Items.Add(btn);
        return this;
    }
    
    public ProRibbonGroup AddSeparator()
    {
        Items.Add(new ProRibbonSeparator());
        return this;
    }
}

/// <summary>
/// Classe de base pour les éléments du ruban
/// </summary>
public abstract class ProRibbonItem
{
    public bool IsEnabled { get; set; } = true;
    public bool IsVisible { get; set; } = true;
}

/// <summary>
/// Bouton du ruban (petit ou grand)
/// </summary>
public class ProRibbonButton : ProRibbonItem
{
    public string Label { get; set; } = "";
    public string? Icon { get; set; }
    public string? Tooltip { get; set; }
    public bool IsLarge { get; set; } = false;
    public bool HasDropdown { get; set; } = false;
    public bool IsToggle { get; set; } = false;
    public bool IsChecked { get; set; } = false;
    
    public Action? OnClick { get; set; }
    public ProContextMenu? DropdownMenu { get; set; }
    
    // État visuel (géré par ProRibbon)
    internal bool IsHovered { get; set; }
    internal bool IsPressed { get; set; }
    
    public ProRibbonButton() { }
    
    public ProRibbonButton(string label, string? icon = null, bool isLarge = false)
    {
        Label = label;
        Icon = icon;
        IsLarge = isLarge;
    }
    
    internal void RaiseClick()
    {
        if (!IsEnabled) return;
        
        if (IsToggle)
            IsChecked = !IsChecked;
        
        OnClick?.Invoke();
    }
}

/// <summary>
/// Séparateur vertical dans un groupe du ruban
/// </summary>
public class ProRibbonSeparator : ProRibbonItem
{
}

/// <summary>
/// ComboBox dans le ruban
/// </summary>
public class ProRibbonComboBox : ProRibbonItem
{
    public string Label { get; set; } = "";
    public double Width { get; set; } = 120;
    public ObservableCollection<string> Items { get; } = new();
    public int SelectedIndex { get; set; } = -1;
    public string? SelectedItem => SelectedIndex >= 0 && SelectedIndex < Items.Count 
        ? Items[SelectedIndex] : null;
    
    public event EventHandler<int>? SelectionChanged;
    
    internal void RaiseSelectionChanged(int index)
    {
        SelectedIndex = index;
        SelectionChanged?.Invoke(this, index);
    }
}

/// <summary>
/// Galerie dans le ruban (grille de choix visuels)
/// </summary>
public class ProRibbonGallery : ProRibbonItem
{
    public string Label { get; set; } = "";
    public int Columns { get; set; } = 5;
    public double ItemWidth { get; set; } = 40;
    public double ItemHeight { get; set; } = 40;
    public ObservableCollection<ProRibbonGalleryItem> Items { get; } = new();
    public int SelectedIndex { get; set; } = -1;
    
#pragma warning disable CS0067 // Event never used - reserved for future gallery implementation
    public event EventHandler<int>? SelectionChanged;
#pragma warning restore CS0067
}

/// <summary>
/// Item d'une galerie
/// </summary>
public class ProRibbonGalleryItem
{
    public string? Icon { get; set; }
    public string? Label { get; set; }
    public Color? Color { get; set; }
    public object? Tag { get; set; }
}

/// <summary>
/// Groupe de boutons toggle (un seul actif à la fois)
/// </summary>
public class ProRibbonToggleGroup : ProRibbonItem
{
    public ObservableCollection<ProRibbonButton> Buttons { get; } = new();
    public int SelectedIndex { get; set; } = -1;
    
    public event EventHandler<int>? SelectionChanged;
    
    public ProRibbonToggleGroup AddButton(string label, string? icon = null)
    {
        var btn = new ProRibbonButton
        {
            Label = label,
            Icon = icon,
            IsToggle = true
        };
        
        var index = Buttons.Count;
        btn.OnClick = () => SelectButton(index);
        
        Buttons.Add(btn);
        return this;
    }
    
    private void SelectButton(int index)
    {
        for (int i = 0; i < Buttons.Count; i++)
        {
            Buttons[i].IsChecked = (i == index);
        }
        SelectedIndex = index;
        SelectionChanged?.Invoke(this, index);
    }
}
