using Avalonia.Media;

namespace ProControls.Theme;

/// <summary>Variante visuelle de la suite</summary>
public enum ProThemeVariant
{
    Light,
    Dark
}

/// <summary>
/// Palette complète d'une variante (les deux instances : Light et Dark)
/// </summary>
public sealed class ProPalette
{
    // Backgrounds
    public Color BackgroundWindow { get; init; }
    public Color BackgroundPanel { get; init; }
    public Color BackgroundPanelAlt { get; init; }
    public Color BackgroundToolbar { get; init; }
    public Color BackgroundStatusBar { get; init; }
    public Color BackgroundControl { get; init; }
    public Color BackgroundControlHover { get; init; }
    public Color BackgroundControlPressed { get; init; }
    public Color BackgroundControlDisabled { get; init; }
    public Color BackgroundControlFocused { get; init; }
    public Color BackgroundSelection { get; init; }
    public Color BackgroundSelectionInactive { get; init; }
    public Color BackgroundSuccess { get; init; }
    public Color BackgroundWarning { get; init; }
    public Color BackgroundError { get; init; }
    public Color BackgroundInfo { get; init; }

    // Borders
    public Color BorderDefault { get; init; }
    public Color BorderHover { get; init; }
    public Color BorderPressed { get; init; }
    public Color BorderFocused { get; init; }
    public Color BorderDisabled { get; init; }
    public Color BorderSubtle { get; init; }
    public Color BorderStrong { get; init; }
    public Color BorderFocusOuter { get; init; }
    public Color BorderFocusInner { get; init; }

    // Text
    public Color TextPrimary { get; init; }
    public Color TextSecondary { get; init; }
    public Color TextTertiary { get; init; }
    public Color TextDisabled { get; init; }
    public Color TextPlaceholder { get; init; }
    public Color TextOnAccent { get; init; }
    public Color TextLink { get; init; }
    public Color TextLinkHover { get; init; }
    public Color TextSuccess { get; init; }
    public Color TextWarning { get; init; }
    public Color TextError { get; init; }

    // Accents
    public Color AccentPrimary { get; init; }
    public Color AccentPrimaryHover { get; init; }
    public Color AccentPrimaryPressed { get; init; }
    public Color AccentSecondary { get; init; }
    public Color AccentSecondaryHover { get; init; }
    public Color AccentSecondaryPressed { get; init; }
    public Color AccentSuccess { get; init; }
    public Color AccentWarning { get; init; }
    public Color AccentError { get; init; }

    // Danger
    public Color DangerDefault { get; init; }
    public Color DangerHover { get; init; }
    public Color DangerPressed { get; init; }
    public Color DangerDisabledBackground { get; init; }
    public Color DangerDisabledBorder { get; init; }
    public Color DangerDisabledText { get; init; }

    // Chrome + disabled génériques
    public Color ChromeCloseHover { get; init; }
    public Color ChromeClosePressed { get; init; }
    public Color DisabledBackground { get; init; }
    public Color DisabledBorder { get; init; }
    public Color DisabledText { get; init; }

    // Ombres
    public Color ShadowColor { get; init; }
    public Color ShadowColorStrong { get; init; }

