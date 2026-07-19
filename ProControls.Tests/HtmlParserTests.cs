using Avalonia.Media;
using ProControls.Documents;
using Xunit;

namespace ProControls.Tests;

public class HtmlParserTests
{
    private static DocParagraph Para(ProDocument doc, int index = 0)
        => Assert.IsType<DocParagraph>(doc.Blocks[index]);

    private static DocRun FirstRun(DocParagraph p)
        => Assert.IsType<DocRun>(p.Inlines[0]);

    // ── Texte et structure ─────────────────────────────────────────

    [Fact]
    public void PlainText_BecomesParagraph()
    {
        var doc = HtmlParser.Parse("Bonjour le monde");
        Assert.Equal("Bonjour le monde", FirstRun(Para(doc)).Text);
    }

    [Fact]
    public void Paragraphs_AreSeparated()
    {
        var doc = HtmlParser.Parse("<p>Un</p><p>Deux</p>");
        Assert.Equal(2, doc.Blocks.Count);
        Assert.Equal("Un", FirstRun(Para(doc, 0)).Text);
        Assert.Equal("Deux", FirstRun(Para(doc, 1)).Text);
    }

    [Fact]
    public void Headings_CarryLevel()
    {
        var doc = HtmlParser.Parse("<h2>Titre</h2><p>Corps</p>");
        Assert.Equal(2, Para(doc, 0).HeadingLevel);
        Assert.Equal(0, Para(doc, 1).HeadingLevel);
    }

    [Fact]
    public void Br_InsertsLineBreak()
    {
        var doc = HtmlParser.Parse("a<br>b");
        var p = Para(doc);
        Assert.Equal(3, p.Inlines.Count);
        Assert.IsType<DocLineBreak>(p.Inlines[1]);
    }

    [Fact]
    public void Whitespace_IsCollapsed()
    {
        var doc = HtmlParser.Parse("<p>a\n\n   b\t c</p>");
        Assert.Equal("a b c", FirstRun(Para(doc)).Text);
    }

    [Fact]
    public void Hr_BecomesSeparator()
    {
        var doc = HtmlParser.Parse("<p>a</p><hr><p>b</p>");
        Assert.IsType<DocSeparator>(doc.Blocks[1]);
    }

    // ── Styles ─────────────────────────────────────────────────────

    [Fact]
    public void BoldItalic_AreNested()
    {
        var doc = HtmlParser.Parse("<b>gras <i>et italique</i></b>");
        var p = Para(doc);
        Assert.True(FirstRun(p).Style.Bold);
        var second = Assert.IsType<DocRun>(p.Inlines[1]);
        Assert.True(second.Style.Bold);
        Assert.True(second.Style.Italic);
    }

    [Fact]
    public void MisnestedTags_AreTolerated()
    {
        // <b><i></b></i> : classique du HTML de mail
        var doc = HtmlParser.Parse("<b><i>x</b></i>y");
        var p = Para(doc);
        var runX = FirstRun(p);
        Assert.True(runX.Style.Bold);
        Assert.True(runX.Style.Italic);
        var runY = Assert.IsType<DocRun>(p.Inlines[1]);
        Assert.NotEqual(true, runY.Style.Bold);
    }

    [Fact]
    public void InlineCss_ColorAndSize()
    {
        var doc = HtmlParser.Parse("<span style=\"color:#ff0000;font-size:18px\">x</span>");
        var style = FirstRun(Para(doc)).Style;
        Assert.Equal(Colors.Red, style.Foreground);
        Assert.Equal(18, style.FontSize);
    }

    [Fact]
    public void FontTag_ColorAttribute()
    {
        var doc = HtmlParser.Parse("<font color=\"blue\">x</font>");
        Assert.Equal(Colors.Blue, FirstRun(Para(doc)).Style.Foreground);
    }

    [Theory]
    [InlineData("12pt", 16)]
    [InlineData("16px", 16)]
    [InlineData("2em", 28)]
    public void CssFontSizes_AreConverted(string css, double expected)
    {
        Assert.True(HtmlParser.TryParseCssFontSize(css, out var size));
        Assert.Equal(expected, size, precision: 5);
    }

    [Fact]
    public void CssColor_Rgb()
    {
        Assert.True(HtmlParser.TryParseCssColor("rgb(255, 0, 0)", out var color));
        Assert.Equal(Colors.Red, color);
    }

