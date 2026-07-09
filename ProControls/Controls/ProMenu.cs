using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.VisualTree;
using ProControls.Theme;
using System.Collections.ObjectModel;

namespace ProControls.Controls;

/// <summary>
/// Popup contenant une liste de ProMenuItem
/// Utilisé par ProMenu et ProContextMenu
/// </summary>
public class ProMenuPopup : Popup
{
    private readonly Border _container;
    private readonly StackPanel _itemsPanel;

    /// <summary>
    /// Popup parent dans une chaîne de sous-menus (null pour un menu de premier niveau)
    /// </summary>
    internal ProMenuPopup? ParentPopup { get; set; }

    private TopLevel? _guardRoot;

    /// <summary>true si la dernière fermeture vient d'un clic extérieur (garde)</summary>
    internal bool WasClosedByGuard { get; private set; }

    public ProMenuPopup()
    {
        // Pas de light dismiss Avalonia : il ferme aussi le popup à la moindre
        // désactivation de la fenêtre (turbulences de focus fréquentes sous
        // GNOME/XWayland -> les menus se refermaient instantanément).
        // Remplacé par un garde : clic extérieur + désactivation avec délai de grâce.
        IsLightDismissEnabled = false;
        Placement = PlacementMode.BottomEdgeAlignedLeft;

        Opened += (s, e) => { WasClosedByGuard = false; InstallDismissGuard(); };

        // À la fermeture : refermer les sous-menus encore ouverts de nos items,
        // et se détacher de l'arbre logique (le popup est recréé à chaque ouverture)
        Closed += (s, e) =>
        {
            foreach (var child in _itemsPanel!.Children)
            {
                if (child is ProMenuItem item)
                    item.CloseSubmenu();
            }

            ((ISetLogicalParent)this).SetParent(null);
            RemoveDismissGuard();
        };

        _itemsPanel = new StackPanel
        {
            Orientation = Orientation.Vertical
        };
        
        _container = new Border
        {
            Background = new SolidColorBrush(ProTheme.Background.Panel),
            BorderBrush = new SolidColorBrush(ProTheme.Border.Subtle),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(ProTheme.Size.CornerRadiusLarge),
            Padding = new Thickness(6),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0,
                OffsetY = 4,
                Blur = 16,
                Color = Color.FromArgb(40, 0, 0, 0)
            }),
            Child = _itemsPanel
        };
        
        // Les popups vivent dans un arbre visuel séparé : l'option de rendu
        // subpixel posée sur la fenêtre ne s'y propage pas
        RenderOptions.SetTextRenderingMode(_container, TextRenderingMode.SubpixelAntialias);

        Child = _container;
    }
    
    public void SetItems(IEnumerable<ProMenuItem> sourceItems)
    {
        _itemsPanel.Children.Clear();
        foreach (var item in sourceItems.ToList())
        {
            // Les items originaux sont affichés directement (pas de copie), pour que
            // l'état (IsChecked...) et les handlers restent sur la même instance.
            // Un item ne peut avoir qu'un parent visuel : on le retire de l'ancien popup.
            if (item.Parent is Panel oldPanel)
                oldPanel.Children.Remove(item);

            item.OwnerPopup = this;
            _itemsPanel.Children.Add(item);
        }
    }

    private void InstallDismissGuard()
    {
        _guardRoot = PlacementTarget != null ? TopLevel.GetTopLevel(PlacementTarget) : null;
        if (_guardRoot == null) return;

        _guardRoot.AddHandler(PointerPressedEvent, OnRootPointerPressed,
            Avalonia.Interactivity.RoutingStrategies.Tunnel, handledEventsToo: true);
        if (_guardRoot is Window w)
            w.Deactivated += OnRootDeactivated;
    }

    private void RemoveDismissGuard()
    {
        if (_guardRoot == null) return;
        _guardRoot.RemoveHandler(PointerPressedEvent, OnRootPointerPressed);
        if (_guardRoot is Window w)
            w.Deactivated -= OnRootDeactivated;
        _guardRoot = null;
    }

    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // En mode overlay (headless), le contenu des popups vit dans l'arbre de
        // la fenêtre : ignorer les presses venant de l'intérieur d'un popup
        if (e.Source is Visual v && IsInsidePopupHost(v)) return;

        WasClosedByGuard = true;
        Close();
    }

    internal static bool IsInsidePopupHost(Visual v)
    {
        foreach (var a in v.GetVisualAncestors())
        {
            if (a is Avalonia.Controls.Primitives.OverlayPopupHost) return true;
        }
        return false;
    }

    private void OnRootDeactivated(object? sender, EventArgs e)
    {
        // Certains bureaux (GNOME/XWayland) font rebondir l'activation de la
        // fenêtre en permanence : ne fermer que si elle reste inactive 250 ms
        Avalonia.Threading.DispatcherTimer.RunOnce(() =>
        {
            if (IsOpen && _guardRoot is Window { IsActive: false })
                Close();
        }, TimeSpan.FromMilliseconds(250));
    }

    /// <summary>
    /// Ferme ce popup et tous ses parents (clic sur un item de sous-menu)
    /// </summary>
    internal void CloseChain()
    {
        Close();
        ParentPopup?.CloseChain();
    }
    
    public void ShowAt(Control target, Point offset = default)
    {
        EnsureLogicalParent(target);
        PlacementTarget = target;
        HorizontalOffset = offset.X;
        VerticalOffset = offset.Y;
        Open();
    }

    public void ShowAtPosition(Control anchor, double x, double y)
    {
        EnsureLogicalParent(anchor);
        PlacementTarget = anchor;
        Placement = PlacementMode.Pointer;
        HorizontalOffset = x;
        VerticalOffset = y;
        Open();
    }

    /// <summary>
    /// Un Popup détaché de l'arbre logique n'applique jamais son contenu :
    /// comme FlyoutBase, on se parente à la cible le temps de l'affichage.
    /// </summary>
    private void EnsureLogicalParent(Control target)
    {
        if (Parent == null)
            ((ISetLogicalParent)this).SetParent(target);
    }
}

