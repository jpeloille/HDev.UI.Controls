using Avalonia.Media;
using HDev.UI.Controls.Documents;
using Xunit;

namespace HDev.UI.Controls.Tests;

/// <summary>
/// RichEdit v2 : listes, alignement, couleurs/tailles, liens, collage HTML,
/// HTML de la sélection. Les documents sont construits via HtmlParser (même
/// chemin que HDevRichEdit.Html) et vérifiés via ToHtml.
/// </summary>
public class DocEditorV2Tests
{
    private static DocEditor Editor(string html) => new(HtmlParser.Parse(html));

    private static void SelectAll(DocEditor e)
    {
        e.Anchor = new DocCaret(0, 0);
        var last = e.Paragraphs.Count - 1;
        e.Caret = new DocCaret(last, e.GetParagraphLength(last));
    }

    // ── Listes : wrap / unwrap / conversion ──────────────────────────────────

    [Fact]
    public void ToggleBulletList_WrapsSelectedParagraphsIntoOneList()
    {
        var e = Editor("<p>un</p><p>deux</p><p>trois</p>");
        SelectAll(e);
        e.ToggleBulletList();

        var html = e.ToHtml();
        Assert.Contains("<ul><li><p>un</p></li><li><p>deux</p></li><li><p>trois</p></li></ul>", html);
        Assert.Equal(3, e.Paragraphs.Count); // texte inchangé, aplatissement stable
    }

    [Fact]
    public void ToggleBulletList_OnBulletedList_Unwraps()
    {
        var e = Editor("<ul><li>un</li><li>deux</li></ul>");
        SelectAll(e);
        e.ToggleBulletList();

        var html = e.ToHtml();
        Assert.DoesNotContain("<ul>", html);
        Assert.Contains("<p>un</p>", html);
        Assert.Contains("<p>deux</p>", html);
    }

    [Fact]
    public void ToggleNumberedList_OnBulletedList_ConvertsInPlace()
    {
        var e = Editor("<ul><li>un</li><li>deux</li></ul>");
        SelectAll(e);
        e.ToggleNumberedList();

        Assert.Contains("<ol>", e.ToHtml());
        Assert.DoesNotContain("<ul>", e.ToHtml());
    }

    [Fact]
    public void GetListState_ReportsListAndType()
    {
        var e = Editor("<ol><li>un</li></ol><p>hors</p>");
        e.Caret = new DocCaret(0, 1);
        Assert.Equal((true, true), e.GetListState());
        e.Caret = new DocCaret(1, 0);
        Assert.Equal((false, false), e.GetListState());
    }

    // ── Listes : Enter / Backspace ───────────────────────────────────────────

    [Fact]
    public void Enter_InListItem_CreatesNewItem()
    {
        var e = Editor("<ul><li>unDeux</li></ul>");
        e.Caret = new DocCaret(0, 2); // entre "un" et "Deux"
        e.SplitParagraph();

        Assert.Contains("<ul><li><p>un</p></li><li><p>Deux</p></li></ul>", e.ToHtml());
        Assert.Equal(new DocCaret(1, 0), e.Caret);
    }

    [Fact]
    public void Enter_OnEmptyListItem_ExitsList()
    {
        // Flux réel : Enter en fin d'item crée un item vide, Enter à nouveau SORT
        var e = Editor("<ul><li>un</li></ul>");
        e.Caret = new DocCaret(0, 2);
        e.SplitParagraph(); // nouvel item vide
        Assert.Equal((true, false), e.GetListState());

        e.SplitParagraph(); // item vide → sortie de liste
        var html = e.ToHtml();
        Assert.Contains("<ul><li><p>un</p></li></ul>", html);
        Assert.EndsWith("<p></p>", html); // le paragraphe est SORTI de la liste
        Assert.Equal((false, false), e.GetListState());
    }

