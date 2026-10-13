using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace HDev.UI.Controls.Theme;

/// <summary>Variante visuelle de la suite</summary>
public enum HDevThemeVariant
{
    Light,
    Dark
}

/// <summary>
/// Palette complète d'une variante (les deux instances : Light et Dark). Record : une
/// palette hôte se dérive d'une palette existante, <c>HDevPalette.Light with { … }</c>
/// </summary>
public sealed partial record HDevPalette
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

    // Tokens à contraste garanti. Facultatifs : une palette qui ne les fixe pas (palette
    // hôte antérieure à leur création) les voit dérivés de ses propres couleurs — jamais
    // de l'orange Yaru (cf. HDevTheme.Accent.Fill)
    public Color? AccentFill { get; init; }
    public Color? AccentFillHover { get; init; }
    public Color? AccentFillPressed { get; init; }
    public Color? AccentInfo { get; init; }
    public Color? AccentSuccessFill { get; init; }
    public Color? AccentInfoFill { get; init; }
    public Color? TextOnWarning { get; init; }
    public Color? BackgroundTextSelection { get; init; }

    /// <summary>Yaru / libadwaita clair (palette d'origine)</summary>
    public static readonly HDevPalette Light = new()
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
        TextTertiary = Color.Parse("#706F74"),       // 4,8:1 sur Window (#77767B : 4,3)
        TextDisabled = Color.Parse("#9A9996"),
        TextPlaceholder = Color.Parse("#8B8A8D"),
        TextOnAccent = Color.Parse("#FFFFFF"),
        TextLink = Color.Parse("#1965C1"),           // 4,6:1 sur Background.Info (#1C71D8 : 3,9)
        TextLinkHover = Color.Parse("#1A5FB4"),
        TextSuccess = Color.Parse("#1B7443"),
        TextWarning = Color.Parse("#8A6203"),        // 4,8:1 sur Background.Warning (#9C6E03 : 4,0)
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
        ShadowColorStrong = Color.Parse("#33000000"),

        // Orange assombri sous le texte blanc : 4,9 / 5,8 / 6,4:1 (#E95420 n'en fait que 3,65)
        AccentFill = Color.Parse("#C64614"),
        AccentFillHover = Color.Parse("#B53C12"),
        AccentFillPressed = Color.Parse("#A93811"),
        AccentInfo = Color.Parse("#1C71D8"),
        AccentSuccessFill = Color.Parse("#1E8254"),
        AccentInfoFill = Color.Parse("#1C71D8"),
        TextOnWarning = Color.Parse("#333333"),
        BackgroundTextSelection = Color.Parse("#F8CCBC")
    };

    /// <summary>
    /// Yaru / libadwaita sombre : fonds gris profonds, textes clairs, orange
    /// éclairci pour le contraste, ombres renforcées
    /// </summary>
    public static readonly HDevPalette Dark = new()
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
        ShadowColorStrong = Color.Parse("#66000000"),

        // Le survol éclaircit en sombre, sans repasser sous 4,5:1 (4,9 / 4,7 / 5,8:1)
        AccentFill = Color.Parse("#C64614"),
        AccentFillHover = Color.Parse("#CD4414"),
        AccentFillPressed = Color.Parse("#B53C12"),
        AccentInfo = Color.Parse("#62A0EA"),
        AccentSuccessFill = Color.Parse("#1E8254"),
        AccentInfoFill = Color.Parse("#1C71D8"),
        TextOnWarning = Color.Parse("#333333"),
        BackgroundTextSelection = Color.Parse("#7A3E2A")
    };
}

/// <summary>
/// Design tokens Ubuntu 26.04 (Yaru / libadwaita) — variantes claire et
/// sombre commutables à chaud. L'API historique (HDevTheme.Background.Panel…)
/// est inchangée : les propriétés délèguent à la palette active.
/// </summary>
public static class HDevTheme
{
    private static HDevPalette _current = HDevPalette.Light;
    private static HDevThemeVariant _variant = HDevThemeVariant.Light;

    // Palettes injectées par l'app hôte (null = défaut Yaru pour la variante).
    private static HDevPalette? _lightOverride;
    private static HDevPalette? _darkOverride;

    // Thème du catalogue appliqué (null = mode historique Variant/SetPalettes).
    private static HDevThemeDefinition? _theme;

    /// <summary>Palette active</summary>
    public static HDevPalette Current => _current;

    /// <summary>
    /// Thème du catalogue actif, ou null si la suite est pilotée par
    /// <see cref="Variant"/> / <see cref="SetPalettes"/> (comportement historique)
    /// </summary>
    public static HDevThemeDefinition? Theme => _theme;