    /// <summary>Yaru / libadwaita clair (palette d'origine)</summary>
    public static readonly ProPalette Light = new()
    {
        BackgroundWindow = Color.Parse("#FAFAFA"),
        BackgroundPanel = Color.Parse("#FFFFFF"),
        BackgroundPanelAlt = Color.Parse("#F6F5F4"),
        BackgroundToolbar = Color.Parse("#F6F5F4"),
        BackgroundStatusBar = Color.Parse("#F6F5F4"),
        BackgroundControl = Color.Parse("#EDEDEC"),
        BackgroundControlHover = Color.Parse("#E2E2E0"),
        BackgroundControlPressed = Color.Parse("#D4D4D2"),
        BackgroundControlDisabled = Color.Parse("#F4F4F3"),
        BackgroundControlFocused = Color.Parse("#FFFFFF"),
        BackgroundSelection = Color.Parse("#E95420"),
        BackgroundSelectionInactive = Color.Parse("#D9D7D4"),
        BackgroundSuccess = Color.Parse("#DDF2E4"),
        BackgroundWarning = Color.Parse("#F9F0DC"),
        BackgroundError = Color.Parse("#FADEE1"),
        BackgroundInfo = Color.Parse("#DDE8F9"),

        BorderDefault = Color.Parse("#D0CCC8"),
        BorderHover = Color.Parse("#B7B2AD"),
        BorderPressed = Color.Parse("#A39E99"),
        BorderFocused = Color.Parse("#E95420"),
        BorderDisabled = Color.Parse("#E8E6E3"),
        BorderSubtle = Color.Parse("#EBEBEA"),
        BorderStrong = Color.Parse("#9A9996"),
        BorderFocusOuter = Color.Parse("#E95420"),
        BorderFocusInner = Color.Parse("#FFFFFF"),

        TextPrimary = Color.Parse("#333333"),
        TextSecondary = Color.Parse("#5E5C64"),
        TextTertiary = Color.Parse("#77767B"),
        TextDisabled = Color.Parse("#9A9996"),
        TextPlaceholder = Color.Parse("#8B8A8D"),
        TextOnAccent = Color.Parse("#FFFFFF"),
        TextLink = Color.Parse("#1C71D8"),
        TextLinkHover = Color.Parse("#1A5FB4"),
        TextSuccess = Color.Parse("#1B7443"),
        TextWarning = Color.Parse("#9C6E03"),
        TextError = Color.Parse("#C01C28"),

        AccentPrimary = Color.Parse("#E95420"),
        AccentPrimaryHover = Color.Parse("#DB4C1B"),
        AccentPrimaryPressed = Color.Parse("#C64614"),
        AccentSecondary = Color.Parse("#77216F"),
        AccentSecondaryHover = Color.Parse("#883A82"),
        AccentSecondaryPressed = Color.Parse("#5E1A58"),
        AccentSuccess = Color.Parse("#26A269"),
        AccentWarning = Color.Parse("#E5A50A"),
        AccentError = Color.Parse("#C01C28"),

        DangerDefault = Color.Parse("#C01C28"),
        DangerHover = Color.Parse("#A51D2D"),
        DangerPressed = Color.Parse("#8C1A25"),
        DangerDisabledBackground = Color.Parse("#F5E5E6"),
        DangerDisabledBorder = Color.Parse("#E0BFC2"),
        DangerDisabledText = Color.Parse("#B58F94"),

        ChromeCloseHover = Color.Parse("#C01C28"),
        ChromeClosePressed = Color.Parse("#A51D2D"),
        DisabledBackground = Color.Parse("#F1F0EF"),
        DisabledBorder = Color.Parse("#DBD8D5"),
        DisabledText = Color.Parse("#9A9996"),

        ShadowColor = Color.Parse("#1A000000"),
        ShadowColorStrong = Color.Parse("#33000000")
    };

