using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using ProControls.Theme;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;

namespace ProControls.Controls;

/// <summary>
/// Contenu structuré d'un item (produit par l'adaptateur de l'application)
/// </summary>
public class ProListItemContent
{
    /// <summary>Ligne principale (expéditeur, nom...)</summary>
    public string Title { get; set; } = "";

    /// <summary>Ligne secondaire (objet + aperçu...), tronquée avec ellipse</summary>
    public string? Subtitle { get; set; }

    /// <summary>Texte de droite (date, taille...), masqué au survol si actions</summary>
    public string? Trailing { get; set; }

    /// <summary>Icône emoji ou initiales (pastille ronde à gauche)</summary>
    public string? Icon { get; set; }

    /// <summary>Pastille (compteur, étiquette) sous le Trailing</summary>
    public string? Badge { get; set; }

    /// <summary>Item mis en avant : titre gras + barre accent (non-lu)</summary>
    public bool Emphasized { get; set; }
}

/// <summary>Action affichée au survol d'un item (glyphe à droite)</summary>
public class ProListHoverAction
{
    public string Glyph { get; }
    public string Tooltip { get; }
    public Action<object> Execute { get; }

    public ProListHoverAction(string glyph, string tooltip, Action<object> execute)
    {
        Glyph = glyph;
        Tooltip = tooltip;
        Execute = execute;
    }
}

/// <summary>
/// Liste virtualisée à items riches (équivalent liste de messages Outlook) :
/// adaptateur structuré (titre/aperçu/date/icône/badge), groupes repliables,
/// sélection simple/multiple, actions au survol. Le cheval de trait du shell
/// (mails, contacts, résultats de recherche).
/// </summary>
public class ProListView : Control
{
    private const double HeaderHeight = 30;
    private const double ScrollBarSize = 14;
    private const double ActionSize = 26;

    private IEnumerable? _itemsSource;
    private double _rowHeight = 56;
    private double _verticalOffset;
    private bool _syncingScroll;

    private readonly List<Row> _rows = new();
    private readonly HashSet<string> _collapsedGroups = new();
    private readonly List<object> _selected = new();
    private object? _currentItem;
    private int _hoveredRow = -1;
    private int _hoveredAction = -1;

    private readonly ScrollBar _scrollBar;

    private readonly record struct Row(bool IsHeader, string? GroupKey, object? Item, int GroupCount);

    // ═══════════════════════════════════════════════════════════════
    // API
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Adaptateur : objet métier → contenu affiché (requis)</summary>
    public Func<object, ProListItemContent>? ItemAdapter { get; set; }

    /// <summary>Clé de groupe par item (groupes adjacents, ex : Aujourd'hui/Hier)</summary>
    public Func<object, string>? GroupSelector { get; set; }

    /// <summary>Actions affichées au survol (la zone de droite remplace le Trailing)</summary>
    public List<ProListHoverAction> HoverActions { get; } = new();

    public SelectionMode SelectionMode { get; set; } = SelectionMode.Single;

    /// <summary>Texte affiché quand la liste est vide</summary>
    public string EmptyText { get; set; } = "Aucun élément";

    /// <summary>Les items peuvent être glissés (drag &amp; drop vers un ProTreeView...)</summary>
    public bool EnableDragItems { get; set; }

    /// <summary>Format de glisser-déposer des items (reste dans le processus)</summary>
    public static readonly DataFormat<object> DragDataFormat =
        DataFormat.CreateInProcessFormat<object>("procontrols-item");

    private Point _pressPoint;
    private object? _pressItem;
    // DoDragDropAsync exige l'appui d'origine ; le seuil n'est franchi qu'au Moved
    private PointerPressedEventArgs? _pressArgs;
    private bool _dragStarted;

    public event EventHandler? SelectionChanged;
    public event EventHandler<object>? ItemDoubleClicked;

