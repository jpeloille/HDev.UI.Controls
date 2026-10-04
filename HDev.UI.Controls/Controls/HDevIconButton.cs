using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

/// <summary>
/// Bouton carré à icône vectorielle (page précédente / suivante…) : mêmes variantes,
/// tailles et événement <c>Click</c> que <see cref="HDevButton"/>, sans libellé.
/// Le côté vaut la hauteur de la taille (28 / 34 / 42) et le glyphe grandit avec
/// elle ; nommer l'action par une infobulle (<c>ToolTip.Tip</c>), reprise comme nom
/// accessible.
/// </summary>
public class HDevIconButton : HDevButton
{
    public static readonly StyledProperty<HDevEditorIcon> IconProperty =
        AvaloniaProperty.Register<HDevIconButton, HDevEditorIcon>(nameof(Icon), HDevEditorIcon.None);

    public HDevEditorIcon Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    static HDevIconButton()
    {
        AffectsRender<HDevIconButton>(IconProperty);

        // Carré : pas plus étiré en largeur qu'en hauteur (centrage vertical hérité)
        HorizontalAlignmentProperty.OverrideDefaultValue<HDevIconButton>(HorizontalAlignment.Left);
    }

    public HDevIconButton()
    {
        Text = "";
        Variant = ButtonVariant.Outline;
    }

    private double Side => Metrics.Height;

    /// <summary>Glyphe de 8 px en Small, agrandi en Medium et Large avec son trait</summary>
    private double GlyphScale => Size switch
    {
        ButtonSize.Small => 1.0,
        ButtonSize.Large => 1.5,
        _ => 1.25
    };

    protected override Size MeasureOverride(Size availableSize) => new(Side, Side);

    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);
        var width = double.IsNaN(Width) ? Math.Min(Side, Bounds.Width) : Bounds.Width;
        var rect = FaceRect(width);
        var radius = HDevTheme.Size.CornerRadiusSmall;
        var (background, border, foreground) = ResolveColors();

        context.DrawRectangle(new SolidColorBrush(background), null, rect, radius, radius);
        context.DrawRectangle(null, new Pen(new SolidColorBrush(border), 1.0), rect.Deflate(0.5), radius, radius);

        if (IsFocusVisible && IsEnabled)
            DrawFocusRing(context, rect, radius);

        var offset = IsPressed ? new Vector(0, 0.5) : default;
        HDevEditorGlyphButton.DrawIcon(context, Icon, rect.Center + offset, foreground, GlyphScale);
    }
}
