using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

/// <summary>
/// Champ de saisie professionnel style VS2022
/// Wrapper stylisé autour du TextBox natif
/// </summary>
public class HDevTextBox : Border
{
    private readonly TextBox _innerTextBox;
    private bool _isHovered;
    private bool _isFocusedState;
    
    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<HDevTextBox, string?>(nameof(Text), 
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    
    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<HDevTextBox, string?>(nameof(Placeholder));
    
    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<HDevTextBox, bool>(nameof(IsReadOnly));
    
    public static readonly StyledProperty<bool> HasErrorProperty =
        AvaloniaProperty.Register<HDevTextBox, bool>(nameof(HasError));
    
    public static readonly StyledProperty<bool> IsPasswordProperty =
        AvaloniaProperty.Register<HDevTextBox, bool>(nameof(IsPassword));
    
    public static readonly StyledProperty<int> MaxLengthProperty =
        AvaloniaProperty.Register<HDevTextBox, int>(nameof(MaxLength));

    public static readonly StyledProperty<bool> IsMultilineProperty =
        AvaloniaProperty.Register<HDevTextBox, bool>(nameof(IsMultiline));

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    
    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }
    
    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }
    
    public bool HasError
    {
        get => GetValue(HasErrorProperty);
        set => SetValue(HasErrorProperty, value);
    }
    
    public bool IsPassword
    {
        get => GetValue(IsPasswordProperty);
        set => SetValue(IsPasswordProperty, value);
    }
    
    public int MaxLength
    {
        get => GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    /// <summary>
    /// Mode multi-lignes (rôle MemoEdit) : Entrée insère un saut de ligne,
    /// le texte s'enroule et défile verticalement
    /// </summary>
    public bool IsMultiline
    {
        get => GetValue(IsMultilineProperty);
        set => SetValue(IsMultilineProperty, value);
    }
    
    // ═══════════════════════════════════════════════════════════════
    // ÉVÉNEMENT
    // ═══════════════════════════════════════════════════════════════
    
    public event EventHandler<TextChangedEventArgs>? TextChanged;
    
    // ═══════════════════════════════════════════════════════════════
    // CONSTRUCTEUR
    // ═══════════════════════════════════════════════════════════════
    
    public HDevTextBox()
    {
        CornerRadius = new CornerRadius(HDevTheme.Size.CornerRadiusSmall);
        BorderThickness = new Thickness(1);
        MinHeight = HDevTheme.Size.ControlHeightMedium;
        ClipToBounds = true;
        
        _innerTextBox = new TextBox
        {
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Padding = new Thickness(10, 6),
            Margin = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily(HDevTheme.Typography.FontFamily),
            FontSize = HDevTheme.Typography.FontSizeBody,
            CaretBrush = new SolidColorBrush(HDevTheme.Text.Primary),
            SelectionBrush = new SolidColorBrush(HDevTheme.WithOpacity(HDevTheme.Accent.Primary, 80)),
            SelectionForegroundBrush = new SolidColorBrush(Colors.White)
        };
        
        _innerTextBox.TextChanged += (s, e) =>
        {
            SetCurrentValue(TextProperty, _innerTextBox.Text);
            TextChanged?.Invoke(this, e);
        };
        
        _innerTextBox.GotFocus += (s, e) =>
        {
            _isFocusedState = true;
            UpdateVisualState();
        };
        
        _innerTextBox.LostFocus += (s, e) =>
        {
            _isFocusedState = false;
            UpdateVisualState();
        };
        
        Child = _innerTextBox;
        UpdateVisualState();
    }
    
    static HDevTextBox()
    {
        TextProperty.Changed.AddClassHandler<HDevTextBox>((x, e) => x.OnTextChanged());
        PlaceholderProperty.Changed.AddClassHandler<HDevTextBox>((x, e) => x.OnPlaceholderChanged());
        IsReadOnlyProperty.Changed.AddClassHandler<HDevTextBox>((x, e) => x.OnIsReadOnlyChanged());
        HasErrorProperty.Changed.AddClassHandler<HDevTextBox>((x, _) => x.UpdateVisualState());
        IsPasswordProperty.Changed.AddClassHandler<HDevTextBox>((x, e) => x.OnIsPasswordChanged());
        MaxLengthProperty.Changed.AddClassHandler<HDevTextBox>((x, e) => x.OnMaxLengthChanged());
        IsMultilineProperty.Changed.AddClassHandler<HDevTextBox>((x, _) => x.OnIsMultilineChanged());
        IsEnabledProperty.Changed.AddClassHandler<HDevTextBox>((x, _) => x.UpdateVisualState());
    }
    
    // ═══════════════════════════════════════════════════════════════
    // HANDLERS PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    private void OnTextChanged()
    {
        if (_innerTextBox.Text != Text)
            _innerTextBox.Text = Text;
    }
    
    private void OnPlaceholderChanged() => _innerTextBox.PlaceholderText = Placeholder;
    private void OnIsReadOnlyChanged() => _innerTextBox.IsReadOnly = IsReadOnly;
    private void OnIsPasswordChanged() => _innerTextBox.PasswordChar = IsPassword ? '●' : '\0';
    private void OnMaxLengthChanged() => _innerTextBox.MaxLength = MaxLength;

    private void OnIsMultilineChanged()
    {
        if (IsMultiline)
        {
            _innerTextBox.AcceptsReturn = true;
            _innerTextBox.TextWrapping = TextWrapping.Wrap;
            _innerTextBox.VerticalAlignment = VerticalAlignment.Stretch;
            _innerTextBox.VerticalContentAlignment = VerticalAlignment.Top;
            MinHeight = HDevTheme.Size.ControlHeightMedium * 2.6; // ~3 lignes
        }
        else
        {
            _innerTextBox.AcceptsReturn = false;
            _innerTextBox.TextWrapping = TextWrapping.NoWrap;
            _innerTextBox.VerticalAlignment = VerticalAlignment.Center;
            _innerTextBox.VerticalContentAlignment = VerticalAlignment.Center;
            MinHeight = HDevTheme.Size.ControlHeightMedium;
        }
    }
    
    // ═══════════════════════════════════════════════════════════════
    // ÉVÉNEMENTS SOURIS
    // ═══════════════════════════════════════════════════════════════
    
    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        HDev.UI.Controls.Theme.HDevTheme.VariantChanged += OnThemeVariantChanged;
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        HDev.UI.Controls.Theme.HDevTheme.VariantChanged -= OnThemeVariantChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        _innerTextBox.CaretBrush = new SolidColorBrush(HDevTheme.Text.Primary);
        _innerTextBox.SelectionBrush = new SolidColorBrush(HDevTheme.WithOpacity(HDevTheme.Accent.Primary, 80));
        UpdateVisualState();
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        _isHovered = true;
        UpdateVisualState();
    }
    
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _isHovered = false;
        UpdateVisualState();
    }
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _innerTextBox.Focus();
    }
    
    // ═══════════════════════════════════════════════════════════════
    // MISE À JOUR VISUELLE
    // ═══════════════════════════════════════════════════════════════
    
    private void UpdateVisualState()
    {
        Color bgColor, borderColor;
        double borderThickness = 1;
        
        if (!IsEnabled)
        {
            bgColor = HDevTheme.Background.ControlDisabled;
            borderColor = HDevTheme.Border.Disabled;
            _innerTextBox.Foreground = new SolidColorBrush(HDevTheme.Text.Disabled);
            _innerTextBox.IsEnabled = false;
        }
        else if (HasError)
        {
            bgColor = HDevTheme.Background.Error;
            borderColor = HDevTheme.Accent.Error;
            borderThickness = 1.5;
            _innerTextBox.Foreground = new SolidColorBrush(HDevTheme.Text.Primary);
            _innerTextBox.IsEnabled = true;
        }
        else if (_isFocusedState)
        {
            bgColor = HDevTheme.Background.Control;
            borderColor = HDevTheme.Border.Focused;
            borderThickness = 1.5;
            _innerTextBox.Foreground = new SolidColorBrush(HDevTheme.Text.Primary);
            _innerTextBox.IsEnabled = true;
        }
        else if (_isHovered)
        {
            bgColor = HDevTheme.Background.Control;
            borderColor = HDevTheme.Border.Hover;
            _innerTextBox.Foreground = new SolidColorBrush(HDevTheme.Text.Primary);
            _innerTextBox.IsEnabled = true;
        }
        else
        {
            bgColor = HDevTheme.Background.Control;
            borderColor = HDevTheme.Border.Default;
            _innerTextBox.Foreground = new SolidColorBrush(HDevTheme.Text.Primary);
            _innerTextBox.IsEnabled = true;
        }
        
        Background = new SolidColorBrush(bgColor);
        BorderBrush = new SolidColorBrush(borderColor);
        BorderThickness = new Thickness(borderThickness);
        
        // Focus ring via BoxShadow
        if (_isFocusedState && IsEnabled && !HasError)
        {
            BoxShadow = new BoxShadows(new BoxShadow
            {
                Blur = 0, Spread = 2,
                Color = HDevTheme.WithOpacity(HDevTheme.Border.Focused, 50)
            });
        }
        else if (HasError)
        {
            BoxShadow = new BoxShadows(new BoxShadow
            {
                Blur = 0, Spread = 2,
                Color = HDevTheme.WithOpacity(HDevTheme.Accent.Error, 40)
            });
        }
        else
        {
            BoxShadow = new BoxShadows();
        }
    }
    
    // ═══════════════════════════════════════════════════════════════
    // MÉTHODES PUBLIQUES
    // ═══════════════════════════════════════════════════════════════
    
    public void FocusTextBox() => _innerTextBox.Focus();
    public void SelectAll() => _innerTextBox.SelectAll();
    public void Clear() { Text = ""; _innerTextBox.Clear(); }
}
