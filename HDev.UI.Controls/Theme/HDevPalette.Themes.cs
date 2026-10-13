using Avalonia.Media;

namespace HDev.UI.Controls.Theme;

/// <summary>
/// Palettes complémentaires de la suite. Les palettes Ubuntu 26 (Yaru clair et
/// sombre) restent <see cref="Light"/> et <see cref="Dark"/> ; celles-ci
/// s'ajoutent sans rien changer à l'existant. Catalogue et bascule :
/// <see cref="HDevThemeDefinition"/> et <see cref="HDevTheme.Apply"/>.
/// </summary>
public sealed partial record HDevPalette
{
    /// <summary>Ardoise : gris froids, accent indigo — sobre et dense</summary>
    public static readonly HDevPalette Ardoise = new()
    {
        BackgroundWindow = Color.Parse("#F4F5F7"),
        BackgroundPanel = Color.Parse("#FFFFFF"),
        BackgroundPanelAlt = Color.Parse("#F1F3F5"),
        BackgroundToolbar = Color.Parse("#F1F3F5"),
        BackgroundStatusBar = Color.Parse("#F1F3F5"),
        BackgroundControl = Color.Parse("#E9ECEF"),
        BackgroundControlHover = Color.Parse("#DEE2E6"),
        BackgroundControlPressed = Color.Parse("#CED4DA"),
        BackgroundControlDisabled = Color.Parse("#F1F3F5"),
        BackgroundControlFocused = Color.Parse("#FFFFFF"),
        BackgroundSelection = Color.Parse("#3355A8"),
        BackgroundSelectionInactive = Color.Parse("#D5DAE0"),
        BackgroundSuccess = Color.Parse("#DDF2E4"),
        BackgroundWarning = Color.Parse("#F9F0DC"),
        BackgroundError = Color.Parse("#FADEE1"),
        BackgroundInfo = Color.Parse("#E3EAF7"),

        BorderDefault = Color.Parse("#CED4DA"),
        BorderHover = Color.Parse("#ADB5BD"),
        BorderPressed = Color.Parse("#98A2AD"),
        BorderFocused = Color.Parse("#3355A8"),
        BorderDisabled = Color.Parse("#E4E7EB"),
        BorderSubtle = Color.Parse("#E9ECEF"),
        BorderStrong = Color.Parse("#8A939D"),
        BorderFocusOuter = Color.Parse("#3355A8"),
        BorderFocusInner = Color.Parse("#FFFFFF"),

        TextPrimary = Color.Parse("#1F2933"),
        TextSecondary = Color.Parse("#4A5563"),
        TextTertiary = Color.Parse("#5F6B7A"),
        TextDisabled = Color.Parse("#9AA5B1"),
        TextPlaceholder = Color.Parse("#7B8794"),
        TextOnAccent = Color.Parse("#FFFFFF"),
        TextLink = Color.Parse("#2F5BC2"),
        TextLinkHover = Color.Parse("#24407F"),
        TextSuccess = Color.Parse("#1B7443"),
        TextWarning = Color.Parse("#8A6203"),       // 4,8:1 sur BackgroundWarning
        TextError = Color.Parse("#C01C28"),

        AccentPrimary = Color.Parse("#3355A8"),
        AccentPrimaryHover = Color.Parse("#2C4A93"),
        AccentPrimaryPressed = Color.Parse("#24407F"),
        AccentSecondary = Color.Parse("#5F6B7A"),
        AccentSecondaryHover = Color.Parse("#4E5967"),
        AccentSecondaryPressed = Color.Parse("#3E4752"),
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
        DisabledBackground = Color.Parse("#F1F3F5"),
        DisabledBorder = Color.Parse("#DDE1E6"),
        DisabledText = Color.Parse("#9AA5B1"),

        ShadowColor = Color.Parse("#1A000000"),
        ShadowColorStrong = Color.Parse("#33000000")
    };

