using HDev.UI.Controls.Documents;
using Xunit;

namespace HDev.UI.Controls.Tests;

/// <summary>
/// Contrat storage-v1 : le HTML produit par TipTap (getHTML()) est accepté
/// sans diagnostic, l'aller-retour parse→serialize est un point fixe, et le
/// sérialiseur n'émet que du vocabulaire. Corpus = sorties getHTML()
/// réalistes, une par extension requise (voir StorageHtml-v1.md).
/// </summary>
public class StorageHtmlContractTests
{
    public static TheoryData<string> Corpus => new()
    {
        // StarterKit : marques de base
        "<p>Du <strong>gras</strong>, de l'<em>italique</em>, du <u>souligné</u> et du <s>barré</s>.</p>",
        // Titres + TextAlign (graphie TipTap avec espace)
        "<h1>Titre</h1><h2 style=\"text-align: center\">Centré</h2><p style=\"text-align: right\">Droite</p>",
        // Liste à puces imbriquée (forme canonique : sous-liste en fin de li)
        "<ul><li><p>un</p></li><li><p>deux</p><ul><li><p>deux-a</p></li><li><p>deux-b</p></li></ul></li></ul>",
        // Liste ordonnée
        "<ol><li><p>premier</p></li><li><p>second</p></li></ol>",
        // Citation + séparateur
        "<blockquote><p>citation</p></blockquote><hr><p>suite</p>",
        // CodeBlock
        "<pre><code>if (x &lt; 2) return;</code></pre>",
        // Color (les deux formats émis par TipTap)
        "<p><span style=\"color: rgb(209, 52, 56)\">rouge</span> et <span style=\"color: #0078d4\">bleu</span></p>",
        // Highlight multicolor
        "<p><mark data-color=\"#fff3a1\" style=\"background-color: #fff3a1\">surligné</mark></p>",
        // FontSize + code inline
        "<p><span style=\"font-size: 20px\">grand</span> et <code>du code</code></p>",
        // Link (attributs par défaut de l'extension)
        "<p><a target=\"_blank\" rel=\"noopener noreferrer nofollow\" href=\"https://aircal.nc\">le site</a></p>",
        // HardBreak + Image (data:, alt, title)
        "<p>ligne 1<br>ligne 2</p><p><img src=\"data:image/png;base64,AAAA\" alt=\"logo\" title=\"Logo\"></p>",
        // Table (statut compat) : tbody, th colspan, cellules à paragraphes
        "<table><tbody><tr><th colspan=\"2\"><p>En-tête</p></th></tr><tr><td><p>a</p></td><td><p>b</p></td></tr></tbody></table>",
        // Lignes vides (TipTap émet <p></p>)
        "<p>premier</p><p></p><p>troisième</p>",
    };

    [Theory]
    [MemberData(nameof(Corpus))]
    public void TipTapOutput_IsAcceptedWithoutDiagnostics(string html)
    {
        var result = HtmlParser.Parse(html, HtmlParseOptions.Storage);
        Assert.True(result.IsCleanStorageHtml,
            $"diagnostics inattendus : {string.Join("; ", result.Diagnostics)}");
    }

    [Theory]
    [MemberData(nameof(Corpus))]
    public void ParseSerialize_ReachesFixedPointAfterOneNormalization(string html)
    {
        var h1 = HtmlSerializer.Serialize(HtmlParser.Parse(html));
        var h2 = HtmlSerializer.Serialize(HtmlParser.Parse(h1));
        Assert.Equal(h1, h2);
    }

    [Theory]
    [MemberData(nameof(Corpus))]
    public void SerializerOutput_IsCleanStorageHtml(string html)
    {
        var emitted = HtmlSerializer.Serialize(HtmlParser.Parse(html));
        var result = HtmlParser.Parse(emitted, HtmlParseOptions.Storage);
        Assert.True(result.IsCleanStorageHtml,
            $"le sérialiseur a émis du hors-vocabulaire : {string.Join("; ", result.Diagnostics)}");
    }

    [Fact]
    public void LegacyDialect_IsNormalizedOnSave()
    {
        // Un vieux contenu <b>/<i>/<strike> reste lisible (profil tolérant)
        // et ressort en storage-v1 propre à la première sauvegarde
        var html = HtmlSerializer.Serialize(
            HtmlParser.Parse("<p><b>gras</b> <i>ita</i> <strike>barré</strike></p>"));
        Assert.Contains("<strong>gras</strong>", html);
        Assert.Contains("<em>ita</em>", html);
        Assert.Contains("<s>barré</s>", html);
        Assert.True(HtmlParser.Parse(html, HtmlParseOptions.Storage).IsCleanStorageHtml);
    }

    [Fact]
    public void CompatTags_AreAllInVocabulary()
    {
        foreach (var tag in HtmlVocabulary.CompatTags)
            Assert.True(HtmlVocabulary.IsAllowedTag(tag), $"{tag} absent du vocabulaire");
    }
}