    // ── Liens et images ────────────────────────────────────────────

    [Fact]
    public void Link_CapturesHrefAndText()
    {
        var doc = HtmlParser.Parse("<a href=\"https://x.nc\">clic</a>");
        var link = Assert.IsType<DocLink>(Para(doc).Inlines[0]);
        Assert.Equal("https://x.nc", link.Href);
        Assert.Equal("clic", Assert.IsType<DocRun>(link.Children[0]).Text);
    }

    [Fact]
    public void JavascriptHref_IsStripped()
    {
        var doc = HtmlParser.Parse("<a href=\"javascript:alert(1)\">clic</a>");
        var link = Assert.IsType<DocLink>(Para(doc).Inlines[0]);
        Assert.Equal("", link.Href);
    }

    [Fact]
    public void Image_DataUri_IsDecoded()
    {
        var payload = Convert.ToBase64String(new byte[] { 1, 2, 3 });
        var doc = HtmlParser.Parse($"<img src=\"data:image/png;base64,{payload}\" width=40>");
        var img = Assert.IsType<DocImage>(Para(doc).Inlines[0]);
        Assert.Equal(new byte[] { 1, 2, 3 }, img.Data);
        Assert.Equal(40, img.Width);
    }

    [Fact]
    public void Image_RemoteUrl_KeepsReferenceOnly()
    {
        var doc = HtmlParser.Parse("<img src=\"https://tracker.example/pixel.png\">");
        var img = Assert.IsType<DocImage>(Para(doc).Inlines[0]);
        Assert.Null(img.Data);                       // AUCUN réseau au parsing
        Assert.Equal("https://tracker.example/pixel.png", img.Source);
    }

    // ── Listes, tables, citations, code ────────────────────────────

    [Fact]
    public void UnorderedList_WithUnclosedLi()
    {
        var doc = HtmlParser.Parse("<ul><li>a<li>b</ul>");
        var list = Assert.IsType<DocList>(doc.Blocks[0]);
        Assert.False(list.Ordered);
        Assert.Equal(2, list.Items.Count);
    }

    [Fact]
    public void Table_RowsCellsColspan()
    {
        var doc = HtmlParser.Parse(
            "<table><tr><th>A</th><th>B</th></tr><tr><td colspan=2>fusion</td></tr></table>");
        var table = Assert.IsType<DocTable>(doc.Blocks[0]);
        Assert.Equal(2, table.Rows.Count);
        Assert.True(table.Rows[0].Cells[0].IsHeader);
        Assert.Equal(2, table.Rows[1].Cells[0].ColumnSpan);
        Assert.Equal(2, table.ColumnCount);
    }

    [Fact]
    public void Blockquote_NestsBlocks()
    {
        var doc = HtmlParser.Parse("<blockquote><p>cité</p></blockquote><p>réponse</p>");
        var quote = Assert.IsType<DocQuote>(doc.Blocks[0]);
        Assert.Equal("cité", FirstRun(Assert.IsType<DocParagraph>(quote.Blocks[0])).Text);
        Assert.Equal("réponse", FirstRun(Para(doc, 1)).Text);
    }

    [Fact]
    public void Pre_PreservesWhitespace()
    {
        var doc = HtmlParser.Parse("<pre>ligne 1\n  indent</pre>");
        var code = Assert.IsType<DocCodeBlock>(doc.Blocks[0]);
        Assert.Equal("ligne 1\n  indent", code.Text);
    }

    // ── Round-trip stockage : mark, paragraphes vides, imbrication ─

    [Fact]
    public void Mark_ReadsBackgroundFromStyle()
    {
        var doc = HtmlParser.Parse(
            "<p><mark data-color=\"#fff3a1\" style=\"background-color: #fff3a1\">x</mark></p>");
        Assert.Equal(Color.Parse("#fff3a1"), FirstRun(Para(doc)).Style.Background);
    }

    [Fact]
    public void Mark_FallsBackToDataColor()
    {
        var doc = HtmlParser.Parse("<mark data-color=\"rgb(255, 192, 120)\">x</mark>");
        Assert.Equal(Color.FromRgb(255, 192, 120), FirstRun(Para(doc)).Style.Background);
    }