    /// <summary>Lagon : sarcelle et eau claire, accent bleu-vert</summary>
    public static readonly HDevPalette Lagon = new()
    {
        BackgroundWindow = Color.Parse("#F4F9F9"),
        BackgroundPanel = Color.Parse("#FFFFFF"),
        BackgroundPanelAlt = Color.Parse("#EEF6F6"),
        BackgroundToolbar = Color.Parse("#EAF4F4"),
        BackgroundStatusBar = Color.Parse("#EAF4F4"),
        BackgroundControl = Color.Parse("#E1EEEE"),
        BackgroundControlHover = Color.Parse("#D3E6E6"),
        BackgroundControlPressed = Color.Parse("#C1DADB"),
        BackgroundControlDisabled = Color.Parse("#EFF5F5"),
        BackgroundControlFocused = Color.Parse("#FFFFFF"),
        BackgroundSelection = Color.Parse("#0E7C80"),
        BackgroundSelectionInactive = Color.Parse("#CFE2E2"),
        BackgroundSuccess = Color.Parse("#DDF2E4"),
        BackgroundWarning = Color.Parse("#F9F0DC"),
        BackgroundError = Color.Parse("#FADEE1"),
        BackgroundInfo = Color.Parse("#DCEFF0"),

        BorderDefault = Color.Parse("#BFD9DA"),
        BorderHover = Color.Parse("#9DC3C5"),
        BorderPressed = Color.Parse("#86B2B4"),
        BorderFocused = Color.Parse("#0E7C80"),
        BorderDisabled = Color.Parse("#DCEBEB"),
        BorderSubtle = Color.Parse("#E3EFEF"),
        BorderStrong = Color.Parse("#7FA4A6"),
        BorderFocusOuter = Color.Parse("#0E7C80"),
        BorderFocusInner = Color.Parse("#FFFFFF"),

        TextPrimary = Color.Parse("#12282B"),
        TextSecondary = Color.Parse("#3E5559"),
        TextTertiary = Color.Parse("#4F6A6E"),
        TextDisabled = Color.Parse("#93A9AB"),
        TextPlaceholder = Color.Parse("#6E8789"),
        TextOnAccent = Color.Parse("#FFFFFF"),
        TextLink = Color.Parse("#0A6B8F"),
        TextLinkHover = Color.Parse("#08566F"),
        TextSuccess = Color.Parse("#1B7443"),
        TextWarning = Color.Parse("#8A6203"),       // 4,8:1 sur BackgroundWarning
        TextError = Color.Parse("#C01C28"),

        AccentPrimary = Color.Parse("#0E7C80"),
        AccentPrimaryHover = Color.Parse("#0B6D71"),
        AccentPrimaryPressed = Color.Parse("#095A5D"),
        AccentSecondary = Color.Parse("#2E6F95"),
        AccentSecondaryHover = Color.Parse("#275F80"),
        AccentSecondaryPressed = Color.Parse("#1F4E6A"),
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
        DisabledBackground = Color.Parse("#EFF5F5"),
        DisabledBorder = Color.Parse("#D3E3E3"),
        DisabledText = Color.Parse("#93A9AB"),

        ShadowColor = Color.Parse("#1A000000"),
        ShadowColorStrong = Color.Parse("#33000000")
    };

    /// <summary>Indigo &amp; Or : fond chaud, accent indigo, touches d'or — signature HDev</summary>
    public static readonly HDevPalette IndigoOr = new()
    {
        BackgroundWindow = Color.Parse("#F7F5F0"),
        BackgroundPanel = Color.Parse("#FFFFFF"),
        BackgroundPanelAlt = Color.Parse("#FAF9F6"),
        BackgroundToolbar = Color.Parse("#F3F1EB"),
        BackgroundStatusBar = Color.Parse("#F3F1EB"),
        BackgroundControl = Color.Parse("#ECE8DE"),
        BackgroundControlHover = Color.Parse("#E2DDD0"),
        BackgroundControlPressed = Color.Parse("#D5CFBF"),
        BackgroundControlDisabled = Color.Parse("#F4F2ED"),
        BackgroundControlFocused = Color.Parse("#FFFFFF"),
        BackgroundSelection = Color.Parse("#2B3A73"),
        BackgroundSelectionInactive = Color.Parse("#DCD8CC"),
        BackgroundSuccess = Color.Parse("#DDF2E4"),
        BackgroundWarning = Color.Parse("#F9F0DC"),
        BackgroundError = Color.Parse("#FADEE1"),
        BackgroundInfo = Color.Parse("#E6E8F3"),

        BorderDefault = Color.Parse("#D9D4C7"),
        BorderHover = Color.Parse("#BFB8A6"),
        BorderPressed = Color.Parse("#ABA390"),
        BorderFocused = Color.Parse("#2B3A73"),
        BorderDisabled = Color.Parse("#E8E4DA"),
        BorderSubtle = Color.Parse("#ECE9E1"),
        BorderStrong = Color.Parse("#9A927E"),
        BorderFocusOuter = Color.Parse("#2B3A73"),
        BorderFocusInner = Color.Parse("#FFFFFF"),

        TextPrimary = Color.Parse("#1C1F33"),
        TextSecondary = Color.Parse("#45485C"),
        TextTertiary = Color.Parse("#5B5E72"),
        TextDisabled = Color.Parse("#A09E96"),
        TextPlaceholder = Color.Parse("#807E86"),
        TextOnAccent = Color.Parse("#FFFFFF"),
        TextLink = Color.Parse("#34479A"),
        TextLinkHover = Color.Parse("#2B3A73"),
        TextSuccess = Color.Parse("#1B7443"),
        TextWarning = Color.Parse("#8A6203"),       // 4,8:1 sur BackgroundWarning
        TextError = Color.Parse("#B3261E"),

        AccentPrimary = Color.Parse("#2B3A73"),
        AccentPrimaryHover = Color.Parse("#25326A"),
        AccentPrimaryPressed = Color.Parse("#1C2752"),
        AccentSecondary = Color.Parse("#B39A55"),
        AccentSecondaryHover = Color.Parse("#A08848"),
        AccentSecondaryPressed = Color.Parse("#8A743B"),
        AccentSuccess = Color.Parse("#26A269"),
        AccentWarning = Color.Parse("#D9A520"),
        AccentError = Color.Parse("#B3261E"),

        DangerDefault = Color.Parse("#B3261E"),
        DangerHover = Color.Parse("#9A1F18"),
        DangerPressed = Color.Parse("#821A14"),
        DangerDisabledBackground = Color.Parse("#F5E6E4"),
        DangerDisabledBorder = Color.Parse("#E0C2BE"),
        DangerDisabledText = Color.Parse("#B5918D"),

        ChromeCloseHover = Color.Parse("#B3261E"),
        ChromeClosePressed = Color.Parse("#9A1F18"),
        DisabledBackground = Color.Parse("#F3F1EB"),
        DisabledBorder = Color.Parse("#DDD8CC"),
        DisabledText = Color.Parse("#A09E96"),

        ShadowColor = Color.Parse("#1A000000"),
        ShadowColorStrong = Color.Parse("#33000000")
    };

