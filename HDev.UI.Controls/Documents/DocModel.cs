using Avalonia.Media;
using System.Text;

namespace HDev.UI.Controls.Documents;

/// <summary>
/// Style de caractères d'un run (les null héritent du contexte englobant)
/// </summary>
public record DocStyle
{
    public bool? Bold { get; init; }
    public bool? Italic { get; init; }
    public bool? Underline { get; init; }
    public bool? Strikethrough { get; init; }
    public double? FontSize { get; init; }
    public string? FontFamily { get; init; }
    public Color? Foreground { get; init; }
    public Color? Background { get; init; }
    public bool? IsCode { get; init; }

    public static readonly DocStyle Empty = new();

    /// <summary>Fusion : les valeurs de overlay priment, les null héritent de this</summary>
    public DocStyle Merge(DocStyle overlay) => new()
    {
        Bold = overlay.Bold ?? Bold,
        Italic = overlay.Italic ?? Italic,
        Underline = overlay.Underline ?? Underline,
        Strikethrough = overlay.Strikethrough ?? Strikethrough,
        FontSize = overlay.FontSize ?? FontSize,
        FontFamily = overlay.FontFamily ?? FontFamily,
        Foreground = overlay.Foreground ?? Foreground,
        Background = overlay.Background ?? Background,
        IsCode = overlay.IsCode ?? IsCode
    };
}

// ═══════════════════════════════════════════════════════════════════════════
// INLINES
// ═══════════════════════════════════════════════════════════════════════════

public abstract class DocInline
{
    internal abstract void AppendText(StringBuilder sb);
}

/// <summary>Segment de texte homogène</summary>
public class DocRun : DocInline
{
    public string Text { get; set; } = "";
    public DocStyle Style { get; set; } = DocStyle.Empty;

    /// <summary>
    /// Lien porté par le run lui-même (forme normalisée utilisée par l'éditeur ;
    /// le parseur produit des conteneurs DocLink, DocEditor les aplatit)
    /// </summary>
    public string? LinkHref { get; set; }

    public DocRun() { }
    public DocRun(string text, DocStyle? style = null)
    {
        Text = text;
        Style = style ?? DocStyle.Empty;
    }

    internal override void AppendText(StringBuilder sb) => sb.Append(Text);
}

/// <summary>Lien hypertexte (conteneur d'inlines)</summary>
public class DocLink : DocInline
{
    public string Href { get; set; } = "";
    public List<DocInline> Children { get; } = new();

    internal override void AppendText(StringBuilder sb)
    {
        foreach (var child in Children)
            child.AppendText(sb);
    }
}

/// <summary>
/// Image : données embarquées (base64/local) rendues nativement ; les sources
/// distantes passent par le résolveur fourni par l'application — le document
/// ne fait JAMAIS de réseau lui-même
/// </summary>
public class DocImage : DocInline
{
    /// <summary>Source d'origine (URL, data:, chemin)</summary>
    public string Source { get; set; } = "";

    /// <summary>Données décodées (posées par le parseur pour data:, ou par le résolveur)</summary>
    public byte[]? Data { get; set; }

    public double? Width { get; set; }
    public double? Height { get; set; }
    public string Alt { get; set; } = "";
    public string Title { get; set; } = "";

    internal override void AppendText(StringBuilder sb)
    {
        if (!string.IsNullOrEmpty(Alt))
            sb.Append('[').Append(Alt).Append(']');
    }
}

/// <summary>Saut de ligne dans un paragraphe (br)</summary>
public class DocLineBreak : DocInline
{
    internal override void AppendText(StringBuilder sb) => sb.Append('\n');
}

// ═══════════════════════════════════════════════════════════════════════════
// BLOCS
// ═══════════════════════════════════════════════════════════════════════════

public abstract class DocBlock
{
    internal abstract void AppendText(StringBuilder sb);
}

