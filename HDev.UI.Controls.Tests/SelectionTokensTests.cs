using HDev.UI.Controls.Theme;
using Xunit;

namespace HDev.UI.Controls.Tests;

// La sélection d'une collection est une teinte : le texte garde sa couleur normale dessus,
// actif comme inactif, et la teinte se distingue du panneau.
[Collection(nameof(ThemeCollection))]
public class SelectionTokensTests
{
    public static TheoryData<string> Names => new() { "Light", "Dark" };

    private static HDevPalette Palette(string name) => name == "Dark" ? HDevPalette.Dark : HDevPalette.Light;

    [Theory, MemberData(nameof(Names))]
    public void Text_StaysReadable_OnSelectionTints(string name)
    {
        var p = Palette(name);
        Assert.True(HDevTheme.Contrast(p.TextPrimary, p.BackgroundSelection) >= 4.5, "actif");
        Assert.True(HDevTheme.Contrast(p.TextPrimary, p.BackgroundSelectionInactive) >= 4.5, "inactif");
    }

    [Theory, MemberData(nameof(Names))]
    public void SelectionTint_IsDistinct_FromPanel(string name)
    {
        var p = Palette(name);
        Assert.NotEqual(p.BackgroundPanel, p.BackgroundSelection);
        Assert.NotEqual(p.BackgroundSelection, p.BackgroundSelectionInactive);
    }

    [Fact]
    public void SelectionFill_FollowsFocus()
    {
        Assert.Equal(HDevTheme.Background.Selection, HDevTheme.SelectionFill(true));
        Assert.Equal(HDevTheme.Background.SelectionInactive, HDevTheme.SelectionFill(false));
    }

    [Theory, MemberData(nameof(Names))]
    public void Disabled_HasOneTokenSet_WithReadableOrdering(string name)
    {
        var p = Palette(name);
        // Le texte désactivé reste distinguable du fond désactivé (≥ 2:1, la norme exempte les contrôles inactifs)
        Assert.True(HDevTheme.Contrast(p.DisabledText, p.DisabledBackground) >= 2.0);
    }
}
