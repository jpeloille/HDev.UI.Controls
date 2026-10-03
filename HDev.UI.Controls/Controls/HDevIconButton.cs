using Avalonia;
using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

/// <summary>
/// Bouton carré à icône vectorielle (page précédente / suivante…) : mêmes variantes,
/// tailles et événement <c>Click</c> que <see cref="HDevButton"/>, sans libellé.
/// Le côté vaut la hauteur de la taille (26 / 34 / 42, sans la marge d'ombre du
/// bouton texte) ; nommer l'action par une infobulle (<c>ToolTip.Tip</c>).
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
    }

    public HDevIconButton()
    {
        Text = "";
        Variant = ButtonVariant.Outline;
    }

    private double Side => Size switch
    {
        ButtonSize.Small => HDevTheme.Size.ControlHeightSmall,
        ButtonSize.Large => HDevTheme.Size.ControlHeightLarge,
        _ => HDevTheme.Size.ControlHeightMedium
    };

    protected override Size MeasureOverride(Size availableSize) => new(Side, Side);

    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);
        var rect = new Rect(Bounds.Size);
        var radius = HDevTheme.Size.CornerRadiusMedium;
        var (background, border, foreground) = ResolveColors();

        context.DrawRectangle(new SolidColorBrush(background), null, rect, radius, radius);
        context.DrawRectangle(null, new Pen(new SolidColorBrush(border), 1.0), rect.Deflate(0.5), radius, radius);

        if (IsFocused && IsEnabled)
            DrawFocusRing(context, rect, radius);

        var offset = IsPressed ? new Vector(0, 0.5) : default;
        HDevEditorGlyphButton.DrawIcon(context, Icon, rect.Center + offset, foreground);
    }
}