    /// <summary>
    /// Yaru / libadwaita sombre : fonds gris profonds, textes clairs, orange
    /// éclairci pour le contraste, ombres renforcées
    /// </summary>
    public static readonly ProPalette Dark = new()
    {
        BackgroundWindow = Color.Parse("#242424"),
        BackgroundPanel = Color.Parse("#303030"),
        BackgroundPanelAlt = Color.Parse("#2A2A2A"),
        BackgroundToolbar = Color.Parse("#2E2E2E"),
        BackgroundStatusBar = Color.Parse("#2E2E2E"),
        BackgroundControl = Color.Parse("#3A3A3A"),
        BackgroundControlHover = Color.Parse("#464646"),
        BackgroundControlPressed = Color.Parse("#525252"),
        BackgroundControlDisabled = Color.Parse("#333333"),
        BackgroundControlFocused = Color.Parse("#3A3A3A"),
        BackgroundSelection = Color.Parse("#E95420"),
        BackgroundSelectionInactive = Color.Parse("#4A4A4A"),
        BackgroundSuccess = Color.Parse("#26402F"),
        BackgroundWarning = Color.Parse("#453B1E"),
        BackgroundError = Color.Parse("#442527"),
        BackgroundInfo = Color.Parse("#253549"),

        BorderDefault = Color.Parse("#4A4A4A"),
        BorderHover = Color.Parse("#5F5F5F"),
        BorderPressed = Color.Parse("#6F6F6F"),
        BorderFocused = Color.Parse("#F2704B"),
        BorderDisabled = Color.Parse("#3A3A3A"),
        BorderSubtle = Color.Parse("#3C3C3C"),
        BorderStrong = Color.Parse("#707070"),
        BorderFocusOuter = Color.Parse("#F2704B"),
        BorderFocusInner = Color.Parse("#242424"),

        TextPrimary = Color.Parse("#F2F2F2"),
        TextSecondary = Color.Parse("#B8B6BE"),
        TextTertiary = Color.Parse("#9A99A0"),
        TextDisabled = Color.Parse("#6F6E73"),
        TextPlaceholder = Color.Parse("#85848A"),
        TextOnAccent = Color.Parse("#FFFFFF"),
        TextLink = Color.Parse("#62A0EA"),
        TextLinkHover = Color.Parse("#99C1F1"),
        TextSuccess = Color.Parse("#6BDD9C"),
        TextWarning = Color.Parse("#E5C55A"),
        TextError = Color.Parse("#FF7B63"),

        AccentPrimary = Color.Parse("#E95420"),
        AccentPrimaryHover = Color.Parse("#F2704B"),      // éclairci (fond sombre)
        AccentPrimaryPressed = Color.Parse("#D14B1D"),
        AccentSecondary = Color.Parse("#A855A0"),          // aubergine éclaircie
        AccentSecondaryHover = Color.Parse("#B96CB2"),
        AccentSecondaryPressed = Color.Parse("#96488F"),
        AccentSuccess = Color.Parse("#33D17A"),
        AccentWarning = Color.Parse("#F6D32D"),
        AccentError = Color.Parse("#F66151"),

        DangerDefault = Color.Parse("#C01C28"),
        DangerHover = Color.Parse("#D62F3C"),              // éclairci au survol
        DangerPressed = Color.Parse("#A51D2D"),
        DangerDisabledBackground = Color.Parse("#43282A"),
        DangerDisabledBorder = Color.Parse("#5C3A3D"),
        DangerDisabledText = Color.Parse("#8F6B6E"),

        ChromeCloseHover = Color.Parse("#C01C28"),
        ChromeClosePressed = Color.Parse("#A51D2D"),
        DisabledBackground = Color.Parse("#333333"),
        DisabledBorder = Color.Parse("#454545"),
        DisabledText = Color.Parse("#6F6E73"),

        ShadowColor = Color.Parse("#40000000"),
        ShadowColorStrong = Color.Parse("#66000000")
    };
}

/// <summary>
/// Design tokens Ubuntu 26.04 (Yaru / libadwaita) — variantes claire et
/// sombre commutables à chaud. L'API historique (ProTheme.Background.Panel…)
/// est inchangée : les propriétés délèguent à la palette active.
/// </summary>
public static class ProTheme
{
    private static ProPalette _current = ProPalette.Light;
    private static ProThemeVariant _variant = ProThemeVariant.Light;

    // Palettes injectées par l'app hôte (null = défaut Yaru pour la variante).
    private static ProPalette? _lightOverride;
    private static ProPalette? _darkOverride;

    /// <summary>Palette active</summary>
    public static ProPalette Current => _current;

    /// <summary>Déclenché après un changement de variante (rebrush/relayout)</summary>
    public static event EventHandler? VariantChanged;