    [Fact]
    public void Backspace_AtStartOfListItem_RemovesBullet()
    {
        var e = Editor("<ul><li>un</li><li>deux</li></ul>");
        e.Caret = new DocCaret(1, 0);
        e.DeleteBackward();

        var html = e.ToHtml();
        Assert.Contains("<ul><li><p>un</p></li></ul>", html);
        Assert.Contains("<p>deux</p>", html);   // sorti, PAS fusionné dans « un »
        Assert.Equal("deux", e.GetParagraphText(1));
    }

    [Fact]
    public void Unwrap_MiddleItem_SplitsListInTwo()
    {
        var e = Editor("<ul><li>a</li><li>b</li><li>c</li></ul>");
        e.Caret = new DocCaret(1, 0);
        e.DeleteBackward(); // b sort du milieu

        var html = e.ToHtml();
        Assert.Contains("<ul><li><p>a</p></li></ul><p>b</p><ul><li><p>c</p></li></ul>", html);
    }

    // ── Alignement ───────────────────────────────────────────────────────────

    [Fact]
    public void SetAlignment_AppliesAndSerializes()
    {
        var e = Editor("<p>texte</p>");
        e.Caret = new DocCaret(0, 2);
        e.SetAlignment(TextAlignment.Center);
        Assert.Contains("text-align:center", e.ToHtml());

        e.SetAlignment(TextAlignment.Justify);
        Assert.Contains("text-align:justify", e.ToHtml());
    }

    // ── Couleurs / taille (pose ET effacement) ───────────────────────────────

    [Fact]
    public void SetTextColor_AppliesThenClears()
    {
        var e = Editor("<p>rouge</p>");
        SelectAll(e);
        e.SetTextColor(Color.Parse("#D13438"));
        Assert.Contains("color:#d13438", e.ToHtml());

        SelectAll(e);
        e.SetTextColor(null); // effacement — impossible via ApplyStyle/Merge
        Assert.DoesNotContain("color:", e.ToHtml());
    }

    [Fact]
    public void SetFontSize_OnPartialSelection_SplitsRuns()
    {
        var e = Editor("<p>abcdef</p>");
        e.Anchor = new DocCaret(0, 2);
        e.Caret = new DocCaret(0, 4);
        e.SetFontSize(20);

        var html = e.ToHtml();
        Assert.Contains("font-size:20px", html);
        Assert.Contains("ab", html);
        Assert.Equal("abcdef", e.GetParagraphText(0)); // texte intact
    }

    // ── Liens ────────────────────────────────────────────────────────────────

    [Fact]
    public void SetLink_AppliesToSelection_AndReads()
    {
        var e = Editor("<p>voir le site</p>");
        e.Anchor = new DocCaret(0, 5);
        e.Caret = new DocCaret(0, 12);
        e.SetLink("https://aircal.nc");

        Assert.Contains("<a target=\"_blank\" rel=\"noopener noreferrer nofollow\" href=\"https://aircal.nc\">le site</a>", e.ToHtml());
        Assert.Equal("https://aircal.nc", e.GetCurrentLink());
    }

    [Fact]
    public void SetLink_Null_RemovesLink()
    {
        var e = Editor("<p><a href=\"https://x.nc\">lien</a></p>");
        SelectAll(e);
        e.SetLink(null);
        Assert.DoesNotContain("<a ", e.ToHtml());
    }

    // ── Collage HTML ─────────────────────────────────────────────────────────

    [Fact]
    public void InsertHtml_SingleParagraph_MergesInline()
    {
        var e = Editor("<p>avantaprès</p>");
        e.Caret = new DocCaret(0, 5);
        e.InsertHtml("<p>du <b>gras</b></p>");

        Assert.Equal("avantdu grasaprès", e.GetParagraphText(0));
        Assert.Contains("<strong>gras</strong>", e.ToHtml());
        Assert.Equal(new DocCaret(0, 12), e.Caret); // après le fragment
    }

