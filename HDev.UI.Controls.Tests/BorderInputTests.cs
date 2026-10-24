using Avalonia.Media;
using HDev.UI.Controls.Theme;
using Xunit;

namespace HDev.UI.Controls.Tests;

// Le filet d'un champ ou d'une case doit tenir 3:1 sur le panneau (WCAG 1.4.11) : Yaru fixe ses
// valeurs, une palette hôte qui ne les fournit pas les voit dérivées.
[Collection(nameof(ThemeCollection))]
public class BorderInputTests : IDisposable
{
    public void Dispose()
    {
        HDevTheme.SetPalettes();
        HDevTheme.Variant = HDevThemeVariant.Light;
    }

    private static void Use(HDevPalette palette, HDevThemeVariant variant)
    {
        HDevTheme.SetPalettes(
            variant == HDevThemeVariant.Light ? palette : null,
            variant == HDevThemeVariant.Dark ? palette : null);
        HDevTheme.Variant = variant;
    }

    [Theory]
    [InlineData(HDevThemeVariant.Light)]
    [InlineData(HDevThemeVariant.Dark)]
    public void Yaru_InputBorder_Reaches_3to1(HDevThemeVariant variant)
    {
        Use(variant == HDevThemeVariant.Dark ? HDevPalette.Dark : HDevPalette.Light, variant);
        Assert.True(HDevTheme.Contrast(HDevTheme.Border.Input, HDevTheme.Background.Panel) >= 3);
        Assert.True(HDevTheme.Contrast(HDevTheme.Border.InputHover, HDevTheme.Background.Panel) >= 3);
    }

    [Fact]
    public void HostPalette_WithoutInputBorder_GetsADerivedOne_At_3to1()
    {
        var host = HDevPalette.Light with
        {
            BorderDefault = Color.Parse("#D8E6EA"), BorderInput = null, BorderInputHover = null,
        };
        Use(host, HDevThemeVariant.Light);
        Assert.NotEqual(host.BorderDefault, HDevTheme.Border.Input);
        Assert.True(HDevTheme.Contrast(HDevTheme.Border.Input, host.BackgroundPanel) >= 3);
        Assert.True(HDevTheme.Contrast(HDevTheme.Border.InputHover, host.BackgroundPanel) >= 3);
    }

    [Fact]
    public void HostPalette_WithAnAlreadyStrongBorder_IsLeftUntouched()
    {
        var host = HDevPalette.Light with { BorderDefault = Color.Parse("#555555"), BorderInput = null };
        Use(host, HDevThemeVariant.Light);
        Assert.Equal(host.BorderDefault, HDevTheme.Border.Input);
    }
}
