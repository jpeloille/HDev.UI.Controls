using Avalonia.Media;

namespace ProControls.Theme;

/// <summary>
/// Design tokens Ubuntu 26.04 (Yaru / libadwaita, variante claire)
/// Toutes les couleurs, dimensions et constantes visuelles
/// </summary>
public static class ProTheme
{
    // ═══════════════════════════════════════════════════════════════
    // COULEURS DE FOND
    // ═══════════════════════════════════════════════════════════════

    public static class Background
    {
        public static readonly Color Window = Color.Parse("#FAFAFA");
        public static readonly Color Panel = Color.Parse("#FFFFFF");
        public static readonly Color PanelAlt = Color.Parse("#F6F5F4");
        public static readonly Color Toolbar = Color.Parse("#F6F5F4");   // Headerbar GNOME clair
        public static readonly Color StatusBar = Color.Parse("#F6F5F4");

        // Contrôles (boutons plats libadwaita : noir à ~8/12/17 % sur blanc)
        public static readonly Color Control = Color.Parse("#EDEDEC");
        public static readonly Color ControlHover = Color.Parse("#E2E2E0");
        public static readonly Color ControlPressed = Color.Parse("#D4D4D2");
        public static readonly Color ControlDisabled = Color.Parse("#F4F4F3");
        public static readonly Color ControlFocused = Color.Parse("#FFFFFF");

        // Sélection
        public static readonly Color Selection = Color.Parse("#E95420");        // Orange Ubuntu
        public static readonly Color SelectionInactive = Color.Parse("#D9D7D4");

        // États spéciaux (teintes adw claires)
        public static readonly Color Success = Color.Parse("#DDF2E4");
        public static readonly Color Warning = Color.Parse("#F9F0DC");
        public static readonly Color Error = Color.Parse("#FADEE1");
        public static readonly Color Info = Color.Parse("#DDE8F9");
    }

    // ═══════════════════════════════════════════════════════════════
    // COULEURS DE BORDURE
    // ═══════════════════════════════════════════════════════════════

    public static class Border
    {
        public static readonly Color Default = Color.Parse("#D0CCC8");
        public static readonly Color Hover = Color.Parse("#B7B2AD");
        public static readonly Color Pressed = Color.Parse("#A39E99");
        public static readonly Color Focused = Color.Parse("#E95420");
        public static readonly Color Disabled = Color.Parse("#E8E6E3");

        public static readonly Color Subtle = Color.Parse("#EBEBEA");
        public static readonly Color Strong = Color.Parse("#9A9996");

        // Focus ring
        public static readonly Color FocusOuter = Color.Parse("#E95420");
        public static readonly Color FocusInner = Color.Parse("#FFFFFF");
    }

    // ═══════════════════════════════════════════════════════════════
    // COULEURS DE TEXTE
    // ═══════════════════════════════════════════════════════════════

    public static class Text
    {
        public static readonly Color Primary = Color.Parse("#333333");
        public static readonly Color Secondary = Color.Parse("#5E5C64");
        public static readonly Color Tertiary = Color.Parse("#77767B");
        public static readonly Color Disabled = Color.Parse("#9A9996");
        public static readonly Color Placeholder = Color.Parse("#8B8A8D");

        public static readonly Color OnAccent = Color.Parse("#FFFFFF");
        public static readonly Color Link = Color.Parse("#1C71D8");
        public static readonly Color LinkHover = Color.Parse("#1A5FB4");

        public static readonly Color Success = Color.Parse("#1B7443");
        public static readonly Color Warning = Color.Parse("#9C6E03");
        public static readonly Color Error = Color.Parse("#C01C28");
    }

    // ═══════════════════════════════════════════════════════════════
    // COULEURS D'ACCENT
    // ═══════════════════════════════════════════════════════════════

    public static class Accent
    {
        public static readonly Color Primary = Color.Parse("#E95420");        // Orange Ubuntu
        public static readonly Color PrimaryHover = Color.Parse("#DB4C1B");
        public static readonly Color PrimaryPressed = Color.Parse("#C64614");

        public static readonly Color Secondary = Color.Parse("#77216F");      // Aubergine Ubuntu
        public static readonly Color SecondaryHover = Color.Parse("#883A82");
        public static readonly Color SecondaryPressed = Color.Parse("#5E1A58");

        public static readonly Color Success = Color.Parse("#26A269");
        public static readonly Color Warning = Color.Parse("#E5A50A");
        public static readonly Color Error = Color.Parse("#C01C28");
    }

    // ═══════════════════════════════════════════════════════════════
    // COULEURS DANGER (BOUTONS DESTRUCTIFS)
    // ═══════════════════════════════════════════════════════════════

    public static class Danger
    {
        public static readonly Color Default = Color.Parse("#C01C28");
        public static readonly Color Hover = Color.Parse("#A51D2D");
        public static readonly Color Pressed = Color.Parse("#8C1A25");

