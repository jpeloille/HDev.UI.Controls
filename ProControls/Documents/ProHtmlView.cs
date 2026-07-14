using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ProControls.Documents;
using ProControls.Theme;

namespace ProControls.Controls;

/// <summary>
/// Vue HTML native (sous-ensemble sanitisé) : volet de lecture de mails,
/// aide, aperçus. Rendu 100 % Skia — aucun WebView, aucun réseau : les
/// images distantes passent par ImageResolver, fourni (ou non) par l'app.
/// Hauteur au contenu : placer dans un ScrollViewer pour défiler.
/// </summary>
public class ProHtmlView : Control
{
    private string _html = "";
    private ProDocument _document = new();
    private DocLayoutResult? _layout;
    private double _layoutWidth = -1;
    private DocLinkRegion? _hoveredLink;

    /// <summary>Résout les images distantes (URL → octets) ; null = placeholder.
    /// Le contrôle ne fait JAMAIS de réseau lui-même.</summary>
    public Func<string, byte[]?>? ImageResolver { get; set; }

    /// <summary>Déclenché au clic sur un lien (l'app décide : navigateur, mailto...)</summary>
    public event EventHandler<string>? LinkClicked;

    public string Html
    {
        get => _html;
        set
        {
            if (_html == value) return;
            _html = value ?? "";
            _document = HtmlParser.Parse(_html);
            ResolveImages();
            _layoutWidth = -1; // force le re-layout
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    /// <summary>Document sous-jacent (accès au modèle, extraction texte...)</summary>
    public ProDocument Document => _document;

    /// <summary>Texte brut du document (copie, recherche)</summary>
    public string GetText() => _document.GetText();

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
        var link = LinkAt(e.GetPosition(this));
        if (!ReferenceEquals(link, _hoveredLink))
        {
            _hoveredLink = link;
            Cursor = link != null ? new Cursor(StandardCursorType.Hand) : Cursor.Default;
            ToolTip.SetTip(this, link?.Href);
        }
        base.OnPointerMoved(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var link = LinkAt(e.GetPosition(this));
        if (link != null && !string.IsNullOrEmpty(link.Href))
        {
            LinkClicked?.Invoke(this, link.Href);
            e.Handled = true;
        }
        base.OnPointerPressed(e);
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(new SolidColorBrush(ProTheme.Background.Panel), bounds);

        if (_layout == null) return;

        DocLayoutEngine.Render(context, _layout, bounds, new Vector(8, 8));
    }
}
