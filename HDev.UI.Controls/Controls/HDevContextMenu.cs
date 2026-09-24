using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using HDev.UI.Controls.Theme;
using System.Collections.ObjectModel;

namespace HDev.UI.Controls;

/// <summary>
/// Menu contextuel professionnel style VS2022
/// S'attache à un contrôle et s'ouvre au clic droit
/// </summary>
public class HDevContextMenu
{
    private HDevMenuPopup? _popup;
    private Control? _target;
    
    public ObservableCollection<HDevMenuItem> Items { get; } = new();
    
    /// <summary>
    /// Événement déclenché avant l'ouverture (pour construire le menu dynamiquement)
    /// </summary>
    public event EventHandler? Opening;
    
    /// <summary>
    /// Événement déclenché après fermeture
    /// </summary>
    public event EventHandler? Closed;
    
    public HDevContextMenu()
    {
    }
    
    /// <summary>
    /// Attache ce menu contextuel à un contrôle
    /// </summary>
    public void Attach(Control target)
    {
        _target = target;
        target.PointerPressed += OnTargetPointerPressed;
    }
    
    /// <summary>
    /// Détache le menu du contrôle
    /// </summary>
    public void Detach()
    {
        if (_target != null)
        {
            _target.PointerPressed -= OnTargetPointerPressed;
            _target = null;
        }
    }
    
    private void OnTargetPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(_target).Properties.IsRightButtonPressed)
        {
            e.Handled = true;
            var position = e.GetPosition(_target);
            Show(_target!, position);
        }
    }
    
    /// <summary>
    /// Ouvre le menu à une position spécifique
    /// </summary>
    public void Show(Control anchor, Point position)
    {
        Opening?.Invoke(this, EventArgs.Empty);
        
        if (Items.Count == 0) return;
        
        _popup = new HDevMenuPopup
        {
            Placement = PlacementMode.Pointer
        };
        
        _popup.SetItems(Items);
        
        _popup.Closed += (s, e) =>
        {
            Closed?.Invoke(this, EventArgs.Empty);
            _popup = null;
        };

        // ShowAt assure le parentage logique du popup (sinon contenu jamais rendu)
        _popup.ShowAt(anchor);
    }
    
    /// <summary>
    /// Ferme le menu s'il est ouvert
    /// </summary>
    public void Close()
    {
        _popup?.Close();
        _popup = null;
    }
    
    // ═══════════════════════════════════════════════════════════════
    // MÉTHODES UTILITAIRES POUR CONSTRUCTION RAPIDE
    // ═══════════════════════════════════════════════════════════════
    
    public HDevContextMenu Add(string header, string? icon = null, string? shortcut = null, Action? onClick = null)
    {
        var item = new HDevMenuItem(header, icon, shortcut, onClick);
        Items.Add(item);
        return this;
    }
    
    public HDevContextMenu AddSeparator()
    {
        Items.Add(HDevMenuItem.Separator());
        return this;
    }
    
    public HDevContextMenu AddCheckable(string header, bool isChecked, Action<bool>? onToggle = null)
    {
        var item = new HDevMenuItem
        {
            Header = header,
            IsCheckable = true,
            IsChecked = isChecked
        };
        item.Click += (s, e) => onToggle?.Invoke(item.IsChecked);
        Items.Add(item);
        return this;
    }
    
    public HDevContextMenu AddSubmenu(string header, string? icon, Action<HDevContextMenu> buildSubmenu)
    {
        var submenuItems = new ObservableCollection<HDevMenuItem>();
        var tempMenu = new HDevContextMenu();
        buildSubmenu(tempMenu);
        
        var item = new HDevMenuItem
        {
            Header = header,
            Icon = icon,
            Items = new ObservableCollection<HDevMenuItem>(tempMenu.Items)
        };
        Items.Add(item);
        return this;
    }
}

/// <summary>
/// Extension pour attacher facilement un HDevContextMenu à un Control
/// </summary>
public static class HDevContextMenuExtensions
{
    private static readonly AttachedProperty<HDevContextMenu?> ContextMenuProperty =
        AvaloniaProperty.RegisterAttached<Control, HDevContextMenu?>("HDevContextMenu", typeof(HDevContextMenuExtensions));
    
    public static void SetHDevContextMenu(Control control, HDevContextMenu? menu)
    {
        var oldMenu = control.GetValue(ContextMenuProperty);
        oldMenu?.Detach();
        
        control.SetValue(ContextMenuProperty, menu);
        menu?.Attach(control);
    }
    
    public static HDevContextMenu? GetHDevContextMenu(Control control)
    {
        return control.GetValue(ContextMenuProperty);
    }
}
