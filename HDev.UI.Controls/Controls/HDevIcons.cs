using Avalonia;
using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

/// <summary>
/// Jeu de glyphes vectoriels de la suite : un seul trait (1,2 px), une seule grille,
/// teintés par le contrôle hôte (texte normal, secondaire, désactivé…). Remplace les
/// caractères Unicode et emoji, dont le rendu dépend de la police du système.
/// </summary>
public enum HDevIcon
{
    /// <summary>Pas d'icône</summary>
    None,
    Calendar,
    ChevronDown,
    ChevronUp,
    ChevronLeft,
    ChevronRight,
    Close,
    Check,
    Search,
    Plus,
    Minus,
    More,
    Info,
    Warning,
    Error,
    Question,
    Trash,
    Pencil,
    Link,
    Indent,
    Outdent,
    Recurrence,
    Image,
    Pin,
    ArrowUp,
    ArrowDown
}

public static class HDevIcons
{
    // Glyphes dessinés à la main (traits nets calés sur le pixel), conçus pour une boîte de 12 px
    private static bool IsLegacy(HDevIcon icon) => icon is HDevIcon.Calendar
        or HDevIcon.ChevronDown or HDevIcon.ChevronUp or HDevIcon.ChevronLeft or HDevIcon.ChevronRight;

    /// <summary>Taille d'origine du glyphe : 12 px pour les chevrons et le calendrier, 16 px pour les autres</summary>
    public static double NaturalSize(HDevIcon icon) => IsLegacy(icon) ? 12 : 16;

    // Les autres glyphes : grille 16×16, traits (SVG path data) et pastilles pleines (centre, rayon)
    private sealed record Shape(string Strokes, (double X, double Y, double R)[]? Dots = null);

    private static readonly Dictionary<HDevIcon, Lazy<(Geometry Strokes, (double X, double Y, double R)[] Dots)>> Shapes = Build(new()
    {
        [HDevIcon.Close] = new("M4 4 L12 12 M12 4 L4 12"),
        [HDevIcon.Check] = new("M3.5 8.5 L6.5 11.5 L12.5 4.5"),
        [HDevIcon.Plus] = new("M8 3 V13 M3 8 H13"),
        [HDevIcon.Minus] = new("M3 8 H13"),
        [HDevIcon.More] = new("", new[] { (3.5, 8.0, 1.25), (8.0, 8.0, 1.25), (12.5, 8.0, 1.25) }),
        [HDevIcon.Search] = new("M11.5 7 A4.5 4.5 0 1 1 2.5 7 A4.5 4.5 0 1 1 11.5 7 M10.3 10.3 L13.5 13.5"),
        [HDevIcon.Info] = new("M14 8 A6 6 0 1 1 2 8 A6 6 0 1 1 14 8 M8 7.4 V11", new[] { (8.0, 5.2, 0.85) }),
        [HDevIcon.Warning] = new("M8 2.6 L14.2 13.2 H1.8 Z M8 6.6 V9.6", new[] { (8.0, 11.4, 0.85) }),
        [HDevIcon.Error] = new("M14 8 A6 6 0 1 1 2 8 A6 6 0 1 1 14 8 M5.9 5.9 L10.1 10.1 M10.1 5.9 L5.9 10.1"),
        [HDevIcon.Question] = new("M14 8 A6 6 0 1 1 2 8 A6 6 0 1 1 14 8 M6.2 6.4 C6.2 5.2 7 4.7 8 4.7 C9 4.7 9.8 5.3 9.8 6.3 C9.8 7.5 8 7.5 8 9",
            new[] { (8.0, 11.0, 0.85) }),
        [HDevIcon.Trash] = new("M3 4.5 H13 M6.5 4.5 V3 H9.5 V4.5 M4.6 4.5 L5.3 13 H10.7 L11.4 4.5 M7 7 V10.8 M9 7 V10.8"),
        [HDevIcon.Pencil] = new("M3 13 L3.7 10.4 L10.9 3.2 L12.8 5.1 L5.6 12.3 Z M9.6 4.5 L11.5 6.4"),
        [HDevIcon.Link] = new("M6.6 9.4 L9.4 6.6 M7.4 4.9 L8.5 3.8 A2.6 2.6 0 0 1 12.2 7.5 L11.1 8.6 M8.6 11.1 L7.5 12.2 A2.6 2.6 0 0 1 3.8 8.5 L4.9 7.4"),
        [HDevIcon.Indent] = new("M3 3.5 H13 M8 6.5 H13 M8 9.5 H13 M3 12.5 H13 M3 6 L5.5 8 L3 10"),
        [HDevIcon.Outdent] = new("M3 3.5 H13 M8 6.5 H13 M8 9.5 H13 M3 12.5 H13 M5.5 6 L3 8 L5.5 10"),
        [HDevIcon.Recurrence] = new("M12.6 8 A4.6 4.6 0 1 1 10.7 4.3 M11 2 V4.7 H8.3"),
        [HDevIcon.Image] = new("M2.5 3.5 H13.5 V12.5 H2.5 Z M2.5 11.5 L6.3 8 L8.8 10.1 L10.8 8.5 L13.5 11", new[] { (5.4, 6.2, 1.05) }),
        [HDevIcon.Pin] = new("M8 14 C8 14 3.5 9.6 3.5 6.5 A4.5 4.5 0 0 1 12.5 6.5 C12.5 9.6 8 14 8 14 Z M9.5 6.5 A1.5 1.5 0 1 1 6.5 6.5 A1.5 1.5 0 1 1 9.5 6.5"),
        [HDevIcon.ArrowUp] = new("M8 13 V3 M4 7 L8 3 L12 7"),
        [HDevIcon.ArrowDown] = new("M8 3 V13 M4 9 L8 13 L12 9"),
    });

