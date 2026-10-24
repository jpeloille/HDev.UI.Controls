using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

/// <summary>
/// Icône vectorielle autonome (boîte de dialogue, champ de recherche, étiquette…) :
/// un <see cref="HDevIcon"/> teinté, de la taille d'un jeton Icon*.
/// </summary>
public class HDevIconView : Control
{
    public static readonly StyledProperty<HDevIcon> IconProperty =
        AvaloniaProperty.Register<HDevIconView, HDevIcon>(nameof(Icon));

    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<HDevIconView, double>(nameof(IconSize), HDevTheme.Size.IconMedium);

    /// <summary>Couleur du trait ; non posée, texte secondaire de la palette active (ou « désactivé »)</summary>
    public static readonly StyledProperty<Color?> ColorProperty =
        AvaloniaProperty.Register<HDevIconView, Color?>(nameof(Color));

    public HDevIcon Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public Color? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    static HDevIconView()
    {
        AffectsRender<HDevIconView>(IconProperty, ColorProperty, IsEffectivelyEnabledProperty);
        AffectsMeasure<HDevIconView>(IconSizeProperty);
        // Hauteur fixe : centré dans une rangée plus haute
        VerticalAlignmentProperty.OverrideDefaultValue<HDevIconView>(Avalonia.Layout.VerticalAlignment.Center);
        HorizontalAlignmentProperty.OverrideDefaultValue<HDevIconView>(Avalonia.Layout.HorizontalAlignment.Left);
    }

    protected override Size MeasureOverride(Size availableSize) => new(IconSize, IconSize);

    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);
        var color = !IsEffectivelyEnabled ? HDevTheme.Disabled.Text : Color ?? HDevTheme.Text.Secondary;
        HDevIcons.Draw(context, Icon, new Point(Bounds.Width / 2, Bounds.Height / 2), IconSize, color);
    }
}
