using Avalonia.Media;
using System.Text;

namespace ProControls.Documents;

/// <summary>
/// Parseur HTML en sous-ensemble sanitisé → ProDocument.
/// Tolérant au HTML de mail réel (balises non fermées, casse, attributs sans
/// guillemets). SANITISATION PAR CONSTRUCTION : tout ce qui n'est pas dans le
/// sous-ensemble supporté est éliminé (script, style, iframe, object,
/// gestionnaires d'événements, positionnement...). Aucun accès réseau : les
/// images data: sont décodées, les URL restent des références à résoudre par
/// l'application.
/// </summary>
public static class HtmlParser
{
    // Éléments dont le CONTENU entier est ignoré
    private static readonly HashSet<string> SkippedContent = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "head", "title", "iframe", "object", "embed",
        "svg", "noscript", "template", "form", "select", "button", "textarea"
    };

    private sealed class Context
    {
        public List<DocBlock> Sink { get; }
        public DocParagraph? Paragraph;

        public Context(List<DocBlock> sink) => Sink = sink;
    }

    public static ProDocument Parse(string html)
    {
        var document = new ProDocument();
        if (string.IsNullOrEmpty(html))
            return document;

        var contexts = new Stack<Context>();
        contexts.Push(new Context(document.Blocks));

        var styleStack = new Stack<(string Tag, DocStyle Style)>();
        styleStack.Push(("", DocStyle.Empty));

        var linkStack = new Stack<DocLink>();
        var listStack = new Stack<DocList>();
        var tableStack = new Stack<DocTable>();
        var alignStack = new Stack<(string Tag, TextAlignment Align)>();

        int pos = 0;
        var pendingHeading = 0;
        var preDepth = 0;
        var preText = new StringBuilder();

        Context Ctx() => contexts.Peek();
        DocStyle CurrentStyle() => styleStack.Peek().Style;

        DocParagraph EnsureParagraph()
        {
            var ctx = Ctx();
            if (ctx.Paragraph == null)
            {
                ctx.Paragraph = new DocParagraph
                {
                    HeadingLevel = pendingHeading,
                    Alignment = alignStack.Count > 0 ? alignStack.Peek().Align : TextAlignment.Left
                };
                ctx.Sink.Add(ctx.Paragraph);
            }
            return ctx.Paragraph;
        }

        void FlushParagraph() => Ctx().Paragraph = null;

        void AddInline(DocInline inline)
        {
            if (linkStack.Count > 0)
            {
                var link = linkStack.Peek();
                EnsureParagraph();
                if (!Ctx().Paragraph!.Inlines.Contains(link))
                    Ctx().Paragraph!.Inlines.Add(link);
                link.Children.Add(inline);
            }
            else
            {
                EnsureParagraph().Inlines.Add(inline);
            }
        }

        void AddText(string text)
        {
            if (preDepth > 0)
            {
                preText.Append(text);
                return;
            }

            // Effondrement des blancs (règle HTML hors pre)
            var collapsed = CollapseWhitespace(text);
            if (collapsed.Length == 0) return;
            if (collapsed == " " && Ctx().Paragraph == null) return; // blanc inter-blocs

            AddInline(new DocRun(collapsed, CurrentStyle()));
        }

        void PushStyle(string tag, DocStyle overlay)
            => styleStack.Push((tag, CurrentStyle().Merge(overlay)));

        void PopStyle(string tag)
        {
            // Dépiler jusqu'à la balise correspondante (tolérance au mal-imbriqué)
            if (styleStack.Count > 1 && styleStack.Any(s => s.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase)))
            {
                while (styleStack.Count > 1)
                {
                    var (poppedTag, _) = styleStack.Pop();
                    if (poppedTag.Equals(tag, StringComparison.OrdinalIgnoreCase))
                        break;
                }
            }
        }

        while (pos < html.Length)
        {
            var lt = html.IndexOf('<', pos);
            if (lt < 0)
            {
                AddText(DecodeEntities(html[pos..]));
                break;
            }

            if (lt > pos)
                AddText(DecodeEntities(html[pos..lt]));

            // Commentaires / doctype / instructions
            if (lt + 3 < html.Length && html[lt + 1] == '!' && html[lt + 2] == '-' && html[lt + 3] == '-')
            {
                var end = html.IndexOf("-->", lt, StringComparison.Ordinal);
                pos = end < 0 ? html.Length : end + 3;
                continue;
            }
            if (lt + 1 < html.Length && (html[lt + 1] == '!' || html[lt + 1] == '?'))
            {
                var end = html.IndexOf('>', lt);
                pos = end < 0 ? html.Length : end + 1;
                continue;
            }

            var (tag, attrs, isClosing, consumed) = ReadTag(html, lt);
            if (tag.Length == 0)
            {
                // '<' isolé : texte littéral
                AddText("<");
                pos = lt + 1;
                continue;
            }
            pos = consumed;

            // Contenu entièrement ignoré (script, style...)
            if (!isClosing && SkippedContent.Contains(tag))
            {
                var close = html.IndexOf($"</{tag}", pos, StringComparison.OrdinalIgnoreCase);
                if (close < 0) break;
                var closeEnd = html.IndexOf('>', close);
                pos = closeEnd < 0 ? html.Length : closeEnd + 1;
                continue;
            }

            var lower = tag.ToLowerInvariant();

            if (isClosing)
            {
                switch (lower)
                {
                    case "p" or "div" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6" or "tr":
                        FlushParagraph();
                        if (lower[0] == 'h' && lower.Length == 2) pendingHeading = 0;
                        if (alignStack.Count > 0 && alignStack.Peek().Tag == lower) alignStack.Pop();
                        break;

                    case "b" or "strong" or "i" or "em" or "u" or "s" or "strike" or "del"
                        or "span" or "font" or "code" or "sub" or "sup" or "small" or "big":
                        PopStyle(lower);
                        break;

                    case "a":
                        if (linkStack.Count > 0) linkStack.Pop();
                        break;

                    case "ul" or "ol":
                        if (listStack.Count > 0) listStack.Pop();
                        if (contexts.Count > 1) contexts.Pop(); // contexte du dernier li
                        break;

                    case "li":
                        if (contexts.Count > 1) contexts.Pop();
                        break;

                    case "td" or "th":
                        if (contexts.Count > 1) contexts.Pop();
                        break;

                    case "table":
                        if (tableStack.Count > 0) tableStack.Pop();
                        break;

                    case "blockquote":
                        if (contexts.Count > 1) contexts.Pop();
                        break;

                    case "pre":
                        if (preDepth > 0 && --preDepth == 0)
                        {
                            Ctx().Sink.Add(new DocCodeBlock { Text = preText.ToString().Trim('\n') });
                            preText.Clear();
                        }
                        break;
                }
                continue;
            }

            // Balises ouvrantes
            switch (lower)
            {
                case "br":
                    if (preDepth > 0) preText.Append('\n');
                    else AddInline(new DocLineBreak());
                    break;

                case "hr":
                    FlushParagraph();
                    Ctx().Sink.Add(new DocSeparator());
                    break;

                case "p" or "div":
                    FlushParagraph();
                    PushAlign(alignStack, lower, attrs);
                    break;

                case "h1" or "h2" or "h3" or "h4" or "h5" or "h6":
                    FlushParagraph();
                    pendingHeading = lower[1] - '0';
                    PushAlign(alignStack, lower, attrs);
                    break;

                case "b" or "strong":
                    PushStyle(lower, ApplyCss(new DocStyle { Bold = true }, attrs));
                    break;
                case "i" or "em":
                    PushStyle(lower, ApplyCss(new DocStyle { Italic = true }, attrs));
                    break;
                case "u":
                    PushStyle(lower, ApplyCss(new DocStyle { Underline = true }, attrs));
                    break;
                case "s" or "strike" or "del":
                    PushStyle(lower, ApplyCss(new DocStyle { Strikethrough = true }, attrs));
                    break;
                case "code":
                    PushStyle(lower, ApplyCss(new DocStyle { IsCode = true }, attrs));
                    break;
                case "small":
                    PushStyle(lower, ApplyCss(new DocStyle { FontSize = 11 }, attrs));
                    break;
                case "big":
                    PushStyle(lower, ApplyCss(new DocStyle { FontSize = 17 }, attrs));
                    break;
                case "sub" or "sup":
                    PushStyle(lower, ApplyCss(new DocStyle { FontSize = 10 }, attrs));
                    break;

                case "span":
                    PushStyle(lower, ApplyCss(DocStyle.Empty, attrs));
                    break;

                case "font":
                {
                    var style = DocStyle.Empty;
                    if (attrs.TryGetValue("color", out var fc) && Color.TryParse(fc, out var fontColor))
                        style = style with { Foreground = fontColor };
                    PushStyle(lower, ApplyCss(style, attrs));
                    break;
                }

                case "a":
                {
                    var link = new DocLink { Href = attrs.GetValueOrDefault("href", "") };
                    // Liens javascript: éliminés (sanitisation)
                    if (link.Href.TrimStart().StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                        link.Href = "";
                    linkStack.Push(link);
                    break;
                }

                case "img":
                {
                    var image = new DocImage
                    {
                        Source = attrs.GetValueOrDefault("src", ""),
                        Alt = attrs.GetValueOrDefault("alt", "")
                    };
                    if (double.TryParse(attrs.GetValueOrDefault("width"), out var w)) image.Width = w;
                    if (double.TryParse(attrs.GetValueOrDefault("height"), out var h)) image.Height = h;
                    TryDecodeDataUri(image);
                    AddInline(image);
                    break;
                }

                case "ul" or "ol":
                {
                    FlushParagraph();
                    var list = new DocList { Ordered = lower == "ol" };
                    Ctx().Sink.Add(list);
                    listStack.Push(list);
                    break;
                }

                case "li":
                {
                    if (listStack.Count == 0) break;
                    // Fermer l'item précédent resté ouvert (li non fermé, courant en mail)
                    if (contexts.Count > 1 && listStack.Peek().Items.Count > 0 &&
                        ReferenceEquals(Ctx().Sink, listStack.Peek().Items[^1].Blocks))
                        contexts.Pop();

                    var item = new DocListItem();
                    listStack.Peek().Items.Add(item);
                    contexts.Push(new Context(item.Blocks));
                    break;
                }

                case "table":
                {
                    FlushParagraph();
                    var table = new DocTable();
                    Ctx().Sink.Add(table);
                    tableStack.Push(table);
                    break;
                }

                case "tr":
                    if (tableStack.Count > 0)
                        tableStack.Peek().Rows.Add(new DocTableRow());
                    break;

                case "td" or "th":
                {
                    if (tableStack.Count == 0 || tableStack.Peek().Rows.Count == 0) break;
                    var cell = new DocTableCell { IsHeader = lower == "th" };
                    if (int.TryParse(attrs.GetValueOrDefault("colspan"), out var span) && span > 1)
                        cell.ColumnSpan = span;
                    tableStack.Peek().Rows[^1].Cells.Add(cell);
                    contexts.Push(new Context(cell.Blocks));
                    break;
                }

                case "blockquote":
                {
                    FlushParagraph();
                    var quote = new DocQuote();
                    Ctx().Sink.Add(quote);
                    contexts.Push(new Context(quote.Blocks));
                    break;
                }

                case "pre":
                    FlushParagraph();
                    preDepth++;
                    break;

                // Tout le reste : balise inconnue ignorée, contenu conservé
            }
        }

        return document;
    }

    // ═══════════════════════════════════════════════════════════════
    // TOKENIZER
    // ═══════════════════════════════════════════════════════════════

    private static (string Tag, Dictionary<string, string> Attrs, bool IsClosing, int End)
        ReadTag(string html, int lt)
    {
        var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int i = lt + 1;
        var isClosing = i < html.Length && html[i] == '/';
        if (isClosing) i++;

        int nameStart = i;
        while (i < html.Length && (char.IsLetterOrDigit(html[i]))) i++;
        var tag = html[nameStart..i];
        if (tag.Length == 0)
            return ("", attrs, false, lt + 1);

        // Attributs
        while (i < html.Length && html[i] != '>')
        {
            while (i < html.Length && (char.IsWhiteSpace(html[i]) || html[i] == '/')) i++;
            if (i >= html.Length || html[i] == '>') break;

            int attrStart = i;
            while (i < html.Length && html[i] != '=' && html[i] != '>' && !char.IsWhiteSpace(html[i])) i++;
            var name = html[attrStart..i].ToLowerInvariant();

            while (i < html.Length && char.IsWhiteSpace(html[i])) i++;
            if (i < html.Length && html[i] == '=')
            {
                i++;
                while (i < html.Length && char.IsWhiteSpace(html[i])) i++;
                string value;
                if (i < html.Length && (html[i] == '"' || html[i] == '\''))
                {
                    var quote = html[i++];
                    int valueStart = i;
                    while (i < html.Length && html[i] != quote) i++;
                    value = html[valueStart..Math.Min(i, html.Length)];
                    if (i < html.Length) i++;
                }
                else
                {
                    int valueStart = i;
                    while (i < html.Length && !char.IsWhiteSpace(html[i]) && html[i] != '>') i++;
                    value = html[valueStart..i];
                }
                if (name.Length > 0)
                    attrs[name] = DecodeEntities(value);
            }
            else if (name.Length > 0)
            {
                attrs[name] = "";
            }
        }

        return (tag, attrs, isClosing, i < html.Length ? i + 1 : html.Length);
    }

    // ═══════════════════════════════════════════════════════════════
    // CSS INLINE (sous-ensemble) + HELPERS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Applique le sous-ensemble CSS du style inline sur un style de base</summary>
    private static DocStyle ApplyCss(DocStyle style, Dictionary<string, string> attrs)
    {
        if (!attrs.TryGetValue("style", out var css) || string.IsNullOrWhiteSpace(css))
            return style;

        foreach (var declaration in css.Split(';'))
        {
            var colon = declaration.IndexOf(':');
            if (colon <= 0) continue;
            var property = declaration[..colon].Trim().ToLowerInvariant();
            var value = declaration[(colon + 1)..].Trim();

            switch (property)
            {
                case "color":
                    if (TryParseCssColor(value, out var fg)) style = style with { Foreground = fg };
                    break;
                case "background" or "background-color":
                    if (TryParseCssColor(value, out var bg)) style = style with { Background = bg };
                    break;
                case "font-weight":
                    style = style with { Bold = value is "bold" or "bolder" or "600" or "700" or "800" or "900" };
                    break;
                case "font-style":
                    style = style with { Italic = value == "italic" || value == "oblique" };
                    break;
                case "text-decoration" or "text-decoration-line":
                    if (value.Contains("underline")) style = style with { Underline = true };
                    if (value.Contains("line-through")) style = style with { Strikethrough = true };
                    if (value.Contains("none")) style = style with { Underline = false, Strikethrough = false };
                    break;
                case "font-size":
                    if (TryParseCssFontSize(value, out var size)) style = style with { FontSize = size };
                    break;
                case "font-family":
                    style = style with { FontFamily = value.Split(',')[0].Trim().Trim('"', '\'') };
                    break;
                // Tout autre propriété (position, display, float...) : ignorée
            }
        }
        return style;
    }

    /// <summary>Alignement depuis align= ou style="text-align:..."</summary>
    private static void PushAlign(Stack<(string, TextAlignment)> stack, string tag,
        Dictionary<string, string> attrs)
    {
        string? value = attrs.GetValueOrDefault("align");
        if (attrs.TryGetValue("style", out var css))
        {
            foreach (var d in css.Split(';'))
            {
                var colon = d.IndexOf(':');
                if (colon > 0 && d[..colon].Trim().Equals("text-align", StringComparison.OrdinalIgnoreCase))
                    value = d[(colon + 1)..].Trim();
            }
        }

        var align = value?.ToLowerInvariant() switch
        {
            "center" => TextAlignment.Center,
            "right" => TextAlignment.Right,
            "justify" => TextAlignment.Justify,
            _ => (TextAlignment?)null
        };

        if (align.HasValue)
            stack.Push((tag, align.Value));
    }

    internal static bool TryParseCssColor(string value, out Color color)
    {
        value = value.Trim();

        if (value.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
        {
            var open = value.IndexOf('(');
            var close = value.IndexOf(')');
            if (open > 0 && close > open)
            {
                var parts = value[(open + 1)..close].Split(',');
                if (parts.Length >= 3 &&
                    byte.TryParse(parts[0].Trim(), out var r) &&
                    byte.TryParse(parts[1].Trim(), out var g) &&
                    byte.TryParse(parts[2].Trim(), out var b))
                {
                    color = Color.FromRgb(r, g, b);
                    return true;
                }
            }
            color = default;
            return false;
        }

        return Color.TryParse(value, out color);
    }

    internal static bool TryParseCssFontSize(string value, out double size)
    {
        size = 0;
        value = value.Trim().ToLowerInvariant();

        double factor = 1;
        string number = value;

        if (value.EndsWith("px")) number = value[..^2];
        else if (value.EndsWith("pt")) { number = value[..^2]; factor = 96.0 / 72.0; }
        else if (value.EndsWith("em")) { number = value[..^2]; factor = 14; }
        else if (value.EndsWith("%")) { number = value[..^1]; factor = 0.14; }
        else if (!double.TryParse(number, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out _))
            return false;

        if (!double.TryParse(number, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            return false;

        size = Math.Clamp(parsed * factor, 6, 96);
        return true;
    }

    private static void TryDecodeDataUri(DocImage image)
    {
        const string marker = ";base64,";
        if (!image.Source.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return;

        var index = image.Source.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return;

        try
        {
            image.Data = Convert.FromBase64String(image.Source[(index + marker.Length)..]);
        }
        catch (FormatException)
        {
            // data: corrompu : l'image restera un placeholder
        }
    }

    private static string CollapseWhitespace(string text)
    {
        var sb = new StringBuilder(text.Length);
        var lastWasSpace = false;
        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!lastWasSpace) sb.Append(' ');
                lastWasSpace = true;
            }
            else
            {
                sb.Append(ch);
                lastWasSpace = false;
            }
        }
        return sb.ToString();
    }

    internal static string DecodeEntities(string text)
    {
        if (!text.Contains('&')) return text;

        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '&')
            {
                sb.Append(text[i]);
                continue;
            }

            var semi = text.IndexOf(';', i);
            if (semi < 0 || semi - i > 10)
            {
                sb.Append('&');
                continue;
            }

            var entity = text[(i + 1)..semi];
            string? decoded = entity switch
            {
                "amp" => "&",
                "lt" => "<",
                "gt" => ">",
                "quot" => "\"",
                "apos" => "'",
                "nbsp" => " ",
                "copy" => "©",
                "reg" => "®",
                "trade" => "™",
                "eacute" => "é",
                "egrave" => "è",
                "agrave" => "à",
                "ccedil" => "ç",
                "ugrave" => "ù",
                "hellip" => "…",
                "mdash" => "—",
                "ndash" => "–",
                "rsquo" => "'",
                "lsquo" => "'",
                "ldquo" => "“",
                "rdquo" => "”",
                _ => null
            };

            if (decoded == null && entity.StartsWith('#'))
            {
                var isHex = entity.Length > 1 && (entity[1] == 'x' || entity[1] == 'X');
                var numberText = isHex ? entity[2..] : entity[1..];
                if (int.TryParse(numberText,
                        isHex ? System.Globalization.NumberStyles.HexNumber
                              : System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out var code)
                    && code > 0 && code <= 0x10FFFF)
                {
                    decoded = char.ConvertFromUtf32(code);
                }
            }

            if (decoded != null)
            {
                sb.Append(decoded);
                i = semi;
            }
            else
            {
                sb.Append('&');
            }
        }
        return sb.ToString();
    }
}
