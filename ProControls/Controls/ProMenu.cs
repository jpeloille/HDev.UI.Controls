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
        
        // ATTENTION : ne PAS forcer SubpixelAntialias ici — la surface des
        // popups est transparente (ARGB, ombre portée) et le rendu LCD subpixel
        // n'y peint AUCUN glyphe (liste/menu visuellement vides sur X11).
        // L'anti-aliasing par défaut (niveaux de gris) est correct sur popup.
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

    // ═══════════════════════════════════════════════════════════════
    // NAVIGATION CLAVIER (pilotée par ProMenuBar)
    // ═══════════════════════════════════════════════════════════════

    private int _highlightIndex = -1;

    internal IReadOnlyList<ProMenuItem> MenuItems
        => _itemsPanel.Children.OfType<ProMenuItem>().ToList();

    internal ProMenuItem? HighlightedItem
    {
        get
        {
            var items = MenuItems;
            return _highlightIndex >= 0 && _highlightIndex < items.Count
                ? items[_highlightIndex] : null;
        }
    }

    /// <summary>Déplace la surbrillance (saute séparateurs et items désactivés)</summary>
    internal void MoveHighlight(int delta)
    {
        var items = MenuItems;
        if (items.Count == 0) return;

        var index = _highlightIndex;
        for (int attempts = 0; attempts < items.Count; attempts++)
        {
            index = ((index + delta) % items.Count + items.Count) % items.Count;
            if (!items[index].IsSeparator && items[index].IsEnabled)
                break;
        }

        SetHighlight(index);
    }

    private void SetHighlight(int index)
    {
        var items = MenuItems;
        if (_highlightIndex >= 0 && _highlightIndex < items.Count)
            items[_highlightIndex].SetKeyboardHighlight(false);

        _highlightIndex = index;

        if (_highlightIndex >= 0 && _highlightIndex < items.Count)
            items[_highlightIndex].SetKeyboardHighlight(true);
    }

    /// <summary>Enter sur l'item surligné : activation ou ouverture du sous-menu</summary>
    internal ProMenuPopup? ActivateHighlighted()
    {
        var item = HighlightedItem;
        if (item == null) return null;

        if (item.HasItems)
        {
            item.OpenSubmenu();
            item.SubmenuPopup?.MoveHighlight(1); // surligner le premier item
            return item.SubmenuPopup;
        }

        item.Activate();
        return null;
    }

    /// <summary>→ sur un item à sous-menu : l'ouvre et rend son popup</summary>
    internal ProMenuPopup? OpenHighlightedSubmenu()
    {
        var item = HighlightedItem;
        if (item is not { HasItems: true }) return null;
        item.OpenSubmenu();
        item.SubmenuPopup?.MoveHighlight(1);
        return item.SubmenuPopup;
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

    // Raccourcis réels + navigation clavier
    private readonly List<IDisposable> _shortcutRegistrations = new();
    private TopLevel? _root;
    private ProMenuPopup? _activePopup; // popup le plus profond (sous-menus)

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
            RefreshShortcuts();
        };
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _root = TopLevel.GetTopLevel(this);
        if (_root != null)
        {
            // Tunnel : la navigation d'un menu OUVERT prime sur le contrôle focalisé
            _root.AddHandler(InputElement.KeyDownEvent, OnRootKeyDown,
                Avalonia.Interactivity.RoutingStrategies.Tunnel);
        }
        RefreshShortcuts();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_root != null)
            _root.RemoveHandler(InputElement.KeyDownEvent, OnRootKeyDown);
        _root = null;
        ClearShortcuts();
        base.OnDetachedFromVisualTree(e);
    }

    private void ClearShortcuts()
    {
        foreach (var registration in _shortcutRegistrations)
            registration.Dispose();
        _shortcutRegistrations.Clear();
    }

    /// <summary>
    /// (Ré)enregistre les Shortcut de tous les items comme accélérateurs
    /// réels de la fenêtre (appelé à l'attache et sur changement d'Items)
    /// </summary>
    public void RefreshShortcuts()
    {
        ClearShortcuts();
        if (_root == null) return;

        var manager = ProShortcutManager.GetFor(_root);

        void Walk(IEnumerable<ProMenuItem> items)
        {
            foreach (var item in items)
            {
                if (!item.IsSeparator &&
                    ProShortcutManager.TryParse(item.Shortcut) is { } gesture)
                {
                    var captured = item;
                    _shortcutRegistrations.Add(manager.Register(
                        gesture, () => captured.Activate(), () => captured.IsEnabled));
                }
                if (item.Items is { Count: > 0 })
                    Walk(item.Items);
            }
        }

        foreach (var barItem in Items)
            Walk(barItem.Items);
    }

    // ═══════════════════════════════════════════════════════════════
    // CLAVIER : Alt+lettre ouvre, flèches/Enter/Échap naviguent
    // ═══════════════════════════════════════════════════════════════

    private void OnRootKeyDown(object? sender, KeyEventArgs e)
    {
        // Navigation dans un menu ouvert
        if (_openItem != null)
        {
            var popup = _activePopup ?? _openItem.CurrentPopup;
            if (popup == null) return;

            switch (e.Key)
            {
                case Key.Down:
                    popup.MoveHighlight(1);
                    e.Handled = true;
                    break;

                case Key.Up:
                    popup.MoveHighlight(-1);
                    e.Handled = true;
                    break;

                case Key.Enter:
                {
                    var submenu = popup.ActivateHighlighted();
                    if (submenu != null)
                        _activePopup = submenu;
                    else
                        CloseAllMenus();
                    e.Handled = true;
                    break;
                }

                case Key.Right:
                {
                    var submenu = popup.OpenHighlightedSubmenu();
                    if (submenu != null)
                        _activePopup = submenu;
                    else
                        OpenAdjacentMenu(1);
                    e.Handled = true;
                    break;
                }

                case Key.Left:
                    if (_activePopup is { ParentPopup: not null })
                    {
                        var parent = _activePopup.ParentPopup;
                        _activePopup.Close();
                        _activePopup = parent;
                    }
                    else
                    {
                        OpenAdjacentMenu(-1);
                    }
                    e.Handled = true;
                    break;

                case Key.Escape:
                    CloseAllMenus();
                    e.Handled = true;
                    break;
            }
            return;
        }

        // Alt+lettre : ouvre le menu dont l'en-tête commence par la lettre
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt) && e.Key is >= Key.A and <= Key.Z)
        {
            var letter = (char)('A' + (e.Key - Key.A));
            var match = Items.FirstOrDefault(i =>
                i.Header.Length > 0 &&
                char.ToUpperInvariant(i.Header[0]) == letter);

            if (match != null)
            {
                OpenMenuWithKeyboard(match);
                e.Handled = true;
            }
        }
    }

    private void OpenMenuWithKeyboard(ProMenuBarItem item)
    {
        OpenMenu(item);
        item.OpenMenu();
        _activePopup = item.CurrentPopup;
        _activePopup?.MoveHighlight(1); // premier item surligné
    }

    private void OpenAdjacentMenu(int direction)
    {
        if (_openItem == null || Items.Count == 0) return;
        var index = Items.IndexOf(_openItem);
        var next = Items[((index + direction) % Items.Count + Items.Count) % Items.Count];
        CloseAllMenus();
        OpenMenuWithKeyboard(next);
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
        _activePopup = null;
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
    
    /// <summary>Popup actuellement ouvert (navigation clavier)</summary>
    internal ProMenuPopup? CurrentPopup => _popup;

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
