using System.Text;

namespace ProControls.Documents;

/// <summary>Position d'édition : paragraphe éditable + offset caractère</summary>
public readonly record struct DocCaret(int Block, int Offset) : IComparable<DocCaret>
{
    public int CompareTo(DocCaret other)
        => Block != other.Block ? Block.CompareTo(other.Block) : Offset.CompareTo(other.Offset);
}

/// <summary>
/// Cœur d'édition du document (monstre n° 2, moteur) : positions, insertion/
/// suppression, découpe de runs pour le style par plage, fusion/scission de
/// paragraphes, undo/redo par instantanés. Logique pure, testée.
/// V1 : édite les paragraphes (y compris dans listes/citations) ; tables,
/// code et images sont des îlots non éditables.
/// </summary>
public class DocEditor
{
    private readonly List<(ProDocument Doc, DocCaret Caret)> _undo = new();
    private readonly List<(ProDocument Doc, DocCaret Caret)> _redo = new();
    private bool _lastOpWasTyping;
    private const int UndoLimit = 100;

    public ProDocument Document { get; private set; }

    /// <summary>Paragraphes éditables, aplatis (racine + listes + citations)</summary>
    public List<DocParagraph> Paragraphs { get; } = new();

    public DocCaret Caret { get; set; }

    /// <summary>Ancre de sélection (null = pas de sélection)</summary>
    public DocCaret? Anchor { get; set; }

    public bool HasSelection => Anchor.HasValue && Anchor.Value != Caret;

    /// <summary>Déclenché après toute mutation (le contrôle relance le layout)</summary>
    public event EventHandler? Changed;

