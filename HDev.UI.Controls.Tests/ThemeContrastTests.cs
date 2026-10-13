using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls.Tests;

/// <summary>
/// HDevTheme est un état statique : ces tests changent de palette et ne doivent pas
/// tourner en parallèle d'un autre test qui la lirait
/// </summary>
[CollectionDefinition(nameof(ThemeCollection), DisableParallelization = true)]
public sealed class ThemeCollection;

/// <summary>
/// Garde-fou de contraste WCAG 2.x des palettes : texte ≥ 4,5:1, indicateurs ≥ 3:1.
/// Couvre les six thèmes du catalogue (HDevThemeDefinition.All), les palettes Yaru par
/// HDevTheme.Variant ET des palettes hôtes qui ne fixent pas les
/// tokens récents (Accent.Fill…), dont les valeurs sont alors dérivées.
/// </summary>
[Collection(nameof(ThemeCollection))]
public sealed class ThemeContrastTests : IDisposable
{
    // Palette hôte façon Synaxis : accent teal déjà conforme, tokens récents absents
    private static readonly HDevPalette Teal = HDevPalette.Light with
    {
        AccentPrimary = Color.Parse("#267A8C"),
        AccentPrimaryHover = Color.Parse("#1F6A7A"),
        AccentPrimaryPressed = Color.Parse("#195A68"),
        BorderFocused = Color.Parse("#267A8C"),
        BorderFocusOuter = Color.Parse("#267A8C"),
        AccentFill = null,
        AccentFillHover = null,
        AccentFillPressed = null,
        AccentInfo = null,
        AccentSuccessFill = null,
        AccentInfoFill = null,
        TextOnWarning = null,
        BackgroundTextSelection = null
    };

    // Accent trop clair pour porter du blanc : la dérivation doit l'assombrir
    private static readonly HDevPalette Yellow = Teal with { AccentPrimary = Color.Parse("#F6D32D") };

    // Light/Dark : palettes Yaru par HDevTheme.Variant ; les clés du catalogue passent
    // par HDevTheme.Apply (Ubuntu26 et Ubuntu26Dark compris)
    private static readonly string[] Catalogue = HDevThemeDefinition.All.Select(t => t.Key).ToArray();

    public static TheoryData<string> CatalogueThemes => new(Catalogue);

    public static TheoryData<string> CompletePalettes => new(new[] { "Light", "Dark", "Teal" }.Concat(Catalogue));

    public static TheoryData<string> AllPalettes => new(new[] { "Light", "Dark", "Teal", "Yellow" }.Concat(Catalogue));

    private static void Use(string palette)
    {
        if (HDevThemeDefinition.FromKey(palette) is { } theme)
        {
            HDevTheme.SetPalettes();
            HDevTheme.Apply(theme);
            return;
        }
        switch (palette)
        {
            case "Teal": HDevTheme.SetPalettes(light: Teal); break;
            case "Yellow": HDevTheme.SetPalettes(light: Yellow); break;
            default: HDevTheme.SetPalettes(); break;
        }
        HDevTheme.Variant = palette == "Dark" ? HDevThemeVariant.Dark : HDevThemeVariant.Light;
    }

    public void Dispose()
    {
        HDevTheme.SetPalettes();
        HDevTheme.Variant = HDevThemeVariant.Light;
    }

    [Theory, MemberData(nameof(CompletePalettes))]
    public void Texts_AreReadable_OnPanelAndWindow(string palette)
    {
        Use(palette);
        foreach (var background in new[] { HDevTheme.Background.Panel, HDevTheme.Background.Window })
        {
            AssertContrast(HDevTheme.Text.Primary, background, 4.5, "Text.Primary");
            AssertContrast(HDevTheme.Text.Secondary, background, 4.5, "Text.Secondary");
            AssertContrast(HDevTheme.Text.Tertiary, background, 4.5, "Text.Tertiary");
            AssertContrast(HDevTheme.Text.Link, background, 4.5, "Text.Link");
            AssertContrast(HDevTheme.Accent.Foreground, background, 4.5, "Accent.Foreground");
        }
    }

    [Theory, MemberData(nameof(AllPalettes))]
    public void TextOnFills_IsReadable(string palette)
    {
        Use(palette);
        var onAccent = HDevTheme.Text.OnAccent;
        AssertContrast(onAccent, HDevTheme.Accent.Fill, 4.5, "Accent.Fill");
        AssertContrast(onAccent, HDevTheme.Accent.FillHover, 4.5, "Accent.FillHover");
        AssertContrast(onAccent, HDevTheme.Accent.FillPressed, 4.5, "Accent.FillPressed");
        AssertContrast(onAccent, HDevTheme.Accent.SuccessFill, 4.5, "Accent.SuccessFill");
        AssertContrast(onAccent, HDevTheme.Accent.InfoFill, 4.5, "Accent.InfoFill");
        AssertContrast(HDevTheme.Text.OnWarning, HDevTheme.Accent.Warning, 4.5, "Text.OnWarning");
        AssertContrast(HDevTheme.Text.Primary, HDevTheme.Background.TextSelection, 4.5, "Background.TextSelection");
    }

    [Theory, MemberData(nameof(CompletePalettes))]
    public void Danger_CarriesWhiteText(string palette)
    {
        Use(palette);
        AssertContrast(HDevTheme.Text.OnAccent, HDevTheme.Danger.Default, 4.5, "Danger.Default");
        AssertContrast(HDevTheme.Text.OnAccent, HDevTheme.Danger.Hover, 4.5, "Danger.Hover");
        AssertContrast(HDevTheme.Text.OnAccent, HDevTheme.Danger.Pressed, 4.5, "Danger.Pressed");
    }