    public IEnumerable? ItemsSource
    {
        get => _itemsSource;
        set
        {
            if (ReferenceEquals(_itemsSource, value)) return;

            if (_itemsSource is INotifyCollectionChanged oldNotify)
                oldNotify.CollectionChanged -= OnSourceChanged;

            _itemsSource = value;

            if (_itemsSource is INotifyCollectionChanged newNotify)
                newNotify.CollectionChanged += OnSourceChanged;

            RebuildRows();
        }
    }

    /// <summary>Hauteur d'une ligne d'item (56 = deux lignes, 32 = compacte)</summary>
    public double RowHeight
    {
        get => _rowHeight;
        set { _rowHeight = Math.Max(20, value); RebuildRows(); }
    }

    public object? SelectedItem => _selected.FirstOrDefault();
    public IReadOnlyList<object> SelectedItems => _selected;

    public void SelectItem(object? item)
    {
        _selected.Clear();
        if (item != null) _selected.Add(item);
        _currentItem = item;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        InvalidateVisual();
    }

    /// <summary>Replie/déplie un groupe</summary>
    public void ToggleGroup(string key)
    {
        if (!_collapsedGroups.Remove(key))
            _collapsedGroups.Add(key);
        RebuildRows();
    }

    /// <summary>Force la reconstruction (adaptateur/groupes changés)</summary>
    public void Refresh() => RebuildRows();

    // ═══════════════════════════════════════════════════════════════
    // CONSTRUCTION
    // ═══════════════════════════════════════════════════════════════

    static ProListView()
    {
        FocusableProperty.OverrideDefaultValue<ProListView>(true);
    }

    public ProListView()
    {
        ClipToBounds = true;

        _scrollBar = new ScrollBar
        {
            Orientation = Avalonia.Layout.Orientation.Vertical,
            Width = ScrollBarSize,
            AllowAutoHide = false
        };
        _scrollBar.Scroll += (s, e) =>
        {
            if (_syncingScroll) return;
            _verticalOffset = _scrollBar.Value;
            InvalidateVisual();
        };
        VisualChildren.Add(_scrollBar);
        LogicalChildren.Add(_scrollBar);
    }