    /// <summary>
    /// Cockpit : sombre, accent ambre façon avionique. L'ambre étant clair, le
    /// texte posé dessus est sombre (<see cref="TextOnAccent"/>), de même que sur
    /// le rouge de danger, éclairci pour garder le contraste
    /// </summary>
    public static readonly HDevPalette Cockpit = new()
    {
        BackgroundWindow = Color.Parse("#0F1318"),
        BackgroundPanel = Color.Parse("#14181D"),
        BackgroundPanelAlt = Color.Parse("#181D23"),
        BackgroundToolbar = Color.Parse("#1C2229"),
        BackgroundStatusBar = Color.Parse("#1C2229"),
        BackgroundControl = Color.Parse("#232A32"),
        BackgroundControlHover = Color.Parse("#2C343D"),
        BackgroundControlPressed = Color.Parse("#36404B"),
        BackgroundControlDisabled = Color.Parse("#1A1F25"),
        BackgroundControlFocused = Color.Parse("#232A32"),
        BackgroundSelection = Color.Parse("#F2A93B"),
        BackgroundSelectionInactive = Color.Parse("#2E3740"),
        BackgroundSuccess = Color.Parse("#1C3527"),
        BackgroundWarning = Color.Parse("#3A3014"),
        BackgroundError = Color.Parse("#3D2023"),
        BackgroundInfo = Color.Parse("#1D3247"),

        BorderDefault = Color.Parse("#2E3740"),
        BorderHover = Color.Parse("#3F4A56"),
        BorderPressed = Color.Parse("#4C5866"),
        BorderFocused = Color.Parse("#F2A93B"),
        BorderDisabled = Color.Parse("#232A32"),
        BorderSubtle = Color.Parse("#252C34"),
        BorderStrong = Color.Parse("#5A6573"),
        BorderFocusOuter = Color.Parse("#F2A93B"),
        BorderFocusInner = Color.Parse("#0F1318"),

        TextPrimary = Color.Parse("#DCE3EA"),
        TextSecondary = Color.Parse("#A9B4C0"),
        TextTertiary = Color.Parse("#8C98A5"),
        TextDisabled = Color.Parse("#5D6874"),
        TextPlaceholder = Color.Parse("#7A8693"),
        TextOnAccent = Color.Parse("#14181D"),
        TextLink = Color.Parse("#6FB3F2"),
        TextLinkHover = Color.Parse("#9ACBF6"),
        TextSuccess = Color.Parse("#6BDD9C"),
        TextWarning = Color.Parse("#F2C94C"),
        TextError = Color.Parse("#FF7B6B"),

        AccentPrimary = Color.Parse("#F2A93B"),
        AccentPrimaryHover = Color.Parse("#F5BA5E"),
        AccentPrimaryPressed = Color.Parse("#D9922A"),
        AccentSecondary = Color.Parse("#4FA3E0"),
        AccentSecondaryHover = Color.Parse("#6DB4E8"),
        AccentSecondaryPressed = Color.Parse("#3C8BC4"),
        AccentSuccess = Color.Parse("#3FCF7F"),
        AccentWarning = Color.Parse("#F6D32D"),
        AccentError = Color.Parse("#F66151"),

        DangerDefault = Color.Parse("#E5534B"),
        DangerHover = Color.Parse("#EE6B63"),
        DangerPressed = Color.Parse("#D35E58"),      // texte sombre (TextOnAccent) : 4,7:1
        DangerDisabledBackground = Color.Parse("#3A2224"),
        DangerDisabledBorder = Color.Parse("#57363A"),
        DangerDisabledText = Color.Parse("#8F6B6E"),

        ChromeCloseHover = Color.Parse("#E5534B"),
        ChromeClosePressed = Color.Parse("#C9443D"),
        DisabledBackground = Color.Parse("#1A1F25"),
        DisabledBorder = Color.Parse("#2E3740"),
        DisabledText = Color.Parse("#5D6874"),

        ShadowColor = Color.Parse("#59000000"),
        ShadowColorStrong = Color.Parse("#80000000")
    };
}
