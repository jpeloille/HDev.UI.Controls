using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;
using ProControls.Theme;

namespace ProControls.Documents;

// ═══════════════════════════════════════════════════════════════════════════
// BOÎTES DE LAYOUT (résultat positionné, réutilisable pour le hit-test)
// ═══════════════════════════════════════════════════════════════════════════

public abstract class DocLayoutBox
{
    public Rect Bounds { get; set; }
}

/// <summary>Paragraphe posé : un TextLayout riche (wrapping, styles par plage)</summary>
public class DocTextBox : DocLayoutBox
{
    public required TextLayout Text { get; init; }
    public DocParagraph? Source { get; init; }
}

/// <summary>Image posée (bitmap décodé au layout, jamais au rendu)</summary>
public class DocImageBox : DocLayoutBox
{
    public required DocImage Source { get; init; }
    public Bitmap? Bitmap { get; init; }
}

/// <summary>Filet horizontal (hr)</summary>
public class DocRuleBox : DocLayoutBox { }

/// <summary>Barre verticale de citation</summary>
public class DocQuoteBarBox : DocLayoutBox { }

/// <summary>Fond (code, en-têtes de table)</summary>
public class DocFillBox : DocLayoutBox
{
    public Color Color { get; init; }
}

/// <summary>Trait de grille de tableau</summary>
public class DocGridLineBox : DocLayoutBox { }

/// <summary>Zone cliquable d'un lien (un lien multi-lignes = plusieurs zones)</summary>
public class DocLinkRegion
{
    public Rect Bounds { get; init; }
    public string Href { get; init; } = "";
}

/// <summary>Résultat du layout : boîtes + hauteur totale + zones de liens</summary>
public class DocLayoutResult
{
    public List<DocLayoutBox> Boxes { get; } = new();
    public List<DocLinkRegion> Links { get; } = new();
    public double Height { get; internal set; }
    public double Width { get; internal set; }
}

// ═══════════════════════════════════════════════════════════════════════════
// MOTEUR
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Pose un ProDocument dans une largeur donnée : wrapping par TextLayout
/// Avalonia, listes indentées, tables à colonnes égales (v1), citations,
/// code, images (bitmaps décodés une fois). Le rendu ne dessine que les
/// boîtes croisant la fenêtre (virtualisation verticale).
/// </summary>
public class DocLayoutEngine
{
    private const double ParagraphSpacing = 8;
    private const double ListIndent = 26;
    private const double QuoteIndent = 14;
    private const double CellPadding = 6;

    private readonly DocLayoutResult _result = new();
    private readonly DocStyle _baseStyle;
    private readonly string _fontFamily;
    private readonly string _monoFamily;

    private DocLayoutEngine(DocStyle baseStyle)
    {
        _baseStyle = baseStyle;
        _fontFamily = baseStyle.FontFamily ?? ProTheme.Typography.FontFamily;
        _monoFamily = "monospace";
    }

    public static DocLayoutResult Layout(ProDocument document, double width)
    {
        var engine = new DocLayoutEngine(document.BaseStyle);
        width = Math.Max(60, width);
        var y = engine.LayoutBlocks(document.Blocks, 0, y: 4, width);
        engine._result.Height = y + 8;
        engine._result.Width = width;
        return engine._result;
    }

    // ═══════════════════════════════════════════════════════════════
    // BLOCS
    // ═══════════════════════════════════════════════════════════════

    private double LayoutBlocks(List<DocBlock> blocks, double x, double y, double width)
    {
        foreach (var block in blocks)
            y = LayoutBlock(block, x, y, width);
        return y;
    }

    private double LayoutBlock(DocBlock block, double x, double y, double width)
    {
        switch (block)
        {
            case DocParagraph paragraph:
                return LayoutParagraph(paragraph, x, y, width);

            case DocList list:
                return LayoutList(list, x, y, width);

            case DocQuote quote:
            {
                var startY = y;
                var innerY = LayoutBlocks(quote.Blocks, x + QuoteIndent, y + 2, width - QuoteIndent);
                _result.Boxes.Insert(0, new DocQuoteBarBox
                {
                    Bounds = new Rect(x + 3, startY + 2, 3, Math.Max(0, innerY - startY - 2))
                });
                return innerY + 4;
            }

            case DocCodeBlock code:
                return LayoutCode(code, x, y, width);

            case DocSeparator:
                _result.Boxes.Add(new DocRuleBox
                {
                    Bounds = new Rect(x, y + 6, width, 1)
                });
                return y + 13;

            case DocTable table:
                return LayoutTable(table, x, y, width);

            default:
                return y;
        }
    }

