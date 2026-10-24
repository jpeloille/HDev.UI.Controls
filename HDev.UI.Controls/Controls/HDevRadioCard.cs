using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

/// <summary>
/// Carte-radio : choix exclusif présenté en carte (titre + sous-titre), pour les
/// sélecteurs dont chaque option mérite une ligne d'explication.
/// Choisie : filet accent 2 px et fond teinté, encore lisibles une fois désactivée.
/// Exclusivité et flèches partagées avec <see cref="HDevRadioButton"/>
/// (<see cref="HDevRadioGroup"/>) : cartes sœurs d'un même panneau, même GroupName.
/// </summary>
public class HDevRadioCard : HDevControlBase
{
    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════

    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<HDevRadioCard, bool>(nameof(IsChecked), false);

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<HDevRadioCard, string>(nameof(Title), "");

    public static readonly StyledProperty<string> SubtitleProperty =
        AvaloniaProperty.Register<HDevRadioCard, string>(nameof(Subtitle), "");

    public static readonly StyledProperty<string> GroupNameProperty =
        AvaloniaProperty.Register<HDevRadioCard, string>(nameof(GroupName), "");

    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string GroupName
    {
        get => GetValue(GroupNameProperty);
        set => SetValue(GroupNameProperty, value);
    }

    // ═══════════════════════════════════════════════════════════════
    // ÉVÉNEMENTS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Déclenché à chaque changement de IsChecked, quelle qu'en soit la source
    /// (souris, clavier, binding, code)
    /// </summary>
    public event EventHandler<bool>? CheckedChanged;

    private const double PaddingH = HDevTheme.Size.PaddingM;
    private const double PaddingV = 10;
    private const double LineGap = 2;
    private const double Radius = HDevTheme.Size.CornerRadiusLarge;
    private const double TitleSize = HDevTheme.Typography.FontSizeBody;
    private const double SubtitleSize = HDevTheme.Typography.FontSizeDense;

    static HDevRadioCard()
    {
        AffectsRender<HDevRadioCard>(IsCheckedProperty, TitleProperty, SubtitleProperty);
        AffectsMeasure<HDevRadioCard>(TitleProperty, SubtitleProperty);
    }

    public HDevRadioCard()
    {
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    protected override void OnClick()
    {
        if (!IsEnabled || IsChecked) return;
        IsChecked = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsCheckedProperty)
        {
            // L'exclusivité vit ici pour s'appliquer aussi aux changements par code
            if (IsChecked)
            {
                foreach (var card in HDevRadioGroup.Siblings(this, c => c.GroupName))
                    card.IsChecked = false;
            }

            CheckedChanged?.Invoke(this, IsChecked);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Navigation par flèches au sein du groupe (coche la cible, comme HDevRadioButton)
        if (!e.Handled && IsEnabled && HDevRadioGroup.Navigate(this, e.Key, c => c.GroupName) is { } next)
        {
            next.IsChecked = true;
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    // ═══════════════════════════════════════════════════════════════
    // MESURE
    // ═══════════════════════════════════════════════════════════════

    protected override Size MeasureOverride(Size availableSize)
    {
        var title = CreateText(Title, HDevTheme.Text.Primary, TitleSize, FontWeight.SemiBold);
        var subtitle = string.IsNullOrEmpty(Subtitle) ? null : CreateText(Subtitle, HDevTheme.Text.Secondary, SubtitleSize);

        var contentWidth = Math.Max(title.Width, subtitle?.Width ?? 0);
        var width = double.IsInfinity(availableSize.Width) ? contentWidth + 2 * PaddingH : availableSize.Width;
        var height = PaddingV + title.Height + (subtitle is null ? 0 : LineGap + subtitle.Height) + PaddingV;
        return new Size(width, height);
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════

    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);
        var bounds = new Rect(Bounds.Size);
        var panel = HDevTheme.Background.Panel;

        Color fill, stroke;
        var thickness = 1.0;

        if (!IsEnabled)
        {
            // Désactivée, la carte choisie le reste visiblement : filet 2 px atténué
            fill = IsChecked ? HDevTheme.Lerp(panel, HDevTheme.Accent.Primary, 0.03) : panel;
            stroke = IsChecked
                ? HDevTheme.Lerp(HDevTheme.Disabled.Border, HDevTheme.Accent.Primary, 0.4)
                : HDevTheme.Disabled.Border;
            thickness = IsChecked ? 2.0 : 1.0;
        }
        else if (IsChecked)
        {
            fill = HDevTheme.Lerp(panel, HDevTheme.Accent.Primary, 0.055);
            stroke = IsPressed ? HDevTheme.Accent.PrimaryPressed : HDevTheme.Accent.Primary;
            thickness = 2.0;
        }
        else if (IsPressed)
        {
            fill = HDevTheme.Lerp(panel, HDevTheme.Accent.Primary, 0.08);
            stroke = HDevTheme.Border.Pressed;
        }
        else if (IsHovered)
        {
            fill = HDevTheme.Lerp(panel, HDevTheme.Accent.Primary, 0.03);
            stroke = HDevTheme.Border.Hover;
        }
        else
        {
            fill = panel;
            stroke = HDevTheme.Border.Default;
        }

        // Le filet est tracé à l'intérieur de la carte : passer de 1 à 2 px ne
        // décale pas le texte
        context.DrawRectangle(new SolidColorBrush(fill), null, bounds, Radius, Radius);
        var inset = thickness / 2;
        context.DrawRectangle(null, new Pen(new SolidColorBrush(stroke), thickness),
            bounds.Deflate(inset), Radius - inset, Radius - inset);

        if (IsFocusVisible && IsEnabled)
            DrawFocusRing(context, bounds, Radius);

        var maxWidth = Math.Max(0, bounds.Width - 2 * PaddingH);

        var title = CreateText(Title, IsEnabled ? HDevTheme.Text.Primary : HDevTheme.Disabled.Text,
            TitleSize, FontWeight.SemiBold);
        title.MaxTextWidth = maxWidth;
        title.Trimming = TextTrimming.CharacterEllipsis;
        title.MaxLineCount = 1;
        context.DrawText(title, Crisp.Snap(new Point(PaddingH, PaddingV)));

        if (!string.IsNullOrEmpty(Subtitle))
        {
            var subtitle = CreateText(Subtitle, IsEnabled ? HDevTheme.Text.Secondary : HDevTheme.Disabled.Text,
                SubtitleSize);
            subtitle.MaxTextWidth = maxWidth;
            subtitle.Trimming = TextTrimming.CharacterEllipsis;
            subtitle.MaxLineCount = 1;
            context.DrawText(subtitle, Crisp.Snap(new Point(PaddingH, PaddingV + title.Height + LineGap)));
        }
    }
}
