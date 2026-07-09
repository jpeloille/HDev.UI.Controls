using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Layout;
using ProControls.Theme;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ProControls.Controls;

/// <summary>
/// Élément de menu individuel avec icône, texte, raccourci et sous-menu
/// </summary>
public class ProMenuItem : Control
{
    private bool _isHovered;
    private bool _isPressed;
    private bool _isSubmenuOpen;
    private ProMenuPopup? _submenuPopup;
    
    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<ProMenuItem, string>(nameof(Header), "");
    
    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<ProMenuItem, string?>(nameof(Icon));
    
    public static readonly StyledProperty<string?> ShortcutProperty =
        AvaloniaProperty.Register<ProMenuItem, string?>(nameof(Shortcut));
    
    public static readonly StyledProperty<bool> IsSeparatorProperty =
        AvaloniaProperty.Register<ProMenuItem, bool>(nameof(IsSeparator));
    
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<ProMenuItem, bool>(nameof(IsChecked));
    
    public static readonly StyledProperty<bool> IsCheckableProperty =
        AvaloniaProperty.Register<ProMenuItem, bool>(nameof(IsCheckable));
    
    public static readonly StyledProperty<ObservableCollection<ProMenuItem>?> ItemsProperty =
        AvaloniaProperty.Register<ProMenuItem, ObservableCollection<ProMenuItem>?>(nameof(Items));

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }
    
    /// <summary>
    /// Caractère icône (emoji ou symbole Unicode) ou chemin vers ressource
    /// </summary>
    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
    
    public string? Shortcut
    {
        get => GetValue(ShortcutProperty);
        set => SetValue(ShortcutProperty, value);
    }
    
    public bool IsSeparator
    {
        get => GetValue(IsSeparatorProperty);
        set => SetValue(IsSeparatorProperty, value);
    }
    
    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }
    
    public bool IsCheckable
    {
        get => GetValue(IsCheckableProperty);
        set => SetValue(IsCheckableProperty, value);
    }
    
    public ObservableCollection<ProMenuItem>? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }
    
    public bool HasItems => Items != null && Items.Count > 0;

    /// <summary>
    /// Popup qui affiche actuellement cet item (renseigné par ProMenuPopup.SetItems)
    /// </summary>
    internal ProMenuPopup? OwnerPopup { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // ÉVÉNEMENTS
    // ═══════════════════════════════════════════════════════════════
    
    public event EventHandler? Click;
    
    /// <summary>
    /// Permet de déclencher l'événement Click depuis l'extérieur (pour les copies)
    /// </summary>
    public void RaiseClick()
    {
        Click?.Invoke(this, EventArgs.Empty);
    }
    
    // ═══════════════════════════════════════════════════════════════
    // CONSTRUCTEUR
    // ═══════════════════════════════════════════════════════════════
    
    static ProMenuItem()
    {
        AffectsRender<ProMenuItem>(HeaderProperty, IconProperty, ShortcutProperty, 
            IsSeparatorProperty, IsCheckedProperty, IsCheckableProperty);
        AffectsMeasure<ProMenuItem>(HeaderProperty, IconProperty, ShortcutProperty, IsSeparatorProperty);
    }
    
    public ProMenuItem()
    {
        ClipToBounds = false;
    }
    
    public ProMenuItem(string header, string? icon = null, string? shortcut = null, Action? onClick = null)
        : this()
    {
        Header = header;
        Icon = icon;
        Shortcut = shortcut;
        if (onClick != null)
            Click += (s, e) => onClick();
    }
    
    public static ProMenuItem Separator() => new() { IsSeparator = true };
    
    // ═══════════════════════════════════════════════════════════════
    // MESURE
    // ═══════════════════════════════════════════════════════════════
    
    protected override Size MeasureOverride(Size availableSize)
    {
        if (IsSeparator)
            return new Size(100, 9);
        
        var headerText = CreateFormattedText(Header, ProTheme.Text.Primary);
        var width = 24 + headerText.Width + 50; // icon + header + shortcut space
        
        if (!string.IsNullOrEmpty(Shortcut))
        {
            var shortcutText = CreateFormattedText(Shortcut, ProTheme.Text.Secondary);
            width += shortcutText.Width;
        }
        
        if (HasItems)
            width += 20; // Arrow space
        
        return new Size(Math.Max(150, width), 28);
    }
    
    // ═══════════════════════════════════════════════════════════════
    // INTERACTIONS
    // ═══════════════════════════════════════════════════════════════
    
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        _isHovered = true;
        InvalidateVisual();

        // Fermer les sous-menus ouverts des items voisins du même popup
        if (Parent is Panel panel)
        {
            foreach (var sibling in panel.Children)
            {
                if (sibling is ProMenuItem other && other != this)
                    other.CloseSubmenu();
            }
        }

        if (HasItems)
            OpenSubmenu();

        base.OnPointerEntered(e);
    }
    
    protected override void OnPointerExited(PointerEventArgs e)
    {
        _isHovered = false;
        _isPressed = false;
        InvalidateVisual();
        base.OnPointerExited(e);
    }
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (IsSeparator) return;
        _isPressed = true;
        InvalidateVisual();
        base.OnPointerPressed(e);
    }
    
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (IsSeparator) return;
        
        if (_isPressed && _isHovered && !HasItems)
        {
            if (IsCheckable)
                IsChecked = !IsChecked;
            
            Click?.Invoke(this, EventArgs.Empty);
            
            // Fermer le menu parent
            CloseAllMenus();
        }
        
        _isPressed = false;
        InvalidateVisual();
        base.OnPointerReleased(e);
    }
    
    internal void OpenSubmenu()
    {
        if (_isSubmenuOpen || Items == null || Items.Count == 0) return;

        // Positionné à droite de l'item ; la fermeture est pilotée par la chaîne
        // de popups (pas de light dismiss propre), sinon un clic dans le sous-menu
        // pourrait fermer le parent avant d'être traité
        _submenuPopup = new ProMenuPopup
        {
            Placement = PlacementMode.RightEdgeAlignedTop,
            IsLightDismissEnabled = false,
            ParentPopup = OwnerPopup,
        };
        _submenuPopup.SetItems(Items);
        _submenuPopup.Closed += (s, e) =>
        {
            _isSubmenuOpen = false;
            InvalidateVisual();
        };
        _submenuPopup.ShowAt(this, new Point(-4, -6));

        _isSubmenuOpen = true;
        InvalidateVisual();
    }

    internal void CloseSubmenu()
    {
        _submenuPopup?.Close();
        _submenuPopup = null;
        _isSubmenuOpen = false;
    }

    internal void CloseAllMenus()
    {
        CloseSubmenu();

        // Fermer le popup qui nous affiche et toute la chaîne de parents
        OwnerPopup?.CloseChain();
    }
    
    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════
    
    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);

        // Fond transparent : sans primitive dessinée couvrant les bounds,
        // le contrôle serait invisible au hit-test du pointeur
        context.FillRectangle(Brushes.Transparent, bounds);

        if (IsSeparator)
        {
            // Ligne séparatrice
            var y = bounds.Height / 2;
            var pen = new Pen(new SolidColorBrush(ProTheme.Border.Subtle), 1);
            context.DrawLine(pen, new Point(8, y), new Point(bounds.Width - 8, y));
            return;
        }
        
        // Fond au survol
        if (_isHovered || _isSubmenuOpen)
        {
            var bgColor = _isPressed 
                ? ProTheme.Background.ControlPressed 
                : ProTheme.Background.ControlHover;
            var bgRect = bounds.Deflate(new Thickness(4, 2));
            context.DrawRectangle(
                new SolidColorBrush(bgColor), 
                null,
                bgRect,
                3, 3);
        }
        
        var x = 8.0;
        var centerY = bounds.Height / 2;
        
        // Checkmark ou icône
        if (IsCheckable)
        {
            if (IsChecked)
            {
                var checkPen = new Pen(new SolidColorBrush(ProTheme.Accent.Primary), 1.5);
                context.DrawLine(checkPen, 
                    new Point(x + 2, centerY), 
                    new Point(x + 6, centerY + 4));
                context.DrawLine(checkPen, 
                    new Point(x + 6, centerY + 4), 
                    new Point(x + 12, centerY - 4));
            }
            x += 20;
        }
        else if (!string.IsNullOrEmpty(Icon))
        {
            var iconText = CreateFormattedText(Icon, ProTheme.Text.Primary, 14);
            context.DrawText(iconText, Crisp.Snap(new Point(x, centerY - iconText.Height / 2)));
            x += 24;
        }
        else
        {
            x += 24;
        }
        
        // Header
        var textColor = IsEnabled ? ProTheme.Text.Primary : ProTheme.Text.Disabled;
        var headerText = CreateFormattedText(Header, textColor);
        context.DrawText(headerText, Crisp.Snap(new Point(x, centerY - headerText.Height / 2)));
        
        // Shortcut (aligné à droite)
        if (!string.IsNullOrEmpty(Shortcut))
        {
            var shortcutText = CreateFormattedText(Shortcut, ProTheme.Text.Tertiary, 12);
            var shortcutX = bounds.Width - shortcutText.Width - (HasItems ? 28 : 12);
            context.DrawText(shortcutText, Crisp.Snap(new Point(shortcutX, centerY - shortcutText.Height / 2)));
        }
        
        // Flèche pour sous-menu
        if (HasItems)
        {
            var arrowX = bounds.Width - 16;
            var arrowPen = new Pen(new SolidColorBrush(ProTheme.Text.Secondary), 1.2);
            context.DrawLine(arrowPen, 
                new Point(arrowX, centerY - 4), 
                new Point(arrowX + 5, centerY));
            context.DrawLine(arrowPen, 
                new Point(arrowX + 5, centerY), 
                new Point(arrowX, centerY + 4));
        }
    }
    
    private FormattedText CreateFormattedText(string text, Color color, double fontSize = ProTheme.Typography.FontSizeBody)
    {
        return new FormattedText(
            text ?? "",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(ProTheme.Typography.FontFamily),
            fontSize,
            new SolidColorBrush(color));
    }
}