/// <summary>Paragraphe (ou titre si HeadingLevel &gt; 0)</summary>
public class DocParagraph : DocBlock
{
    public List<DocInline> Inlines { get; } = new();

    /// <summary>0 = paragraphe normal, 1-6 = titre</summary>
    public int HeadingLevel { get; set; }

    public TextAlignment Alignment { get; set; } = TextAlignment.Left;

    public DocParagraph() { }
    public DocParagraph(string text, DocStyle? style = null)
        => Inlines.Add(new DocRun(text, style));

    internal override void AppendText(StringBuilder sb)
    {
        foreach (var inline in Inlines)
            inline.AppendText(sb);
        sb.Append('\n');
    }
}

/// <summary>Liste à puces ou numérotée</summary>
public class DocList : DocBlock
{
    public bool Ordered { get; set; }
    public List<DocListItem> Items { get; } = new();

    internal override void AppendText(StringBuilder sb)
    {
        for (int i = 0; i < Items.Count; i++)
        {
            sb.Append(Ordered ? $"{i + 1}. " : "• ");
            foreach (var block in Items[i].Blocks)
                block.AppendText(sb);
        }
    }
}

public class DocListItem
{
    public List<DocBlock> Blocks { get; } = new();

    public DocListItem() { }
    public DocListItem(string text) => Blocks.Add(new DocParagraph(text));
}

/// <summary>Tableau simple (largeurs proportionnelles, colspan basique)</summary>
public class DocTable : DocBlock
{
    public List<DocTableRow> Rows { get; } = new();

    public int ColumnCount => Rows.Count == 0 ? 0 : Rows.Max(r => r.Cells.Sum(c => c.ColumnSpan));

    internal override void AppendText(StringBuilder sb)
    {
        foreach (var row in Rows)
        {
            for (int c = 0; c < row.Cells.Count; c++)
            {
                if (c > 0) sb.Append('\t');
                foreach (var block in row.Cells[c].Blocks)
                    block.AppendText(sb);
            }
        }
    }
}

public class DocTableRow
{
    public List<DocTableCell> Cells { get; } = new();
}

public class DocTableCell
{
    public List<DocBlock> Blocks { get; } = new();
    public int ColumnSpan { get; set; } = 1;
    public bool IsHeader { get; set; }

    public DocTableCell() { }
    public DocTableCell(string text) => Blocks.Add(new DocParagraph(text));
}

/// <summary>Citation (blockquote) — conteneur de blocs</summary>
public class DocQuote : DocBlock
{
    public List<DocBlock> Blocks { get; } = new();

    internal override void AppendText(StringBuilder sb)
    {
        foreach (var block in Blocks)
            block.AppendText(sb);
    }
}

/// <summary>Bloc de code préformaté</summary>
public class DocCodeBlock : DocBlock
{
    public string Text { get; set; } = "";

    internal override void AppendText(StringBuilder sb)
        => sb.Append(Text).Append('\n');
}

/// <summary>Séparateur horizontal (hr)</summary>
public class DocSeparator : DocBlock
{
    internal override void AppendText(StringBuilder sb) => sb.Append("———\n");
}

// ═══════════════════════════════════════════════════════════════════════════
// DOCUMENT
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Document riche : socle commun du viewer HTML (HDevHtmlView) et de l'éditeur
/// (HDevRichEdit). Modèle pur — le layout et le rendu vivent ailleurs.
/// </summary>
public class HDevDocument
{
    public List<DocBlock> Blocks { get; } = new();

    /// <summary>Style de base du document (les blocs/runs héritent)</summary>
    public DocStyle BaseStyle { get; set; } = new()
    {
        FontSize = 14,
        Bold = false,
        Italic = false,
        Underline = false,
        Strikethrough = false,
        IsCode = false
    };

    /// <summary>Extraction en texte brut (fallback d'affichage, copie, recherche)</summary>
    public string GetText()
    {
        var sb = new StringBuilder();
        foreach (var block in Blocks)
            block.AppendText(sb);
        return sb.ToString();
    }
}
