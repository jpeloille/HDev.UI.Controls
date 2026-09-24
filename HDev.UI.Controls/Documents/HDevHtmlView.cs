using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using HDev.UI.Controls.Documents;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

/// <summary>
/// Vue HTML native (sous-ensemble sanitisé) : volet de lecture de mails,
/// aide, aperçus. Rendu 100 % Skia — aucun WebView, aucun réseau : les
/// images distantes passent par ImageResolver, fourni (ou non) par l'app.
/// Hauteur au contenu : placer dans un ScrollViewer pour défiler.
/// </summary>
public class HDevHtmlView : Control
{
    private string _html = "";
    private HDevDocument _document = new();
    private DocLayoutResult? _layout;
    private double _layoutWidth = -1;
    private DocLinkRegion? _hoveredLink;

    // Sélection (v2) : position = (index de DocTextBox dans le layout, offset)
    private (int Box, int Offset)? _selAnchor;
    private (int Box, int Offset)? _selCaret;
    private bool _selecting;

    private bool HasSelection => _selAnchor.HasValue && _selCaret.HasValue && _selAnchor != _selCaret;

    /// <summary>Résout les images distantes (URL → octets) ; null = placeholder.
    /// Le contrôle ne fait JAMAIS de réseau lui-même.</summary>
    public Func<string, byte[]?>? ImageResolver { get; set; }

    /// <summary>Déclenché au clic sur un lien (l'app décide : navigateur, mailto...)</summary>
    public event EventHandler<string>? LinkClicked;