    private double LayoutParagraph(DocParagraph paragraph, double x, double y, double width)
    {
        var (text, spans, linkRanges) = BuildParagraphRuns(paragraph);
        if (text.Length == 0)
            return y + ParagraphSpacing; // paragraphe vide = respiration

        var (fontSize, weight) = paragraph.HeadingLevel switch
        {
            1 => (24.0, FontWeight.SemiBold),
            2 => (20.0, FontWeight.SemiBold),
            3 => (17.0, FontWeight.SemiBold),
            >= 4 => (15.0, FontWeight.SemiBold),
            _ => (_baseStyle.FontSize ?? 14, FontWeight.Regular)
        };

        var layout = new TextLayout(
            text,
            new Typeface(_fontFamily, weight: weight),
            fontSize,
            new SolidColorBrush(_baseStyle.Foreground ?? ProTheme.Text.Primary),
            paragraph.Alignment,
            TextWrapping.Wrap,
            maxWidth: width,
            textStyleOverrides: spans);

        var topMargin = paragraph.HeadingLevel > 0 ? 10.0 : 0.0;
        var bounds = new Rect(x, y + topMargin, width, layout.Height);
        _result.Boxes.Add(new DocTextBox { Text = layout, Source = paragraph, Bounds = bounds });

        // Zones cliquables des liens (rectangles réels de la plage de texte)
        foreach (var (start, length, href) in linkRanges)
        {
            foreach (var rect in layout.HitTestTextRange(start, length))
            {
                _result.Links.Add(new DocLinkRegion
                {
                    Bounds = rect.Translate(new Vector(bounds.X, bounds.Y)),
                    Href = href
                });
            }
        }

        y = bounds.Bottom + ParagraphSpacing;

        // Images du paragraphe : posées en bloc sous le texte (simplification v1)
        foreach (var inline in EnumerateInlines(paragraph.Inlines))
        {
            if (inline is DocImage image)
                y = LayoutImage(image, x, y, width);
        }

        return y;
    }

    private static IEnumerable<DocInline> EnumerateInlines(List<DocInline> inlines)
    {
        foreach (var inline in inlines)
        {
            yield return inline;
            if (inline is DocLink link)
            {
                foreach (var child in link.Children)
                    yield return child;
            }
        }
    }

    private double LayoutImage(DocImage image, double x, double y, double width)
    {
        Bitmap? bitmap = null;
        if (image.Data is { Length: > 0 })
        {
            try
            {
                bitmap = new Bitmap(new MemoryStream(image.Data));
            }
            catch
            {
                // données illisibles : placeholder
            }
        }

        double w, h;
        if (bitmap != null)
        {
            w = image.Width ?? bitmap.PixelSize.Width;
            h = image.Height ?? bitmap.PixelSize.Height;
            if (image.Width.HasValue && !image.Height.HasValue)
                h = w * bitmap.PixelSize.Height / Math.Max(1, bitmap.PixelSize.Width);
            if (w > width)
            {
                h *= width / w;
                w = width;
            }
        }
        else
        {
            // Placeholder (image non résolue : distante non chargée, ou corrompue)
            w = Math.Min(width, Math.Max(120, image.Width ?? 200));
            h = Math.Max(36, image.Height ?? 60);
        }

        _result.Boxes.Add(new DocImageBox
        {
            Source = image,
            Bitmap = bitmap,
            Bounds = new Rect(x, y, w, h)
        });

        return y + h + ParagraphSpacing;
    }

