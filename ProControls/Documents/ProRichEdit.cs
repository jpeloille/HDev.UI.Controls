using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using ProControls.Documents;
using ProControls.Theme;

namespace ProControls.Controls;

/// <summary>
/// Éditeur de texte riche natif (monstre n° 2, tête d'édition) : caret,
/// sélection, saisie, formatage, undo/redo — au-dessus de DocEditor (moteur
/// testé) et DocLayoutEngine (même rendu que ProHtmlView).
/// Hauteur au contenu : placer dans un ScrollViewer. Html en entrée/sortie.
/// </summary>
public class ProRichEdit : Control
{
    private const double ContentPadding = 8;

    private DocEditor _editor;
    private DocLayoutResult? _layout;
    private double _layoutWidth = -1;
    private readonly Dictionary<DocParagraph, DocTextBox> _boxByParagraph = new();

    private readonly DispatcherTimer _caretTimer;
    private bool _caretVisible = true;
    private bool _mouseSelecting;
    private double _preferredCaretX = -1; // colonne mémorisée pour ↑/↓

    /// <summary>Déclenché après toute modification du document</summary>
    public event EventHandler? TextChanged;

    public ProRichEdit()
    {
        _editor = new DocEditor();
        _editor.Changed += OnEditorChanged;
        ClipToBounds = true;
        Cursor = new Cursor(StandardCursorType.Ibeam);

        _caretTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(530) };
        _caretTimer.Tick += (s, e) =>
        {
            _caretVisible = !_caretVisible;
            InvalidateVisual();
        };
    }

    static ProRichEdit()
    {
        FocusableProperty.OverrideDefaultValue<ProRichEdit>(true);
    }

    /// <summary>Moteur d'édition sous-jacent (commandes avancées)</summary>
    public DocEditor Editor => _editor;

    /// <summary>Contenu HTML (sortie sanitisée par construction du modèle)</summary>
    public string Html
    {
        get => _editor.ToHtml();
        set
        {
            _editor.Changed -= OnEditorChanged;
            _editor = new DocEditor(HtmlParser.Parse(value ?? ""));
            _editor.Changed += OnEditorChanged;
            _layoutWidth = -1;
            InvalidateMeasure();
            InvalidateVisual();
            TextChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Texte brut du document</summary>
    public string GetText() => _editor.Document.GetText();

    private void OnEditorChanged(object? sender, EventArgs e)
    {
        _layoutWidth = -1;
        InvalidateMeasure();
        InvalidateVisual();
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    // ═══════════════════════════════════════════════════════════════
    // COMMANDES DE FORMATAGE (barre d'outils)
    // ═══════════════════════════════════════════════════════════════

    public void ToggleBold() { _editor.ToggleBold(); FocusAndShowCaret(); }
    public void ToggleItalic() { _editor.ToggleItalic(); FocusAndShowCaret(); }
    public void ToggleUnderline() { _editor.ToggleUnderline(); FocusAndShowCaret(); }
    public void ToggleStrikethrough() { _editor.ToggleStrikethrough(); FocusAndShowCaret(); }
    public void SetHeading(int level) { _editor.SetHeading(level); FocusAndShowCaret(); }
    public void Undo() { _editor.Undo(); FocusAndShowCaret(); }
    public void Redo() { _editor.Redo(); FocusAndShowCaret(); }

    // Commandes v2
    public void ToggleBulletList() { _editor.ToggleBulletList(); FocusAndShowCaret(); }
    public void ToggleNumberedList() { _editor.ToggleNumberedList(); FocusAndShowCaret(); }
    public void SetAlignment(TextAlignment alignment) { _editor.SetAlignment(alignment); FocusAndShowCaret(); }
    public void SetTextColor(Color? color) { _editor.SetTextColor(color); FocusAndShowCaret(); }
    public void SetHighlight(Color? color) { _editor.SetHighlight(color); FocusAndShowCaret(); }
    public void SetFontSize(double? size) { _editor.SetFontSize(size); FocusAndShowCaret(); }
    public void InsertImage(DocImage image) { _editor.InsertImage(image); FocusAndShowCaret(); }
    public void InsertHtmlAtCaret(string html) { _editor.InsertHtml(html); FocusAndShowCaret(); }

    /// <summary>
    /// Pose un lien sur la sélection en demandant l'adresse (Ctrl+K).
    /// Saisie vide = retire le lien.
    /// </summary>
    public async Task InsertLinkInteractiveAsync()
    {
        if (!_editor.HasSelection) return;
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null) return;

        var href = await ProInputBox.ShowAsync(owner,
            "Adresse du lien (vide = retirer) :", "Insérer un lien",
            _editor.GetCurrentLink() ?? "https://");
        if (href == null) return; // annulé

        _editor.SetLink(string.IsNullOrWhiteSpace(href) ? null : href.Trim());
        FocusAndShowCaret();
    }

    /// <summary>Insère une image choisie sur disque (embarquée en data: → HTML portable)</summary>
    public async Task InsertImageInteractiveAsync()
    {
        var top = TopLevel.GetTopLevel(this);
        if (top == null) return;

        var files = await top.StorageProvider.OpenFilePickerAsync(
            new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Insérer une image",
                AllowMultiple = false,
                FileTypeFilter = new[] { Avalonia.Platform.Storage.FilePickerFileTypes.ImageAll }
            });
        if (files.Count == 0) return;

        await using var stream = await files[0].OpenReadAsync();
        using var memory = new System.IO.MemoryStream();
        await stream.CopyToAsync(memory);
        var bytes = memory.ToArray();

        var mime = files[0].Name.ToLowerInvariant() switch
        {
            var n when n.EndsWith(".jpg") || n.EndsWith(".jpeg") => "image/jpeg",
            var n when n.EndsWith(".gif") => "image/gif",
            var n when n.EndsWith(".webp") => "image/webp",
            _ => "image/png"
        };

        _editor.InsertImage(new DocImage
        {
            // data: = l'image traverse ToHtml/Html sans dépendre du disque
            Source = $"data:{mime};base64,{Convert.ToBase64String(bytes)}",
            Data = bytes,
            Alt = files[0].Name
        });
        FocusAndShowCaret();
    }

    private void FocusAndShowCaret()
    {
        Focus();
        _caretVisible = true;
    }

    // ═══════════════════════════════════════════════════════════════
    // LAYOUT + MAPPING PARAGRAPHE ↔ BOÎTE
    // ═══════════════════════════════════════════════════════════════

    private void EnsureLayout(double width)
    {
        if (_layout != null && Math.Abs(width - _layoutWidth) < 0.5) return;
        _layoutWidth = width;
        _layout = DocLayoutEngine.Layout(_editor.Document, width);

        _boxByParagraph.Clear();
        foreach (var box in _layout.Boxes)
        {
            if (box is DocTextBox { Source: { } source } textBox)
                _boxByParagraph[source] = textBox;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width)
            ? (Width > 0 ? Width : 600)
            : availableSize.Width;

        EnsureLayout(Math.Max(60, width - ContentPadding * 2));
        return new Size(width, Math.Max(60, (_layout?.Height ?? 0) + ContentPadding * 2));
    }

    /// <summary>Rectangle du caret (coordonnées contrôle)</summary>
    private Rect? CaretRect()
    {
        if (_layout == null || _editor.Caret.Block >= _editor.Paragraphs.Count) return null;
        var paragraph = _editor.Paragraphs[_editor.Caret.Block];

        if (!_boxByParagraph.TryGetValue(paragraph, out var box))
        {
            // Paragraphe vide : pas de TextLayout — caret en tête de zone estimée
            return new Rect(ContentPadding, ContentPadding, 1.5, _editor.Document.BaseStyle.FontSize ?? 14);
        }

        var hit = box.Text.HitTestTextPosition(_editor.Caret.Offset);
        return new Rect(
            box.Bounds.X + hit.X + ContentPadding,
            box.Bounds.Y + hit.Y + ContentPadding,
            1.5,
            Math.Max(6, hit.Height));
    }

    /// <summary>Position (bloc, offset) au point donné (coordonnées contrôle)</summary>
    private DocCaret? PositionAt(Point point)
    {
        if (_layout == null) return null;
        var local = new Point(point.X - ContentPadding, point.Y - ContentPadding);

        DocTextBox? best = null;
        var bestBlock = -1;

        for (int b = 0; b < _editor.Paragraphs.Count; b++)
        {
            if (!_boxByParagraph.TryGetValue(_editor.Paragraphs[b], out var box)) continue;

            if (local.Y >= box.Bounds.Y && local.Y <= box.Bounds.Bottom)
            {
                best = box;
                bestBlock = b;
                break;
            }
            // Sinon : boîte la plus proche au-dessus (clic dans une marge)
            if (box.Bounds.Y <= local.Y)
            {
                best = box;
                bestBlock = b;
            }
        }

        if (best == null || bestBlock < 0)
            return _editor.Paragraphs.Count > 0 ? new DocCaret(0, 0) : null;

        var hit = best.Text.HitTestPoint(new Point(
            local.X - best.Bounds.X, local.Y - best.Bounds.Y));
        var offset = hit.CharacterHit.FirstCharacterIndex + hit.CharacterHit.TrailingLength;
        return new DocCaret(bestBlock, Math.Clamp(offset, 0, _editor.GetParagraphLength(bestBlock)));
    }

    // ═══════════════════════════════════════════════════════════════
    // SOURIS
    // ═══════════════════════════════════════════════════════════════

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Focus();
        var position = PositionAt(e.GetPosition(this));
        if (position != null)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                _editor.Anchor ??= _editor.Caret;
            }
            else
            {
                _editor.Anchor = position;
            }
            _editor.Caret = position.Value;
            _mouseSelecting = true;
            _preferredCaretX = -1;
            _caretVisible = true;
            e.Pointer.Capture(this);
            InvalidateVisual();
        }
        e.Handled = true;
        base.OnPointerPressed(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_mouseSelecting)
        {
            var position = PositionAt(e.GetPosition(this));
            if (position != null && position.Value != _editor.Caret)
            {
                _editor.Caret = position.Value;
                InvalidateVisual();
            }
        }
        base.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_mouseSelecting)
        {
            _mouseSelecting = false;
            if (_editor.Anchor == _editor.Caret)
                _editor.Anchor = null;
            e.Pointer.Capture(null);
        }
        base.OnPointerReleased(e);
    }

    // ═══════════════════════════════════════════════════════════════
    // CLAVIER + SAISIE
    // ═══════════════════════════════════════════════════════════════

    protected override void OnTextInput(TextInputEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Text) && !char.IsControl(e.Text[0]))
        {
            _editor.InsertText(e.Text);
            _preferredCaretX = -1;
            ScrollCaretIntoView();
            e.Handled = true;
        }
        base.OnTextInput(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var handled = true;

        switch (e.Key)
        {
            case Key.Left: MoveCaret(-1, shift); break;
            case Key.Right: MoveCaret(1, shift); break;
            case Key.Up: MoveCaretVertical(-1, shift); break;
            case Key.Down: MoveCaretVertical(1, shift); break;
            case Key.Home: MoveCaretToLineEdge(start: true, shift); break;
            case Key.End: MoveCaretToLineEdge(start: false, shift); break;

            case Key.Back: _editor.DeleteBackward(); break;
            case Key.Delete: _editor.DeleteForward(); break;
            case Key.Enter: _editor.SplitParagraph(); break;

            case Key.A when ctrl: SelectAll(); break;
            case Key.B when ctrl: _editor.ToggleBold(); break;
            case Key.I when ctrl: _editor.ToggleItalic(); break;
            case Key.U when ctrl: _editor.ToggleUnderline(); break;
            case Key.Z when ctrl: _editor.Undo(); break;
            case Key.Y when ctrl: _editor.Redo(); break;
            case Key.C when ctrl: _ = CopyAsync(); break;
            case Key.X when ctrl: _ = CutAsync(); break;
            case Key.V when ctrl: _ = PasteAsync(); break;
            case Key.K when ctrl: _ = InsertLinkInteractiveAsync(); break;
            case Key.L when ctrl && shift: _editor.ToggleBulletList(); break;
            case Key.E when ctrl: _editor.SetAlignment(TextAlignment.Center); break;
            case Key.R when ctrl && shift: _editor.SetAlignment(TextAlignment.Right); break;
            case Key.J when ctrl: _editor.SetAlignment(TextAlignment.Justify); break;

            default: handled = false; break;
        }

        if (handled)
        {
            _caretVisible = true;
            ScrollCaretIntoView();
            InvalidateVisual();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    private void MoveCaret(int delta, bool extendSelection)
    {
        UpdateAnchor(extendSelection);
        _preferredCaretX = -1;

        var caret = _editor.Caret;
        var offset = caret.Offset + delta;

        if (offset < 0 && caret.Block > 0)
            _editor.Caret = new DocCaret(caret.Block - 1, _editor.GetParagraphLength(caret.Block - 1));
        else if (offset > _editor.GetParagraphLength(caret.Block) && caret.Block < _editor.Paragraphs.Count - 1)
            _editor.Caret = new DocCaret(caret.Block + 1, 0);
        else
            _editor.Caret = caret with { Offset = Math.Clamp(offset, 0, _editor.GetParagraphLength(caret.Block)) };
    }

    private void MoveCaretVertical(int direction, bool extendSelection)
    {
        UpdateAnchor(extendSelection);

        var rect = CaretRect();
        if (rect == null) return;
        if (_preferredCaretX < 0) _preferredCaretX = rect.Value.X;

        var probeY = direction < 0
            ? rect.Value.Y - rect.Value.Height / 2
            : rect.Value.Bottom + rect.Value.Height / 2;

        var position = PositionAt(new Point(_preferredCaretX, probeY));
        if (position != null)
            _editor.Caret = position.Value;
    }

    private void MoveCaretToLineEdge(bool start, bool extendSelection)
    {
        UpdateAnchor(extendSelection);
        _preferredCaretX = -1;

        var paragraph = _editor.Paragraphs[_editor.Caret.Block];
        if (!_boxByParagraph.TryGetValue(paragraph, out var box))
        {
            _editor.Caret = _editor.Caret with { Offset = 0 };
            return;
        }

        // Ligne du caret dans le TextLayout
        var offset = _editor.Caret.Offset;
        foreach (var line in box.Text.TextLines)
        {
            if (offset >= line.FirstTextSourceIndex &&
                offset <= line.FirstTextSourceIndex + line.Length)
            {
                var target = start
                    ? line.FirstTextSourceIndex
                    : line.FirstTextSourceIndex + line.Length -
                      (line.Length > 0 && offset < _editor.GetParagraphLength(_editor.Caret.Block) ? 0 : 0);
                _editor.Caret = _editor.Caret with
                {
                    Offset = Math.Clamp(start ? target : line.FirstTextSourceIndex + line.Length,
                        0, _editor.GetParagraphLength(_editor.Caret.Block))
                };
                return;
            }
        }
    }

    private void UpdateAnchor(bool extendSelection)
    {
        if (extendSelection)
            _editor.Anchor ??= _editor.Caret;
        else
            _editor.Anchor = null;
    }

    public void SelectAll()
    {
        _editor.Anchor = new DocCaret(0, 0);
        var last = _editor.Paragraphs.Count - 1;
        _editor.Caret = new DocCaret(last, _editor.GetParagraphLength(last));
        InvalidateVisual();
    }

    private const string HtmlClipboardFormat = "text/html";

    private async Task CopyAsync()
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null || !_editor.HasSelection) return;

        // Texte brut + HTML : un autre ProRichEdit (ou LibreOffice…) recolle riche
        var data = new DataObject();
        data.Set(DataFormats.Text, _editor.GetSelectedText());
        data.Set(HtmlClipboardFormat, System.Text.Encoding.UTF8.GetBytes(_editor.GetSelectedHtml()));
        await clipboard.SetDataObjectAsync(data);
    }

    private async Task CutAsync()
    {
        await CopyAsync();
        _editor.DeleteSelection();
    }

    private async Task PasteAsync()
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;

        // 1) HTML riche si disponible (selon la plateforme : string ou octets UTF-8)
        var html = await clipboard.GetDataAsync(HtmlClipboardFormat) switch
        {
            string s => s,
            byte[] b => System.Text.Encoding.UTF8.GetString(b),
            _ => null
        };
        if (!string.IsNullOrWhiteSpace(html))
        {
            _editor.InsertHtml(html!);
            ScrollCaretIntoView();
            return;
        }

        // 2) Repli texte brut : chaque ligne devient un paragraphe
        var text = await clipboard.GetTextAsync();
        if (string.IsNullOrEmpty(text)) return;

        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0) _editor.SplitParagraph();
            if (lines[i].Length > 0) _editor.InsertText(lines[i]);
        }
        ScrollCaretIntoView();
    }

    private void ScrollCaretIntoView()
    {
        var rect = CaretRect();
        if (rect != null)
            this.BringIntoView(rect.Value.Inflate(20));
    }

    // ═══════════════════════════════════════════════════════════════
    // FOCUS + CARET CLIGNOTANT
    // ═══════════════════════════════════════════════════════════════

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ProTheme.VariantChanged += OnThemeVariantChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ProTheme.VariantChanged -= OnThemeVariantChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        // Les TextLayouts portent des brushes construits : re-layout complet
        _layoutWidth = -1;
        InvalidateMeasure();
        InvalidateVisual();
    }

    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        _caretVisible = true;
        _caretTimer.Start();
        InvalidateVisual();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(Avalonia.Interactivity.RoutedEventArgs e)
    {
        _caretTimer.Stop();
        _caretVisible = false;
        InvalidateVisual();
        base.OnLostFocus(e);
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(new SolidColorBrush(ProTheme.Background.Control), bounds);

        if (_layout == null) return;

        // Sélection (sous le texte)
        if (_editor.HasSelection)
        {
            var brush = new SolidColorBrush(ProTheme.WithOpacity(ProTheme.Accent.Primary, 60));
            var (start, end) = _editor.SelectionRange();

            for (int b = start.Block; b <= end.Block && b < _editor.Paragraphs.Count; b++)
            {
                if (!_boxByParagraph.TryGetValue(_editor.Paragraphs[b], out var box)) continue;

                var from = b == start.Block ? start.Offset : 0;
                var to = b == end.Block ? end.Offset : _editor.GetParagraphLength(b);
                if (to <= from && b != start.Block) continue;

                foreach (var rect in box.Text.HitTestTextRange(from, Math.Max(0, to - from)))
                {
                    context.FillRectangle(brush, new Rect(
                        rect.X + box.Bounds.X + ContentPadding,
                        rect.Y + box.Bounds.Y + ContentPadding,
                        Math.Max(3, rect.Width), rect.Height));
                }
            }
        }

        DocLayoutEngine.Render(context, _layout, bounds, new Vector(ContentPadding, ContentPadding));

        // Caret
        if (_caretVisible && IsFocused && !_editor.HasSelection)
        {
            var rect = CaretRect();
            if (rect != null)
                context.FillRectangle(new SolidColorBrush(ProTheme.Text.Primary), rect.Value);
        }
    }
}