    /// <summary>
    /// Contenu HTML, parsé au profil Mail (tolérant) : ce viewer est la tête
    /// de lecture du monde extérieur (mails). Le HTML storage-v1 émis par
    /// HDevRichEdit est un sous-ensemble : il s'affiche à l'identique.
    /// </summary>
    public string Html
    {
        get => _html;
        set
        {
            if (_html == value) return;
            _html = value ?? "";
            _document = HtmlParser.Parse(_html);
            ResolveImages();
            _selAnchor = _selCaret = null; // la sélection ne survit pas au contenu
            _layoutWidth = -1; // force le re-layout
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    /// <summary>Document sous-jacent (accès au modèle, extraction texte...)</summary>
    public HDevDocument Document => _document;

    /// <summary>Texte brut du document (copie, recherche)</summary>
    public string GetText() => _document.GetText();

    static HDevHtmlView()
    {
        // Nécessaire pour recevoir Ctrl+C / Ctrl+A / Échap (sélection v2)
        FocusableProperty.OverrideDefaultValue<HDevHtmlView>(true);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        HDevTheme.VariantChanged += OnThemeVariantChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        HDevTheme.VariantChanged -= OnThemeVariantChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        // Les TextLayouts portent des brushes construits : re-layout complet
        _layoutWidth = -1;
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void ResolveImages()
    {
        if (ImageResolver == null) return;

        foreach (var block in _document.Blocks)
            ResolveImagesIn(block);
    }

    private void ResolveImagesIn(DocBlock block)
    {
        switch (block)
        {
            case DocParagraph paragraph:
                foreach (var inline in paragraph.Inlines)
                    ResolveImagesIn(inline);
                break;
            case DocList list:
                foreach (var item in list.Items)
                    foreach (var b in item.Blocks)
                        ResolveImagesIn(b);
                break;
            case DocTable table:
                foreach (var row in table.Rows)
                    foreach (var cell in row.Cells)
                        foreach (var b in cell.Blocks)
                            ResolveImagesIn(b);
                break;
            case DocQuote quote:
                foreach (var b in quote.Blocks)
                    ResolveImagesIn(b);
                break;
        }
    }

    private void ResolveImagesIn(DocInline inline)
    {
        switch (inline)
        {
            case DocImage { Data: null } image when image.Source.StartsWith("http", StringComparison.OrdinalIgnoreCase):
                image.Data = ImageResolver?.Invoke(image.Source);
                break;
            case DocLink link:
                foreach (var child in link.Children)
                    ResolveImagesIn(child);
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // LAYOUT (relancé quand la largeur change)
    // ═══════════════════════════════════════════════════════════════

    private void EnsureLayout(double width)
    {
        if (_layout != null && Math.Abs(width - _layoutWidth) < 0.5) return;
        _layoutWidth = width;
        _layout = DocLayoutEngine.Layout(_document, width);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width)
            ? (Width > 0 ? Width : 600)
            : availableSize.Width;

        EnsureLayout(Math.Max(60, width - 16)); // marges internes
        return new Size(width, (_layout?.Height ?? 0) + 16);
    }

    // ═══════════════════════════════════════════════════════════════
    // LIENS
    // ═══════════════════════════════════════════════════════════════

    private DocLinkRegion? LinkAt(Point pos)
    {
        if (_layout == null) return null;
        var local = pos - new Vector(8, 8);
        foreach (var link in _layout.Links)
        {
            if (link.Bounds.Contains(new Point(local.X, local.Y)))
                return link;
        }
        return null;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var pos = e.GetPosition(this);

        if (_selecting)
        {
            var position = PositionAt(pos);
            if (position != null && position != _selCaret)
            {
                _selCaret = position;
                InvalidateVisual();
            }
            base.OnPointerMoved(e);
            return;
        }

        var link = LinkAt(pos);
        if (!ReferenceEquals(link, _hoveredLink))
        {
            _hoveredLink = link;
            ToolTip.SetTip(this, link?.Href);
        }
        Cursor = link != null ? new Cursor(StandardCursorType.Hand)
            : PositionAt(pos) != null ? new Cursor(StandardCursorType.Ibeam)
            : Cursor.Default;
        base.OnPointerMoved(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var pos = e.GetPosition(this);
        var link = LinkAt(pos);
        if (link != null && !string.IsNullOrEmpty(link.Href))
        {
            LinkClicked?.Invoke(this, link.Href);
            e.Handled = true;
            base.OnPointerPressed(e);
            return;
        }

        // Début de sélection au drag
        Focus();
        var position = PositionAt(pos);
        _selAnchor = position;
        _selCaret = position;
        _selecting = position != null;
        if (_selecting) e.Pointer.Capture(this);
        InvalidateVisual();
        e.Handled = true;
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_selecting)
        {
            _selecting = false;
            if (_selAnchor == _selCaret) { _selAnchor = _selCaret = null; InvalidateVisual(); }
            e.Pointer.Capture(null);
        }
        base.OnPointerReleased(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        switch (e.Key)
        {
            case Key.C when ctrl && HasSelection:
                _ = CopySelectionAsync();
                e.Handled = true;
                break;
            case Key.A when ctrl:
                SelectAllText();
                e.Handled = true;
                break;
            case Key.Escape when HasSelection:
                _selAnchor = _selCaret = null;
                InvalidateVisual();
                e.Handled = true;
                break;
        }
        base.OnKeyDown(e);
    }

    // ═══════════════════════════════════════════════════════════════
    // SÉLECTION (v2) — sur les DocTextBox du layout, dans l'ordre
    // ═══════════════════════════════════════════════════════════════

    private List<DocTextBox> TextBoxes()
        => _layout?.Boxes.OfType<DocTextBox>().ToList() ?? new List<DocTextBox>();

    /// <summary>Texte source d'une boîte (même espace d'offsets que son TextLayout)</summary>
    private static string BoxText(DocTextBox box)
    {
        if (box.Source == null) return "";
        var sb = new System.Text.StringBuilder();
        foreach (var inline in box.Source.Inlines)
        {
            if (inline is DocRun run) sb.Append(run.Text);
            else if (inline is DocLineBreak) sb.Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>Position (boîte, offset) au point donné — boîte contenante, sinon la plus proche au-dessus</summary>
    private (int Box, int Offset)? PositionAt(Point point)
    {
        var boxes = TextBoxes();
        if (boxes.Count == 0) return null;
        var local = new Point(point.X - 8, point.Y - 8);

        var best = -1;
        for (int i = 0; i < boxes.Count; i++)
        {
            var b = boxes[i].Bounds;
            if (local.Y >= b.Y && local.Y <= b.Bottom)
            {
                // Plusieurs boîtes sur la même ligne (cellules de table) : la plus proche en X
                if (best < 0 || Math.Abs(local.X - boxes[i].Bounds.Center.X) <
                    Math.Abs(local.X - boxes[best].Bounds.Center.X))
                    best = i;
            }
            else if (best < 0 && b.Y <= local.Y)
            {
                best = i;
            }
            else if (b.Y <= local.Y && boxes[best].Bounds.Bottom < b.Y &&
                     !(local.Y >= boxes[best].Bounds.Y && local.Y <= boxes[best].Bounds.Bottom))
            {
                best = i; // plus proche au-dessus
            }
        }
        if (best < 0) best = 0;

        var box = boxes[best];
        var hit = box.Text.HitTestPoint(new Point(local.X - box.Bounds.X, local.Y - box.Bounds.Y));
        var offset = hit.CharacterHit.FirstCharacterIndex + hit.CharacterHit.TrailingLength;
        return (best, Math.Clamp(offset, 0, BoxText(box).Length));
    }

    private ((int Box, int Offset) Start, (int Box, int Offset) End) OrderedSelection()
    {
        var a = _selAnchor!.Value;
        var c = _selCaret!.Value;
        return a.Box < c.Box || (a.Box == c.Box && a.Offset <= c.Offset) ? (a, c) : (c, a);
    }

    /// <summary>Sélectionne tout le texte du document</summary>
    public void SelectAllText()
    {
        var boxes = TextBoxes();
        if (boxes.Count == 0) return;
        _selAnchor = (0, 0);
        _selCaret = (boxes.Count - 1, BoxText(boxes[^1]).Length);
        InvalidateVisual();
    }

    /// <summary>Texte brut de la sélection courante</summary>
    public string GetSelectedText()
    {
        if (!HasSelection) return "";
        var boxes = TextBoxes();
        var (start, end) = OrderedSelection();

        if (start.Box == end.Box)
        {
            var text = BoxText(boxes[start.Box]);
            return text[Math.Min(start.Offset, text.Length)..Math.Min(end.Offset, text.Length)];
        }

        var sb = new System.Text.StringBuilder();
        sb.Append(BoxText(boxes[start.Box])[Math.Min(start.Offset, BoxText(boxes[start.Box]).Length)..]).Append('\n');
        for (int i = start.Box + 1; i < end.Box; i++)
            sb.Append(BoxText(boxes[i])).Append('\n');
        sb.Append(BoxText(boxes[end.Box])[..Math.Min(end.Offset, BoxText(boxes[end.Box]).Length)]);
        return sb.ToString();
    }

    private async Task CopySelectionAsync()
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null && HasSelection)
            await clipboard.SetTextAsync(GetSelectedText());
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(new SolidColorBrush(HDevTheme.Background.Panel), bounds);

        if (_layout == null) return;

        // Surlignage de sélection (sous le texte)
        if (HasSelection)
        {
            var brush = new SolidColorBrush(HDevTheme.WithOpacity(HDevTheme.Accent.Primary, 60));
            var boxes = TextBoxes();
            var (start, end) = OrderedSelection();

            for (int i = start.Box; i <= end.Box && i < boxes.Count; i++)
            {
                var box = boxes[i];
                var length = BoxText(box).Length;
                var from = i == start.Box ? start.Offset : 0;
                var to = i == end.Box ? end.Offset : length;
                if (to <= from && i != start.Box) continue;

                foreach (var rect in box.Text.HitTestTextRange(from, Math.Max(0, to - from)))
                {
                    context.FillRectangle(brush, new Rect(
                        rect.X + box.Bounds.X + 8, rect.Y + box.Bounds.Y + 8,
                        Math.Max(3, rect.Width), rect.Height));
                }
            }
        }

        DocLayoutEngine.Render(context, _layout, bounds, new Vector(8, 8));
    }
}