    /// <summary>
    /// Variante Avalonia à poser sur les fenêtres : celle du thème actif, sinon
    /// Light/Dark selon <see cref="Variant"/>
    /// </summary>
    public static Avalonia.Styling.ThemeVariant AvaloniaVariant
        => _theme?.AvaloniaVariant
           ?? (IsDark ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light);

    /// <summary>
    /// Applique un thème du catalogue (<see cref="HDevThemeDefinition.All"/>) :
    /// palette, variante et skin Avalonia d'un seul geste, avec un seul
    /// <see cref="VariantChanged"/> pour re-brosser la suite
    /// </summary>
    public static void Apply(HDevThemeDefinition theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        _theme = theme;
        _variant = theme.Variant;
        _current = theme.Palette;
        VariantChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>Déclenché après un changement de variante (rebrush/relayout)</summary>
    public static event EventHandler? VariantChanged;

    /// <summary>
    /// Variante active — la changer rebrande toute la suite. L'affecter quitte le thème
    /// du catalogue éventuellement appliqué, même à variante égale (Ardoise → Light =
    /// retour à la palette claire Yaru ou injectée)
    /// </summary>
    public static HDevThemeVariant Variant
    {
        get => _variant;
        set
        {
            if (_variant == value && _theme == null) return;
            _theme = null; // retour au mode historique (palettes Yaru ou injectées)
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
    public static void SetPalettes(HDevPalette? light = null, HDevPalette? dark = null)
    {
        _theme = null;
        _lightOverride = light;
        _darkOverride = dark;
        _current = Resolve(_variant);
        VariantChanged?.Invoke(null, EventArgs.Empty);
    }

    private static HDevPalette Resolve(HDevThemeVariant variant)
        => variant == HDevThemeVariant.Dark
            ? _darkOverride ?? HDevPalette.Dark
            : _lightOverride ?? HDevPalette.Light;

    public static bool IsDark => _variant == HDevThemeVariant.Dark;

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

        /// <summary>
        /// Fond des champs de saisie : celui du panneau (blanc en clair), comme
        /// les entrées GTK/libadwaita. Le gris <see cref="Control"/> reste
        /// réservé aux boutons et aux listes déroulantes non éditables.
        /// </summary>
        public static Color Input => _current.BackgroundPanel;

        /// <summary>
        /// Surlignage du texte sélectionné dans un champ, sous <see cref="Text.Primary"/>
        /// (jamais de texte blanc : sur une teinte claire, il devient illisible)
        /// </summary>
        public static Color TextSelection => Derived.TextSelection;
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

        /// <summary>Texte posé sur <see cref="Accent.Warning"/> (foncé : l'ambre ne porte pas le blanc)</summary>
        public static Color OnWarning => Derived.TextOnWarning;
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

        /// <summary>
        /// Accent sous du texte <see cref="Text.OnAccent"/> (bouton Primary, segment
        /// choisi, jour sélectionné, badge) : ≥ 4,5:1. <see cref="Primary"/> reste
        /// l'accent des indicateurs sans texte (focus, coches, progression, curseurs)
        /// </summary>
        public static Color Fill => Derived.AccentFill;
        public static Color FillHover => Derived.AccentFillHover;
        public static Color FillPressed => Derived.AccentFillPressed;

        /// <summary>Accent employé comme texte ou glyphe sur Background.Panel : ≥ 4,5:1</summary>
        public static Color Foreground => Derived.AccentForeground;

        /// <summary>Information (bleu), distincte de l'accent de marque</summary>
        public static Color Info => Derived.AccentInfo;

        /// <summary>Fonds de statut sous du texte <see cref="Text.OnAccent"/> : ≥ 4,5:1</summary>
        public static Color SuccessFill => Derived.AccentSuccessFill;
        public static Color InfoFill => Derived.AccentInfoFill;
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
        // Hauteurs de contrôles. Small = EditorHeight : un bouton Small s'aligne sur
        // les champs d'une rangée de formulaire
        public const double ControlHeightSmall = 28;
        public const double ControlHeightMedium = 34;
        public const double ControlHeightLarge = 42;

        // Champs de saisie mono-ligne (TextBox, ComboBox, Date/Time/Spin...) :
        // une hauteur unique et un retrait texte unique pour que les champs d'un
        // formulaire s'empilent au même rythme et que le texte tombe d'aplomb
        public const double EditorHeight = 28;
        public const double EditorTextInset = 8;   // entre le filet et le 1er glyphe

        // Rayons de coins (libadwaita : boutons 6, popovers 8, cartes 12)
        public const double CornerRadiusSmall = 6;
        public const double CornerRadiusMedium = 8;
        public const double CornerRadiusLarge = 12;

        // Épaisseurs de bordure
        public const double BorderThin = 1;
        public const double BorderMedium = 1.5;
        public const double BorderThick = 2;

        // Anneau de focus clavier (couleur : Border.FocusOuter pleine, ≥ 3:1)
        public const double FocusRingWidth = 2;
        public const double FocusRingOffset = 1;

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
        public const double FontSizeDense = 13;    // texte secondaire des listes et cartes
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

    /// <summary>Couleur <paramref name="top"/>, alpha compris, posée sur un fond opaque</summary>
    public static Color Composite(Color top, Color bottom)
    {
        var a = top.A / 255.0;
        return Color.FromRgb(
            (byte)Math.Round(top.R * a + bottom.R * (1 - a)),
            (byte)Math.Round(top.G * a + bottom.G * (1 - a)),
            (byte)Math.Round(top.B * a + bottom.B * (1 - a)));
    }

    // ═══════════════════════════════════════════════════════════════
    // CONTRASTE (WCAG 2.x)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Rapport de contraste entre deux couleurs opaques, de 1 à 21</summary>
    public static double Contrast(Color a, Color b)
    {
        var la = RelativeLuminance(a);
        var lb = RelativeLuminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    /// <summary>Luminance relative : 0 pour le noir, 1 pour le blanc</summary>
    public static double RelativeLuminance(Color color)
    {
        static double Channel(byte value)
        {
            var s = value / 255.0;
            return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
    }

    /// <summary>
    /// Assombrit (contre un fond plus clair) ou éclaircit (contre un fond plus sombre)
    /// <paramref name="color"/>, teinte et saturation conservées, jusqu'à atteindre
    /// <paramref name="ratio"/>. Rendue telle quelle si le contraste est déjà atteint.
    /// </summary>
    public static Color EnsureContrast(Color color, Color against, double ratio = 4.5)
    {
        if (Contrast(color, against) >= ratio) return color;

        var (h, s, l) = ToHsl(color);
        var darken = RelativeLuminance(against) > RelativeLuminance(color);
        for (var step = 1; step <= 100; step++)
        {
            var candidate = FromHsl(h, s, Math.Clamp(darken ? l - step * 0.01 : l + step * 0.01, 0, 1));
            if (Contrast(candidate, against) >= ratio) return candidate;
        }
        return darken ? Colors.Black : Colors.White;
    }

    /// <summary>
    /// Texte le plus lisible sur un fond de donnée quelconque (barre, événement,
    /// pastille) : blanc ou quasi-noir. Composer d'abord un fond translucide
    /// (<see cref="Composite"/>) sur ce qu'il recouvre.
    /// </summary>
    public static Color OnColor(Color fill)
        => Contrast(Colors.White, fill) >= Contrast(NearBlack, fill) ? Colors.White : NearBlack;

    private static readonly Color NearBlack = Color.FromRgb(0x1E, 0x1E, 0x1E);

    private static (double H, double S, double L) ToHsl(Color c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
        double h = 0, s = 0, l = (max + min) / 2;
        if (max > min)
        {
            var d = max - min;
            s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            h = max == r ? (g - b) / d + (g < b ? 6 : 0)
                : max == g ? (b - r) / d + 2
                : (r - g) / d + 4;
            h /= 6;
        }
        return (h, s, l);
    }

    private static Color FromHsl(double h, double s, double l)
    {
        if (s == 0)
        {
            var v = (byte)Math.Round(l * 255);
            return Color.FromRgb(v, v, v);
        }

        static double Hue(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6) return p + (q - p) * 6 * t;
            if (t < 1.0 / 2) return q;
            if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
            return p;
        }

        var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        var p = 2 * l - q;
        return Color.FromRgb(
            (byte)Math.Round(Hue(p, q, h + 1.0 / 3) * 255),
            (byte)Math.Round(Hue(p, q, h) * 255),
            (byte)Math.Round(Hue(p, q, h - 1.0 / 3) * 255));
    }

    // ═══════════════════════════════════════════════════════════════
    // TOKENS DÉRIVÉS
    // ═══════════════════════════════════════════════════════════════

    private sealed record DerivedTokens(
        Color AccentFill, Color AccentFillHover, Color AccentFillPressed, Color AccentForeground,
        Color AccentInfo, Color AccentSuccessFill, Color AccentInfoFill,
        Color TextOnWarning, Color TextSelection);

    // Une entrée par instance de palette, calculée à la première lecture
    private static readonly ConditionalWeakTable<HDevPalette, DerivedTokens> DerivedCache = new();

    private static DerivedTokens Derived => DerivedCache.GetValue(_current, Derive);

    /// <summary>
    /// Valeurs fixées par la palette, à défaut dérivées de ses propres couleurs avec le
    /// contraste garanti : une palette hôte (accent teal de Synaxis…) n'hérite jamais
    /// de l'orange Yaru
    /// </summary>
    private static DerivedTokens Derive(HDevPalette p)
    {
        var onAccent = p.TextOnAccent;
        var fill = p.AccentFill ?? EnsureContrast(p.AccentPrimary, onAccent);
        var info = p.AccentInfo ?? p.TextLink;
        var isDark = RelativeLuminance(p.BackgroundPanel) < 0.2;

        return new DerivedTokens(
            AccentFill: fill,
            // Survol et pression assombrissent : le contraste ne peut que croître
            AccentFillHover: p.AccentFillHover ?? EnsureContrast(Darken(fill, 0.06), onAccent),
            AccentFillPressed: p.AccentFillPressed ?? EnsureContrast(Darken(fill, 0.12), onAccent),
            AccentForeground: EnsureContrast(EnsureContrast(p.AccentPrimary, p.BackgroundPanel), p.BackgroundWindow),
            AccentInfo: info,
            AccentSuccessFill: p.AccentSuccessFill ?? EnsureContrast(p.AccentSuccess, onAccent),
            AccentInfoFill: p.AccentInfoFill ?? EnsureContrast(info, onAccent),
            TextOnWarning: p.TextOnWarning ?? OnColor(p.AccentWarning),
            TextSelection: p.BackgroundTextSelection
                ?? Lerp(p.BackgroundPanel, p.AccentPrimary, isDark ? 0.40 : 0.30));
    }

    private static Color Darken(Color color, double amount)
    {
        var (h, s, l) = ToHsl(color);
        return FromHsl(h, s, Math.Max(0, l - amount));
    }

    // ═══════════════════════════════════════════════════════════════
    // PROPAGATION
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Tient un contrôle à jour des changements de palette tant qu'il est attaché à
    /// l'arbre visuel : action facultative (re-brosser ce qui a été construit avec la
    /// palette précédente), puis invalidation de la mesure et du rendu. À appeler depuis
    /// le constructeur.
    /// </summary>
    public static void Track(Control control, Action? onChanged = null)
    {
        void OnVariantChanged(object? sender, EventArgs e)
        {
            onChanged?.Invoke();
            control.InvalidateMeasure();
            control.InvalidateVisual();
        }

        control.AttachedToVisualTree += (_, _) => VariantChanged += OnVariantChanged;
        control.DetachedFromVisualTree += (_, _) => VariantChanged -= OnVariantChanged;
    }

    /// <summary>
    /// Publie la palette active dans les ressources de l'application hôte et l'y tient à
    /// jour : accent des contrôles Fluent internes (<c>SystemAccentColor*</c>) et un
    /// pinceau par couleur de palette (<c>{DynamicResource HDevTextSecondaryBrush}</c>…).
    /// Remplace le bloc SystemAccentColor recopié à la main dans chaque App.axaml.
    /// </summary>
    public static void ApplyTo(Application application)
    {
        Publish(application.Resources);
        VariantChanged += (_, _) => Publish(application.Resources);
    }

    private static void Publish(IResourceDictionary resources)
    {
        var accent = Accent.Primary;
        resources["SystemAccentColor"] = accent;
        resources["SystemAccentColorDark1"] = Lerp(accent, Colors.Black, 0.06);
        resources["SystemAccentColorDark2"] = Lerp(accent, Colors.Black, 0.15);
        resources["SystemAccentColorDark3"] = Lerp(accent, Colors.Black, 0.28);
        resources["SystemAccentColorLight1"] = Lerp(accent, Colors.White, 0.2);
        resources["SystemAccentColorLight2"] = Lerp(accent, Colors.White, 0.4);
        resources["SystemAccentColorLight3"] = Lerp(accent, Colors.White, 0.6);

        foreach (var property in typeof(HDevPalette).GetProperties())
        {
            if (property.GetValue(_current) is Color color)
                resources[$"HDev{property.Name}Brush"] = new SolidColorBrush(color);
        }

        // Tokens dérivés : publiés même quand la palette ne les fixe pas
        resources["HDevAccentFillBrush"] = new SolidColorBrush(Accent.Fill);
        resources["HDevAccentForegroundBrush"] = new SolidColorBrush(Accent.Foreground);
        resources["HDevAccentInfoBrush"] = new SolidColorBrush(Accent.Info);
        resources["HDevBackgroundTextSelectionBrush"] = new SolidColorBrush(Background.TextSelection);
    }
}
