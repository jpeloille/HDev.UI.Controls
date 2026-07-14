using ProControls.Documents;
using Xunit;

namespace ProControls.Tests;

public class DocEditorTests
{
    private static DocEditor FromHtml(string html) => new(HtmlParser.Parse(html));

    // ── Insertion ──────────────────────────────────────────────────

    [Fact]
    public void InsertText_AtCaret()
    {
        var editor = FromHtml("<p>bonour</p>");
        editor.Caret = new DocCaret(0, 3);
        editor.InsertText("j");
        Assert.Equal("bonjour", editor.GetParagraphText(0));
        Assert.Equal(4, editor.Caret.Offset);
    }

    [Fact]
    public void InsertText_InheritsLeftStyle()
    {
        var editor = FromHtml("<p><b>gras</b>normal</p>");
        editor.Caret = new DocCaret(0, 4); // fin de « gras »
        editor.InsertText("X");
        // Le X rejoint le run gras
        var run = Assert.IsType<DocRun>(editor.Paragraphs[0].Inlines[0]);
        Assert.Equal("grasX", run.Text);
        Assert.True(run.Style.Bold);
    }

    [Fact]
    public void InsertText_ReplacesSelection()
    {
        var editor = FromHtml("<p>bonjour monde</p>");
        editor.Anchor = new DocCaret(0, 8);
        editor.Caret = new DocCaret(0, 13);
        editor.InsertText("Julien");
        Assert.Equal("bonjour Julien", editor.GetParagraphText(0));
    }

    [Fact]
    public void EmptyDocument_AcceptsTyping()
    {
        var editor = new DocEditor();
        editor.InsertText("a");
        Assert.Equal("a", editor.GetParagraphText(0));
    }

    // ── Suppression ────────────────────────────────────────────────

    [Fact]
    public void DeleteBackward_MidText()
    {
        var editor = FromHtml("<p>abc</p>");
        editor.Caret = new DocCaret(0, 2);
        editor.DeleteBackward();
        Assert.Equal("ac", editor.GetParagraphText(0));
        Assert.Equal(1, editor.Caret.Offset);
    }

    [Fact]
    public void DeleteBackward_AtStart_MergesParagraphs()
    {
        var editor = FromHtml("<p>un</p><p>deux</p>");
        editor.Caret = new DocCaret(1, 0);
        editor.DeleteBackward();
        Assert.Single(editor.Paragraphs);
        Assert.Equal("undeux", editor.GetParagraphText(0));
        Assert.Equal(new DocCaret(0, 2), editor.Caret);
    }

    [Fact]
    public void DeleteForward_AtEnd_MergesWithNext()
    {
        var editor = FromHtml("<p>un</p><p>deux</p>");
        editor.Caret = new DocCaret(0, 2);
        editor.DeleteForward();
        Assert.Single(editor.Paragraphs);
        Assert.Equal("undeux", editor.GetParagraphText(0));
    }

    [Fact]
    public void DeleteSelection_AcrossParagraphs()
    {
        var editor = FromHtml("<p>alpha</p><p>beta</p><p>gamma</p>");
        editor.Anchor = new DocCaret(0, 2);
        editor.Caret = new DocCaret(2, 3);
        editor.DeleteSelection();
        Assert.Single(editor.Paragraphs);
        Assert.Equal("alma", editor.GetParagraphText(0));
        Assert.Equal(new DocCaret(0, 2), editor.Caret);
    }

    // ── Enter / structure ──────────────────────────────────────────

    [Fact]
    public void SplitParagraph_MidText()
    {
        var editor = FromHtml("<p>bonjour</p>");
        editor.Caret = new DocCaret(0, 3);
        editor.SplitParagraph();
        Assert.Equal(2, editor.Paragraphs.Count);
        Assert.Equal("bon", editor.GetParagraphText(0));
        Assert.Equal("jour", editor.GetParagraphText(1));
        Assert.Equal(new DocCaret(1, 0), editor.Caret);
    }

    [Fact]
    public void SplitParagraph_PreservesStyleOnTail()
    {
        var editor = FromHtml("<p><b>grasgras</b></p>");
        editor.Caret = new DocCaret(0, 4);
        editor.SplitParagraph();
        var tail = Assert.IsType<DocRun>(editor.Paragraphs[1].Inlines[0]);
        Assert.True(tail.Style.Bold);
        Assert.Equal("gras", tail.Text);
    }

    [Fact]
    public void SplitParagraph_InsideListItem()
    {
        var editor = FromHtml("<ul><li>item un</li></ul>");
        editor.Caret = new DocCaret(0, 4);
        editor.SplitParagraph();
        Assert.Equal(2, editor.Paragraphs.Count);
        Assert.Equal("item", editor.GetParagraphText(0));
    }

    // ── Styles ─────────────────────────────────────────────────────

    [Fact]
    public void ApplyStyle_SplitsRunsAtBoundaries()
    {
        var editor = FromHtml("<p>bonjour</p>");
        editor.Anchor = new DocCaret(0, 3);
        editor.Caret = new DocCaret(0, 5);
        editor.ApplyStyle(new DocStyle { Bold = true });

        var runs = editor.Paragraphs[0].Inlines.OfType<DocRun>().ToList();
        Assert.Equal(3, runs.Count);
        Assert.Equal("bon", runs[0].Text);
        Assert.NotEqual(true, runs[0].Style.Bold);
        Assert.Equal("jo", runs[1].Text);
        Assert.True(runs[1].Style.Bold);
        Assert.Equal("ur", runs[2].Text);
    }

