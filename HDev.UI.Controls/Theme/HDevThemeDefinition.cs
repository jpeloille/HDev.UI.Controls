using Avalonia.Styling;

namespace HDev.UI.Controls.Theme;

/// <summary>
/// Variantes Avalonia des thèmes de la suite. Ubuntu 26 garde les variantes
/// standard Light/Dark ; les autres thèmes ont leur propre variante, qui hérite
/// de Light ou Dark : les contrôles Fluent internes retombent sur la bonne base,
/// et les ThemeDictionaries des skins (grille, accent Fluent) peuvent les cibler
/// par <c>{x:Static theme:HDevThemeVariants.Ardoise}</c>.
/// </summary>
public static class HDevThemeVariants
{
    public static ThemeVariant Ardoise { get; } = new("Ardoise", ThemeVariant.Light);
    public static ThemeVariant Lagon { get; } = new("Lagon", ThemeVariant.Light);
    public static ThemeVariant IndigoOr { get; } = new("IndigoOr", ThemeVariant.Light);
    public static ThemeVariant Cockpit { get; } = new("Cockpit", ThemeVariant.Dark);
}

/// <summary>
/// Un thème complet de la suite : palette HDevTheme (contrôles dessinés) +
/// variante Avalonia (contrôles Fluent internes, skin de la grille, accent).
/// Appliquer avec <see cref="HDevTheme.Apply"/>.
/// </summary>
public sealed class HDevThemeDefinition
{
    /// <summary>Identifiant stable (persistance des préférences)</summary>
    public string Key { get; }

    /// <summary>Nom affiché</summary>
    public string DisplayName { get; }

    /// <summary>Clair ou sombre (contrôles qui adaptent leur rendu)</summary>
    public HDevThemeVariant Variant { get; }

    /// <summary>Palette des contrôles dessinés</summary>
    public HDevPalette Palette { get; }

    /// <summary>Variante Avalonia posée sur les fenêtres HDev</summary>
    public ThemeVariant AvaloniaVariant { get; }

    private HDevThemeDefinition(string key, string displayName, HDevThemeVariant variant,
        HDevPalette palette, ThemeVariant avaloniaVariant)
    {
        Key = key;
        DisplayName = displayName;
        Variant = variant;
        Palette = palette;
        AvaloniaVariant = avaloniaVariant;
    }

    public static readonly HDevThemeDefinition Ubuntu26 = new(
        "Ubuntu26", "Ubuntu 26", HDevThemeVariant.Light, HDevPalette.Light, ThemeVariant.Light);

    public static readonly HDevThemeDefinition Ubuntu26Dark = new(
        "Ubuntu26Dark", "Ubuntu 26 sombre", HDevThemeVariant.Dark, HDevPalette.Dark, ThemeVariant.Dark);

    public static readonly HDevThemeDefinition Ardoise = new(
        "Ardoise", "Ardoise", HDevThemeVariant.Light, HDevPalette.Ardoise, HDevThemeVariants.Ardoise);

    public static readonly HDevThemeDefinition Lagon = new(
        "Lagon", "Lagon", HDevThemeVariant.Light, HDevPalette.Lagon, HDevThemeVariants.Lagon);

    public static readonly HDevThemeDefinition IndigoOr = new(
        "IndigoOr", "Indigo & Or", HDevThemeVariant.Light, HDevPalette.IndigoOr, HDevThemeVariants.IndigoOr);

    public static readonly HDevThemeDefinition Cockpit = new(
        "Cockpit", "Cockpit", HDevThemeVariant.Dark, HDevPalette.Cockpit, HDevThemeVariants.Cockpit);

    /// <summary>Catalogue, dans l'ordre de présentation</summary>
    public static IReadOnlyList<HDevThemeDefinition> All { get; } =
        [Ubuntu26, Ubuntu26Dark, Ardoise, Lagon, IndigoOr, Cockpit];

    /// <summary>Retrouve un thème par sa clé (null si inconnue)</summary>
    public static HDevThemeDefinition? FromKey(string? key)
        => All.FirstOrDefault(t => string.Equals(t.Key, key, StringComparison.Ordinal));

    public override string ToString() => DisplayName;
}