    [Theory, MemberData(nameof(CompletePalettes))]
    public void StateTexts_AreReadable_OnStateBackgrounds(string palette)
    {
        Use(palette);
        AssertContrast(HDevTheme.Text.Success, HDevTheme.Background.Success, 4.5, "Text.Success sur Background.Success");
        AssertContrast(HDevTheme.Text.Warning, HDevTheme.Background.Warning, 4.5, "Text.Warning sur Background.Warning");
        AssertContrast(HDevTheme.Text.Error, HDevTheme.Background.Error, 4.5, "Text.Error sur Background.Error");
        AssertContrast(HDevTheme.Text.Link, HDevTheme.Background.Info, 4.5, "Text.Link sur Background.Info");
    }

    [Theory, MemberData(nameof(CompletePalettes))]
    public void Indicators_AreVisible(string palette)
    {
        Use(palette);
        foreach (var background in new[] { HDevTheme.Background.Panel, HDevTheme.Background.Window })
        {
            AssertContrast(HDevTheme.Accent.Primary, background, 3, "Accent.Primary");
            AssertContrast(HDevTheme.Border.FocusOuter, background, 3, "Border.FocusOuter");
        }
    }

    [Theory, MemberData(nameof(CatalogueThemes))]
    public void Apply_SetsPaletteVariantAndAvaloniaVariant(string key)
    {
        var theme = HDevThemeDefinition.FromKey(key)!;
        Use(key);
        Assert.Same(theme, HDevTheme.Theme);
        Assert.Same(theme.Palette, HDevTheme.Current);
        Assert.Equal(theme.Variant, HDevTheme.Variant);
        Assert.Equal(theme.AvaloniaVariant, HDevTheme.AvaloniaVariant);

        // La variante Avalonia propre retombe sur Light ou Dark (contrôles Fluent internes)
        var root = theme.AvaloniaVariant;
        while (root.InheritVariant is { } parent) root = parent;
        Assert.Equal(theme.Variant == HDevThemeVariant.Dark ? "Dark" : "Light", root.Key.ToString());
    }

    [Fact]
    public void Variant_LeavesTheCatalogueTheme_EvenAtTheSameVariant()
    {
        Use("Ardoise");
        HDevTheme.Variant = HDevThemeVariant.Light;
        Assert.Null(HDevTheme.Theme);
        Assert.Same(HDevPalette.Light, HDevTheme.Current);
        Assert.Equal(Avalonia.Styling.ThemeVariant.Light, HDevTheme.AvaloniaVariant);
    }

    [Fact]
    public void Dispose_ResetsToUbuntuLight_AfterACatalogueTheme()
    {
        Use("Cockpit");
        Dispose();
        Assert.Null(HDevTheme.Theme);
        Assert.Same(HDevPalette.Light, HDevTheme.Current);
        Assert.False(HDevTheme.IsDark);
    }

    [Fact]
    public void Catalogue_HasSixThemes_WithUniqueKeys()
    {
        Assert.Equal(6, HDevThemeDefinition.All.Count);
        Assert.Equal(6, Catalogue.Distinct().Count());
        Assert.Null(HDevThemeDefinition.FromKey("Inconnu"));
    }

    [Fact]
    public void Derivation_KeepsAnAlreadyCompliantHostAccent()
    {
        Use("Teal");
        Assert.Equal(Color.Parse("#267A8C"), HDevTheme.Accent.Fill);
    }

    [Fact]
    public void Derivation_NeverFallsBackToYaruOrange()
    {
        Use("Teal");
        Assert.NotEqual(HDevPalette.Light.AccentFill, HDevTheme.Accent.Fill);
        Assert.NotEqual(HDevPalette.Light.BackgroundTextSelection, HDevTheme.Background.TextSelection);

        // La sélection de texte est une teinte du teal : bleu-vert, pas orangée
        var selection = HDevTheme.Background.TextSelection;
        Assert.True(selection.B > selection.R && selection.G > selection.R, selection.ToString());
    }

    [Fact]
    public void Derivation_DarkensAnAccentTooLightForWhiteText()
    {
        Use("Yellow");
        Assert.True(HDevTheme.Contrast(Colors.White, Color.Parse("#F6D32D")) < 2);
        AssertContrast(HDevTheme.Text.OnAccent, HDevTheme.Accent.Fill, 4.5, "Accent.Fill dérivé");
    }

    [Fact]
    public void Contrast_MatchesWcagReferenceValues()
    {
        Assert.Equal(21, HDevTheme.Contrast(Colors.White, Colors.Black), 3);
        Assert.Equal(1, HDevTheme.Contrast(Colors.Red, Colors.Red), 6);
        Assert.InRange(HDevTheme.Contrast(Colors.White, Color.Parse("#E95420")), 3.6, 3.7);
    }

    [Fact]
    public void OnColor_PicksTheMoreReadableText()
    {
        Assert.Equal(Colors.White, HDevTheme.OnColor(Color.Parse("#1C71D8")));
        Assert.NotEqual(Colors.White, HDevTheme.OnColor(Color.Parse("#F6D32D")));
    }

    private static void AssertContrast(Color foreground, Color background, double minimum, string token)
    {
        var ratio = HDevTheme.Contrast(foreground, background);
        Assert.True(ratio >= minimum,
            $"{token} : {ratio:0.00}:1 < {minimum}:1 ({foreground} sur {background})");
    }
}