    private static Dictionary<HDevIcon, Lazy<(Geometry, (double, double, double)[])>> Build(Dictionary<HDevIcon, Shape> shapes)
        => shapes.ToDictionary(
            kv => kv.Key,
            kv => new Lazy<(Geometry, (double, double, double)[])>(() =>
                (string.IsNullOrEmpty(kv.Value.Strokes) ? new StreamGeometry() : Geometry.Parse(kv.Value.Strokes),
                 kv.Value.Dots ?? Array.Empty<(double, double, double)>())));

    /// <summary>
    /// Dessine <paramref name="icon"/> centré sur <paramref name="center"/>, dans une boîte de
    /// <paramref name="size"/> px (12, 16 ou 20 : <see cref="HDevTheme.Size.IconSmall"/>…).
    /// </summary>
    public static void Draw(DrawingContext context, HDevIcon icon, Point center, double size, Color color)
    {
        if (icon == HDevIcon.None) return;
        if (IsLegacy(icon))
        {
            DrawLegacy(context, icon, center, color, size / 12.0);
            return;
        }

        if (!Shapes.TryGetValue(icon, out var lazy)) return;
        var (strokes, dots) = lazy.Value;

        var scale = size / 16.0;
        var brush = new SolidColorBrush(color);
        // Trait constant (1,2 px) quelle que soit la taille : l'épaisseur est compensée par l'échelle
        var pen = new Pen(brush, HDevTheme.Size.StrokeGlyph / scale, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);

        var c = Crisp.Snap(center);
        using var transform = context.PushTransform(
            Matrix.CreateTranslation(-8, -8) * Matrix.CreateScale(scale, scale)
            * Matrix.CreateTranslation(c.X + 0.5, c.Y + 0.5));

        context.DrawGeometry(null, pen, strokes);
        foreach (var (x, y, r) in dots)
            context.DrawEllipse(brush, null, new Point(x, y), r, r);
    }

    // Chevrons et calendrier : traits de 1,2 px calés sur le demi-pixel (même graisse que HDevSpinButtons)
    private static void DrawLegacy(DrawingContext context, HDevIcon icon, Point center, Color color, double scale)
    {
        var pen = new Pen(new SolidColorBrush(color), HDevTheme.Size.StrokeGlyph);
        var c = Crisp.Snap(center);
        double cx = c.X + 0.5, cy = c.Y + 0.5;
        using var zoom = context.PushTransform(Matrix.CreateTranslation(-cx, -cy)
            * Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(cx, cy));

        switch (icon)
        {
            case HDevIcon.ChevronDown:
                context.DrawLine(pen, new Point(cx - 4, cy - 2), new Point(cx, cy + 2));
                context.DrawLine(pen, new Point(cx, cy + 2), new Point(cx + 4, cy - 2));
                break;

            case HDevIcon.ChevronUp:
                context.DrawLine(pen, new Point(cx - 4, cy + 2), new Point(cx, cy - 2));
                context.DrawLine(pen, new Point(cx, cy - 2), new Point(cx + 4, cy + 2));
                break;

            case HDevIcon.ChevronLeft:
                context.DrawLine(pen, new Point(cx + 2, cy - 4), new Point(cx - 2, cy));
                context.DrawLine(pen, new Point(cx - 2, cy), new Point(cx + 2, cy + 4));
                break;

            case HDevIcon.ChevronRight:
                context.DrawLine(pen, new Point(cx - 2, cy - 4), new Point(cx + 2, cy));
                context.DrawLine(pen, new Point(cx + 2, cy), new Point(cx - 2, cy + 4));
                break;

            case HDevIcon.Calendar:
                // Cadre 12×11, bandeau d'en-tête, deux anneaux
                var frame = new Rect(cx - 6, cy - 5, 12, 11);
                context.DrawRectangle(null, pen, frame, 1.5, 1.5);
                context.DrawLine(pen, new Point(frame.Left, cy - 2), new Point(frame.Right, cy - 2));
                context.DrawLine(pen, new Point(cx - 3, cy - 7), new Point(cx - 3, cy - 4));
                context.DrawLine(pen, new Point(cx + 3, cy - 7), new Point(cx + 3, cy - 4));
                break;
        }
    }
}