    [Fact]
    public void InsertHtml_MultiBlock_SplitsAtCaret()
    {
        var e = Editor("<p>têtequeue</p>");
        e.Caret = new DocCaret(0, 4);
        e.InsertHtml("<p>un</p><ul><li>item</li></ul>");

        var html = e.ToHtml();
        Assert.Contains("<p>tête</p><p>un</p><ul><li><p>item</p></li></ul><p>queue</p>", html);
        Assert.Equal("queue", e.GetParagraphText(e.Caret.Block)); // caret sur la queue
    }

    [Fact]
    public void InsertHtml_IsOneUndoStep()
    {
        var e = Editor("<p>base</p>");
        e.Caret = new DocCaret(0, 4);
        e.InsertHtml("<p>x</p><p>y</p>");
        Assert.True(e.Paragraphs.Count > 1);

        e.Undo();
        Assert.Equal("<p>base</p>", e.ToHtml());
    }

    [Fact]
    public void InsertImage_BecomesStandaloneBlock()
    {
        var e = Editor("<p>texte</p>");
        e.Caret = new DocCaret(0, 5);
        e.InsertImage(new DocImage { Source = "data:image/png;base64,AAAA", Alt = "logo", Width = 40 });

        var html = e.ToHtml();
        Assert.Contains("<img src=\"data:image/png;base64,AAAA\"", html);
        Assert.Contains("alt=\"logo\"", html);
    }

    // ── HTML de la sélection (copie riche) ───────────────────────────────────

    [Fact]
    public void GetSelectedHtml_PreservesStructureAndStyles()
    {
        var e = Editor("<p>avant</p><ul><li><b>gras</b></li><li>deux</li></ul><p>après</p>");
        e.Anchor = new DocCaret(1, 0);              // début de « gras »
        e.Caret = new DocCaret(2, 4);               // fin de « deux »
        var html = e.GetSelectedHtml();

        Assert.Contains("<ul>", html);              // structure liste préservée
        Assert.Contains("<strong>gras</strong>", html);
        Assert.Contains("deux", html);
        Assert.DoesNotContain("avant", html);
        Assert.DoesNotContain("après", html);
    }

    [Fact]
    public void GetSelectedHtml_DoesNotMutateDocument()
    {
        var e = Editor("<p>un</p><p>deux</p>");
        var before = e.ToHtml();
        e.Anchor = new DocCaret(0, 1);
        e.Caret = new DocCaret(1, 2);
        _ = e.GetSelectedHtml();
        Assert.Equal(before, e.ToHtml());
        Assert.False(e.CanRedo); // l'éditeur TEMPORAIRE a poussé l'undo, pas nous
    }

    // ── Non-régression : Enter hors liste conserve le comportement v1 ────────

    [Fact]
    public void Enter_OutsideList_StillSplitsParagraph()
    {
        var e = Editor("<p>unDeux</p>");
        e.Caret = new DocCaret(0, 2);
        e.SplitParagraph();
        Assert.Equal("<p>un</p><p>Deux</p>", e.ToHtml());
    }

    // ── Dialecte storage-v1 : surlignage et bloc de code ─────────────────────

    [Fact]
    public void SetHighlight_SerializesAsMark_AndRoundTrips()
    {
        var e = Editor("<p>surligné</p>");
        SelectAll(e);
        e.SetHighlight(Color.Parse("#fff3a1"));

        var html = e.ToHtml();
        Assert.Contains("<mark data-color=\"#fff3a1\" style=\"background-color: #fff3a1\">surligné</mark>", html);

        // Round-trip : le collage entre deux HDevRichEdit ne perd pas le surlignage
        var e2 = Editor(html);
        Assert.Equal(html, e2.ToHtml());
    }

    [Fact]
    public void CodeBlock_SerializesAsPreCode_AndRoundTrips()
    {
        var e = Editor("<pre><code>if (x &lt; 2) return;</code></pre>");
        var html = e.ToHtml();
        Assert.Contains("<pre><code>if (x &lt; 2) return;</code></pre>", html);
        Assert.Equal(html, Editor(html).ToHtml());
    }
}
