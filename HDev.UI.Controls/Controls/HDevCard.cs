using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

public enum CardElevation
{
    Flat,
    Low,
    Medium,
    High
}

/// <summary>
/// Carte/conteneur avec titre, style carte libadwaita
/// </summary>
public class HDevCard : ContentControl
{
    private bool _initialized;
    private object? _originalContent;

    // Références conservées pour mettre à jour la carte en place quand une
    // propriété change après construction (Title, HasBorder, Elevation)
    private Border? _cardBorder;
    private TextBlock? _titleBlock;
    private Border? _titleSeparator;
    private Border? _contentBorder;

    public static readonly StyledProperty<CardElevation> ElevationProperty =
        AvaloniaProperty.Register<HDevCard, CardElevation>(nameof(Elevation), CardElevation.Low);

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<HDevCard, string>(nameof(Title), "");

    public static readonly StyledProperty<bool> HasBorderProperty =
        AvaloniaProperty.Register<HDevCard, bool>(nameof(HasBorder), true);

    public CardElevation Elevation
    {
        get => GetValue(ElevationProperty);
        set => SetValue(ElevationProperty, value);
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool HasBorder
    {
        get => GetValue(HasBorderProperty);
        set => SetValue(HasBorderProperty, value);
    }

    public HDevCard()
    {
        // Style de base
        Margin = new Thickness(4);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (!_initialized)
        {
            _initialized = true;
            BuildLayout();
        }

        HDevTheme.VariantChanged += OnThemeVariantChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        HDevTheme.VariantChanged -= OnThemeVariantChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        // Les brushes de la carte sont construits une fois : les re-poser
        if (_cardBorder != null)
        {
            _cardBorder.Background = new SolidColorBrush(HDevTheme.Background.Panel);
            _cardBorder.BorderBrush = new SolidColorBrush(HDevTheme.Border.Subtle);
        }
        if (_titleBlock != null)
            _titleBlock.Foreground = new SolidColorBrush(HDevTheme.Text.Primary);
        if (_titleSeparator != null)
            _titleSeparator.Background = new SolidColorBrush(HDevTheme.Border.Subtle);
        ApplyElevation();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (!_initialized)
            return;

        if (change.Property == TitleProperty)
            ApplyTitle();
        else if (change.Property == HasBorderProperty)
            ApplyBorder();
        else if (change.Property == ElevationProperty)
            ApplyElevation();
    }

    private void BuildLayout()
    {
        // Sauvegarder le contenu original défini par XAML
        _originalContent = Content;

        // Détacher le contenu original de ce ContentControl
        Content = null;

        // Créer la structure de la carte
        _cardBorder = new Border
        {
            Background = new SolidColorBrush(HDevTheme.Background.Panel),
            BorderBrush = new SolidColorBrush(HDevTheme.Border.Subtle),
            CornerRadius = new CornerRadius(HDevTheme.Size.CornerRadiusLarge)
        };

        var innerPanel = new StackPanel();

        // Titre + séparateur (toujours créés, visibilité pilotée par Title)
        _titleBlock = new TextBlock
        {
            FontFamily = new FontFamily(HDevTheme.Typography.FontFamily),
            FontSize = HDevTheme.Typography.FontSizeSubtitle,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(HDevTheme.Text.Primary),
            Margin = new Thickness(16, 12, 16, 8)
        };
        innerPanel.Children.Add(_titleBlock);

        _titleSeparator = new Border
        {
            Height = 1,
            Background = new SolidColorBrush(HDevTheme.Border.Subtle),
            Margin = new Thickness(16, 0, 16, 12)
        };
        innerPanel.Children.Add(_titleSeparator);

        // Contenu original
        _contentBorder = new Border
        {
            Child = _originalContent as Control
        };
        innerPanel.Children.Add(_contentBorder);

        _cardBorder.Child = innerPanel;

        ApplyTitle();
        ApplyBorder();
        ApplyElevation();

        // Remplacer le contenu
        Content = _cardBorder;
    }

    private void ApplyTitle()
    {
        if (_titleBlock == null || _titleSeparator == null || _contentBorder == null)
            return;

        var hasTitle = !string.IsNullOrEmpty(Title);
        _titleBlock.Text = Title;
        _titleBlock.IsVisible = hasTitle;
        _titleSeparator.IsVisible = hasTitle;
        _contentBorder.Margin = hasTitle
            ? new Thickness(16, 0, 16, 16)
            : new Thickness(16);
    }

    private void ApplyBorder()
    {
        if (_cardBorder == null)
            return;

        _cardBorder.BorderThickness = HasBorder ? new Thickness(1) : new Thickness(0);
    }

    private void ApplyElevation()
    {
        if (_cardBorder == null)
            return;

        _cardBorder.BoxShadow = Elevation switch
        {
            CardElevation.Flat => default,
            CardElevation.Low => MakeShadow(HDevTheme.Shadow.Subtle, HDevTheme.Shadow.Color),
            CardElevation.Medium => MakeShadow(HDevTheme.Shadow.Medium, HDevTheme.Shadow.Color),
            CardElevation.High => MakeShadow(HDevTheme.Shadow.Strong, HDevTheme.Shadow.ColorStrong),
            _ => default
        };
    }

    private static BoxShadows MakeShadow((double OffsetX, double OffsetY, double Blur, double Spread) p, Color color)
        => new(new BoxShadow
        {
            OffsetX = p.OffsetX,
            OffsetY = p.OffsetY,
            Blur = p.Blur,
            Spread = p.Spread,
            Color = color
        });
}