        // États disabled
        public static readonly Color DisabledBackground = Color.Parse("#F5E5E6");
        public static readonly Color DisabledBorder = Color.Parse("#E0BFC2");
        public static readonly Color DisabledText = Color.Parse("#B58F94");
    }

    // ═══════════════════════════════════════════════════════════════
    // CHROME DE FENÊTRE
    // ═══════════════════════════════════════════════════════════════

    public static class WindowChrome
    {
        public static readonly Color CloseHover = Color.Parse("#C01C28");
        public static readonly Color ClosePressed = Color.Parse("#A51D2D");
    }

    // ═══════════════════════════════════════════════════════════════
    // ÉTATS DISABLED GÉNÉRIQUES
    // ═══════════════════════════════════════════════════════════════

    public static class Disabled
    {
        public static readonly Color Background = Color.Parse("#F1F0EF");
        public static readonly Color Border = Color.Parse("#DBD8D5");
        public static readonly Color Text = Color.Parse("#9A9996");
    }

    // ═══════════════════════════════════════════════════════════════
    // DIMENSIONS
    // ═══════════════════════════════════════════════════════════════

    public static class Size
    {
        // Hauteurs de contrôles
        public const double ControlHeightSmall = 26;
        public const double ControlHeightMedium = 34;
        public const double ControlHeightLarge = 42;

        // Rayons de coins (libadwaita : boutons 6, popovers 8, cartes 12)
        public const double CornerRadiusSmall = 6;
        public const double CornerRadiusMedium = 8;
        public const double CornerRadiusLarge = 12;

        // Épaisseurs de bordure
        public const double BorderThin = 1;
        public const double BorderMedium = 1.5;
        public const double BorderThick = 2;

        // Espacements
        public const double SpacingXS = 4;
        public const double SpacingS = 8;
        public const double SpacingM = 12;
        public const double SpacingL = 16;
        public const double SpacingXL = 24;

        // Paddings internes
        public const double PaddingXS = 4;
        public const double PaddingS = 8;
        public const double PaddingM = 12;
        public const double PaddingL = 16;

        // Icônes
        public const double IconSmall = 12;
        public const double IconMedium = 16;
        public const double IconLarge = 20;
    }

    // ═══════════════════════════════════════════════════════════════
    // TYPOGRAPHIE
    // ═══════════════════════════════════════════════════════════════

    public static class Typography
    {
        // Inter embarquée (Avalonia.Fonts.Inter, activée par .WithInterFont()
        // dans l'app hôte) : rasterise proprement dans Skia SANS hinting, là où
        // les fontes Ubuntu du système — désormais variables — sortent maigres
        // et sales à 14 px (comparaison A/B validée par Julien le 14/07/2026).
        // Rendu identique sur toute machine. Fallback Ubuntu si Inter absente.
        public static readonly string FontFamily =
            "fonts:Inter#Inter, Inter, Ubuntu Sans, Ubuntu, Segoe UI, sans-serif";

        public const double FontSizeCaption = 12;
        public const double FontSizeBody = 14;     // Ubuntu Sans 11 pt ≈ 14,7 px à 96 dpi
        public const double FontSizeSubtitle = 16;
        public const double FontSizeTitle = 19;
        public const double FontSizeHeader = 24;

        public static readonly FontWeight WeightLight = FontWeight.Light;
        public static readonly FontWeight WeightRegular = FontWeight.Regular;
        public static readonly FontWeight WeightMedium = FontWeight.Medium;
        public static readonly FontWeight WeightSemiBold = FontWeight.SemiBold;
        public static readonly FontWeight WeightBold = FontWeight.Bold;
    }

    // ═══════════════════════════════════════════════════════════════
    // OMBRES
    // ═══════════════════════════════════════════════════════════════

    public static class Shadow
    {
        public static readonly Color Color = Color.Parse("#1A000000");
        public static readonly Color ColorStrong = Color.Parse("#33000000");

        // Paramètres d'ombre (offsetX, offsetY, blur, spread)
        public static readonly (double, double, double, double) Subtle = (0, 1, 2, 0);
        public static readonly (double, double, double, double) Medium = (0, 2, 6, 0);
        public static readonly (double, double, double, double) Strong = (0, 4, 12, 0);
        public static readonly (double, double, double, double) Elevated = (0, 8, 24, 0);
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    public static SolidColorBrush ToBrush(Color color) => new(color);

    public static Pen ToPen(Color color, double thickness = 1)
        => new(new SolidColorBrush(color), thickness);

    public static Color WithOpacity(Color color, byte opacity)
        => Color.FromArgb(opacity, color.R, color.G, color.B);

    public static Color Lerp(Color a, Color b, double t)
    {
        return Color.FromArgb(
            (byte)(a.A + (b.A - a.A) * t),
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t));
    }
}