    private double LayoutList(DocList list, double x, double y, double width)
    {
        for (int i = 0; i < list.Items.Count; i++)
        {
            var marker = list.Ordered ? $"{i + 1}." : "•";
            var markerLayout = new TextLayout(marker,
                new Typeface(_fontFamily), _baseStyle.FontSize ?? 14,
                new SolidColorBrush(ProTheme.Text.Secondary));

            _result.Boxes.Add(new DocTextBox
            {
                Text = markerLayout,
                Bounds = new Rect(x + 6, y, ListIndent - 8, markerLayout.Height)
            });

            y = LayoutBlocks(list.Items[i].Blocks, x + ListIndent, y, width - ListIndent);
        }
        return y;
    }

    private double LayoutCode(DocCodeBlock code, double x, double y, double width)
    {
        var layout = new TextLayout(
            code.Text,
            new Typeface(_monoFamily),
            (_baseStyle.FontSize ?? 14) - 1,
            new SolidColorBrush(ProTheme.Text.Primary),
            maxWidth: width - 16,
            textWrapping: TextWrapping.Wrap);

        _result.Boxes.Add(new DocFillBox
        {
            Color = ProTheme.Background.Toolbar,
            Bounds = new Rect(x, y, width, layout.Height + 12)
        });
        _result.Boxes.Add(new DocTextBox
        {
            Text = layout,
            Bounds = new Rect(x + 8, y + 6, width - 16, layout.Height)
        });

        return y + layout.Height + 12 + ParagraphSpacing;
    }

    private double LayoutTable(DocTable table, double x, double y, double width)
    {
        var columnCount = Math.Max(1, table.ColumnCount);
        var columnWidth = width / columnCount;
        var startY = y;

        foreach (var row in table.Rows)
        {
            var rowY = y;
            double cellX = x;
            double rowHeight = 24;

            // Poser chaque cellule, mémoriser la hauteur max
            var cellStartIndex = _result.Boxes.Count;
            var cellRects = new List<(int FirstBox, double X, double W, bool Header)>();

            foreach (var cell in row.Cells)
            {
                var cellWidth = columnWidth * cell.ColumnSpan;
                var first = _result.Boxes.Count;
                var bottom = LayoutBlocks(cell.Blocks, cellX + CellPadding, rowY + CellPadding,
                    cellWidth - CellPadding * 2);
                cellRects.Add((first, cellX, cellWidth, cell.IsHeader));
                rowHeight = Math.Max(rowHeight, bottom - rowY + CellPadding - ParagraphSpacing);
                cellX += cellWidth;
            }

            // Fonds d'en-tête (insérés SOUS le contenu déjà posé)
            var inserted = 0;
            foreach (var (_, cx, cw, isHeader) in cellRects)
            {
                if (!isHeader) continue;
                _result.Boxes.Insert(cellStartIndex, new DocFillBox
                {
                    Color = ProTheme.Background.Toolbar,
                    Bounds = new Rect(cx, rowY, cw, rowHeight)
                });
                inserted++;
            }
            _ = inserted;

            // Traits horizontaux + verticaux de la ligne
            _result.Boxes.Add(new DocGridLineBox { Bounds = new Rect(x, rowY, width, 1) });
            double vx = x;
            _result.Boxes.Add(new DocGridLineBox { Bounds = new Rect(vx, rowY, 1, rowHeight) });
            foreach (var (_, cx, cw, _) in cellRects)
            {
                vx = cx + cw;
                _result.Boxes.Add(new DocGridLineBox { Bounds = new Rect(vx, rowY, 1, rowHeight) });
            }

            y = rowY + rowHeight;
        }

        _result.Boxes.Add(new DocGridLineBox { Bounds = new Rect(x, y, width, 1) });
        return y + ParagraphSpacing;
    }

    // ═══════════════════════════════════════════════════════════════
    // RUNS D'UN PARAGRAPHE (texte + plages de styles + plages de liens)
    // ═══════════════════════════════════════════════════════════════

