using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using ProControls.Theme;
using System.Globalization;

namespace ProControls.Controls;

/// <summary>
/// Diagramme de Gantt de gestion de projet (phase 1 : affichage) :
/// table des tâches en arbre à gauche, timeline à droite — barres avec
/// avancement, récapitulatives en crochets, jalons en losanges, flèches de
/// dépendances, ombrage des jours chômés, ligne « aujourd'hui ».
/// Virtualisé lignes × fenêtre temporelle (conçu pour &gt; 2 000 tâches).
/// </summary>
public class ProGantt : Control
{
    private const double HeaderHeight = 44;   // 2 niveaux de graduations
    private const double RowHeight = 28;
    private const double SplitterWidth = 5;
    private const double BarHeight = 14;
    private const double ScrollBarSize = 14;

    private GanttProject? _project;
    private double _tableWidth = 340;
    private double _verticalOffset;
    private bool _draggingSplitter;
    private bool _syncingScrollbars;

    private GanttTask? _selectedTask;
    private GanttTask? _hoveredTask;

    // Interactions (phase 2b)
    private enum DragKind { None, Move, ResizeStart, ResizeEnd, Progress, Link }

    private DragKind _dragKind;
    private GanttTask? _dragTask;
    private Point _dragOrigin;
    private DateTime _origStart, _origEnd;
    private DateTime _ghostStart, _ghostEnd;
    private double _ghostProgress;
    private Point _linkCursor;
    private GanttTask? _linkTarget;
    private bool _linkTargetValid;

    private List<(GanttTask Task, int Depth)> _visibleRows = new();
    private readonly Dictionary<GanttTask, int> _rowIndexByTask = new();

    private readonly ScrollBar _vScroll;
    private readonly ScrollBar _hScroll;

    /// <summary>Axe temporel (zoom, origine, conversion temps↔pixels)</summary>
    public GanttTimeAxis Axis { get; } = new();

    private bool _highlightCriticalPath;

    /// <summary>Surligne le chemin critique (barres et jalons en rouge)</summary>
    public bool HighlightCriticalPath
    {
        get => _highlightCriticalPath;
        set
        {
            if (_highlightCriticalPath == value) return;
            _highlightCriticalPath = value;
            InvalidateVisual();
        }
    }

    /// <summary>Déclenché quand la sélection change</summary>
    public event EventHandler? SelectedTaskChanged;

    /// <summary>Déclenché au double-clic sur une tâche</summary>
    public event EventHandler<GanttTask>? TaskDoubleClicked;

    /// <summary>Interactions souris désactivées (affichage seul)</summary>
    public bool IsReadOnly { get; set; }

    /// <summary>Avant application d'un déplacement/redimensionnement (annulable)</summary>
    public event EventHandler<GanttTaskChangeEventArgs>? TaskDatesChanging;

    /// <summary>Après application d'un déplacement/redimensionnement</summary>
    public event EventHandler<GanttTask>? TaskDatesChanged;

    /// <summary>Avant application d'un changement d'avancement (annulable)</summary>
    public event EventHandler<GanttProgressChangeEventArgs>? ProgressChanging;

    /// <summary>Après application d'un changement d'avancement</summary>
    public event EventHandler<GanttTask>? ProgressChanged;

    /// <summary>Avant création d'un lien à la souris (annulable)</summary>
    public event EventHandler<GanttLinkEventArgs>? LinkCreating;

    /// <summary>Après création d'un lien à la souris</summary>
    public event EventHandler<GanttLinkEventArgs>? LinkCreated;

    public GanttProject? Project
    {
        get => _project;
        set
        {
            if (ReferenceEquals(_project, value)) return;

            if (_project != null)
            {
                _project.DataChanged -= OnProjectDataChanged;
                _project.VisualChanged -= OnProjectVisualChanged;
            }

            _project = value;

            if (_project != null)
            {
                _project.DataChanged += OnProjectDataChanged;
                _project.VisualChanged += OnProjectVisualChanged;
                _project.Recalculate();

                var (start, _) = _project.GetBounds();
                Axis.Origin = start.AddDays(-3);
                Axis.ViewOffset = 0;
            }

            RebuildRows();
        }
    }

