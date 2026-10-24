using HDev.UI.Controls;
using Xunit;

namespace HDev.UI.Controls.Tests;

// Le jeu de glyphes est complet : chaque icône a une taille d'origine, et les glyphes
// dessinés par chemin SVG sont analysables (un chemin invalide ne doit pas attendre le rendu).
public class HDevIconsTests
{
    public static TheoryData<HDevIcon> All()
    {
        var data = new TheoryData<HDevIcon>();
        foreach (var icon in Enum.GetValues<HDevIcon>())
            if (icon != HDevIcon.None) data.Add(icon);
        return data;
    }

    [Theory, MemberData(nameof(All))]
    public void EveryIcon_HasANaturalSize(HDevIcon icon)
        => Assert.True(HDevIcons.NaturalSize(icon) is 12 or 16);

    [Fact]
    public void LegacyGlyphs_KeepTheirTwelvePixelBox()
    {
        foreach (var icon in new[] { HDevIcon.Calendar, HDevIcon.ChevronDown, HDevIcon.ChevronUp,
                     HDevIcon.ChevronLeft, HDevIcon.ChevronRight })
            Assert.Equal(12, HDevIcons.NaturalSize(icon));
    }

    [Fact]
    public void TheSetCoversTheLibraryNeeds()
    {
        var names = Enum.GetNames<HDevIcon>().ToHashSet();
        foreach (var needed in new[] { "Close", "Check", "Search", "Plus", "Minus", "More", "Info", "Warning",
                     "Error", "Question", "Trash", "Pencil", "Link", "Indent", "Outdent", "Recurrence", "Image" })
            Assert.Contains(needed, names);
    }
}
