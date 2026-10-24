using HDev.UI.Controls.Theme;
using Xunit;

namespace HDev.UI.Controls.Tests;

// Les échelles de la suite : typographie plancher à 11 px, trois rayons de base plus deux fins,
// hauteurs récurrentes alignées sur la grille de 28/34/42.
public class ThemeScaleTests
{
    [Fact]
    public void Typography_HasAFloor_AtEleven()
    {
        Assert.Equal(11, HDevTheme.Typography.FontSizeMicro);
        Assert.True(HDevTheme.Typography.FontSizeMicro < HDevTheme.Typography.FontSizeCaption);
        Assert.True(HDevTheme.Typography.FontSizeCaption < HDevTheme.Typography.FontSizeDense);
        Assert.True(HDevTheme.Typography.FontSizeDense < HDevTheme.Typography.FontSizeBody);
    }

    [Fact]
    public void Radii_AreAscending()
    {
        var radii = new[]
        {
            HDevTheme.Size.CornerRadiusXS, HDevTheme.Size.CornerRadiusRow, HDevTheme.Size.CornerRadiusSmall,
            HDevTheme.Size.CornerRadiusMedium, HDevTheme.Size.CornerRadiusLarge,
        };
        Assert.Equal(radii.OrderBy(r => r), radii);
    }

    [Fact]
    public void RecurringHeights_FollowTheGrid()
    {
        Assert.Equal(HDevTheme.Size.EditorHeight, HDevTheme.Size.RowHeight);
        Assert.Equal(HDevTheme.Size.ControlHeightSmall, HDevTheme.Size.TabHeight);
        Assert.True(HDevTheme.Size.TabHeightCompact < HDevTheme.Size.TabHeight);
        Assert.Equal(HDevTheme.Size.ControlHeightMedium, HDevTheme.Size.BarHeight);
    }

    [Fact]
    public void Strokes_AreThreeValues()
    {
        Assert.Equal(new[] { 1.0, 1.2, 2.0 },
            new[] { HDevTheme.Size.StrokeHairline, HDevTheme.Size.StrokeGlyph, HDevTheme.Size.StrokeEmphasis });
    }
}