    private void OnSourceChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Purger la sélection des items disparus
        if (_itemsSource != null)
        {
            var alive = new HashSet<object>(_itemsSource.Cast<object>());
            _selected.RemoveAll(i => !alive.Contains(i));
            if (_currentItem != null && !alive.Contains(_currentItem))
                _currentItem = null;
        }
        RebuildRows();
    }

    private void RebuildRows()
    {
        _rows.Clear();

        if (_itemsSource != null)
        {
            if (GroupSelector == null)
            {
                foreach (var item in _itemsSource)
                    _rows.Add(new Row(false, null, item, 0));
            }
            else
            {
                // Groupes adjacents (l'app fournit la source déjà ordonnée)
                string? currentKey = null;
                var groupStart = -1;

                var items = _itemsSource.Cast<object>().ToList();
                for (int i = 0; i <= items.Count; i++)
                {
                    var key = i < items.Count ? GroupSelector(items[i]) : null;
                    if (key != currentKey)
                    {
                        if (groupStart >= 0)
                        {
                            // Poser le compte sur l'en-tête du groupe précédent
                            var header = _rows[groupStart];
                            _rows[groupStart] = header with { GroupCount = CountSince(items, header.GroupKey!, groupStart) };
                        }
                        if (key != null)
                        {
                            _rows.Add(new Row(true, key, null, 0));
                            groupStart = _rows.Count - 1;
                            currentKey = key;
                        }
                    }

                    if (i < items.Count && currentKey != null && !_collapsedGroups.Contains(currentKey))
                        _rows.Add(new Row(false, currentKey, items[i], 0));
                }
            }
        }

        UpdateScrollBar();
        InvalidateVisual();
    }

    private int CountSince(List<object> items, string key, int _)
        => GroupSelector == null ? 0 : items.Count(i => GroupSelector(i) == key);

    // ═══════════════════════════════════════════════════════════════
    // GÉOMÉTRIE + VIRTUALISATION
    // ═══════════════════════════════════════════════════════════════

    private double RowTop(int index)
    {
        // Hauteurs mixtes en-tête/item : cumul arithmétique
        double y = 0;
        for (int i = 0; i < index; i++)
            y += _rows[i].IsHeader ? HeaderHeight : _rowHeight;
        return y;
    }

    private double TotalHeight()
        => _rows.Sum(r => r.IsHeader ? HeaderHeight : _rowHeight);

    private int RowAt(double y)
    {
        double cursor = 0;
        for (int i = 0; i < _rows.Count; i++)
        {
            cursor += _rows[i].IsHeader ? HeaderHeight : _rowHeight;
            if (y < cursor) return i;
        }
        return -1;
    }

    private (int First, int Last) VisibleRange()
    {
        var first = RowAt(_verticalOffset);
        var last = RowAt(_verticalOffset + Bounds.Height);
        return (Math.Max(0, first), last < 0 ? _rows.Count - 1 : last);
    }

    private void UpdateScrollBar()
    {
        _syncingScroll = true;
        var viewport = Math.Max(0, Bounds.Height);
        _scrollBar.Minimum = 0;
        _scrollBar.Maximum = Math.Max(0, TotalHeight() - viewport);
        _scrollBar.ViewportSize = viewport;
        _verticalOffset = Math.Clamp(_verticalOffset, 0, Math.Max(0, _scrollBar.Maximum));
        _scrollBar.Value = _verticalOffset;
        _syncingScroll = false;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _scrollBar.Measure(finalSize);
        _scrollBar.Arrange(new Rect(finalSize.Width - ScrollBarSize, 0, ScrollBarSize, finalSize.Height));
        UpdateScrollBar();
        return finalSize;
    }

    protected override Size MeasureOverride(Size availableSize)
        => new(
            double.IsInfinity(availableSize.Width) ? 320 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 400 : availableSize.Height);

    private double ContentWidth => Math.Max(0, Bounds.Width - ScrollBarSize);

    private List<Rect> ActionRects(int rowIndex)
    {
        var rects = new List<Rect>();
        var y = RowTop(rowIndex) - _verticalOffset;
        var x = ContentWidth - 8;
        for (int i = HoverActions.Count - 1; i >= 0; i--)
        {
            x -= ActionSize + 4;
            rects.Insert(0, new Rect(x, y + (_rowHeight - ActionSize) / 2, ActionSize, ActionSize));
        }
        return rects;
    }

    // ═══════════════════════════════════════════════════════════════
    // SOURIS
    // ═══════════════════════════════════════════════════════════════

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var pos = e.GetPosition(this);

        // Départ de drag : bouton enfoncé sur un item + dépassement du seuil
        if (EnableDragItems && !_dragStarted && _pressItem != null && _pressArgs != null &&
            e.GetCurrentPoint(this).Properties.IsLeftButtonPressed &&
            (Math.Abs(pos.X - _pressPoint.X) > 5 || Math.Abs(pos.Y - _pressPoint.Y) > 5))
        {
            _dragStarted = true;
            var data = new DataTransfer();
            data.Add(DataTransferItem.Create(DragDataFormat, _pressItem));
            _ = DragDrop.DoDragDropAsync(_pressArgs, data, DragDropEffects.Move);
            _pressArgs = null;
            return;
        }

        var row = pos.X < ContentWidth ? RowAt(pos.Y + _verticalOffset) : -1;

        var action = -1;
        if (row >= 0 && !_rows[row].IsHeader && HoverActions.Count > 0)
        {
            var rects = ActionRects(row);
            for (int i = 0; i < rects.Count; i++)
            {
                if (rects[i].Contains(pos)) { action = i; break; }
            }
        }

        if (row != _hoveredRow || action != _hoveredAction)
        {
            _hoveredRow = row;
            _hoveredAction = action;
            ToolTip.SetTip(this, action >= 0 ? HoverActions[action].Tooltip : null);
            Cursor = action >= 0 ? new Cursor(StandardCursorType.Hand) : Cursor.Default;
            InvalidateVisual();
        }
        base.OnPointerMoved(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _hoveredRow = -1;
        _hoveredAction = -1;
        InvalidateVisual();
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Focus();
        var pos = e.GetPosition(this);
        var index = pos.X < ContentWidth ? RowAt(pos.Y + _verticalOffset) : -1;
        if (index < 0)
        {
            base.OnPointerPressed(e);
            return;
        }

        var row = _rows[index];

        if (row.IsHeader)
        {
            ToggleGroup(row.GroupKey!);
            e.Handled = true;
            return;
        }

        // Action au survol
        if (_hoveredAction >= 0 && row.Item != null)
        {
            HoverActions[_hoveredAction].Execute(row.Item);
            e.Handled = true;
            return;
        }

        if (row.Item == null) return;

        // Mémoriser pour un éventuel départ de drag
        _pressPoint = pos;
        _pressItem = row.Item;
        _pressArgs = e;
        _dragStarted = false;

        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        if (SelectionMode == SelectionMode.Multiple && ctrl)
        {
            if (!_selected.Remove(row.Item))
                _selected.Add(row.Item);
            _currentItem = row.Item;
        }
        else if (SelectionMode == SelectionMode.Multiple && shift && _currentItem != null)
        {
            SelectRangeTo(row.Item);
        }
        else
        {
            _selected.Clear();
            _selected.Add(row.Item);
            _currentItem = row.Item;
        }

        SelectionChanged?.Invoke(this, EventArgs.Empty);

        if (e.ClickCount == 2)
            ItemDoubleClicked?.Invoke(this, row.Item);

        e.Handled = true;
        InvalidateVisual();
        base.OnPointerPressed(e);
    }

    private void SelectRangeTo(object item)
    {
        var itemRows = _rows.Where(r => !r.IsHeader && r.Item != null).Select(r => r.Item!).ToList();
        var from = itemRows.IndexOf(_currentItem!);
        var to = itemRows.IndexOf(item);
        if (from < 0 || to < 0) return;
        if (from > to) (from, to) = (to, from);

        _selected.Clear();
        for (int i = from; i <= to; i++)
            _selected.Add(itemRows[i]);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _pressItem = null;
        _pressArgs = null;
        _dragStarted = false;
        base.OnPointerReleased(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        _verticalOffset = Math.Clamp(
            _verticalOffset + (e.Delta.Y > 0 ? -3 : 3) * _rowHeight,
            0, Math.Max(0, TotalHeight() - Bounds.Height));
        UpdateScrollBar();
        InvalidateVisual();
        e.Handled = true;
        base.OnPointerWheelChanged(e);
    }

    // ═══════════════════════════════════════════════════════════════
    // CLAVIER
    // ═══════════════════════════════════════════════════════════════

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var itemRows = _rows.Where(r => !r.IsHeader && r.Item != null).Select(r => r.Item!).ToList();
        if (itemRows.Count == 0)
        {
            base.OnKeyDown(e);
            return;
        }

        var index = _currentItem != null ? itemRows.IndexOf(_currentItem) : -1;
        var handled = true;

        switch (e.Key)
        {
            case Key.Up: MoveTo(Math.Max(0, index - 1)); break;
            case Key.Down: MoveTo(Math.Min(itemRows.Count - 1, index + 1)); break;
            case Key.Home: MoveTo(0); break;
            case Key.End: MoveTo(itemRows.Count - 1); break;
            case Key.PageUp: MoveTo(Math.Max(0, index - (int)(Bounds.Height / _rowHeight))); break;
            case Key.PageDown: MoveTo(Math.Min(itemRows.Count - 1, index + (int)(Bounds.Height / _rowHeight))); break;

            case Key.Enter when _currentItem != null:
                ItemDoubleClicked?.Invoke(this, _currentItem);
                break;

            case Key.A when e.KeyModifiers.HasFlag(KeyModifiers.Control)
                && SelectionMode == SelectionMode.Multiple:
                _selected.Clear();
                _selected.AddRange(itemRows);
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                InvalidateVisual();
                break;

            default: handled = false; break;
        }

        if (handled) e.Handled = true;
        base.OnKeyDown(e);

        void MoveTo(int target)
        {
            if (target < 0 || target >= itemRows.Count) return;
            var item = itemRows[target];

            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && SelectionMode == SelectionMode.Multiple
                && _currentItem != null)
            {
                SelectRangeTo(item);
                _currentItem = item;
            }
            else
            {
                _selected.Clear();
                _selected.Add(item);
                _currentItem = item;
            }

            ScrollItemIntoView(item);
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            InvalidateVisual();
        }
    }

    private void ScrollItemIntoView(object item)
    {
        var index = _rows.FindIndex(r => ReferenceEquals(r.Item, item));
        if (index < 0) return;
        var top = RowTop(index);
        var bottom = top + _rowHeight;

        if (top < _verticalOffset)
            _verticalOffset = top;
        else if (bottom > _verticalOffset + Bounds.Height)
            _verticalOffset = bottom - Bounds.Height;
        UpdateScrollBar();
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════

    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(new SolidColorBrush(ProTheme.Background.Panel), bounds);

        if (_rows.Count == 0)
        {
            var empty = Text(EmptyText, ProTheme.Text.Secondary, 13);
            context.DrawText(empty, Crisp.Snap(new Point(
                (bounds.Width - empty.Width) / 2, (bounds.Height - empty.Height) / 2)));
            return;
        }

        var (first, last) = VisibleRange();
        var width = ContentWidth;

        for (int i = first; i <= last && i < _rows.Count; i++)
        {
            var row = _rows[i];
            var y = RowTop(i) - _verticalOffset;

            if (row.IsHeader)
                RenderHeader(context, row, y, width);
            else if (row.Item != null)
                RenderItem(context, row.Item, i, y, width);
        }
    }

    private void RenderHeader(DrawingContext context, Row row, double y, double width)
    {
        context.FillRectangle(new SolidColorBrush(ProTheme.Background.Toolbar),
            new Rect(0, y, width, HeaderHeight));

        var collapsed = _collapsedGroups.Contains(row.GroupKey!);
        var pen = new Pen(new SolidColorBrush(ProTheme.Text.Secondary), 1.4);
        var cx = 14.0;
        var cy = y + HeaderHeight / 2;
        if (collapsed)
        {
            context.DrawLine(pen, new Point(cx - 1.5, cy - 3.5), new Point(cx + 2, cy));
            context.DrawLine(pen, new Point(cx + 2, cy), new Point(cx - 1.5, cy + 3.5));
        }
        else
        {
            context.DrawLine(pen, new Point(cx - 3.5, cy - 1.5), new Point(cx, cy + 2));
            context.DrawLine(pen, new Point(cx, cy + 2), new Point(cx + 3.5, cy - 1.5));
        }

        var label = Text($"{row.GroupKey}  ({row.GroupCount})", ProTheme.Text.Secondary, 12, FontWeight.SemiBold);
        context.DrawText(label, Crisp.Snap(new Point(26, y + (HeaderHeight - label.Height) / 2)));
    }

    private void RenderItem(DrawingContext context, object item, int rowIndex, double y, double width)
    {
        var content = ItemAdapter?.Invoke(item) ?? new ProListItemContent { Title = item.ToString() ?? "" };
        var isSelected = _selected.Contains(item);
        var isHovered = rowIndex == _hoveredRow;
        var rowRect = new Rect(0, y, width, _rowHeight);

        if (isSelected)
            context.FillRectangle(new SolidColorBrush(ProTheme.WithOpacity(ProTheme.Accent.Primary, 28)), rowRect);
        else if (isHovered)
            context.FillRectangle(new SolidColorBrush(ProTheme.Background.ControlHover), rowRect);

        // Barre accent des non-lus
        if (content.Emphasized)
            context.FillRectangle(new SolidColorBrush(ProTheme.Accent.Primary),
                new Rect(0, y + 4, 3, _rowHeight - 8));

        double x = 12;

        // Pastille icône/initiales
        if (!string.IsNullOrEmpty(content.Icon))
        {
            var circle = new Rect(x, y + (_rowHeight - 32) / 2, 32, 32);
            context.DrawEllipse(new SolidColorBrush(ProTheme.Background.Toolbar),
                new Pen(new SolidColorBrush(ProTheme.Border.Subtle), 1),
                circle.Center, 16, 16);
            var icon = Text(content.Icon, ProTheme.Text.Primary, 13, FontWeight.SemiBold);
            context.DrawText(icon, Crisp.Snap(new Point(
                circle.Center.X - icon.Width / 2, circle.Center.Y - icon.Height / 2)));
            x += 42;
        }

        var showActions = isHovered && HoverActions.Count > 0;
        var reservedRight = showActions
            ? HoverActions.Count * (ActionSize + 4) + 12
            : 8.0;

        // Trailing (masqué quand les actions sont visibles)
        double trailingWidth = 0;
        if (!showActions && !string.IsNullOrEmpty(content.Trailing))
        {
            var trailing = Text(content.Trailing, ProTheme.Text.Secondary, 11);
            trailingWidth = trailing.Width + 12;
            context.DrawText(trailing, Crisp.Snap(new Point(
                width - trailing.Width - 10, y + 8)));
        }

        // Titre (gras si non-lu)
        var titleMax = Math.Max(20, width - x - Math.Max(trailingWidth, reservedRight) - 8);
        var title = Text(content.Title,
            ProTheme.Text.Primary, 14,
            content.Emphasized ? FontWeight.SemiBold : FontWeight.Regular);
        title.MaxTextWidth = titleMax;
        title.Trimming = TextTrimming.CharacterEllipsis;
        title.MaxLineCount = 1;
        context.DrawText(title, Crisp.Snap(new Point(x, y + 7)));

        // Aperçu
        if (!string.IsNullOrEmpty(content.Subtitle) && _rowHeight >= 44)
        {
            var subtitle = Text(content.Subtitle, ProTheme.Text.Secondary, 12);
            subtitle.MaxTextWidth = Math.Max(20, width - x - reservedRight - 8);
            subtitle.Trimming = TextTrimming.CharacterEllipsis;
            subtitle.MaxLineCount = 1;
            context.DrawText(subtitle, Crisp.Snap(new Point(x, y + 28)));
        }

        // Badge (sous le trailing)
        if (!showActions && !string.IsNullOrEmpty(content.Badge))
        {
            var badge = Text(content.Badge, Colors.White, 10, FontWeight.SemiBold);
            var badgeWidth = Math.Max(16, badge.Width + 10);
            var badgeRect = new Rect(width - badgeWidth - 10, y + _rowHeight - 22, badgeWidth, 15);
            context.DrawRectangle(new SolidColorBrush(ProTheme.Accent.Primary), null, badgeRect, 7.5, 7.5);
            context.DrawText(badge, Crisp.Snap(new Point(
                badgeRect.Center.X - badge.Width / 2, badgeRect.Center.Y - badge.Height / 2)));
        }

        // Actions au survol
        if (showActions)
        {
            var rects = ActionRects(rowIndex);
            for (int a = 0; a < HoverActions.Count; a++)
            {
                var rect = rects[a];
                if (a == _hoveredAction)
                    context.DrawEllipse(new SolidColorBrush(ProTheme.Background.ControlPressed),
                        null, rect.Center, ActionSize / 2, ActionSize / 2);

                var glyph = Text(HoverActions[a].Glyph, ProTheme.Text.Primary, 13);
                context.DrawText(glyph, Crisp.Snap(new Point(
                    rect.Center.X - glyph.Width / 2, rect.Center.Y - glyph.Height / 2)));
            }
        }

        // Séparateur
        context.DrawLine(new Pen(new SolidColorBrush(ProTheme.WithOpacity(ProTheme.Border.Subtle, 110)), 1),
            new Point(x, y + _rowHeight - 0.5), new Point(width, y + _rowHeight - 0.5));
    }

    private static FormattedText Text(string text, Color color, double size,
        FontWeight weight = FontWeight.Regular)
        => new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(ProTheme.Typography.FontFamily, weight: weight), size,
            new SolidColorBrush(color));
}
