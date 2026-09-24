using Avalonia;
using Avalonia.Media;
using HDev.UI.Controls.Theme;
using System.Collections.ObjectModel;

namespace HDev.UI.Controls;

/// <summary>
/// Onglet du ruban contenant des groupes
/// </summary>
public class HDevRibbonTab
{
    public string Header { get; set; } = "";
    public ObservableCollection<HDevRibbonGroup> Groups { get; } = new();
    
    /// <summary>
    /// Couleur d'accent optionnelle pour cet onglet (pour onglets contextuels)
    /// </summary>
    public Color? AccentColor { get; set; }
    
    /// <summary>
    /// Indique si c'est un onglet contextuel (apparaît selon le contexte)
    /// </summary>
    public bool IsContextual { get; set; }
    
    public HDevRibbonTab() { }
    
    public HDevRibbonTab(string header)
    {
        Header = header;
    }
    
    public HDevRibbonTab AddGroup(HDevRibbonGroup group)
    {
        Groups.Add(group);
        return this;
    }
    
    public HDevRibbonTab AddGroup(string header, Action<HDevRibbonGroup> configure)
    {
        var group = new HDevRibbonGroup(header);
        configure(group);
        Groups.Add(group);
        return this;
    }
}

/// <summary>
/// Groupe de contrôles dans un onglet du ruban
/// </summary>
public class HDevRibbonGroup
{
    public string Header { get; set; } = "";
    public ObservableCollection<HDevRibbonItem> Items { get; } = new();
    
    /// <summary>
    /// Bouton de dialogue en bas à droite du groupe (optionnel)
    /// </summary>
    public Action? DialogLauncher { get; set; }
    
    public HDevRibbonGroup() { }
    
    public HDevRibbonGroup(string header)
    {
        Header = header;
    }
    
    public HDevRibbonGroup AddButton(HDevRibbonButton button)
    {
        Items.Add(button);
        return this;
    }
    
    public HDevRibbonGroup AddLargeButton(string label, string icon, Action? onClick = null)
    {
        Items.Add(new HDevRibbonButton
        {
            Label = label,
            Icon = icon,
            IsLarge = true,
            OnClick = onClick
        });
        return this;
    }
    
    public HDevRibbonGroup AddSmallButton(string label, string? icon = null, Action? onClick = null)
    {
        Items.Add(new HDevRibbonButton
        {
            Label = label,
            Icon = icon,
            IsLarge = false,
            OnClick = onClick
        });
        return this;
    }
    
    public HDevRibbonGroup AddDropdownButton(string label, string icon, bool isLarge, Action<HDevContextMenu>? buildMenu = null)
    {
        var btn = new HDevRibbonButton
        {
            Label = label,
            Icon = icon,
            IsLarge = isLarge,
            HasDropdown = true
        };
        
        if (buildMenu != null)
        {
            var menu = new HDevContextMenu();
            buildMenu(menu);
            btn.DropdownMenu = menu;
        }
        
        Items.Add(btn);
        return this;
    }
    
    public HDevRibbonGroup AddSeparator()
    {
        Items.Add(new HDevRibbonSeparator());
        return this;
    }
}

/// <summary>
/// Classe de base pour les éléments du ruban
/// </summary>
public abstract class HDevRibbonItem
{
    public bool IsEnabled { get; set; } = true;
    public bool IsVisible { get; set; } = true;
}

/// <summary>
/// Bouton du ruban (petit ou grand)
/// </summary>
public class HDevRibbonButton : HDevRibbonItem
{
    public string Label { get; set; } = "";
    public string? Icon { get; set; }
    public string? Tooltip { get; set; }

    /// <summary>Accélérateur clavier réel (« Ctrl+P »...), enregistré par HDevRibbon</summary>
    public string? Shortcut { get; set; }

    public bool IsLarge { get; set; } = false;
    public bool HasDropdown { get; set; } = false;
    public bool IsToggle { get; set; } = false;
    public bool IsChecked { get; set; } = false;
    
    public Action? OnClick { get; set; }
    public HDevContextMenu? DropdownMenu { get; set; }
    
    // État visuel (géré par HDevRibbon)
    internal bool IsHovered { get; set; }
    internal bool IsPressed { get; set; }
    
    public HDevRibbonButton() { }
    
    public HDevRibbonButton(string label, string? icon = null, bool isLarge = false)
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
public class HDevRibbonSeparator : HDevRibbonItem
{
}

// NOTE : HDevRibbonComboBox, HDevRibbonGallery et HDevRibbonToggleGroup (API
// DevExpress) ont été retirés : déclarés mais jamais rendus par HDevRibbon,
// ils donnaient une fausse impression de parité. À réintroduire avec leur
// rendu réel (cf. context.md, lot « Ribbon avancé »).

