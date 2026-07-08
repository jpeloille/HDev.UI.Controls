using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using ProControls.Theme;
using System.Collections.ObjectModel;

namespace ProControls.Controls;

/// <summary>
/// Menu contextuel professionnel style VS2022
/// S'attache à un contrôle et s'ouvre au clic droit
/// </summary>
public class ProContextMenu
{
    private ProMenuPopup? _popup;
    private Control? _target;
    
    public ObservableCollection<ProMenuItem> Items { get; } = new();
    
    /// <summary>
    /// Événement déclenché avant l'ouverture (pour construire le menu dynamiquement)
    /// </summary>
    public event EventHandler? Opening;
    
    /// <summary>
    /// Événement déclenché après fermeture
    /// </summary>
    public event EventHandler? Closed;
    
    public ProContextMenu()
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
        
        _popup = new ProMenuPopup
        {
            Placement = PlacementMode.Pointer
        };
        
        _popup.SetItems(Items);
        
        _popup.Closed += (s, e) =>
        {
            Closed?.Invoke(this, EventArgs.Empty);
            _popup = null;
        };
        
        _popup.PlacementTarget = anchor;
        _popup.Open();
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
    
    public ProContextMenu Add(string header, string? icon = null, string? shortcut = null, Action? onClick = null)
    {
        var item = new ProMenuItem(header, icon, shortcut, onClick);
        Items.Add(item);
        return this;
    }
    
    public ProContextMenu AddSeparator()
    {
        Items.Add(ProMenuItem.Separator());
        return this;
    }
    
    public ProContextMenu AddCheckable(string header, bool isChecked, Action<bool>? onToggle = null)
    {
        var item = new ProMenuItem
        {
            Header = header,
            IsCheckable = true,
            IsChecked = isChecked
        };
        item.Click += (s, e) => onToggle?.Invoke(item.IsChecked);
        Items.Add(item);
        return this;
    }
    
    public ProContextMenu AddSubmenu(string header, string? icon, Action<ProContextMenu> buildSubmenu)
    {
        var submenuItems = new ObservableCollection<ProMenuItem>();
        var tempMenu = new ProContextMenu();
        buildSubmenu(tempMenu);
        
        var item = new ProMenuItem
        {
            Header = header,
            Icon = icon,
            Items = new ObservableCollection<ProMenuItem>(tempMenu.Items)
        };
        Items.Add(item);
        return this;
    }
}

/// <summary>
/// Extension pour attacher facilement un ProContextMenu à un Control
/// </summary>
public static class ProContextMenuExtensions
{
    private static readonly AttachedProperty<ProContextMenu?> ContextMenuProperty =
        AvaloniaProperty.RegisterAttached<Control, ProContextMenu?>("ProContextMenu", typeof(ProContextMenuExtensions));
    
    public static void SetProContextMenu(Control control, ProContextMenu? menu)
    {
        var oldMenu = control.GetValue(ContextMenuProperty);
        oldMenu?.Detach();
        
        control.SetValue(ContextMenuProperty, menu);
        menu?.Attach(control);
    }
    
    public static ProContextMenu? GetProContextMenu(Control control)
    {
        return control.GetValue(ContextMenuProperty);
    }
}
