using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Layout;
using ProControls.Theme;

namespace ProControls.Controls;

/// <summary>
/// Fenêtre professionnelle style VS2022
/// Chrome personnalisé avec barre de titre, boutons de contrôle
/// </summary>
public class ProWindow : Window
{
    // ═══════════════════════════════════════════════════════════════
    // ÉLÉMENTS UI
    // ═══════════════════════════════════════════════════════════════
    
    private Border? _titleBar;
    private Border? _contentArea;
    private Panel? _windowButtonsPanel;
    private TextBlock? _titleText;
    private Image? _iconImage;
    
    private ProWindowButton? _minimizeButton;
    private ProWindowButton? _maximizeButton;
    private ProWindowButton? _closeButton;
    
    // État pour restore après maximize
#pragma warning disable CS0169 // Never used - reserved for future window state management
    private bool _isMaximized;
    private PixelPoint _restorePosition;
    private Size _restoreSize;
#pragma warning restore CS0169
    
    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    public static readonly StyledProperty<IBrush?> TitleBarBackgroundProperty =
        AvaloniaProperty.Register<ProWindow, IBrush?>(nameof(TitleBarBackground));
    
    public static readonly StyledProperty<IBrush?> TitleBarForegroundProperty =
        AvaloniaProperty.Register<ProWindow, IBrush?>(nameof(TitleBarForeground));
    
    public static readonly StyledProperty<double> TitleBarHeightProperty =
        AvaloniaProperty.Register<ProWindow, double>(nameof(TitleBarHeight), 32);
    
    public static readonly StyledProperty<bool> ShowIconProperty =
        AvaloniaProperty.Register<ProWindow, bool>(nameof(ShowIcon), true);
    
    public static readonly StyledProperty<bool> CanMinimizeProperty =
        AvaloniaProperty.Register<ProWindow, bool>(nameof(CanMinimize), true);
    
    public static readonly StyledProperty<bool> CanMaximizeProperty =
        AvaloniaProperty.Register<ProWindow, bool>(nameof(CanMaximize), true);
    
    public static readonly StyledProperty<object?> TitleBarContentProperty =
        AvaloniaProperty.Register<ProWindow, object?>(nameof(TitleBarContent));

    public IBrush? TitleBarBackground
    {
        get => GetValue(TitleBarBackgroundProperty);
        set => SetValue(TitleBarBackgroundProperty, value);
    }
    
    public IBrush? TitleBarForeground
    {
        get => GetValue(TitleBarForegroundProperty);
        set => SetValue(TitleBarForegroundProperty, value);
    }
    
    public double TitleBarHeight
    {
        get => GetValue(TitleBarHeightProperty);
        set => SetValue(TitleBarHeightProperty, value);
    }
    
    public bool ShowIcon
    {
        get => GetValue(ShowIconProperty);
        set => SetValue(ShowIconProperty, value);
    }
    
    public bool CanMinimize
    {
        get => GetValue(CanMinimizeProperty);
        set => SetValue(CanMinimizeProperty, value);
    }
    
    public bool CanMaximize
    {
        get => GetValue(CanMaximizeProperty);
        set => SetValue(CanMaximizeProperty, value);
    }
    
    public object? TitleBarContent
    {
        get => GetValue(TitleBarContentProperty);
        set => SetValue(TitleBarContentProperty, value);
    }
    
    // ═══════════════════════════════════════════════════════════════
    // CONSTRUCTEUR
    // ═══════════════════════════════════════════════════════════════
    
    public ProWindow()
    {
        // Rendu de texte subpixel (type ClearType) pour toute la fenêtre :
        // l'anti-aliasing niveaux de gris par défaut paraît flou à 96 dpi
        RenderOptions.SetTextRenderingMode(this, TextRenderingMode.SubpixelAntialias);

        // Fenêtre sans chrome système
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
        ExtendClientAreaTitleBarHeightHint = -1;
        
        // Valeurs par défaut VS2022
        TitleBarBackground = new SolidColorBrush(VS2022Theme.Background.Toolbar);
        TitleBarForeground = new SolidColorBrush(VS2022Theme.Text.Primary);
        Background = new SolidColorBrush(VS2022Theme.Background.Window);
        
        // Bordure subtile
        BorderBrush = new SolidColorBrush(VS2022Theme.Border.Default);
        BorderThickness = new Thickness(1);
        
        // Construire l'UI
        InitializeComponent();
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        // Appliquer les propriétés après que tout soit initialisé
        if (_titleBar != null)
        {
            _titleBar.Background = TitleBarBackground;
            _titleBar.Height = TitleBarHeight;
        }
        if (_titleText != null)
        {
            _titleText.Foreground = TitleBarForeground;
        }
    }
    
