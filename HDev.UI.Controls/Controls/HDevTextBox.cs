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
        MinHeight = HDevTheme.Size.EditorHeight;
        // Mono-ligne : jamais plus haut que EditorHeight. Dans une rangée plus haute, le
        // champ se centre (Stretch borné) au lieu de s'étirer ; le multiligne lève la borne
        MaxHeight = HDevTheme.Size.EditorHeight;
        // Pas de clip sur l'hôte : il couperait le halo de focus (BoxShadow) ;
        // le TextBox interne clippe son propre contenu

        _innerTextBox = new TextBox
        {
            Padding = new Thickness(HDevTheme.Size.EditorTextInset, 0),
            ClipToBounds = true,
            Margin = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily(HDevTheme.Typography.FontFamily),
            FontSize = HDevTheme.Typography.FontSizeBody,
            CaretBrush = new SolidColorBrush(HDevTheme.Text.Primary),
            SelectionBrush = new SolidColorBrush(HDevTheme.Background.TextSelection),
            SelectionForegroundBrush = new SolidColorBrush(HDevTheme.Text.Primary)
        };
        HDevInputChrome.Neutralize(_innerTextBox);
        
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
            _innerTextBox.Padding = new Thickness(HDevTheme.Size.EditorTextInset, 5);
            MinHeight = HDevTheme.Size.EditorHeight * 3; // ~3 lignes
            MaxHeight = double.PositiveInfinity;
        }
        else
        {
            _innerTextBox.AcceptsReturn = false;
            _innerTextBox.TextWrapping = TextWrapping.NoWrap;
            _innerTextBox.VerticalAlignment = VerticalAlignment.Center;
            _innerTextBox.VerticalContentAlignment = VerticalAlignment.Center;
            _innerTextBox.Padding = new Thickness(HDevTheme.Size.EditorTextInset, 0);
            MinHeight = HDevTheme.Size.EditorHeight;
            MaxHeight = HDevTheme.Size.EditorHeight;
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
        _innerTextBox.SelectionBrush = new SolidColorBrush(HDevTheme.Background.TextSelection);
        _innerTextBox.SelectionForegroundBrush = new SolidColorBrush(HDevTheme.Text.Primary);
        HDevInputChrome.Neutralize(_innerTextBox);
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
        => HDevInputChrome.ApplyState(this, _innerTextBox,
            IsEnabled, HasError, _isFocusedState, _isHovered);

    /// <summary>Fige survol et focus pour la planche visuelle headless (tests/Gallery)</summary>
    internal void ForceVisualState(bool hovered = false, bool focused = false)
    {
        _isHovered = hovered;
        _isFocusedState = focused;
        UpdateVisualState();
    }

    // ═══════════════════════════════════════════════════════════════
    // MÉTHODES PUBLIQUES
    // ═══════════════════════════════════════════════════════════════
    
    public void FocusTextBox() => _innerTextBox.Focus();
    public void SelectAll() => _innerTextBox.SelectAll();
    public void Clear() { Text = ""; _innerTextBox.Clear(); }
}
