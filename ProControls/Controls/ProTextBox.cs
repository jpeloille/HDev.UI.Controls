using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using ProControls.Theme;

namespace ProControls.Controls;

/// <summary>
/// Champ de saisie professionnel style VS2022
/// Wrapper stylisé autour du TextBox natif
/// </summary>
public class ProTextBox : Border
{
    private readonly TextBox _innerTextBox;
    private bool _isHovered;
    private bool _isFocusedState;
    
    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<ProTextBox, string?>(nameof(Text), 
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    
    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<ProTextBox, string?>(nameof(Placeholder));
    
    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<ProTextBox, bool>(nameof(IsReadOnly));
    
    public static readonly StyledProperty<bool> HasErrorProperty =
        AvaloniaProperty.Register<ProTextBox, bool>(nameof(HasError));
    
    public static readonly StyledProperty<bool> IsPasswordProperty =
        AvaloniaProperty.Register<ProTextBox, bool>(nameof(IsPassword));
    
    public static readonly StyledProperty<int> MaxLengthProperty =
        AvaloniaProperty.Register<ProTextBox, int>(nameof(MaxLength));

    public static readonly StyledProperty<bool> IsMultilineProperty =
        AvaloniaProperty.Register<ProTextBox, bool>(nameof(IsMultiline));

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
    
    public ProTextBox()
    {
        CornerRadius = new CornerRadius(ProTheme.Size.CornerRadiusSmall);
        BorderThickness = new Thickness(1);
        MinHeight = ProTheme.Size.ControlHeightMedium;
        ClipToBounds = true;
        
        _innerTextBox = new TextBox
        {
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Padding = new Thickness(10, 6),
            Margin = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily(ProTheme.Typography.FontFamily),
            FontSize = ProTheme.Typography.FontSizeBody,
            CaretBrush = new SolidColorBrush(ProTheme.Text.Primary),
            SelectionBrush = new SolidColorBrush(ProTheme.WithOpacity(ProTheme.Accent.Primary, 80)),
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
    
    static ProTextBox()
    {
        TextProperty.Changed.AddClassHandler<ProTextBox>((x, e) => x.OnTextChanged());
        PlaceholderProperty.Changed.AddClassHandler<ProTextBox>((x, e) => x.OnPlaceholderChanged());
        IsReadOnlyProperty.Changed.AddClassHandler<ProTextBox>((x, e) => x.OnIsReadOnlyChanged());
        HasErrorProperty.Changed.AddClassHandler<ProTextBox>((x, _) => x.UpdateVisualState());
        IsPasswordProperty.Changed.AddClassHandler<ProTextBox>((x, e) => x.OnIsPasswordChanged());
        MaxLengthProperty.Changed.AddClassHandler<ProTextBox>((x, e) => x.OnMaxLengthChanged());
        IsMultilineProperty.Changed.AddClassHandler<ProTextBox>((x, _) => x.OnIsMultilineChanged());
        IsEnabledProperty.Changed.AddClassHandler<ProTextBox>((x, _) => x.UpdateVisualState());
    }
    
    // ═══════════════════════════════════════════════════════════════
    // HANDLERS PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    private void OnTextChanged()
    {
        if (_innerTextBox.Text != Text)
            _innerTextBox.Text = Text;
    }
    
    private void OnPlaceholderChanged() => _innerTextBox.Watermark = Placeholder;
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
            MinHeight = ProTheme.Size.ControlHeightMedium * 2.6; // ~3 lignes
        }
        else
        {
            _innerTextBox.AcceptsReturn = false;
            _innerTextBox.TextWrapping = TextWrapping.NoWrap;
            _innerTextBox.VerticalAlignment = VerticalAlignment.Center;
            _innerTextBox.VerticalContentAlignment = VerticalAlignment.Center;
            MinHeight = ProTheme.Size.ControlHeightMedium;
        }
    }
    
    // ═══════════════════════════════════════════════════════════════
    // ÉVÉNEMENTS SOURIS
    // ═══════════════════════════════════════════════════════════════
    
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
            bgColor = ProTheme.Background.ControlDisabled;
            borderColor = ProTheme.Border.Disabled;
            _innerTextBox.Foreground = new SolidColorBrush(ProTheme.Text.Disabled);
            _innerTextBox.IsEnabled = false;
        }
        else if (HasError)
        {
            bgColor = ProTheme.Background.Error;
            borderColor = ProTheme.Accent.Error;
            borderThickness = 1.5;
            _innerTextBox.Foreground = new SolidColorBrush(ProTheme.Text.Primary);
            _innerTextBox.IsEnabled = true;
        }
        else if (_isFocusedState)
        {
            bgColor = ProTheme.Background.Control;
            borderColor = ProTheme.Border.Focused;
            borderThickness = 1.5;
            _innerTextBox.Foreground = new SolidColorBrush(ProTheme.Text.Primary);
            _innerTextBox.IsEnabled = true;
        }
        else if (_isHovered)
        {
            bgColor = ProTheme.Background.Control;
            borderColor = ProTheme.Border.Hover;
            _innerTextBox.Foreground = new SolidColorBrush(ProTheme.Text.Primary);
            _innerTextBox.IsEnabled = true;
        }
        else
        {
            bgColor = ProTheme.Background.Control;
            borderColor = ProTheme.Border.Default;
            _innerTextBox.Foreground = new SolidColorBrush(ProTheme.Text.Primary);
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
                Color = ProTheme.WithOpacity(ProTheme.Border.Focused, 50)
            });
        }
        else if (HasError)
        {
            BoxShadow = new BoxShadows(new BoxShadow
            {
                Blur = 0, Spread = 2,
                Color = ProTheme.WithOpacity(ProTheme.Accent.Error, 40)
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
