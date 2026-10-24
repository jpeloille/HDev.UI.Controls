using HDev.UI.Controls.Theme;
using Xunit;

namespace HDev.UI.Controls.Tests;

// Huit teintes de données, lisibles (3:1) sur le panneau de leur variante.
public class CategoricalPaletteTests
{
    [Fact]
    public void LightSeries_Reaches_3to1_OnWhitePanel()
    {
        var panel = HDevPalette.Light.BackgroundPanel;
        var series = HDevPalette.CategoricalLight;
        Assert.Equal(8, series.Length);
        foreach (var c in series)
            Assert.True(HDevTheme.Contrast(c, panel) >= 3, $"{c} sur {panel} : {HDevTheme.Contrast(c, panel):0.00}");
    }

    [Fact]
    public void DarkSeries_Reaches_3to1_OnDarkPanel()
    {
        var panel = HDevPalette.Dark.BackgroundPanel;
        var series = HDevPalette.CategoricalDark;
        Assert.Equal(8, series.Length);
        foreach (var c in series)
            Assert.True(HDevTheme.Contrast(c, panel) >= 3, $"{c} sur {panel} : {HDevTheme.Contrast(c, panel):0.00}");
    }

    [Fact]
    public void Series_AreDistinct()
    {
        Assert.Equal(8, HDevPalette.CategoricalLight.Distinct().Count());
        Assert.Equal(8, HDevPalette.CategoricalDark.Distinct().Count());
    }

    [Fact]
    public void Index_WrapsAround_AndNeverFails()
    {
        Assert.Equal(HDevTheme.Categorical(1), HDevTheme.Categorical(1 + HDevTheme.CategoricalCount));
        Assert.Equal(HDevTheme.Categorical(HDevTheme.CategoricalCount - 1), HDevTheme.Categorical(-1));
    }

    [Fact]
    public void TextOnEverySeriesColor_IsReadable()
    {
        foreach (var c in HDevPalette.CategoricalLight.Concat(HDevPalette.CategoricalDark))
            Assert.True(HDevTheme.Contrast(HDevTheme.OnColor(c), c) >= 4.5, $"{c}");
    }
}