/// <summary>
/// Barre de menu principale (File, Edit, View, etc.)
/// </summary>
public class ProMenuBar : Panel
{
    private ProMenuBarItem? _openItem;
    private bool _isMenuMode; // Mode menu actif (après premier clic)
    
    public ObservableCollection<ProMenuBarItem> Items { get; } = new();
    
    public ProMenuBar()
    {
        Height = 34;
        ClipToBounds = false;
        Background = new SolidColorBrush(ProTheme.Background.Toolbar);
        
        Items.CollectionChanged += (s, e) =>
        {
            Children.Clear();
            foreach (var item in Items)
            {
                Children.Add(item);
            }
            InvalidateMeasure();
        };
    }
    
    protected override Size MeasureOverride(Size availableSize)
    {
        double totalWidth = 0;
        foreach (var child in Children)
        {
            child.Measure(availableSize);
            totalWidth += child.DesiredSize.Width;
        }
        return new Size(Math.Max(totalWidth, availableSize.Width), 34);
    }
    
    protected override Size ArrangeOverride(Size finalSize)
    {
        double x = 0;
        foreach (var child in Children)
        {
            child.Arrange(new Rect(x, 0, child.DesiredSize.Width, finalSize.Height));
            x += child.DesiredSize.Width;
        }
        return finalSize;
    }
    
    internal void OpenMenu(ProMenuBarItem item)
    {
        if (_openItem == item) return;
        
        _openItem?.CloseMenu();
        _openItem = item;
        _isMenuMode = true;
    }
    
    internal void CloseAllMenus()
    {
        _openItem?.CloseMenu();
        _openItem = null;
        _isMenuMode = false;
    }
    
    internal bool IsMenuMode => _isMenuMode;
    
    internal void OnItemHovered(ProMenuBarItem item)
    {
        if (_isMenuMode && _openItem != item)
        {
            OpenMenu(item);
            item.OpenMenu();
        }
    }
}