    public GanttTask? SelectedTask
    {
        get => _selectedTask;
        set
        {
            if (ReferenceEquals(_selectedTask, value)) return;
            _selectedTask = value;
            InvalidateVisual();
            SelectedTaskChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    static ProGantt()
    {
        FocusableProperty.OverrideDefaultValue<ProGantt>(true);
    }

    public ProGantt()
    {
        ClipToBounds = true;

        _vScroll = new ScrollBar
        {
            Orientation = Avalonia.Layout.Orientation.Vertical,
            Width = ScrollBarSize,
            AllowAutoHide = false
        };
        _vScroll.Scroll += (s, e) =>
        {
            if (_syncingScrollbars) return;
            _verticalOffset = _vScroll.Value;
            InvalidateVisual();
        };

        _hScroll = new ScrollBar
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Height = ScrollBarSize,
            AllowAutoHide = false
        };
        _hScroll.Scroll += (s, e) =>
        {
            if (_syncingScrollbars) return;
            Axis.ViewOffset = _hScroll.Value;
            InvalidateVisual();
        };

        VisualChildren.Add(_vScroll);
        VisualChildren.Add(_hScroll);
        LogicalChildren.Add(_vScroll);
        LogicalChildren.Add(_hScroll);
    }

    // ═══════════════════════════════════════════════════════════════
    // LIGNES VISIBLES (virtualisation verticale)
    // ═══════════════════════════════════════════════════════════════

    private void OnProjectDataChanged(object? sender, EventArgs e) => RebuildRows();
    private void OnProjectVisualChanged(object? sender, EventArgs e) => RebuildRows();

    private void RebuildRows()
    {
        _visibleRows = new List<(GanttTask, int)>();
        _rowIndexByTask.Clear();

        if (_project != null)
            Flatten(_project.Tasks, 0);

        UpdateScrollBars();
        InvalidateVisual();
    }

    private void Flatten(IEnumerable<GanttTask> tasks, int depth)
    {
        foreach (var task in tasks)
        {
            _rowIndexByTask[task] = _visibleRows.Count;
            _visibleRows.Add((task, depth));
            if (task.IsExpanded && task.IsSummary)
                Flatten(task.Children, depth + 1);
        }
    }

    private Rect RowsViewport => new(
        0, HeaderHeight,
        Math.Max(0, Bounds.Width - ScrollBarSize),
        Math.Max(0, Bounds.Height - HeaderHeight - ScrollBarSize));

    private double TimelineX => _tableWidth + SplitterWidth;
    private double TimelineWidth => Math.Max(0, Bounds.Width - ScrollBarSize - TimelineX);

    private (int First, int Last) VisibleRowRange()
    {
        var viewport = RowsViewport;
        var first = Math.Max(0, (int)(_verticalOffset / RowHeight));
        var last = Math.Min(_visibleRows.Count - 1,
            (int)((_verticalOffset + viewport.Height) / RowHeight) + 1);
        return (first, last);
    }

    private double RowY(int index) => HeaderHeight + index * RowHeight - _verticalOffset;

    // ═══════════════════════════════════════════════════════════════
    // SCROLLBARS
    // ═══════════════════════════════════════════════════════════════

    private void UpdateScrollBars()
    {
        _syncingScrollbars = true;

        var viewport = RowsViewport;
        var totalHeight = _visibleRows.Count * RowHeight;
        _vScroll.Minimum = 0;
        _vScroll.Maximum = Math.Max(0, totalHeight - viewport.Height);
        _vScroll.ViewportSize = viewport.Height;
        _verticalOffset = Math.Clamp(_verticalOffset, 0, Math.Max(0, _vScroll.Maximum));
        _vScroll.Value = _verticalOffset;

        if (_project != null)
        {
            var (start, end) = _project.GetBounds();
            var totalPixels = (end.AddDays(14) - Axis.Origin).TotalDays * Axis.PixelsPerDay;
            _hScroll.Minimum = Math.Min(0, (start.AddDays(-14) - Axis.Origin).TotalDays * Axis.PixelsPerDay);
            _hScroll.Maximum = Math.Max(0, totalPixels - TimelineWidth);
            _hScroll.ViewportSize = TimelineWidth;
            _hScroll.Value = Math.Clamp(Axis.ViewOffset, _hScroll.Minimum, Math.Max(_hScroll.Minimum, _hScroll.Maximum));
        }

        _syncingScrollbars = false;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _vScroll.Measure(finalSize);
        _hScroll.Measure(finalSize);
        _vScroll.Arrange(new Rect(
            finalSize.Width - ScrollBarSize, HeaderHeight,
            ScrollBarSize, Math.Max(0, finalSize.Height - HeaderHeight - ScrollBarSize)));
        _hScroll.Arrange(new Rect(
            TimelineX, finalSize.Height - ScrollBarSize,
            Math.Max(0, finalSize.Width - ScrollBarSize - TimelineX), ScrollBarSize));

        UpdateScrollBars();
        return finalSize;
    }

    protected override Size MeasureOverride(Size availableSize)
        => new(
            double.IsInfinity(availableSize.Width) ? 800 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 400 : availableSize.Height);

    // ═══════════════════════════════════════════════════════════════
    // API PUBLIQUE
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Amène « aujourd'hui » au tiers gauche de la timeline</summary>
    public void ScrollToToday()
    {
        Axis.ViewOffset = (DateTime.Today - Axis.Origin).TotalDays * Axis.PixelsPerDay
            - TimelineWidth / 3;
        UpdateScrollBars();
        InvalidateVisual();
    }

    /// <summary>Rend une tâche visible (scroll vertical + horizontal)</summary>
    public void ScrollToTask(GanttTask task)
    {
        if (!_rowIndexByTask.TryGetValue(task, out var index)) return;

        var viewport = RowsViewport;
        var y = index * RowHeight;
        if (y < _verticalOffset || y + RowHeight > _verticalOffset + viewport.Height)
            _verticalOffset = Math.Max(0, y - viewport.Height / 2);

        Axis.ViewOffset = (task.EffectiveStart - Axis.Origin).TotalDays * Axis.PixelsPerDay
            - TimelineWidth / 4;

        UpdateScrollBars();
        InvalidateVisual();
    }

    // ═══════════════════════════════════════════════════════════════
    // SOURIS
    // ═══════════════════════════════════════════════════════════════

    private bool IsOnSplitter(Point pos)
        => pos.X >= _tableWidth && pos.X < _tableWidth + SplitterWidth && pos.Y > HeaderHeight;

    private (GanttTask Task, int Depth, int Index)? RowAt(Point pos)
    {
        if (pos.Y < HeaderHeight || pos.Y > RowsViewport.Bottom) return null;
        var index = (int)((pos.Y - HeaderHeight + _verticalOffset) / RowHeight);
        if (index < 0 || index >= _visibleRows.Count) return null;
        var (task, depth) = _visibleRows[index];
        return (task, depth, index);
    }

    private static Rect ChevronRect(int depth, double rowY)
        => new(6 + depth * 16, rowY + (RowHeight - 14) / 2, 14, 14);

    /// <summary>Bornes X (pixels vue) de la barre d'une tâche</summary>
    private (double X1, double X2) GetBarX(GanttTask task)
        => (TimelineX + Axis.ToX(task.EffectiveStart),
            TimelineX + Axis.ToX(task.EffectiveEnd.AddDays(1)));

    /// <summary>
    /// Zone chaude sous le curseur dans la timeline : bords = resize,
    /// milieu = déplacement, connecteur = lien, grip = avancement
    /// </summary>
    private (GanttTask Task, DragKind Zone)? BarHitTest(Point pos)
    {
        if (pos.X < TimelineX) return null;
        var row = RowAt(pos);
        if (row == null) return null;

        var (task, _, _) = row.Value;
        if (task.IsSummary) return null; // récapitulatives : dates par roll-up

        var (x1, x2) = GetBarX(task);
        var centerY = RowY(row.Value.Index) + RowHeight / 2;

        if (task.IsMilestone)
        {
            var mx = x1 + Axis.PixelsPerDay / 2;
            if (Math.Abs(pos.X - mx) <= 9 && Math.Abs(pos.Y - centerY) <= 9)
                return (task, DragKind.Move);
            return null;
        }

        var inBarBand = Math.Abs(pos.Y - centerY) <= BarHeight / 2 + 3;

        // Connecteur de lien (cercle à droite de la barre, tâche survolée)
        if (ReferenceEquals(task, _hoveredTask) &&
            Math.Abs(pos.X - (x2 + 10)) <= 6 && Math.Abs(pos.Y - centerY) <= 6)
            return (task, DragKind.Link);

        // Grip d'avancement (sous la barre)
        var progressX = x1 + (x2 - x1) * task.EffectiveProgress / 100;
        if (ReferenceEquals(task, _hoveredTask) &&
            pos.Y > centerY + BarHeight / 2 && pos.Y <= centerY + BarHeight / 2 + 8 &&
            Math.Abs(pos.X - progressX) <= 6)
            return (task, DragKind.Progress);

        if (!inBarBand) return null;

        if (Math.Abs(pos.X - x1) <= 5) return (task, DragKind.ResizeStart);
        if (Math.Abs(pos.X - x2) <= 5) return (task, DragKind.ResizeEnd);
        if (pos.X > x1 && pos.X < x2) return (task, DragKind.Move);

        return null;
    }

    /// <summary>Cale une date de début sur un jour ouvré (vers l'avant)</summary>
    private DateTime SnapStart(DateTime date)
        => _project?.Calendar.AddWorkingDays(date, 0) ?? date;

    /// <summary>Cale une date de fin sur un jour ouvré (vers l'arrière)</summary>
    private DateTime SnapEnd(DateTime date)
    {
        var calendar = _project?.Calendar;
        if (calendar == null) return date;
        var d = date.Date;
        while (!calendar.IsWorkingDay(d))
            d = d.AddDays(-1);
        return d;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var pos = e.GetPosition(this);

        if (_draggingSplitter)
        {
            _tableWidth = Math.Clamp(pos.X, 140, Math.Max(140, Bounds.Width - 200));
            UpdateScrollBars();
            InvalidateVisual();
            return;
        }

        // Drag en cours : mettre à jour le fantôme
        if (_dragKind != DragKind.None && _dragTask != null && _project != null)
        {
            UpdateDragGhost(pos);
            InvalidateVisual();
            return;
        }

        // Curseur selon la zone chaude
        var hit = IsReadOnly ? null : BarHitTest(pos);
        Cursor = IsOnSplitter(pos) ? new Cursor(StandardCursorType.SizeWestEast)
            : hit?.Zone switch
            {
                DragKind.ResizeStart or DragKind.ResizeEnd or DragKind.Progress
                    => new Cursor(StandardCursorType.SizeWestEast),
                DragKind.Move => new Cursor(StandardCursorType.Hand),
                DragKind.Link => new Cursor(StandardCursorType.Cross),
                _ => Cursor.Default
            };

        var row = RowAt(pos);
        var task = row?.Task;
        if (!ReferenceEquals(task, _hoveredTask))
        {
            _hoveredTask = task;
            ToolTip.SetTip(this, task == null ? null : BuildTooltip(task));
            InvalidateVisual();
        }

        base.OnPointerMoved(e);
    }

    private void UpdateDragGhost(Point pos)
    {
        var calendar = _project!.Calendar;

        switch (_dragKind)
        {
            case DragKind.Move:
            {
                var deltaDays = (int)Math.Round((pos.X - _dragOrigin.X) / Axis.PixelsPerDay);
                var start = _origStart.AddDays(deltaDays);

                if (_dragTask!.IsMilestone)
                {
                    _ghostStart = _ghostEnd = start;
                }
                else
                {
                    // Durée OUVRÉE préservée, début calé sur un jour ouvré
                    var duration = Math.Max(1, calendar.CountWorkingDays(_origStart, _origEnd));
                    _ghostStart = SnapStart(start);
                    _ghostEnd = calendar.AddWorkingDays(_ghostStart, duration - 1);
                }
                break;
            }

            case DragKind.ResizeStart:
            {
                var date = SnapStart(Axis.ToDate(pos.X - TimelineX).Date);
                _ghostStart = date > _ghostEnd ? _ghostEnd : date;
                break;
            }

            case DragKind.ResizeEnd:
            {
                var date = SnapEnd(Axis.ToDate(pos.X - TimelineX).Date);
                _ghostEnd = date < _ghostStart ? _ghostStart : date;
                break;
            }

            case DragKind.Progress:
            {
                var (x1, x2) = GetBarX(_dragTask!);
                var raw = (pos.X - x1) / Math.Max(1, x2 - x1) * 100;
                _ghostProgress = Math.Clamp(Math.Round(raw / 5) * 5, 0, 100);
                break;
            }

            case DragKind.Link:
            {
                _linkCursor = pos;
                var row = RowAt(pos);
                var target = row?.Task;
                _linkTarget = target != null && !target.IsSummary && !ReferenceEquals(target, _dragTask)
                    ? target : null;
                _linkTargetValid = _linkTarget != null
                    && !_linkTarget.Predecessors.Any(d => ReferenceEquals(d.Predecessor, _dragTask))
                    && !GanttScheduler.WouldCreateCycle(_dragTask!, _linkTarget);
                break;
            }
        }
    }

    private string BuildTooltip(GanttTask task)
    {
        var calendar = _project?.Calendar ?? new GanttCalendar();
        var duration = calendar.CountWorkingDays(task.EffectiveStart, task.EffectiveEnd);
        return task.IsMilestone
            ? $"{task.Name}\n{task.EffectiveStart:d} (jalon)"
            : $"{task.Name}\n{task.EffectiveStart:d} → {task.EffectiveEnd:d} · {duration} j ouvrés · {task.EffectiveProgress:F0} %";
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _hoveredTask = null;
        ToolTip.SetTip(this, null);
        InvalidateVisual();
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var pos = e.GetPosition(this);
        Focus();

        if (IsOnSplitter(pos))
        {
            _draggingSplitter = true;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        var row = RowAt(pos);
        if (row == null)
        {
            base.OnPointerPressed(e);
            return;
        }

        var (task, depth, index) = row.Value;

        // Chevron (dans la table) : toggle sans changer la sélection
        if (pos.X < _tableWidth && task.IsSummary &&
            ChevronRect(depth, RowY(index)).Inflate(2).Contains(pos))
        {
            task.IsExpanded = !task.IsExpanded;
            e.Handled = true;
            return;
        }

        // Zone chaude d'une barre : démarrer un drag (déplacement, resize,
        // avancement, lien) — le fantôme suit, la mutation attend le relâcher
        if (!IsReadOnly && _project != null)
        {
            var hit = BarHitTest(pos);
            if (hit != null)
            {
                (_dragTask, _dragKind) = hit.Value;
                _dragOrigin = pos;
                _origStart = _dragTask.EffectiveStart;
                _origEnd = _dragTask.EffectiveEnd;
                _ghostStart = _origStart;
                _ghostEnd = _origEnd;
                _ghostProgress = _dragTask.EffectiveProgress;
                _linkCursor = pos;
                _linkTarget = null;
                _linkTargetValid = false;

                SelectedTask = _dragTask;
                e.Pointer.Capture(this);
                e.Handled = true;
                InvalidateVisual();
                return;
            }
        }

        SelectedTask = task;

        if (e.ClickCount == 2)
            TaskDoubleClicked?.Invoke(this, task);

        e.Handled = true;
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_draggingSplitter)
        {
            _draggingSplitter = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
        else if (_dragKind != DragKind.None)
        {
            CommitDrag();
            e.Pointer.Capture(null);
            e.Handled = true;
        }
        base.OnPointerReleased(e);
    }

    private void CommitDrag()
    {
        var task = _dragTask;
        var kind = _dragKind;
        _dragKind = DragKind.None;
        _dragTask = null;

        if (task == null || _project == null)
        {
            InvalidateVisual();
            return;
        }

        switch (kind)
        {
            case DragKind.Move:
            case DragKind.ResizeStart:
            case DragKind.ResizeEnd:
                if (_ghostStart != _origStart || _ghostEnd != _origEnd)
                {
                    var args = new GanttTaskChangeEventArgs(task, _ghostStart, _ghostEnd);
                    TaskDatesChanging?.Invoke(this, args);
                    if (!args.Cancel)
                    {
                        task.SetDatesSilent(args.NewStart, args.NewEnd);
                        _project.NotifyDataChange();
                        TaskDatesChanged?.Invoke(this, task);
                    }
                }
                break;

            case DragKind.Progress:
                if (Math.Abs(_ghostProgress - task.Progress) > 0.01)
                {
                    var args = new GanttProgressChangeEventArgs(task, _ghostProgress);
                    ProgressChanging?.Invoke(this, args);
                    if (!args.Cancel)
                    {
                        task.Progress = args.NewProgress;
                        ProgressChanged?.Invoke(this, task);
                    }
                }
                break;

            case DragKind.Link:
                if (_linkTargetValid && _linkTarget != null)
                {
                    var args = new GanttLinkEventArgs(task, _linkTarget);
                    LinkCreating?.Invoke(this, args);
                    if (!args.Cancel)
                    {
                        _linkTarget.DependsOn(task);
                        LinkCreated?.Invoke(this, args);
                    }
                }
                break;
        }

        _linkTarget = null;
        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        var pos = e.GetPosition(this);

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            // Zoom centré sur le curseur (dans la timeline)
            var pivot = Math.Max(0, pos.X - TimelineX);
            Axis.ZoomAt(pivot, e.Delta.Y > 0 ? 1.25 : 0.8);
        }
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            Axis.ViewOffset += (e.Delta.Y > 0 ? -1 : 1) * 80;
        }
        else
        {
            _verticalOffset = Math.Clamp(
                _verticalOffset + (e.Delta.Y > 0 ? -3 : 3) * RowHeight,
                0, Math.Max(0, _visibleRows.Count * RowHeight - RowsViewport.Height));
        }

        UpdateScrollBars();
        InvalidateVisual();
        e.Handled = true;
        base.OnPointerWheelChanged(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Échap annule le drag en cours (le fantôme disparaît, rien n'est appliqué)
        if (e.Key == Key.Escape && _dragKind != DragKind.None)
        {
            _dragKind = DragKind.None;
            _dragTask = null;
            _linkTarget = null;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_visibleRows.Count == 0)
        {
            base.OnKeyDown(e);
            return;
        }

        var index = _selectedTask != null && _rowIndexByTask.TryGetValue(_selectedTask, out var i)
            ? i : -1;

        switch (e.Key)
        {
            case Key.Up:
                SelectRow(Math.Max(0, index - 1));
                e.Handled = true;
                break;
            case Key.Down:
                SelectRow(Math.Min(_visibleRows.Count - 1, index + 1));
                e.Handled = true;
                break;
            case Key.Right when _selectedTask is { IsSummary: true, IsExpanded: false }:
                _selectedTask.IsExpanded = true;
                e.Handled = true;
                break;
            case Key.Left when _selectedTask is { IsSummary: true, IsExpanded: true }:
                _selectedTask.IsExpanded = false;
                e.Handled = true;
                break;
            case Key.Left when _selectedTask?.Parent != null:
                SelectedTask = _selectedTask.Parent;
                e.Handled = true;
                break;
        }

        base.OnKeyDown(e);
    }

    private void SelectRow(int index)
    {
        if (index < 0 || index >= _visibleRows.Count) return;
        SelectedTask = _visibleRows[index].Task;

        // Garder la ligne visible
        var y = index * RowHeight;
        var viewport = RowsViewport;
        if (y < _verticalOffset)
            _verticalOffset = y;
        else if (y + RowHeight > _verticalOffset + viewport.Height)
            _verticalOffset = y + RowHeight - viewport.Height;
        UpdateScrollBars();
        InvalidateVisual();
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(new SolidColorBrush(ProTheme.Background.Panel), bounds);

        if (_project == null || _visibleRows.Count == 0)
        {
            var empty = CreateText("Aucun projet", ProTheme.Text.Secondary, 13);
            context.DrawText(empty, Crisp.Snap(new Point(
                (bounds.Width - empty.Width) / 2, (bounds.Height - empty.Height) / 2)));
            return;
        }

        var (first, last) = VisibleRowRange();

        RenderTimelineBackground(context);
        RenderRowsBackground(context, first, last);
        RenderDependencyArrows(context, first, last);
        RenderBars(context, first, last);
        RenderNowLine(context);
        RenderDragOverlay(context);
        RenderTable(context, first, last);
        RenderHeader(context);
        RenderSplitter(context);
    }

    /// <summary>
    /// Overlay d'interaction : fantôme des dates pendant déplacement/resize,
    /// ligne élastique pendant la création de lien, grip d'avancement
    /// </summary>
    private void RenderDragOverlay(DrawingContext context)
    {
        var timelineRect = new Rect(TimelineX, HeaderHeight, TimelineWidth, RowsViewport.Height);

        using (context.PushClip(timelineRect))
        {
            // Affordances de la barre survolée (hors drag)
            if (_dragKind == DragKind.None && !IsReadOnly &&
                _hoveredTask is { IsSummary: false } hovered &&
                _rowIndexByTask.TryGetValue(hovered, out var hoveredIndex))
            {
                var (x1, x2) = GetBarX(hovered);
                var centerY = RowY(hoveredIndex) + RowHeight / 2;

                if (!hovered.IsMilestone)
                {
                    // Connecteur de lien (cercle à droite)
                    context.DrawEllipse(new SolidColorBrush(ProTheme.Background.Panel),
                        new Pen(new SolidColorBrush(ProTheme.Accent.Primary), 1.4),
                        new Point(x2 + 10, centerY), 4.5, 4.5);

                    // Grip d'avancement (triangle sous la barre)
                    var progressX = x1 + (x2 - x1) * hovered.EffectiveProgress / 100;
                    var gripY = centerY + BarHeight / 2 + 1;
                    var grip = new StreamGeometry();
                    using (var g = grip.Open())
                    {
                        g.BeginFigure(new Point(progressX, gripY), true);
                        g.LineTo(new Point(progressX - 4, gripY + 6));
                        g.LineTo(new Point(progressX + 4, gripY + 6));
                        g.EndFigure(true);
                    }
                    context.DrawGeometry(new SolidColorBrush(ProTheme.Accent.Primary), null, grip);
                }
            }

            if (_dragTask == null) return;

            // Fantôme déplacement/resize + étiquette de dates
            if (_dragKind is DragKind.Move or DragKind.ResizeStart or DragKind.ResizeEnd
                && _rowIndexByTask.TryGetValue(_dragTask, out var dragIndex))
            {
                var y = RowY(dragIndex) + RowHeight / 2;
                var gx1 = TimelineX + Axis.ToX(_ghostStart);
                var gx2 = TimelineX + Axis.ToX(_ghostEnd.AddDays(1));

                var ghostPen = new Pen(new SolidColorBrush(ProTheme.Accent.Primary), 1.5)
                {
                    DashStyle = new DashStyle(new double[] { 3, 3 }, 0)
                };

                if (_dragTask.IsMilestone)
                {
                    var mx = gx1 + Axis.PixelsPerDay / 2;
                    context.DrawEllipse(null, ghostPen, new Point(mx, y), 8, 8);
                }
                else
                {
                    context.DrawRectangle(
                        new SolidColorBrush(ProTheme.WithOpacity(ProTheme.Accent.Primary, 30)),
                        ghostPen,
                        new Rect(gx1, y - BarHeight / 2, Math.Max(2, gx2 - gx1), BarHeight), 3, 3);
                }

                var label = _dragTask.IsMilestone
                    ? _ghostStart.ToString("dd/MM")
                    : $"{_ghostStart:dd/MM} → {_ghostEnd:dd/MM}";
                var text = CreateText(label, ProTheme.Text.Primary, 12);
                var labelPos = new Point(gx1, y - BarHeight / 2 - text.Height - 4);
                context.FillRectangle(new SolidColorBrush(ProTheme.Background.Panel),
                    new Rect(labelPos.X - 3, labelPos.Y - 1, text.Width + 6, text.Height + 2), 3);
                context.DrawText(text, Crisp.Snap(labelPos));
            }

            // Poignée d'avancement en cours de drag
            if (_dragKind == DragKind.Progress
                && _rowIndexByTask.TryGetValue(_dragTask, out var progressIndex))
            {
                var (x1, x2) = GetBarX(_dragTask);
                var y = RowY(progressIndex) + RowHeight / 2;
                var px = x1 + (x2 - x1) * _ghostProgress / 100;

                context.DrawLine(new Pen(new SolidColorBrush(ProTheme.Accent.Primary), 2),
                    new Point(px, y - BarHeight / 2 - 3), new Point(px, y + BarHeight / 2 + 3));

                var text = CreateText($"{_ghostProgress:F0} %", ProTheme.Text.Primary, 12);
                context.DrawText(text, Crisp.Snap(new Point(px + 6, y - BarHeight / 2 - text.Height - 2)));
            }

            // Ligne élastique de création de lien
            if (_dragKind == DragKind.Link)
            {
                var (_, x2) = GetBarX(_dragTask);
                if (_rowIndexByTask.TryGetValue(_dragTask, out var linkIndex))
                {
                    var fromY = RowY(linkIndex) + RowHeight / 2;
                    var color = _linkTarget == null
                        ? ProTheme.Text.Secondary
                        : _linkTargetValid ? ProTheme.Accent.Success : ProTheme.Accent.Error;

                    context.DrawLine(
                        new Pen(new SolidColorBrush(color), 1.5)
                        {
                            DashStyle = new DashStyle(new double[] { 4, 3 }, 0)
                        },
                        new Point(x2 + 10, fromY), _linkCursor);

                    // Surligner la cible
                    if (_linkTarget != null &&
                        _rowIndexByTask.TryGetValue(_linkTarget, out var targetIndex))
                    {
                        var (tx1, tx2) = GetBarX(_linkTarget);
                        var ty = RowY(targetIndex) + RowHeight / 2;
                        context.DrawRectangle(null, new Pen(new SolidColorBrush(color), 2),
                            new Rect(tx1 - 2, ty - BarHeight / 2 - 2,
                                Math.Max(4, tx2 - tx1) + 4, BarHeight + 4), 4, 4);
                    }
                }
            }
        }
    }

    private void RenderHeader(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(new SolidColorBrush(ProTheme.Background.Toolbar),
            new Rect(0, 0, bounds.Width, HeaderHeight));

        // Colonnes de la table
        var captions = GetTableColumns();
        double x = 0;
        foreach (var (caption, width) in captions)
        {
            var text = CreateText(caption, ProTheme.Text.Secondary, 12);
            context.DrawText(text, Crisp.Snap(new Point(x + 8, HeaderHeight - text.Height - 4)));
            x += width;
        }

        // Graduations timeline (clippées à la zone timeline)
        using (context.PushClip(new Rect(TimelineX, 0, TimelineWidth, HeaderHeight)))
        {
            var tickPen = new Pen(new SolidColorBrush(ProTheme.Border.Subtle), 1);

            foreach (var tick in Axis.MajorTicks(TimelineWidth))
            {
                var tx = TimelineX + tick.X;
                context.DrawLine(tickPen, new Point(tx, 2), new Point(tx, HeaderHeight));
                var text = CreateText(tick.Label, ProTheme.Text.Primary, 12);
                context.DrawText(text, Crisp.Snap(new Point(tx + 4, 4)));
            }

            foreach (var tick in Axis.MinorTicks(TimelineWidth))
            {
                var tx = TimelineX + tick.X;
                context.DrawLine(tickPen, new Point(tx, 24), new Point(tx, HeaderHeight));
                var text = CreateText(tick.Label, ProTheme.Text.Secondary, 10);
                context.DrawText(text, Crisp.Snap(new Point(tx + 3, 26)));
            }
        }

        context.DrawLine(new Pen(new SolidColorBrush(ProTheme.Border.Default), 1),
            new Point(0, HeaderHeight - 0.5), new Point(bounds.Width, HeaderHeight - 0.5));
    }

    private (string Caption, double Width)[] GetTableColumns()
    {
        var nameWidth = Math.Max(60, _tableWidth - 90 - 90 - 44);
        return new[]
        {
            ("Tâche", nameWidth),
            ("Début", 90.0),
            ("Fin", 90.0),
            ("%", 44.0)
        };
    }

    private void RenderTimelineBackground(DrawingContext context)
    {
        var viewport = RowsViewport;
        var timelineRect = new Rect(TimelineX, HeaderHeight, TimelineWidth, viewport.Height);

        using (context.PushClip(timelineRect))
        {
            // Ombrage des jours chômés (seulement quand les jours sont discernables)
            if (Axis.PixelsPerDay >= 6 && _project != null)
            {
                var shade = new SolidColorBrush(ProTheme.WithOpacity(ProTheme.Text.Secondary, 14));
                var (start, end) = Axis.VisibleRange(TimelineWidth);
                for (var d = start; d <= end; d = d.AddDays(1))
                {
                    if (_project.Calendar.IsWorkingDay(d)) continue;
                    context.FillRectangle(shade, new Rect(
                        TimelineX + Axis.ToX(d), HeaderHeight,
                        Axis.PixelsPerDay, viewport.Height));
                }
            }

            // Lignes verticales des graduations
            var gridPen = new Pen(new SolidColorBrush(ProTheme.WithOpacity(ProTheme.Border.Subtle, 120)), 1);
            foreach (var tick in Axis.MinorTicks(TimelineWidth))
            {
                var tx = TimelineX + tick.X;
                context.DrawLine(gridPen, new Point(tx, HeaderHeight), new Point(tx, timelineRect.Bottom));
            }
        }
    }

    private void RenderRowsBackground(DrawingContext context, int first, int last)
    {
        var width = Bounds.Width - ScrollBarSize;
        var rowPen = new Pen(new SolidColorBrush(ProTheme.WithOpacity(ProTheme.Border.Subtle, 90)), 1);

        for (int i = first; i <= last; i++)
        {
            var y = RowY(i);
            var task = _visibleRows[i].Task;
            var rowRect = new Rect(0, y, width, RowHeight);

            if (ReferenceEquals(task, _selectedTask))
            {
                context.FillRectangle(new SolidColorBrush(
                    ProTheme.WithOpacity(ProTheme.Accent.Primary, 26)), rowRect);
            }
            else if (ReferenceEquals(task, _hoveredTask))
            {
                context.FillRectangle(new SolidColorBrush(ProTheme.Background.ControlHover), rowRect);
            }

            context.DrawLine(rowPen,
                new Point(0, y + RowHeight - 0.5), new Point(width, y + RowHeight - 0.5));
        }
    }

    private void RenderTable(DrawingContext context, int first, int last)
    {
        var columns = GetTableColumns();

        using (context.PushClip(new Rect(0, HeaderHeight, _tableWidth, RowsViewport.Height)))
        {
            for (int i = first; i <= last; i++)
            {
                var (task, depth) = _visibleRows[i];
                var y = RowY(i);
                var isSummary = task.IsSummary;

                // Chevron
                if (isSummary)
                {
                    var chevron = ChevronRect(depth, y);
                    var pen = new Pen(new SolidColorBrush(ProTheme.Text.Secondary), 1.4);
                    var c = chevron.Center;
                    if (task.IsExpanded)
                    {
                        context.DrawLine(pen, new Point(c.X - 3.5, c.Y - 1.5), new Point(c.X, c.Y + 2));
                        context.DrawLine(pen, new Point(c.X, c.Y + 2), new Point(c.X + 3.5, c.Y - 1.5));
                    }
                    else
                    {
                        context.DrawLine(pen, new Point(c.X - 1.5, c.Y - 3.5), new Point(c.X + 2, c.Y));
                        context.DrawLine(pen, new Point(c.X + 2, c.Y), new Point(c.X - 1.5, c.Y + 3.5));
                    }
                }

                // Nom (gras pour les récapitulatives)
                var nameText = new FormattedText(task.Name, CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(ProTheme.Typography.FontFamily,
                        weight: isSummary ? FontWeight.SemiBold : FontWeight.Regular),
                    13, new SolidColorBrush(ProTheme.Text.Primary));

                var nameX = 24 + depth * 16;
                using (context.PushClip(new Rect(0, y, columns[0].Width, RowHeight)))
                {
                    context.DrawText(nameText, Crisp.Snap(new Point(
                        nameX, y + (RowHeight - nameText.Height) / 2)));
                }

                // Début / Fin / %
                double x = columns[0].Width;
                var cells = new[]
                {
                    task.EffectiveStart.ToString("dd/MM/yy"),
                    task.IsMilestone ? "—" : task.EffectiveEnd.ToString("dd/MM/yy"),
                    $"{task.EffectiveProgress:F0}"
                };
                for (int c = 0; c < cells.Length; c++)
                {
                    var cellText = CreateText(cells[c], ProTheme.Text.Secondary, 12);
                    context.DrawText(cellText, Crisp.Snap(new Point(
                        x + 8, y + (RowHeight - cellText.Height) / 2)));
                    x += columns[c + 1].Width;
                }
            }
        }
    }

    private void RenderBars(DrawingContext context, int first, int last)
    {
        var timelineRect = new Rect(TimelineX, HeaderHeight, TimelineWidth, RowsViewport.Height);

        using (context.PushClip(timelineRect))
        {
            for (int i = first; i <= last; i++)
            {
                var task = _visibleRows[i].Task;
                var y = RowY(i);
                var x1 = TimelineX + Axis.ToX(task.EffectiveStart);
                var x2 = TimelineX + Axis.ToX(task.EffectiveEnd.AddDays(1)); // fin incluse

                if (x2 < TimelineX - 50 || x1 > timelineRect.Right + 50)
                    continue; // hors fenêtre temporelle

                var centerY = y + RowHeight / 2;

                var isCritical = _highlightCriticalPath && task.IsCritical;

                if (task.IsMilestone)
                {
                    // Losange
                    var mx = TimelineX + Axis.ToX(task.EffectiveStart) + Axis.PixelsPerDay / 2;
                    var geometry = new StreamGeometry();
                    using (var g = geometry.Open())
                    {
                        g.BeginFigure(new Point(mx, centerY - 7), true);
                        g.LineTo(new Point(mx + 7, centerY));
                        g.LineTo(new Point(mx, centerY + 7));
                        g.LineTo(new Point(mx - 7, centerY));
                        g.EndFigure(true);
                    }
                    context.DrawGeometry(new SolidColorBrush(
                        isCritical ? ProTheme.Accent.Error : ProTheme.Accent.Secondary), null, geometry);
                }
                else if (task.IsSummary)
                {
                    // Crochet récapitulatif (barre fine + pattes)
                    var brush = new SolidColorBrush(
                        isCritical ? ProTheme.Accent.Error : ProTheme.Text.Primary);
                    var barY = centerY - 5;
                    context.FillRectangle(brush, new Rect(x1, barY, Math.Max(2, x2 - x1), 5));
                    var legs = new StreamGeometry();
                    using (var g = legs.Open())
                    {
                        g.BeginFigure(new Point(x1, barY), true);
                        g.LineTo(new Point(x1 + 5, barY));
                        g.LineTo(new Point(x1, barY + 10));
                        g.EndFigure(true);
                        g.BeginFigure(new Point(x2, barY), true);
                        g.LineTo(new Point(x2 - 5, barY));
                        g.LineTo(new Point(x2, barY + 10));
                        g.EndFigure(true);
                    }
                    context.DrawGeometry(brush, null, legs);
                }
                else
                {
                    // Barre de tâche + remplissage d'avancement
                    var barRect = new Rect(x1, centerY - BarHeight / 2,
                        Math.Max(2, x2 - x1), BarHeight);

                    var barColor = isCritical ? ProTheme.Accent.Error : ProTheme.Accent.Primary;
                    context.DrawRectangle(
                        new SolidColorBrush(ProTheme.WithOpacity(barColor, 70)),
                        new Pen(new SolidColorBrush(barColor), 1),
                        barRect, 3, 3);

                    if (task.EffectiveProgress > 0)
                    {
                        var progressWidth = barRect.Width * task.EffectiveProgress / 100;
                        context.DrawRectangle(new SolidColorBrush(barColor), null,
                            new Rect(barRect.X, barRect.Y, progressWidth, barRect.Height), 3, 3);
                    }
                }
            }
        }
    }

    private void RenderDependencyArrows(DrawingContext context, int first, int last)
    {
        var timelineRect = new Rect(TimelineX, HeaderHeight, TimelineWidth, RowsViewport.Height);
        var brush = new SolidColorBrush(ProTheme.WithOpacity(ProTheme.Text.Secondary, 170));
        var pen = new Pen(brush, 1.3) { LineJoin = PenLineJoin.Round };

        using (context.PushClip(timelineRect))
        {
            for (int i = first; i <= last; i++)
            {
                var successor = _visibleRows[i].Task;
                foreach (var dep in successor.Predecessors)
                {
                    // Le prédécesseur peut être hors plage visible : il faut son index réel
                    if (!_rowIndexByTask.TryGetValue(dep.Predecessor, out var predIndex))
                        continue; // masqué (ancêtre replié)

                    var exitFromEnd = dep.Type is GanttDependencyType.FinishToStart
                        or GanttDependencyType.FinishToFinish;
                    var enterAtStart = dep.Type is GanttDependencyType.FinishToStart
                        or GanttDependencyType.StartToStart;

                    var fromX = TimelineX + Axis.ToX(exitFromEnd
                        ? dep.Predecessor.EffectiveEnd.AddDays(1)
                        : dep.Predecessor.EffectiveStart);
                    var toX = TimelineX + Axis.ToX(enterAtStart
                        ? successor.EffectiveStart
                        : successor.EffectiveEnd.AddDays(1));

                    var fromY = RowY(predIndex) + RowHeight / 2;
                    var toY = RowY(i) + RowHeight / 2;

                    DrawDependencyPath(context, pen, brush,
                        fromX, fromY, toX, toY, exitFromEnd, enterAtStart);
                }
            }
        }
    }

    /// <summary>
    /// Routage orthogonal style MS Project : sortie courte de la barre du
    /// prédécesseur, descente verticale immédiate, entrée horizontale dans le
    /// successeur. Détour en S quand le successeur est « à rebours ».
    /// </summary>
    private void DrawDependencyPath(DrawingContext context, Pen pen, IBrush brush,
        double fromX, double fromY, double toX, double toY,
        bool exitRight, bool enterFromLeft)
    {
        const double stub = 9;
        var exitX = fromX + (exitRight ? stub : -stub);
        var points = new List<Point> { new(fromX, fromY), new(exitX, fromY) };

        var directPathClear = enterFromLeft
            ? exitX <= toX - stub   // on peut descendre puis entrer par la gauche
            : exitX >= toX + stub;  // ... ou par la droite

        if (directPathClear)
        {
            // L simple : descente au ras de la barre, puis entrée horizontale
            points.Add(new Point(exitX, toY));
            points.Add(new Point(toX, toY));
        }
        else
        {
            // S : descendre entre les deux lignes, revenir, puis entrer
            var midY = toY > fromY ? toY - RowHeight / 2 : toY + RowHeight / 2;
            var approachX = toX + (enterFromLeft ? -stub : stub);
            points.Add(new Point(exitX, midY));
            points.Add(new Point(approachX, midY));
            points.Add(new Point(approachX, toY));
            points.Add(new Point(toX, toY));
        }

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(points[0], false);
            for (int i = 1; i < points.Count; i++)
                g.LineTo(points[i]);
            g.EndFigure(false);
        }
        context.DrawGeometry(null, pen, geometry);

        // Pointe pleine, orientée selon le côté d'entrée
        var dir = enterFromLeft ? 1 : -1;
        var arrow = new StreamGeometry();
        using (var g = arrow.Open())
        {
            g.BeginFigure(new Point(toX, toY), true);
            g.LineTo(new Point(toX - 6 * dir, toY - 3.5));
            g.LineTo(new Point(toX - 6 * dir, toY + 3.5));
            g.EndFigure(true);
        }
        context.DrawGeometry(brush, null, arrow);
    }

    private void RenderNowLine(DrawingContext context)
    {
        var x = TimelineX + Axis.ToX(DateTime.Today) + Axis.PixelsPerDay *
            DateTime.Now.TimeOfDay.TotalDays;
        if (x < TimelineX || x > TimelineX + TimelineWidth) return;

        context.DrawLine(
            new Pen(new SolidColorBrush(ProTheme.Accent.Error), 1.5),
            new Point(x, HeaderHeight), new Point(x, RowsViewport.Bottom));
    }

    private void RenderSplitter(DrawingContext context)
    {
        context.FillRectangle(new SolidColorBrush(ProTheme.Background.Toolbar),
            new Rect(_tableWidth, HeaderHeight, SplitterWidth, RowsViewport.Height));
        context.DrawLine(new Pen(new SolidColorBrush(ProTheme.Border.Default), 1),
            new Point(_tableWidth + SplitterWidth - 0.5, HeaderHeight),
            new Point(_tableWidth + SplitterWidth - 0.5, RowsViewport.Bottom));
    }

    private static FormattedText CreateText(string text, Color color, double size)
        => new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(ProTheme.Typography.FontFamily), size, new SolidColorBrush(color));
}