    /// <summary>Variante active — la changer rebrande toute la suite</summary>
    public static ProThemeVariant Variant
    {
        get => _variant;
        set
        {
            if (_variant == value) return;
            _variant = value;
            _current = Resolve(value);
            VariantChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Substitue des palettes personnalisées à la palette Yaru par défaut. Passer
    /// <c>null</c> pour une variante conserve le défaut d'origine. L'effet est
    /// immédiat : la palette active est recalculée et <see cref="VariantChanged"/>
    /// est levé pour re-brosser la suite, même quand la variante ne change pas
    /// (cas d'une injection au démarrage, la variante restant Light).
    /// </summary>
    public static void SetPalettes(ProPalette? light = null, ProPalette? dark = null)
    {
        _lightOverride = light;
        _darkOverride = dark;
        _current = Resolve(_variant);
        VariantChanged?.Invoke(null, EventArgs.Empty);
    }

    private static ProPalette Resolve(ProThemeVariant variant)
        => variant == ProThemeVariant.Dark
            ? _darkOverride ?? ProPalette.Dark
            : _lightOverride ?? ProPalette.Light;

    public static bool IsDark => _variant == ProThemeVariant.Dark;

    // ═══════════════════════════════════════════════════════════════
    // TOKENS (délégation à la palette active — API historique intacte)
    // ═══════════════════════════════════════════════════════════════

    public static class Background
    {
        public static Color Window => _current.BackgroundWindow;
        public static Color Panel => _current.BackgroundPanel;
        public static Color PanelAlt => _current.BackgroundPanelAlt;
        public static Color Toolbar => _current.BackgroundToolbar;
        public static Color StatusBar => _current.BackgroundStatusBar;
        public static Color Control => _current.BackgroundControl;
        public static Color ControlHover => _current.BackgroundControlHover;
        public static Color ControlPressed => _current.BackgroundControlPressed;
        public static Color ControlDisabled => _current.BackgroundControlDisabled;
        public static Color ControlFocused => _current.BackgroundControlFocused;
        public static Color Selection => _current.BackgroundSelection;
        public static Color SelectionInactive => _current.BackgroundSelectionInactive;
        public static Color Success => _current.BackgroundSuccess;
        public static Color Warning => _current.BackgroundWarning;
        public static Color Error => _current.BackgroundError;
        public static Color Info => _current.BackgroundInfo;
    }

    public static class Border
    {
        public static Color Default => _current.BorderDefault;
        public static Color Hover => _current.BorderHover;
        public static Color Pressed => _current.BorderPressed;
        public static Color Focused => _current.BorderFocused;
        public static Color Disabled => _current.BorderDisabled;
        public static Color Subtle => _current.BorderSubtle;
        public static Color Strong => _current.BorderStrong;
        public static Color FocusOuter => _current.BorderFocusOuter;
        public static Color FocusInner => _current.BorderFocusInner;
    }

    public static class Text
    {
        public static Color Primary => _current.TextPrimary;
        public static Color Secondary => _current.TextSecondary;
        public static Color Tertiary => _current.TextTertiary;
        public static Color Disabled => _current.TextDisabled;
        public static Color Placeholder => _current.TextPlaceholder;
        public static Color OnAccent => _current.TextOnAccent;
        public static Color Link => _current.TextLink;
        public static Color LinkHover => _current.TextLinkHover;
        public static Color Success => _current.TextSuccess;
        public static Color Warning => _current.TextWarning;
        public static Color Error => _current.TextError;
    }

    public static class Accent
    {
        public static Color Primary => _current.AccentPrimary;
        public static Color PrimaryHover => _current.AccentPrimaryHover;
        public static Color PrimaryPressed => _current.AccentPrimaryPressed;
        public static Color Secondary => _current.AccentSecondary;
        public static Color SecondaryHover => _current.AccentSecondaryHover;
        public static Color SecondaryPressed => _current.AccentSecondaryPressed;
        public static Color Success => _current.AccentSuccess;
        public static Color Warning => _current.AccentWarning;
        public static Color Error => _current.AccentError;
    }

    public static class Danger
    {
        public static Color Default => _current.DangerDefault;
        public static Color Hover => _current.DangerHover;
        public static Color Pressed => _current.DangerPressed;
        public static Color DisabledBackground => _current.DangerDisabledBackground;
        public static Color DisabledBorder => _current.DangerDisabledBorder;
        public static Color DisabledText => _current.DangerDisabledText;
    }

    public static class WindowChrome
    {
        public static Color CloseHover => _current.ChromeCloseHover;
        public static Color ClosePressed => _current.ChromeClosePressed;
    }

    public static class Disabled
    {
        public static Color Background => _current.DisabledBackground;
        public static Color Border => _current.DisabledBorder;
        public static Color Text => _current.DisabledText;
    }

    public static class Shadow
    {
        public static Color Color => _current.ShadowColor;
        public static Color ColorStrong => _current.ShadowColorStrong;

        // Paramètres d'ombre (offsetX, offsetY, blur, spread)
        public static readonly (double, double, double, double) Subtle = (0, 1, 2, 0);
        public static readonly (double, double, double, double) Medium = (0, 2, 6, 0);
        public static readonly (double, double, double, double) Strong = (0, 4, 12, 0);
        public static readonly (double, double, double, double) Elevated = (0, 8, 24, 0);
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