    private void InitializeComponent()
    {
        var mainGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*")
        };
        
        // ══════════════════════════════════════════════════════════
        // BARRE DE TITRE
        // ══════════════════════════════════════════════════════════
        
        _titleBar = new Border
        {
            Height = TitleBarHeight,
            Background = TitleBarBackground,
            ClipToBounds = true
        };
        
        var titleBarGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
        };
        
        // Icône + Titre
        var leftPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };
        
        _iconImage = new Image
        {
            Width = 16,
            Height = 16,
            Margin = new Thickness(0, 0, 8, 0),
            IsVisible = ShowIcon
        };
        leftPanel.Children.Add(_iconImage);
        
        _titleText = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = VS2022Theme.Typography.FontSizeBody,
            Foreground = TitleBarForeground
        };
        // Binding au titre de la fenêtre
        _titleText.Bind(TextBlock.TextProperty, this.GetObservable(TitleProperty));
        leftPanel.Children.Add(_titleText);
        
        Grid.SetColumn(leftPanel, 0);
        titleBarGrid.Children.Add(leftPanel);
        
        // Contenu personnalisé au centre (optionnel - menus, etc.)
        var centerContent = new ContentControl
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        centerContent.Bind(ContentControl.ContentProperty, this.GetObservable(TitleBarContentProperty));
        Grid.SetColumn(centerContent, 1);
        titleBarGrid.Children.Add(centerContent);
        
        // Boutons de contrôle (minimize, maximize, close)
        _windowButtonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Top
        };
        
        _minimizeButton = new ProWindowButton(ProWindowButtonType.Minimize);
        _minimizeButton.Click += (s, e) => WindowState = WindowState.Minimized;
        _minimizeButton.Bind(ProWindowButton.IsVisibleProperty, this.GetObservable(CanMinimizeProperty));
        _windowButtonsPanel.Children.Add(_minimizeButton);
        
        _maximizeButton = new ProWindowButton(ProWindowButtonType.Maximize);
        _maximizeButton.Click += (s, e) => ToggleMaximize();
        _maximizeButton.Bind(ProWindowButton.IsVisibleProperty, this.GetObservable(CanMaximizeProperty));
        _windowButtonsPanel.Children.Add(_maximizeButton);
        
        _closeButton = new ProWindowButton(ProWindowButtonType.Close);
        _closeButton.Click += (s, e) => Close();
        _windowButtonsPanel.Children.Add(_closeButton);
        
        Grid.SetColumn(_windowButtonsPanel, 2);
        titleBarGrid.Children.Add(_windowButtonsPanel);
        
        _titleBar.Child = titleBarGrid;
        
        // Gestion du drag de la fenêtre
        _titleBar.PointerPressed += OnTitleBarPointerPressed;
        _titleBar.DoubleTapped += OnTitleBarDoubleTapped;
        
        Grid.SetRow(_titleBar, 0);
        mainGrid.Children.Add(_titleBar);
        
        // ══════════════════════════════════════════════════════════
        // ZONE DE CONTENU
        // ══════════════════════════════════════════════════════════
        
        _contentArea = new Border
        {
            Background = Background,
            ClipToBounds = true
        };
        
        // On va déplacer le Content original ici
        Grid.SetRow(_contentArea, 1);
        mainGrid.Children.Add(_contentArea);
        
        // Remplacer le contenu de la fenêtre
        base.Content = mainGrid;
    }
    
    public new object? Content
    {
        get => _contentArea?.Child;
        set
        {
            if (_contentArea != null)
            {
                _contentArea.Child = value as Control;
            }
        }
    }
    
    // ═══════════════════════════════════════════════════════════════
    // INTERACTIONS BARRE DE TITRE
    // ═══════════════════════════════════════════════════════════════
    
    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            // Vérifier qu'on n'est pas sur un bouton
            if (e.Source is ProWindowButton)
                return;
            
            BeginMoveDrag(e);
        }
    }
    
    private void OnTitleBarDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (CanMaximize)
        {
            ToggleMaximize();
        }
    }
    
    private void ToggleMaximize()
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            _maximizeButton?.SetButtonType(ProWindowButtonType.Maximize);
        }
        else
        {
            WindowState = WindowState.Maximized;
            _maximizeButton?.SetButtonType(ProWindowButtonType.Restore);
        }
    }
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == WindowStateProperty)
        {
            var state = (WindowState)change.NewValue!;
            _maximizeButton?.SetButtonType(
                state == WindowState.Maximized 
                    ? ProWindowButtonType.Restore 
                    : ProWindowButtonType.Maximize);
            
            // Ajuster la bordure en mode maximisé
            BorderThickness = state == WindowState.Maximized 
                ? new Thickness(0) 
                : new Thickness(1);
        }
        
        if (change.Property == TitleBarHeightProperty && _titleBar != null)
        {
            _titleBar.Height = TitleBarHeight;
        }
        
        if (change.Property == TitleBarBackgroundProperty && _titleBar != null)
        {
            _titleBar.Background = TitleBarBackground;
        }
        
        if (change.Property == TitleBarForegroundProperty && _titleText != null)
        {
            _titleText.Foreground = TitleBarForeground;
        }
        
        if (change.Property == ShowIconProperty && _iconImage != null)
        {
            _iconImage.IsVisible = ShowIcon;
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════
// BOUTON DE CONTRÔLE FENÊTRE (Minimize, Maximize, Close)
// ═══════════════════════════════════════════════════════════════════════

public enum ProWindowButtonType
{
    Minimize,
    Maximize,
    Restore,
    Close
}

public class ProWindowButton : Control
{
    private ProWindowButtonType _buttonType;
    private bool _isHovered;
    private bool _isPressed;
    
    public event EventHandler? Click;
    
    public ProWindowButton(ProWindowButtonType type)
    {
        _buttonType = type;
        Width = 46;
        Height = 32;
        ClipToBounds = true;
    }
    
    public void SetButtonType(ProWindowButtonType type)
    {
        _buttonType = type;
        InvalidateVisual();
    }
    
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        _isHovered = true;
        InvalidateVisual();
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
        _isPressed = true;
        InvalidateVisual();
        e.Handled = true;
        base.OnPointerPressed(e);
    }
    
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_isPressed && _isHovered)
        {
            Click?.Invoke(this, EventArgs.Empty);
        }
        _isPressed = false;
        InvalidateVisual();
        base.OnPointerReleased(e);
    }
    
    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        
        // Couleur de fond selon l'état
        Color? bgColor = null;
        
        if (_buttonType == ProWindowButtonType.Close)
        {
            if (_isPressed)
                bgColor = VS2022Theme.WindowChrome.ClosePressed;
            else if (_isHovered)
                bgColor = VS2022Theme.WindowChrome.CloseHover;
        }
        else
        {
            if (_isPressed)
                bgColor = VS2022Theme.Background.ControlPressed;
            else if (_isHovered)
                bgColor = VS2022Theme.Background.ControlHover;
        }
        
        if (bgColor.HasValue)
        {
            context.FillRectangle(new SolidColorBrush(bgColor.Value), bounds);
        }
        
        // Couleur de l'icône
        var iconColor = _buttonType == ProWindowButtonType.Close && _isHovered
            ? Colors.White
            : VS2022Theme.Text.Primary;
        
        var iconPen = new Pen(new SolidColorBrush(iconColor), 1);
        var center = bounds.Center;
        
        // Dessiner l'icône selon le type
        switch (_buttonType)
        {
            case ProWindowButtonType.Minimize:
                // Ligne horizontale
                context.DrawLine(iconPen,
                    new Point(center.X - 5, center.Y),
                    new Point(center.X + 5, center.Y));
                break;
                
            case ProWindowButtonType.Maximize:
                // Rectangle
                var maxRect = new Rect(center.X - 5, center.Y - 5, 10, 10);
                context.DrawRectangle(null, iconPen, maxRect);
                break;
                
            case ProWindowButtonType.Restore:
                // Deux rectangles superposés
                var backRect = new Rect(center.X - 3, center.Y - 6, 8, 8);
                var frontRect = new Rect(center.X - 6, center.Y - 3, 8, 8);
                
                // Rectangle arrière (juste les côtés visibles)
                context.DrawLine(iconPen, 
                    new Point(backRect.Left, backRect.Top), 
                    new Point(backRect.Right, backRect.Top));
                context.DrawLine(iconPen, 
                    new Point(backRect.Right, backRect.Top), 
                    new Point(backRect.Right, backRect.Bottom - 2));
                
                // Rectangle avant
                context.DrawRectangle(new SolidColorBrush(VS2022Theme.Background.Toolbar), iconPen, frontRect);
                break;
                
            case ProWindowButtonType.Close:
                // Croix
                context.DrawLine(iconPen,
                    new Point(center.X - 5, center.Y - 5),
                    new Point(center.X + 5, center.Y + 5));
                context.DrawLine(iconPen,
                    new Point(center.X + 5, center.Y - 5),
                    new Point(center.X - 5, center.Y + 5));
                break;
        }
    }
}