/// <summary>
/// Arguments d'un changement de dates par interaction (annulable) ;
/// NewStart/NewEnd modifiables par le handler (validation métier)
/// </summary>
public class GanttTaskChangeEventArgs : System.ComponentModel.CancelEventArgs
{
    public GanttTask Task { get; }
    public DateTime NewStart { get; set; }
    public DateTime NewEnd { get; set; }

    public GanttTaskChangeEventArgs(GanttTask task, DateTime newStart, DateTime newEnd)
    {
        Task = task;
        NewStart = newStart;
        NewEnd = newEnd;
    }
}

/// <summary>
/// Arguments d'un changement d'avancement par interaction (annulable)
/// </summary>
public class GanttProgressChangeEventArgs : System.ComponentModel.CancelEventArgs
{
    public GanttTask Task { get; }
    public double NewProgress { get; set; }

    public GanttProgressChangeEventArgs(GanttTask task, double newProgress)
    {
        Task = task;
        NewProgress = newProgress;
    }
}

/// <summary>
/// Arguments de création de lien à la souris (annulable)
/// </summary>
public class GanttLinkEventArgs : System.ComponentModel.CancelEventArgs
{
    public GanttTask Predecessor { get; }
    public GanttTask Successor { get; }

    public GanttLinkEventArgs(GanttTask predecessor, GanttTask successor)
    {
        Predecessor = predecessor;
        Successor = successor;
    }
}