/// <summary>
/// Item de la barre de menu (ex: "File", "Edit")
/// </summary>
public class ProMenuBarItem : Control
{
    private bool _isHovered;
    private bool _isOpen;
    private ProMenuPopup? _popup;
    private DateTime _lastCloseTime;

    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<ProMenuBarItem, string>(nameof(Header), "");
    
    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }
    
    public ObservableCollection<ProMenuItem> Items { get; } = new();
    
    static ProMenuBarItem()
    {
        AffectsRender<ProMenuBarItem>(HeaderProperty);
        AffectsMeasure<ProMenuBarItem>(HeaderProperty);
    }
    
    public ProMenuBarItem()
    {
        ClipToBounds = false;
    }
    
    public ProMenuBarItem(string header) : this()
    {
        Header = header;
    }
    
    protected override Size MeasureOverride(Size availableSize)
    {
        var text = new FormattedText(
            Header,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(ProTheme.Typography.FontFamily),
            ProTheme.Typography.FontSizeBody,
            Brushes.Black);
        
        return new Size(text.Width + 24, 34);
    }
    
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        _isHovered = true;
        InvalidateVisual();
        
        if (Parent is ProMenuBar menuBar)
            menuBar.OnItemHovered(this);
        
        base.OnPointerEntered(e);
    }
    
    protected override void OnPointerExited(PointerEventArgs e)
    {
        _isHovered = false;
        InvalidateVisual();
        base.OnPointerExited(e);
    }
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (_isOpen)
        {
            CloseMenu();
        }
        else if ((DateTime.UtcNow - _lastCloseTime).TotalMilliseconds > 250)
        {
            // Le garde du popup vient peut-être de le fermer sur CE même press
            // (tunnel) : ne pas rouvrir aussitôt, sinon le toggle est impossible
            OpenMenu();
        }

        base.OnPointerPressed(e);
    }
    
    internal void OpenMenu()
    {
        if (Items.Count == 0) return;
        
        _popup = new ProMenuPopup();
        _popup.SetItems(Items);
        
        _popup.Closed += (s, e) =>
        {
            _isOpen = false;
            // N'armer l'anti-réouverture que si le garde a fermé (clic extérieur,
            // typiquement sur CE bar-item) — pas pour un clic d'item de menu
            if (s is ProMenuPopup p && p.WasClosedByGuard)
                _lastCloseTime = DateTime.UtcNow;
            InvalidateVisual();
            if (Parent is ProMenuBar menuBar)
                menuBar.CloseAllMenus();
        };
        
        _popup.ShowAt(this);
        _isOpen = true;
        InvalidateVisual();
        
        if (Parent is ProMenuBar menuBar)
            menuBar.OpenMenu(this);
    }
    
    internal void CloseMenu()
    {
        _popup?.Close();
        _popup = null;
        _isOpen = false;
        InvalidateVisual();
    }
    
    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);

        // Fond transparent : sans primitive dessinée couvrant les bounds,
        // le contrôle serait invisible au hit-test du pointeur
        context.FillRectangle(Brushes.Transparent, bounds);

        // Fond au survol ou si menu ouvert : pilule arrondie GNOME
        if (_isHovered || _isOpen)
        {
            var bgColor = _isOpen
                ? ProTheme.Background.ControlPressed
                : ProTheme.Background.ControlHover;
            var pill = bounds.Deflate(new Thickness(2, 4));
            context.DrawRectangle(new SolidColorBrush(bgColor), null, pill, 6, 6);
        }
        
        // Texte
        var text = new FormattedText(
            Header,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(ProTheme.Typography.FontFamily),
            ProTheme.Typography.FontSizeBody,
            new SolidColorBrush(ProTheme.Text.Primary));
        
        var textX = (bounds.Width - text.Width) / 2;
        var textY = (bounds.Height - text.Height) / 2;
        context.DrawText(text, Crisp.Snap(new Point(textX, textY)));
    }
}
