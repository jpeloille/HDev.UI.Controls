using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ProControls.Theme;

namespace ProControls.Controls;

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
public class ProCard : ContentControl
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
        AvaloniaProperty.Register<ProCard, CardElevation>(nameof(Elevation), CardElevation.Low);

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ProCard, string>(nameof(Title), "");

    public static readonly StyledProperty<bool> HasBorderProperty =
        AvaloniaProperty.Register<ProCard, bool>(nameof(HasBorder), true);

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

    public ProCard()
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
            Background = new SolidColorBrush(ProTheme.Background.Panel),
            BorderBrush = new SolidColorBrush(ProTheme.Border.Subtle),
            CornerRadius = new CornerRadius(ProTheme.Size.CornerRadiusLarge)
        };

        var innerPanel = new StackPanel();

        // Titre + séparateur (toujours créés, visibilité pilotée par Title)
        _titleBlock = new TextBlock
        {
            FontFamily = new FontFamily(ProTheme.Typography.FontFamily),
            FontSize = ProTheme.Typography.FontSizeSubtitle,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(ProTheme.Text.Primary),
            Margin = new Thickness(16, 12, 16, 8)
        };
        innerPanel.Children.Add(_titleBlock);

        _titleSeparator = new Border
        {
            Height = 1,
            Background = new SolidColorBrush(ProTheme.Border.Subtle),
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
            CardElevation.Low => MakeShadow(ProTheme.Shadow.Subtle, ProTheme.Shadow.Color),
            CardElevation.Medium => MakeShadow(ProTheme.Shadow.Medium, ProTheme.Shadow.Color),
            CardElevation.High => MakeShadow(ProTheme.Shadow.Strong, ProTheme.Shadow.ColorStrong),
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