    [Fact]
    public void Mark_Bare_DefaultsToYellow()
    {
        var doc = HtmlParser.Parse("<mark>x</mark>y");
        var p = Para(doc);
        Assert.Equal(Colors.Yellow, FirstRun(p).Style.Background);
        Assert.Null(Assert.IsType<DocRun>(p.Inlines[1]).Style.Background); // </mark> referme
    }

    [Fact]
    public void EmptyParagraph_SurvivesParsing()
    {
        var doc = HtmlParser.Parse("<p>un</p><p></p><p>deux</p>");
        Assert.Equal(3, doc.Blocks.Count);
        Assert.Empty(Para(doc, 1).Inlines);
    }

    [Fact]
    public void EmptyHeading_KeepsItsLevel()
    {
        var doc = HtmlParser.Parse("<h2></h2>");
        Assert.Equal(2, Para(doc).HeadingLevel);
    }

    [Fact]
    public void EmptyDiv_RendersNothing()
    {
        // Règle navigateur : <div></div> vide n'a pas de hauteur, <p></p> si
        var doc = HtmlParser.Parse("<p>a</p><div></div><p>b</p>");
        Assert.Equal(2, doc.Blocks.Count);
    }

    [Fact]
    public void EmptyListItemParagraph_SurvivesParsing()
    {
        var doc = HtmlParser.Parse("<ul><li><p></p></li></ul>");
        var list = Assert.IsType<DocList>(doc.Blocks[0]);
        Assert.Single(list.Items[0].Blocks);
    }

    [Fact]
    public void NestedList_CanonicalTipTapForm()
    {
        var doc = HtmlParser.Parse(
            "<ul><li><p>a</p><ul><li><p>b</p></li></ul></li><li><p>c</p></li></ul>");
        var list = Assert.IsType<DocList>(doc.Blocks[0]);
        Assert.Equal(2, list.Items.Count);
        Assert.Equal(2, list.Items[0].Blocks.Count);
        var sub = Assert.IsType<DocList>(list.Items[0].Blocks[1]);
        Assert.Equal("b", FirstRun(Assert.IsType<DocParagraph>(sub.Items[0].Blocks[0])).Text);
        Assert.Equal("c", FirstRun(Assert.IsType<DocParagraph>(list.Items[1].Blocks[0])).Text);
    }

    [Fact]
    public void ContentAfterNestedList_StaysInItsItem()
    {
        var doc = HtmlParser.Parse(
            "<ul><li><p>a</p><ul><li><p>b</p></li></ul><p>c</p></li></ul>");
        var list = Assert.IsType<DocList>(Assert.Single(doc.Blocks)); // rien à la racine
        Assert.Equal(3, list.Items[0].Blocks.Count);                  // p, sous-liste, p
    }

    [Fact]
    public void ContentAfterListInBlockquote_StaysInQuote()
    {
        var doc = HtmlParser.Parse(
            "<blockquote><ul><li><p>a</p></li></ul><p>c</p></blockquote>");
        var quote = Assert.IsType<DocQuote>(doc.Blocks[0]);
        Assert.Equal(2, quote.Blocks.Count);
    }

    [Fact]
    public void PreWithInnerCode_YieldsSingleCodeBlock()
    {
        var doc = HtmlParser.Parse("<pre><code>if (x &lt; 2) return;</code></pre>");
        var code = Assert.IsType<DocCodeBlock>(Assert.Single(doc.Blocks));
        Assert.Equal("if (x < 2) return;", code.Text);
    }

    [Fact]
    public void Tbody_IsTransparent()
    {
        var doc = HtmlParser.Parse("<table><tbody><tr><td>a</td></tr></tbody></table>");
        var table = Assert.IsType<DocTable>(doc.Blocks[0]);
        Assert.Single(table.Rows);
    }

    [Fact]
    public void CssColor_RgbWithSpaces_InSpan()
    {
        var doc = HtmlParser.Parse("<span style=\"color: rgb(209, 52, 56)\">x</span>");
        Assert.Equal(Color.FromRgb(209, 52, 56), FirstRun(Para(doc)).Style.Foreground);
    }

    [Fact]
    public void Image_TitleAttribute_IsKept()
    {
        var doc = HtmlParser.Parse("<img src=\"x.png\" alt=\"logo\" title=\"Logo\">");
        var img = Assert.IsType<DocImage>(Para(doc).Inlines[0]);
        Assert.Equal("Logo", img.Title);
    }

