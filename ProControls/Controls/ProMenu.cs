using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Layout;
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
    
    public ProMenuPopup()
    {
        IsLightDismissEnabled = true;
        Placement = PlacementMode.BottomEdgeAlignedLeft;
        
        _itemsPanel = new StackPanel
        {
            Orientation = Orientation.Vertical
        };
        
        _container = new Border
        {
            Background = new SolidColorBrush(VS2022Theme.Background.Panel),
            BorderBrush = new SolidColorBrush(VS2022Theme.Border.Default),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(2, 4),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0,
                OffsetY = 4,
                Blur = 16,
                Color = Color.FromArgb(40, 0, 0, 0)
            }),
            Child = _itemsPanel
        };
        
        Child = _container;
    }
    
    public void SetItems(IEnumerable<ProMenuItem> sourceItems)
    {
        _itemsPanel.Children.Clear();
        foreach (var item in sourceItems)
        {
            // Créer une copie pour le popup
            var menuItem = new ProMenuItem
            {
                Header = item.Header,
                Icon = item.Icon,
                Shortcut = item.Shortcut,
                IsSeparator = item.IsSeparator,
                IsCheckable = item.IsCheckable,
                IsChecked = item.IsChecked
            };
            menuItem.Click += (s, e) => item.RaiseClick();
            _itemsPanel.Children.Add(menuItem);
        }
    }
    
    public void ShowAt(Control target, Point offset = default)
    {
        PlacementTarget = target;
        HorizontalOffset = offset.X;
        VerticalOffset = offset.Y;
        Open();
    }
    
    public void ShowAtPosition(Control anchor, double x, double y)
    {
        PlacementTarget = anchor;
        Placement = PlacementMode.Pointer;
        HorizontalOffset = x;
        VerticalOffset = y;
        Open();
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
        Height = 28;
        ClipToBounds = false;
        Background = new SolidColorBrush(VS2022Theme.Background.Toolbar);
        
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
        return new Size(Math.Max(totalWidth, availableSize.Width), 28);
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
            new Typeface(VS2022Theme.Typography.FontFamily),
            VS2022Theme.Typography.FontSizeBody,
            Brushes.Black);
        
        return new Size(text.Width + 20, 28);
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
            CloseMenu();
        else
            OpenMenu();
        
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
        
        // Fond au survol ou si menu ouvert
        if (_isHovered || _isOpen)
        {
            var bgColor = _isOpen 
                ? VS2022Theme.Background.ControlPressed 
                : VS2022Theme.Background.ControlHover;
            context.FillRectangle(new SolidColorBrush(bgColor), bounds);
        }
        
        // Texte
        var text = new FormattedText(
            Header,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(VS2022Theme.Typography.FontFamily),
            VS2022Theme.Typography.FontSizeBody,
            new SolidColorBrush(VS2022Theme.Text.Primary));
        
        var textX = (bounds.Width - text.Width) / 2;
        var textY = (bounds.Height - text.Height) / 2;
        context.DrawText(text, new Point(textX, textY));
    }
}