    private (string Text, List<ValueSpan<TextRunProperties>> Spans,
        List<(int Start, int Length, string Href)> Links)
        BuildParagraphRuns(DocParagraph paragraph)
    {
        var sb = new System.Text.StringBuilder();
        var spans = new List<ValueSpan<TextRunProperties>>();
        var links = new List<(int, int, string)>();

        void AppendRun(DocRun run, string? href)
        {
            if (run.Text.Length == 0) return;

            // Lien porté par le run lui-même (forme normalisée de l'éditeur)
            if (!string.IsNullOrEmpty(run.LinkHref))
            {
                href = run.LinkHref;
                links.Add((sb.Length, run.Text.Length, run.LinkHref));
            }

            var start = sb.Length;
            sb.Append(run.Text);
            var style = _baseStyle.Merge(run.Style);
            var isLink = !string.IsNullOrEmpty(href);

            var decorations = new TextDecorationCollection();
            if (style.Underline == true || isLink)
                decorations.Add(new TextDecoration { Location = TextDecorationLocation.Underline });
            if (style.Strikethrough == true)
                decorations.Add(new TextDecoration { Location = TextDecorationLocation.Strikethrough });

            var foreground = isLink
                ? ProTheme.Accent.Primary
                : style.Foreground ?? ProTheme.Text.Primary;

            var typeface = new Typeface(
                style.IsCode == true ? _monoFamily : style.FontFamily ?? _fontFamily,
                style.Italic == true ? FontStyle.Italic : FontStyle.Normal,
                style.Bold == true ? FontWeight.SemiBold : FontWeight.Regular);

            var properties = new GenericTextRunProperties(
                typeface,
                style.FontSize ?? _baseStyle.FontSize ?? 14,
                decorations.Count > 0 ? decorations : null,
                new SolidColorBrush(foreground),
                style.Background.HasValue ? new SolidColorBrush(style.Background.Value) : null);

            spans.Add(new ValueSpan<TextRunProperties>(start, run.Text.Length, properties));
        }

        void Walk(List<DocInline> inlines, string? href)
        {
            foreach (var inline in inlines)
            {
                switch (inline)
                {
                    case DocRun run:
                        AppendRun(run, href);
                        break;
                    case DocLineBreak:
                        sb.Append('\n');
                        break;
                    case DocLink link:
                    {
                        var start = sb.Length;
                        Walk(link.Children, link.Href);
                        if (sb.Length > start && !string.IsNullOrEmpty(link.Href))
                            links.Add((start, sb.Length - start, link.Href));
                        break;
                    }
                    case DocImage:
                        break; // posées en bloc après le texte (v1)
                }
            }
        }

        Walk(paragraph.Inlines, null);
        return (sb.ToString(), spans, links);
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU (seules les boîtes croisant la fenêtre sont dessinées)
    // ═══════════════════════════════════════════════════════════════

    public static void Render(DrawingContext context, DocLayoutResult layout,
        Rect viewport, Vector offset)
    {
        foreach (var box in layout.Boxes)
        {
            var bounds = box.Bounds.Translate(offset);
            if (bounds.Bottom < viewport.Y || bounds.Y > viewport.Bottom)
                continue;

            switch (box)
            {
                case DocFillBox fill:
                    context.FillRectangle(new SolidColorBrush(fill.Color), bounds, 3);
                    break;

                case DocTextBox text:
                    text.Text.Draw(context, bounds.TopLeft);
                    break;

                case DocImageBox image when image.Bitmap != null:
                    context.DrawImage(image.Bitmap, bounds);
                    break;

                case DocImageBox placeholder:
                    context.DrawRectangle(
                        new SolidColorBrush(ProTheme.Background.Toolbar),
                        new Pen(new SolidColorBrush(ProTheme.Border.Default), 1),
                        bounds, 4, 4);
                    var alt = new TextLayout(
                        string.IsNullOrEmpty(placeholder.Source.Alt) ? "🖼 image" : $"🖼 {placeholder.Source.Alt}",
                        new Typeface(ProTheme.Typography.FontFamily), 12,
                        new SolidColorBrush(ProTheme.Text.Secondary),
                        maxWidth: Math.Max(20, bounds.Width - 12));
                    alt.Draw(context, new Point(bounds.X + 6, bounds.Center.Y - alt.Height / 2));
                    break;

                case DocRuleBox:
                case DocGridLineBox:
                    context.FillRectangle(new SolidColorBrush(ProTheme.Border.Subtle), bounds);
                    break;

                case DocQuoteBarBox:
                    context.FillRectangle(new SolidColorBrush(ProTheme.Border.Default), bounds, 1.5f);
                    break;
            }
        }
    }
}