    [Fact]
    public void ToggleBold_OnAndOff()
    {
        var editor = FromHtml("<p>texte</p>");
        editor.Anchor = new DocCaret(0, 0);
        editor.Caret = new DocCaret(0, 5);

        editor.ToggleBold();
        Assert.True(editor.GetCurrentStyle().Bold);

        editor.ToggleBold();
        Assert.NotEqual(true, editor.GetCurrentStyle().Bold);
        Assert.Equal("texte", editor.GetParagraphText(0)); // texte intact
    }

    [Fact]
    public void ApplyStyle_AcrossParagraphs()
    {
        var editor = FromHtml("<p>aaa</p><p>bbb</p>");
        editor.Anchor = new DocCaret(0, 1);
        editor.Caret = new DocCaret(1, 2);
        editor.ApplyStyle(new DocStyle { Italic = true });

        Assert.True(editor.Paragraphs[0].Inlines.OfType<DocRun>().Last().Style.Italic);
        Assert.True(editor.Paragraphs[1].Inlines.OfType<DocRun>().First().Style.Italic);
    }

    [Fact]
    public void SetHeading_OnCaretParagraph()
    {
        var editor = FromHtml("<p>titre</p>");
        editor.SetHeading(2);
        Assert.Equal(2, editor.Paragraphs[0].HeadingLevel);
        editor.SetHeading(0);
        Assert.Equal(0, editor.Paragraphs[0].HeadingLevel);
    }

    // ── Sélection / texte ──────────────────────────────────────────

    [Fact]
    public void GetSelectedText_MultiParagraph()
    {
        var editor = FromHtml("<p>alpha</p><p>beta</p>");
        editor.Anchor = new DocCaret(0, 3);
        editor.Caret = new DocCaret(1, 2);
        Assert.Equal("ha\nbe", editor.GetSelectedText());
    }

    [Fact]
    public void SelectionRange_IsOrdered_WhenSelectingBackwards()
    {
        var editor = FromHtml("<p>abc</p>");
        editor.Anchor = new DocCaret(0, 3);
        editor.Caret = new DocCaret(0, 1);
        var (start, end) = editor.SelectionRange();
        Assert.Equal(1, start.Offset);
        Assert.Equal(3, end.Offset);
    }

    // ── Liens normalisés ───────────────────────────────────────────

    [Fact]
    public void Links_AreNormalizedToRuns()
    {
        var editor = FromHtml("<p>voir <a href=\"https://x.nc\">ici</a> svp</p>");
        var runs = editor.Paragraphs[0].Inlines.OfType<DocRun>().ToList();
        Assert.Equal(3, runs.Count);
        Assert.Equal("https://x.nc", runs[1].LinkHref);
        Assert.Equal("voir ici svp", editor.GetParagraphText(0));
    }

    // ── Undo / Redo ────────────────────────────────────────────────

    [Fact]
    public void Undo_RestoresText_AndRedoReplays()
    {
        var editor = FromHtml("<p>base</p>");
        editor.Caret = new DocCaret(0, 4);
        editor.InsertText("X");
        Assert.Equal("baseX", editor.GetParagraphText(0));

        editor.Undo();
        Assert.Equal("base", editor.GetParagraphText(0));

        editor.Redo();
        Assert.Equal("baseX", editor.GetParagraphText(0));
    }

    [Fact]
    public void ConsecutiveTyping_IsCoalescedInOneUndoStep()
    {
        var editor = FromHtml("<p></p>");
        editor.InsertText("a");
        editor.InsertText("b");
        editor.InsertText("c");
        editor.Undo();
        Assert.Equal("", editor.GetParagraphText(0));
    }

    [Fact]
    public void Undo_RestoresStructure()
    {
        var editor = FromHtml("<p>unique</p>");
        editor.Caret = new DocCaret(0, 3);
        editor.SplitParagraph();
        Assert.Equal(2, editor.Paragraphs.Count);

        editor.Undo();
        Assert.Single(editor.Paragraphs);
        Assert.Equal("unique", editor.GetParagraphText(0));
    }

    // ── Sérialisation HTML ─────────────────────────────────────────

    [Fact]
    public void ToHtml_SerializesStyles()
    {
        var editor = FromHtml("<p>normal <b>gras</b> <i>italique</i></p>");
        var html = editor.ToHtml();
        Assert.Contains("<b>gras</b>", html);
        Assert.Contains("<i>italique</i>", html);
    }

    [Fact]
    public void ToHtml_EscapesSpecialCharacters()
    {
        var editor = new DocEditor();
        editor.InsertText("a < b & c");
        Assert.Contains("a &lt; b &amp; c", editor.ToHtml());
    }

    [Fact]
    public void ToHtml_MergesAdjacentLinkRuns()
    {
        var editor = FromHtml("<p><a href=\"https://x.nc\">un <b>lien</b> long</a></p>");
        var html = editor.ToHtml();
        // Un seul <a> malgré les 3 runs normalisés
        Assert.Equal(1, html.Split("<a ").Length - 1);
        Assert.Contains("href=\"https://x.nc\"", html);
    }

    [Fact]
    public void RoundTrip_HtmlToEditorToHtml_IsStable()
    {
        const string source = "<h2>Titre</h2><p>Du <b>gras</b> et un <a href=\"https://x.nc\">lien</a>.</p><ul><li>a</li><li>b</li></ul>";
        var editor = FromHtml(source);
        var html = editor.ToHtml();

        // Re-parse du HTML produit : le texte doit être identique
        var second = FromHtml(html);
        Assert.Equal(editor.Document.GetText(), second.Document.GetText());
        Assert.Contains("<h2>", html);
        Assert.Contains("<ul>", html);
    }
}