    public DocEditor(ProDocument? document = null)
    {
        Document = document ?? new ProDocument();
        Normalize(Document);
        Reflatten();
        if (Paragraphs.Count == 0)
        {
            Document.Blocks.Add(new DocParagraph());
            Reflatten();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // NORMALISATION + APLATISSEMENT
    // ═══════════════════════════════════════════════════════════════

    /// <summary>DocLink (conteneurs du parseur) → runs plats portant LinkHref</summary>
    private static void Normalize(ProDocument document)
    {
        foreach (var paragraph in AllParagraphs(document.Blocks))
        {
            for (int i = 0; i < paragraph.Inlines.Count; i++)
            {
                if (paragraph.Inlines[i] is not DocLink link) continue;

                paragraph.Inlines.RemoveAt(i);
                var insert = i;
                foreach (var child in link.Children)
                {
                    if (child is DocRun run)
                        run.LinkHref = link.Href;
                    paragraph.Inlines.Insert(insert++, child);
                }
                i = insert - 1;
            }
        }
    }

    private static IEnumerable<DocParagraph> AllParagraphs(List<DocBlock> blocks)
    {
        foreach (var block in blocks)
        {
            switch (block)
            {
                case DocParagraph p:
                    yield return p;
                    break;
                case DocList list:
                    foreach (var item in list.Items)
                        foreach (var p in AllParagraphs(item.Blocks))
                            yield return p;
                    break;
                case DocQuote quote:
                    foreach (var p in AllParagraphs(quote.Blocks))
                        yield return p;
                    break;
            }
        }
    }

    private void Reflatten()
    {
        Paragraphs.Clear();
        Paragraphs.AddRange(AllParagraphs(Document.Blocks));
    }

    // ═══════════════════════════════════════════════════════════════
    // TEXTE ET OFFSETS (même espace que le layout : runs + \n pour br)
    // ═══════════════════════════════════════════════════════════════

    public string GetParagraphText(int block)
    {
        if (block < 0 || block >= Paragraphs.Count) return "";
        var sb = new StringBuilder();
        foreach (var inline in Paragraphs[block].Inlines)
        {
            if (inline is DocRun run) sb.Append(run.Text);
            else if (inline is DocLineBreak) sb.Append('\n');
        }
        return sb.ToString();
    }

    public int GetParagraphLength(int block) => GetParagraphText(block).Length;

    /// <summary>Bornes ordonnées de la sélection</summary>
    public (DocCaret Start, DocCaret End) SelectionRange()
    {
        var anchor = Anchor ?? Caret;
        return anchor.CompareTo(Caret) <= 0 ? (anchor, Caret) : (Caret, anchor);
    }

    /// <summary>Texte brut de la sélection (copie)</summary>
    public string GetSelectedText()
    {
        if (!HasSelection) return "";
        var (start, end) = SelectionRange();
        if (start.Block == end.Block)
            return GetParagraphText(start.Block)[start.Offset..end.Offset];

        var sb = new StringBuilder();
        sb.Append(GetParagraphText(start.Block)[start.Offset..]).Append('\n');
        for (int b = start.Block + 1; b < end.Block; b++)
            sb.Append(GetParagraphText(b)).Append('\n');
        sb.Append(GetParagraphText(end.Block)[..end.Offset]);
        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    // OPÉRATIONS
    // ═══════════════════════════════════════════════════════════════

    public void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        PushUndo(coalesceTyping: !HasSelection && text.Length == 1);

        if (HasSelection)
            DeleteRangeInternal();

        var paragraph = Paragraphs[Caret.Block];
        var (runIndex, runOffset, style) = LocateRun(paragraph, Caret.Offset);

        if (runIndex >= 0 && paragraph.Inlines[runIndex] is DocRun target)
        {
            target.Text = target.Text.Insert(runOffset, text);
        }
        else
        {
            var run = new DocRun(text, style);
            paragraph.Inlines.Insert(Math.Max(0, runIndex == -1 ? paragraph.Inlines.Count : runIndex), run);
        }

        Caret = Caret with { Offset = Caret.Offset + text.Length };
        Anchor = null;
        RaiseChanged();
    }

    /// <summary>
    /// Enter : scinde le paragraphe au caret. Dans une liste : crée un NOUVEL
    /// ITEM ; sur un item vide : sort de la liste (idiome Word).
    /// </summary>
    public void SplitParagraph()
    {
        PushUndo();
        if (HasSelection) DeleteRangeInternal();

        var paragraph = Paragraphs[Caret.Block];
        var context = FindListItem(paragraph);

        if (context is { } ctx)
        {
            // Item vide → Enter SORT de la liste
            if (GetParagraphLength(Caret.Block) == 0 && ctx.Item.Blocks.Count == 1)
            {
                UnwrapFromList(paragraph);
                Reflatten();
                Caret = ClampCaret(Caret with { Offset = 0 });
                Anchor = null;
                RaiseChanged();
                return;
            }

            // Scission en nouvel item après l'item courant
            var itemParagraph = new DocParagraph { Alignment = paragraph.Alignment };
            MoveTail(paragraph, Caret.Offset, itemParagraph);
            var newItem = new DocListItem();
            newItem.Blocks.Add(itemParagraph);
            ctx.List.Items.Insert(ctx.ItemIndex + 1, newItem);
            Reflatten();
            Caret = new DocCaret(Caret.Block + 1, 0);
            Anchor = null;
            RaiseChanged();
            return;
        }

        var newParagraph = new DocParagraph { Alignment = paragraph.Alignment };
        MoveTail(paragraph, Caret.Offset, newParagraph);

        InsertBlockAfter(paragraph, newParagraph);
        Reflatten();

        Caret = new DocCaret(Caret.Block + 1, 0);
        Anchor = null;
        RaiseChanged();
    }

    public void DeleteBackward()
    {
        if (HasSelection) { DeleteSelection(); return; }
        if (Caret.Offset > 0)
        {
            PushUndo(coalesceTyping: true);
            DeleteTextInParagraph(Paragraphs[Caret.Block], Caret.Offset - 1, 1);
            Caret = Caret with { Offset = Caret.Offset - 1 };
            RaiseChanged();
        }
        // Début du premier paragraphe d'un item de liste : Backspace RETIRE la
        // puce (le paragraphe sort de la liste) au lieu de fusionner (Word)
        else if (FindListItem(Paragraphs[Caret.Block]) is { } ctx &&
                 ReferenceEquals(ctx.Item.Blocks.FirstOrDefault(), Paragraphs[Caret.Block]))
        {
            PushUndo();
            UnwrapFromList(Paragraphs[Caret.Block]);
            Reflatten();
            Caret = ClampCaret(Caret with { Offset = 0 });
            RaiseChanged();
        }
        else if (Caret.Block > 0)
        {
            PushUndo();
            var previousLength = GetParagraphLength(Caret.Block - 1);
            MergeWithPrevious(Caret.Block);
            Reflatten();
            Caret = new DocCaret(Caret.Block - 1, previousLength);
            RaiseChanged();
        }
    }

    public void DeleteForward()
    {
        if (HasSelection) { DeleteSelection(); return; }
        if (Caret.Offset < GetParagraphLength(Caret.Block))
        {
            PushUndo(coalesceTyping: true);
            DeleteTextInParagraph(Paragraphs[Caret.Block], Caret.Offset, 1);
            RaiseChanged();
        }
        else if (Caret.Block < Paragraphs.Count - 1)
        {
            PushUndo();
            MergeWithPrevious(Caret.Block + 1);
            Reflatten();
            RaiseChanged();
        }
    }

    public void DeleteSelection()
    {
        if (!HasSelection) return;
        PushUndo();
        DeleteRangeInternal();
        RaiseChanged();
    }

    /// <summary>Applique un style sur la sélection (découpe des runs aux frontières)</summary>
    public void ApplyStyle(DocStyle overlay)
    {
        if (!HasSelection) return;
        PushUndo();

        var (start, end) = SelectionRange();
        for (int b = start.Block; b <= end.Block; b++)
        {
            var from = b == start.Block ? start.Offset : 0;
            var to = b == end.Block ? end.Offset : GetParagraphLength(b);
            ApplyStyleToRange(Paragraphs[b], from, to, overlay);
        }
        RaiseChanged();
    }

    /// <summary>Style au caret / sur la sélection (état des boutons de la barre)</summary>
    public DocStyle GetCurrentStyle()
    {
        var (start, _) = SelectionRange();
        var paragraph = Paragraphs[Math.Clamp(start.Block, 0, Paragraphs.Count - 1)];
        var probe = HasSelection ? start.Offset : Math.Max(0, Caret.Offset - 1);
        var (runIndex, _, style) = LocateRun(paragraph, probe);
        if (runIndex >= 0 && runIndex < paragraph.Inlines.Count &&
            paragraph.Inlines[runIndex] is DocRun run)
            return run.Style;
        return style;
    }

    public void ToggleBold() => ToggleFlag(s => s.Bold, v => new DocStyle { Bold = v });
    public void ToggleItalic() => ToggleFlag(s => s.Italic, v => new DocStyle { Italic = v });
    public void ToggleUnderline() => ToggleFlag(s => s.Underline, v => new DocStyle { Underline = v });
    public void ToggleStrikethrough() => ToggleFlag(s => s.Strikethrough, v => new DocStyle { Strikethrough = v });

    private void ToggleFlag(Func<DocStyle, bool?> read, Func<bool, DocStyle> make)
    {
        var current = read(GetCurrentStyle()) == true;
        ApplyStyle(make(!current));
    }

    /// <summary>Niveau de titre des paragraphes de la sélection (0 = normal)</summary>
    public void SetHeading(int level)
    {
        PushUndo();
        var (start, end) = SelectionRange();
        for (int b = start.Block; b <= end.Block && b < Paragraphs.Count; b++)
            Paragraphs[b].HeadingLevel = Math.Clamp(level, 0, 6);
        RaiseChanged();
    }

    // ═══════════════════════════════════════════════════════════════
    // V2 — PARAGRAPHE : alignement
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Alignement des paragraphes de la sélection (ou du paragraphe courant)</summary>
    public void SetAlignment(Avalonia.Media.TextAlignment alignment)
    {
        PushUndo();
        var (start, end) = SelectionRange();
        for (int b = start.Block; b <= end.Block && b < Paragraphs.Count; b++)
            Paragraphs[b].Alignment = alignment;
        RaiseChanged();
    }

    // ═══════════════════════════════════════════════════════════════
    // V2 — STYLE : couleurs, taille (transform = sait aussi EFFACER,
    // ce que ApplyStyle/Merge ne permet pas — un null y hérite)
    // ═══════════════════════════════════════════════════════════════

    public void SetTextColor(Avalonia.Media.Color? color)
        => TransformSelection(run => run.Style = run.Style with { Foreground = color });

    public void SetHighlight(Avalonia.Media.Color? color)
        => TransformSelection(run => run.Style = run.Style with { Background = color });

    public void SetFontSize(double? size)
        => TransformSelection(run => run.Style = run.Style with { FontSize = size });

    // ═══════════════════════════════════════════════════════════════
    // V2 — LIENS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Pose (ou retire : null) un lien sur la sélection</summary>
    public void SetLink(string? href)
        => TransformSelection(run => run.LinkHref = string.IsNullOrWhiteSpace(href) ? null : href);

    /// <summary>Lien au caret / au début de la sélection (null si aucun)</summary>
    public string? GetCurrentLink()
    {
        var (start, _) = SelectionRange();
        var paragraph = Paragraphs[Math.Clamp(start.Block, 0, Paragraphs.Count - 1)];
        // LocateRun est biaisé à GAUCHE aux frontières de runs : pour une
        // sélection, sonder start+1 vise le PREMIER caractère sélectionné
        // (sinon on lirait le run juste avant la sélection).
        var probe = HasSelection
            ? Math.Min(start.Offset + 1, GetParagraphLength(start.Block))
            : Caret.Offset;
        var (runIndex, _, _) = LocateRun(paragraph, probe);
        return runIndex >= 0 && runIndex < paragraph.Inlines.Count &&
               paragraph.Inlines[runIndex] is DocRun run ? run.LinkHref : null;
    }

    /// <summary>Applique une mutation aux runs couverts par la sélection (découpe aux frontières)</summary>
    private void TransformSelection(Action<DocRun> transform)
    {
        if (!HasSelection) return;
        PushUndo();

        var (start, end) = SelectionRange();
        for (int b = start.Block; b <= end.Block; b++)
        {
            var paragraph = Paragraphs[b];
            var from = b == start.Block ? start.Offset : 0;
            var to = b == end.Block ? end.Offset : GetParagraphLength(b);
            if (to <= from) continue;

            SplitRunAt(paragraph, from);
            SplitRunAt(paragraph, to);

            var position = 0;
            foreach (var inline in paragraph.Inlines)
            {
                switch (inline)
                {
                    case DocRun run:
                        if (position >= from && position + run.Text.Length <= to)
                            transform(run);
                        position += run.Text.Length;
                        break;
                    case DocLineBreak:
                        position++;
                        break;
                }
            }
        }
        RaiseChanged();
    }

    // ═══════════════════════════════════════════════════════════════
    // V2 — LISTES (puces / numérotées)
    // ═══════════════════════════════════════════════════════════════

    public void ToggleBulletList() => ToggleList(ordered: false);
    public void ToggleNumberedList() => ToggleList(ordered: true);

    /// <summary>Le paragraphe courant est-il dans une liste (et de quel type) ?</summary>
    public (bool InList, bool Ordered) GetListState()
    {
        var (start, _) = SelectionRange();
        var paragraph = Paragraphs[Math.Clamp(start.Block, 0, Paragraphs.Count - 1)];
        return FindListItem(paragraph) is { } ctx ? (true, ctx.List.Ordered) : (false, false);
    }

    private void ToggleList(bool ordered)
    {
        PushUndo();
        var (start, end) = SelectionRange();
        var targets = new List<DocParagraph>();
        for (int b = start.Block; b <= end.Block && b < Paragraphs.Count; b++)
            targets.Add(Paragraphs[b]);

        // Tous déjà dans une liste du bon type → SORTIR de la liste
        if (targets.All(p => FindListItem(p) is { } c && c.List.Ordered == ordered))
        {
            foreach (var paragraph in targets)
                UnwrapFromList(paragraph);
        }
        else
        {
            foreach (var paragraph in targets)
            {
                if (FindListItem(paragraph) is { } ctx)
                {
                    ctx.List.Ordered = ordered; // conversion de type sur place
                    continue;
                }

                var parent = FindParentCollection(paragraph);
                if (parent == null) continue;
                var index = parent.IndexOf(paragraph);

                // Fusion avec une liste adjacente du même type (paragraphes
                // consécutifs sélectionnés → UNE liste, pas N)
                if (index > 0 && parent[index - 1] is DocList previous && previous.Ordered == ordered)
                {
                    parent.RemoveAt(index);
                    var item = new DocListItem();
                    item.Blocks.Add(paragraph);
                    previous.Items.Add(item);
                }
                else
                {
                    var list = new DocList { Ordered = ordered };
                    var item = new DocListItem();
                    item.Blocks.Add(paragraph);
                    list.Items.Add(item);
                    parent[index] = list;
                }
            }
        }

        Reflatten();
        Caret = ClampCaret(Caret);
        if (Anchor.HasValue) Anchor = ClampCaret(Anchor.Value);
        RaiseChanged();
    }

    /// <summary>Contexte de liste d'un paragraphe (item DIRECT uniquement)</summary>
    private (DocList List, DocListItem Item, int ItemIndex)? FindListItem(DocParagraph paragraph)
    {
        foreach (var list in AllLists(Document.Blocks))
            for (int i = 0; i < list.Items.Count; i++)
                if (list.Items[i].Blocks.Contains(paragraph))
                    return (list, list.Items[i], i);
        return null;
    }

    private static IEnumerable<DocList> AllLists(List<DocBlock> blocks)
    {
        foreach (var block in blocks)
        {
            switch (block)
            {
                case DocList list:
                    yield return list;
                    foreach (var item in list.Items)
                        foreach (var nested in AllLists(item.Blocks))
                            yield return nested;
                    break;
                case DocQuote quote:
                    foreach (var nested in AllLists(quote.Blocks))
                        yield return nested;
                    break;
            }
        }
    }

    /// <summary>Collection qui contient DIRECTEMENT ce bloc (racine, citation, item)</summary>
    private List<DocBlock>? FindParentCollection(DocBlock block)
        => FindParentIn(Document.Blocks, block);

    private static List<DocBlock>? FindParentIn(List<DocBlock> blocks, DocBlock target)
    {
        if (blocks.Contains(target)) return blocks;
        foreach (var block in blocks)
        {
            var found = block switch
            {
                DocList list => list.Items.Select(i => FindParentIn(i.Blocks, target))
                    .FirstOrDefault(r => r != null),
                DocQuote quote => FindParentIn(quote.Blocks, target),
                _ => null
            };
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// Sort un paragraphe de sa liste : son item disparaît, la liste est
    /// SCINDÉE autour de lui s'il était au milieu, supprimée si vide.
    /// </summary>
    private void UnwrapFromList(DocParagraph paragraph)
    {
        if (FindListItem(paragraph) is not { } ctx) return;
        var parent = FindParentCollection(ctx.List);
        if (parent == null) return;
        var listIndex = parent.IndexOf(ctx.List);

        var escaping = ctx.Item.Blocks.ToList(); // tout l'item sort ensemble
        ctx.List.Items.RemoveAt(ctx.ItemIndex);

        if (ctx.ItemIndex == 0)
        {
            parent.InsertRange(listIndex, escaping);
            if (ctx.List.Items.Count == 0)
                parent.Remove(ctx.List);
        }
        else if (ctx.ItemIndex >= ctx.List.Items.Count)
        {
            parent.InsertRange(listIndex + 1, escaping);
        }
        else
        {
            // Milieu : scinder la liste en deux autour du paragraphe sorti
            var tail = new DocList { Ordered = ctx.List.Ordered };
            while (ctx.List.Items.Count > ctx.ItemIndex)
            {
                tail.Items.Add(ctx.List.Items[ctx.ItemIndex]);
                ctx.List.Items.RemoveAt(ctx.ItemIndex);
            }
            parent.InsertRange(listIndex + 1, escaping.Append((DocBlock)tail));
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // V2 — COLLAGE RICHE + IMAGES + HTML DE LA SÉLECTION
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Insère un fragment HTML au caret (collage riche). Un fragment d'un seul
    /// paragraphe se fond dans le paragraphe courant ; plusieurs blocs sont
    /// insérés en scindant au caret. Une seule étape d'undo.
    /// </summary>
    public void InsertHtml(string html)
    {
        var fragment = HtmlParser.Parse(html);
        Normalize(fragment);
        if (fragment.Blocks.Count == 0) return;

        // Fragment d'un seul paragraphe sans image : fusion inline
        if (fragment.Blocks.Count == 1 && fragment.Blocks[0] is DocParagraph single &&
            !single.Inlines.OfType<DocImage>().Any())
        {
            PushUndo();
            if (HasSelection) DeleteRangeInternal();
            InsertInlinesAtCaret(single.Inlines);
            RaiseChanged();
            return;
        }

        InsertBlocksAtCaret(fragment.Blocks);
    }

    /// <summary>Insère une image (bloc dédié — les images sont des blocs dans le layout v1)</summary>
    public void InsertImage(DocImage image)
    {
        var paragraph = new DocParagraph();
        paragraph.Inlines.Add(image);
        InsertBlocksAtCaret(new List<DocBlock> { paragraph });
    }

    /// <summary>
    /// HTML de la sélection (copie riche). Implémenté par clonage du document
    /// puis suppression de tout ce qui est HORS sélection sur un éditeur
    /// temporaire : la structure (listes, titres, styles) est préservée.
    /// </summary>
    public string GetSelectedHtml()
    {
        if (!HasSelection) return "";
        var (start, end) = SelectionRange();

        var temp = new DocEditor(DocCloner.Clone(Document));

        // Supprimer après la fin…
        var last = temp.Paragraphs.Count - 1;
        temp.Anchor = end;
        temp.Caret = new DocCaret(last, temp.GetParagraphLength(last));
        if (temp.HasSelection) temp.DeleteSelection();

        // …puis avant le début
        temp.Anchor = new DocCaret(0, 0);
        temp.Caret = start;
        if (temp.HasSelection) temp.DeleteSelection();

        return temp.ToHtml();
    }

    private void InsertBlocksAtCaret(List<DocBlock> blocks)
    {
        if (blocks.Count == 0) return;
        PushUndo();
        if (HasSelection) DeleteRangeInternal();

        var paragraph = Paragraphs[Caret.Block];
        var topIndex = TopLevelIndexOf(paragraph);
        if (topIndex < 0) return;

        DocParagraph? tail = null;
        if (ReferenceEquals(Document.Blocks[topIndex], paragraph))
        {
            // Paragraphe de premier niveau : scinder au caret, insérer entre les moitiés
            tail = new DocParagraph { Alignment = paragraph.Alignment };
            MoveTail(paragraph, Caret.Offset, tail);
            Document.Blocks.Insert(topIndex + 1, tail);
            Document.Blocks.InsertRange(topIndex + 1, blocks);
        }
        else
        {
            // Caret dans une structure (liste, citation) : insérer après elle
            Document.Blocks.InsertRange(topIndex + 1, blocks);
        }

        Reflatten();
        var target = tail != null ? Paragraphs.IndexOf(tail) : -1;
        Caret = target >= 0 ? new DocCaret(target, 0) : ClampCaret(Caret);
        Anchor = null;
        RaiseChanged();
    }

    /// <summary>Insère des inlines au caret dans le paragraphe courant</summary>
    private void InsertInlinesAtCaret(List<DocInline> inlines)
    {
        var paragraph = Paragraphs[Caret.Block];
        SplitRunAt(paragraph, Caret.Offset);

        // Index d'insertion : premier inline commençant à l'offset du caret
        var position = 0;
        var insertAt = paragraph.Inlines.Count;
        for (int i = 0; i < paragraph.Inlines.Count; i++)
        {
            if (position >= Caret.Offset) { insertAt = i; break; }
            position += paragraph.Inlines[i] switch
            {
                DocRun run => run.Text.Length,
                DocLineBreak => 1,
                _ => 0
            };
        }

        var length = 0;
        foreach (var inline in inlines)
        {
            paragraph.Inlines.Insert(insertAt++, inline);
            length += inline switch
            {
                DocRun run => run.Text.Length,
                DocLineBreak => 1,
                _ => 0
            };
        }

        Caret = Caret with { Offset = Caret.Offset + length };
        Anchor = null;
    }

    /// <summary>Index du bloc de PREMIER NIVEAU contenant ce paragraphe</summary>
    private int TopLevelIndexOf(DocParagraph paragraph)
    {
        for (int i = 0; i < Document.Blocks.Count; i++)
            if (ReferenceEquals(Document.Blocks[i], paragraph) || Contains(Document.Blocks[i], paragraph))
                return i;
        return -1;
    }

    private static bool Contains(DocBlock block, DocParagraph paragraph) => block switch
    {
        DocList list => list.Items.Any(item =>
            item.Blocks.Any(b => ReferenceEquals(b, paragraph) || Contains(b, paragraph))),
        DocQuote quote => quote.Blocks.Any(b => ReferenceEquals(b, paragraph) || Contains(b, paragraph)),
        _ => false
    };

    // ═══════════════════════════════════════════════════════════════
    // UNDO / REDO (instantanés)
    // ═══════════════════════════════════════════════════════════════

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public void Undo()
    {
        if (!CanUndo) return;
        _redo.Add((DocCloner.Clone(Document), Caret));
        var (doc, caret) = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        Restore(doc, caret);
    }

    public void Redo()
    {
        if (!CanRedo) return;
        _undo.Add((DocCloner.Clone(Document), Caret));
        var (doc, caret) = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        Restore(doc, caret);
    }

    private void Restore(ProDocument doc, DocCaret caret)
    {
        Document = doc;
        Reflatten();
        Caret = ClampCaret(caret);
        Anchor = null;
        _lastOpWasTyping = false;
        RaiseChanged();
    }

    private void PushUndo(bool coalesceTyping = false)
    {
        // Frappes consécutives regroupées en une seule étape d'undo
        if (coalesceTyping && _lastOpWasTyping && _undo.Count > 0)
            return;

        _undo.Add((DocCloner.Clone(Document), Caret));
        if (_undo.Count > UndoLimit)
            _undo.RemoveAt(0);
        _redo.Clear();
        _lastOpWasTyping = coalesceTyping;
    }

    // ═══════════════════════════════════════════════════════════════
    // SORTIE
    // ═══════════════════════════════════════════════════════════════

    public string ToHtml() => HtmlSerializer.Serialize(Document);

    // ═══════════════════════════════════════════════════════════════
    // MÉCANIQUE INTERNE
    // ═══════════════════════════════════════════════════════════════

    private DocCaret ClampCaret(DocCaret caret)
    {
        var block = Math.Clamp(caret.Block, 0, Math.Max(0, Paragraphs.Count - 1));
        return new DocCaret(block, Math.Clamp(caret.Offset, 0, GetParagraphLength(block)));
    }

    /// <summary>
    /// Localise (index d'inline, offset dans le run, style hérité) pour un
    /// offset caractère. runIndex -1 = paragraphe vide (insérer en tête).
    /// </summary>
    private static (int RunIndex, int RunOffset, DocStyle Style) LocateRun(
        DocParagraph paragraph, int offset)
    {
        var position = 0;
        DocStyle lastStyle = DocStyle.Empty;

        for (int i = 0; i < paragraph.Inlines.Count; i++)
        {
            switch (paragraph.Inlines[i])
            {
                case DocRun run:
                    if (offset <= position + run.Text.Length)
                        return (i, offset - position, run.Style);
                    position += run.Text.Length;
                    lastStyle = run.Style;
                    break;
                case DocLineBreak:
                    if (offset <= position) return (i, 0, lastStyle);
                    position++;
                    break;
            }
        }
        return (-1, 0, lastStyle);
    }

    private static void DeleteTextInParagraph(DocParagraph paragraph, int offset, int count)
    {
        var remaining = count;
        var position = 0;

        for (int i = 0; i < paragraph.Inlines.Count && remaining > 0; i++)
        {
            switch (paragraph.Inlines[i])
            {
                case DocRun run:
                {
                    var runStart = position;
                    var runEnd = position + run.Text.Length;
                    if (offset < runEnd && offset + remaining > runStart)
                    {
                        var localStart = Math.Max(0, offset - runStart);
                        var localCount = Math.Min(run.Text.Length - localStart,
                            remaining - Math.Max(0, runStart - offset));
                        run.Text = run.Text.Remove(localStart, localCount);
                        remaining -= localCount;
                        if (run.Text.Length == 0)
                        {
                            paragraph.Inlines.RemoveAt(i);
                            i--;
                            position = runStart;
                            continue;
                        }
                    }
                    position = runEnd;
                    break;
                }
                case DocLineBreak:
                    if (offset <= position && remaining > 0)
                    {
                        paragraph.Inlines.RemoveAt(i);
                        i--;
                        remaining--;
                        continue;
                    }
                    position++;
                    break;
            }
        }
    }

    private void DeleteRangeInternal()
    {
        var (start, end) = SelectionRange();

        if (start.Block == end.Block)
        {
            DeleteTextInParagraph(Paragraphs[start.Block], start.Offset, end.Offset - start.Offset);
        }
        else
        {
            // Queue du premier + tête du dernier, paragraphes intermédiaires retirés
            DeleteTextInParagraph(Paragraphs[start.Block], start.Offset,
                GetParagraphLength(start.Block) - start.Offset);
            DeleteTextInParagraph(Paragraphs[end.Block], 0, end.Offset);

            for (int b = end.Block - 1; b > start.Block; b--)
                RemoveBlock(Paragraphs[b]);

            // La liste aplatie est périmée après les retraits : la rafraîchir
            // AVANT la fusion, sinon on fusionne un paragraphe déjà retiré
            Reflatten();
            MergeWithPrevious(start.Block + 1);
            Reflatten();
        }

        Caret = start;
        Anchor = null;
    }

    private void ApplyStyleToRange(DocParagraph paragraph, int from, int to, DocStyle overlay)
    {
        if (to <= from) return;

        // Découper les runs aux frontières puis styler les runs couverts
        SplitRunAt(paragraph, from);
        SplitRunAt(paragraph, to);

        var position = 0;
        foreach (var inline in paragraph.Inlines)
        {
            switch (inline)
            {
                case DocRun run:
                    if (position >= from && position + run.Text.Length <= to)
                        run.Style = run.Style.Merge(overlay);
                    position += run.Text.Length;
                    break;
                case DocLineBreak:
                    position++;
                    break;
            }
        }
    }

    private static void SplitRunAt(DocParagraph paragraph, int offset)
    {
        var position = 0;
        for (int i = 0; i < paragraph.Inlines.Count; i++)
        {
            if (paragraph.Inlines[i] is DocRun run)
            {
                if (offset > position && offset < position + run.Text.Length)
                {
                    var local = offset - position;
                    var tail = new DocRun(run.Text[local..], run.Style) { LinkHref = run.LinkHref };
                    run.Text = run.Text[..local];
                    paragraph.Inlines.Insert(i + 1, tail);
                    return;
                }
                position += run.Text.Length;
            }
            else if (paragraph.Inlines[i] is DocLineBreak)
            {
                position++;
            }
        }
    }

    private static void MoveTail(DocParagraph source, int offset, DocParagraph target)
    {
        SplitRunAt(source, offset);

        var position = 0;
        var cut = source.Inlines.Count;
        for (int i = 0; i < source.Inlines.Count; i++)
        {
            if (position >= offset) { cut = i; break; }
            position += source.Inlines[i] switch
            {
                DocRun run => run.Text.Length,
                DocLineBreak => 1,
                _ => 0
            };
        }

        while (source.Inlines.Count > cut)
        {
            target.Inlines.Add(source.Inlines[cut]);
            source.Inlines.RemoveAt(cut);
        }
    }

    private void MergeWithPrevious(int block)
    {
        if (block <= 0 || block >= Paragraphs.Count) return;
        var previous = Paragraphs[block - 1];
        var current = Paragraphs[block];
        previous.Inlines.AddRange(current.Inlines);
        RemoveBlock(current);
    }

    // Structure : retrouver/insérer/retirer un paragraphe dans l'arbre de blocs
    private void InsertBlockAfter(DocParagraph reference, DocParagraph newParagraph)
        => InsertAfterIn(Document.Blocks, reference, newParagraph);

    private static bool InsertAfterIn(List<DocBlock> blocks, DocParagraph reference, DocParagraph newParagraph)
    {
        for (int i = 0; i < blocks.Count; i++)
        {
            if (ReferenceEquals(blocks[i], reference))
            {
                blocks.Insert(i + 1, newParagraph);
                return true;
            }
            var nested = blocks[i] switch
            {
                DocList list => list.Items.Any(item => InsertAfterIn(item.Blocks, reference, newParagraph)),
                DocQuote quote => InsertAfterIn(quote.Blocks, reference, newParagraph),
                _ => false
            };
            if (nested) return true;
        }
        return false;
    }

    private void RemoveBlock(DocParagraph paragraph)
        => RemoveIn(Document.Blocks, paragraph);

    private static bool RemoveIn(List<DocBlock> blocks, DocParagraph paragraph)
    {
        for (int i = 0; i < blocks.Count; i++)
        {
            if (ReferenceEquals(blocks[i], paragraph))
            {
                blocks.RemoveAt(i);
                return true;
            }
            var nested = blocks[i] switch
            {
                DocList list => list.Items.Any(item => RemoveIn(item.Blocks, paragraph)),
                DocQuote quote => RemoveIn(quote.Blocks, paragraph),
                _ => false
            };
            if (nested) return true;
        }
        return false;
    }

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}

// ═══════════════════════════════════════════════════════════════════════════
// CLONE PROFOND (instantanés d'undo)
// ═══════════════════════════════════════════════════════════════════════════

internal static class DocCloner
{
    public static ProDocument Clone(ProDocument source)
    {
        var clone = new ProDocument { BaseStyle = source.BaseStyle };
        foreach (var block in source.Blocks)
            clone.Blocks.Add(CloneBlock(block));
        return clone;
    }

    private static DocBlock CloneBlock(DocBlock block)
    {
        switch (block)
        {
            case DocParagraph p:
            {
                var clone = new DocParagraph { HeadingLevel = p.HeadingLevel, Alignment = p.Alignment };
                foreach (var inline in p.Inlines)
                    clone.Inlines.Add(CloneInline(inline));
                return clone;
            }
            case DocList list:
            {
                var clone = new DocList { Ordered = list.Ordered };
                foreach (var item in list.Items)
                {
                    var itemClone = new DocListItem();
                    foreach (var b in item.Blocks)
                        itemClone.Blocks.Add(CloneBlock(b));
                    clone.Items.Add(itemClone);
                }
                return clone;
            }
            case DocQuote quote:
            {
                var clone = new DocQuote();
                foreach (var b in quote.Blocks)
                    clone.Blocks.Add(CloneBlock(b));
                return clone;
            }
            case DocTable table:
            {
                var clone = new DocTable();
                foreach (var row in table.Rows)
                {
                    var rowClone = new DocTableRow();
                    foreach (var cell in row.Cells)
                    {
                        var cellClone = new DocTableCell { ColumnSpan = cell.ColumnSpan, IsHeader = cell.IsHeader };
                        foreach (var b in cell.Blocks)
                            cellClone.Blocks.Add(CloneBlock(b));
                        rowClone.Cells.Add(cellClone);
                    }
                    clone.Rows.Add(rowClone);
                }
                return clone;
            }
            case DocCodeBlock code:
                return new DocCodeBlock { Text = code.Text };
            default:
                return new DocSeparator();
        }
    }

    private static DocInline CloneInline(DocInline inline) => inline switch
    {
        DocRun run => new DocRun(run.Text, run.Style) { LinkHref = run.LinkHref },
        DocLineBreak => new DocLineBreak(),
        DocImage img => new DocImage
        {
            Source = img.Source, Data = img.Data, Width = img.Width, Height = img.Height, Alt = img.Alt
        },
        DocLink link => CloneLink(link),
        _ => new DocRun("")
    };

    private static DocLink CloneLink(DocLink link)
    {
        var clone = new DocLink { Href = link.Href };
        foreach (var child in link.Children)
            clone.Children.Add(CloneInline(child));
        return clone;
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// SÉRIALISATION HTML
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>ProDocument → HTML (sortie du ProRichEdit, envoi de mails)</summary>
public static class HtmlSerializer
{
    public static string Serialize(ProDocument document)
    {
        var sb = new StringBuilder();
        SerializeBlocks(sb, document.Blocks);
        return sb.ToString();
    }

    private static void SerializeBlocks(StringBuilder sb, List<DocBlock> blocks)
    {
        foreach (var block in blocks)
        {
            switch (block)
            {
                case DocParagraph p:
                    SerializeParagraph(sb, p);
                    break;
                case DocList list:
                    sb.Append(list.Ordered ? "<ol>" : "<ul>");
                    foreach (var item in list.Items)
                    {
                        sb.Append("<li>");
                        SerializeBlocks(sb, item.Blocks);
                        sb.Append("</li>");
                    }
                    sb.Append(list.Ordered ? "</ol>" : "</ul>");
                    break;
                case DocQuote quote:
                    sb.Append("<blockquote>");
                    SerializeBlocks(sb, quote.Blocks);
                    sb.Append("</blockquote>");
                    break;
                case DocCodeBlock code:
                    sb.Append("<pre>").Append(Escape(code.Text)).Append("</pre>");
                    break;
                case DocSeparator:
                    sb.Append("<hr>");
                    break;
                case DocTable table:
                    sb.Append("<table>");
                    foreach (var row in table.Rows)
                    {
                        sb.Append("<tr>");
                        foreach (var cell in row.Cells)
                        {
                            var tag = cell.IsHeader ? "th" : "td";
                            sb.Append('<').Append(tag);
                            if (cell.ColumnSpan > 1)
                                sb.Append(" colspan=\"").Append(cell.ColumnSpan).Append('"');
                            sb.Append('>');
                            SerializeBlocks(sb, cell.Blocks);
                            sb.Append("</").Append(tag).Append('>');
                        }
                        sb.Append("</tr>");
                    }
                    sb.Append("</table>");
                    break;
            }
        }
    }

    private static void SerializeParagraph(StringBuilder sb, DocParagraph p)
    {
        var tag = p.HeadingLevel > 0 ? $"h{p.HeadingLevel}" : "p";
        sb.Append('<').Append(tag);
        if (p.Alignment == Avalonia.Media.TextAlignment.Center)
            sb.Append(" style=\"text-align:center\"");
        else if (p.Alignment == Avalonia.Media.TextAlignment.Right)
            sb.Append(" style=\"text-align:right\"");
        else if (p.Alignment == Avalonia.Media.TextAlignment.Justify)
            sb.Append(" style=\"text-align:justify\"");
        sb.Append('>');

        string? openLink = null;

        foreach (var inline in p.Inlines)
        {
            switch (inline)
            {
                case DocRun run:
                {
                    // Regrouper les runs adjacents du même lien dans un seul <a>
                    if (run.LinkHref != openLink)
                    {
                        if (openLink != null) sb.Append("</a>");
                        openLink = string.IsNullOrEmpty(run.LinkHref) ? null : run.LinkHref;
                        if (openLink != null)
                            sb.Append("<a href=\"").Append(Escape(openLink)).Append("\">");
                    }
                    SerializeRun(sb, run);
                    break;
                }
                case DocLineBreak:
                    if (openLink != null) { sb.Append("</a>"); openLink = null; }
                    sb.Append("<br>");
                    break;
                case DocImage img:
                    if (openLink != null) { sb.Append("</a>"); openLink = null; }
                    sb.Append("<img src=\"").Append(Escape(img.Source)).Append('"');
                    if (img.Width.HasValue) sb.Append(" width=\"").Append((int)img.Width.Value).Append('"');
                    if (img.Height.HasValue) sb.Append(" height=\"").Append((int)img.Height.Value).Append('"');
                    if (!string.IsNullOrEmpty(img.Alt)) sb.Append(" alt=\"").Append(Escape(img.Alt)).Append('"');
                    sb.Append('>');
                    break;
                case DocLink link:
                    if (openLink != null) { sb.Append("</a>"); openLink = null; }
                    sb.Append("<a href=\"").Append(Escape(link.Href)).Append("\">");
                    foreach (var child in link.Children)
                    {
                        if (child is DocRun childRun) SerializeRun(sb, childRun);
                    }
                    sb.Append("</a>");
                    break;
            }
        }

        if (openLink != null) sb.Append("</a>");
        sb.Append("</").Append(tag).Append('>');
    }

    private static void SerializeRun(StringBuilder sb, DocRun run)
    {
        var style = run.Style;
        var close = new Stack<string>();

        var css = new StringBuilder();
        if (style.Foreground is { } fg)
            css.Append($"color:#{fg.R:x2}{fg.G:x2}{fg.B:x2};");
        if (style.Background is { } bg)
            css.Append($"background-color:#{bg.R:x2}{bg.G:x2}{bg.B:x2};");
        if (style.FontSize is { } size)
            css.Append($"font-size:{size.ToString(System.Globalization.CultureInfo.InvariantCulture)}px;");

        if (css.Length > 0)
        {
            sb.Append("<span style=\"").Append(css).Append("\">");
            close.Push("</span>");
        }
        if (style.Bold == true) { sb.Append("<b>"); close.Push("</b>"); }
        if (style.Italic == true) { sb.Append("<i>"); close.Push("</i>"); }
        if (style.Underline == true) { sb.Append("<u>"); close.Push("</u>"); }
        if (style.Strikethrough == true) { sb.Append("<s>"); close.Push("</s>"); }
        if (style.IsCode == true) { sb.Append("<code>"); close.Push("</code>"); }

        sb.Append(Escape(run.Text));

        while (close.Count > 0)
            sb.Append(close.Pop());
    }

    private static string Escape(string text) => text
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;");
}