    [Fact]
    public void Link_TargetAndRel_AreTolerated()
    {
        var doc = HtmlParser.Parse(
            "<a target=\"_blank\" rel=\"noopener noreferrer nofollow\" href=\"https://aircal.nc\">x</a>");
        var link = Assert.IsType<DocLink>(Para(doc).Inlines[0]);
        Assert.Equal("https://aircal.nc", link.Href);
    }

    // ── Sanitisation ───────────────────────────────────────────────

    [Fact]
    public void Script_And_Style_AreEliminated()
    {
        var doc = HtmlParser.Parse(
            "<p>avant</p><script>alert('xss')</script><style>p{color:red}</style><p>après</p>");
        Assert.Equal(2, doc.Blocks.Count);
        Assert.DoesNotContain("alert", doc.GetText());
    }

    [Fact]
    public void Iframe_IsEliminated()
    {
        var doc = HtmlParser.Parse("<p>a</p><iframe src=\"https://evil\"></iframe><p>b</p>");
        Assert.Equal(2, doc.Blocks.Count);
    }

    [Fact]
    public void EventHandlers_AreNotRepresentable()
    {
        // onclick n'existe pas dans le modèle : perdu par construction
        var doc = HtmlParser.Parse("<p onclick=\"alert(1)\">x</p>");
        Assert.Equal("x", FirstRun(Para(doc)).Text);
    }

    // ── Entités et robustesse ──────────────────────────────────────

    [Theory]
    [InlineData("&amp;", "&")]
    [InlineData("&lt;b&gt;", "<b>")]
    [InlineData("&eacute;t&eacute;", "été")]
    [InlineData("&#233;", "é")]
    [InlineData("&#x27;", "'")]
    [InlineData("&nimporte;", "&nimporte;")]
    public void Entities_AreDecoded(string html, string expected)
        => Assert.Equal(expected, HtmlParser.DecodeEntities(html));

    [Fact]
    public void MalformedHtml_DoesNotThrow()
    {
        var samples = new[]
        {
            "<", "<p", "<p><b>x", "</div></div>", "<table><td>orphan",
            "<a href=>x</a>", "text < 5 et > 2", "<img src=\"data:image/png;base64,@@@\">",
            "<UL><LI>MAJUSCULES</UL>", ""
        };
        foreach (var html in samples)
        {
            var doc = HtmlParser.Parse(html);
            Assert.NotNull(doc);
            _ = doc.GetText();
        }
    }

    [Fact]
    public void RealisticEmail_ParsesCompletely()
    {
        var html = """
            <!DOCTYPE html><html><head><title>x</title><style>body{margin:0}</style></head>
            <body><div style="text-align:center"><h1>Newsletter</h1></div>
            <table><tr><td><b>Produit</b></td><td align=right>Prix</td></tr>
            <tr><td>Billet Nouméa</td><td>150&nbsp;000 XPF</td></tr></table>
            <p>Cordialement,<br>L&rsquo;équipe</p>
            <blockquote><p>Message d&#39;origine</p></blockquote>
            </body></html>
            """;

        var doc = HtmlParser.Parse(html);

        Assert.Contains(doc.Blocks, b => b is DocParagraph { HeadingLevel: 1 });
        Assert.Contains(doc.Blocks, b => b is DocTable { Rows.Count: 2 });
        Assert.Contains(doc.Blocks, b => b is DocQuote);
        Assert.Contains("L'équipe", doc.GetText());
        Assert.DoesNotContain("margin", doc.GetText());
    }

    // ── Modèle : GetText et Merge ──────────────────────────────────

    [Fact]
    public void GetText_FlattensDocument()
    {
        var doc = HtmlParser.Parse("<h1>T</h1><ul><li>a</li><li>b</li></ul>");
        var text = doc.GetText();
        Assert.Contains("T", text);
        Assert.Contains("• a", text);
        Assert.Contains("• b", text);
    }

    [Fact]
    public void DocStyle_Merge_OverlayWins_NullInherits()
    {
        var baseStyle = new DocStyle { Bold = true, FontSize = 14 };
        var merged = baseStyle.Merge(new DocStyle { FontSize = 20, Italic = true });

        Assert.True(merged.Bold);          // hérité
        Assert.Equal(20, merged.FontSize); // écrasé
        Assert.True(merged.Italic);        // ajouté
    }
}
