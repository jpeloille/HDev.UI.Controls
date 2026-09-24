using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using HDev.UI.Controls.Theme;
using System.Collections.ObjectModel;
using System.Globalization;

namespace HDev.UI.Controls;

public enum SchedulerViewMode
{
    Day,
    WorkWeek,
    Week,
    Month
}

/// <summary>
/// Arguments d'un déplacement/redimensionnement d'événement (annulable)
/// </summary>
public class ScheduleEventChangeEventArgs : System.ComponentModel.CancelEventArgs
{
    public ScheduleEvent Event { get; }
    public DateTime NewStart { get; set; }
    public DateTime NewEnd { get; set; }

    public ScheduleEventChangeEventArgs(ScheduleEvent evt, DateTime newStart, DateTime newEnd)
    {
        Event = evt;
        NewStart = newStart;
        NewEnd = newEnd;
    }
}

/// <summary>
/// Agenda (équivalent SchedulerControl) : vues Jour / Semaine ouvrée /
/// Semaine / Mois, événements chevauchants côte à côte, récurrences,
/// bandeau toute-la-journée, drag/resize avec événements annulables.
/// </summary>
public class HDevScheduler : Control
{
    private const double DayHeaderHeight = 30;
    private const double AllDayHeight = 26;
    private const double HourHeight = 44;
    private const double TimeGutterWidth = 46;
    private const double ScrollBarSize = 14;
    private const double SnapMinutes = 15;

    private DateTime _displayDate = DateTime.Today;
    private SchedulerViewMode _viewMode = SchedulerViewMode.Week;
    private double _timeOffset = 7 * HourHeight; // ouverture sur 07:00
    private bool _syncingScroll;

    private ScheduleEvent? _selectedEvent;
    private ScheduleEvent? _hoveredEvent;

    // Blocs posés à la dernière passe de rendu (hit-test)
    private readonly List<(Rect Rect, ScheduleOccurrence Occurrence, bool AllDay)> _blocks = new();

    // Drag en cours
    private enum DragKind { None, Move, ResizeEnd }
    private DragKind _dragKind;
    private ScheduleEvent? _dragEvent;
    private Point _dragOrigin;
    private DateTime _origStart, _origEnd;
    private DateTime _ghostStart, _ghostEnd;

    private readonly ScrollBar _scrollBar;

    public ObservableCollection<ScheduleEvent> Events { get; } = new();

    /// <summary>Heures ouvrées (fond clair dans la grille)</summary>
    public int WorkDayStart { get; set; } = 8;
    public int WorkDayEnd { get; set; } = 18;

    public bool IsReadOnly { get; set; }

    public event EventHandler? SelectedEventChanged;
    public event EventHandler<ScheduleEvent>? EventDoubleClicked;

    /// <summary>Double-clic sur un créneau vide (création)</summary>
    public event EventHandler<DateTime>? TimeSlotDoubleClicked;

    /// <summary>Avant application d'un déplacement/resize (annulable)</summary>
    public event EventHandler<ScheduleEventChangeEventArgs>? EventChanging;

    /// <summary>Après application</summary>
    public event EventHandler<ScheduleEvent>? EventChanged;

    public DateTime DisplayDate
    {
        get => _displayDate;
        set
        {
            if (_displayDate.Date == value.Date) return;
            _displayDate = value.Date;
            InvalidateVisual();
        }
    }

    public SchedulerViewMode ViewMode
    {
        get => _viewMode;
        set
        {
            if (_viewMode == value) return;
            _viewMode = value;
            UpdateScrollBar();
            InvalidateVisual();
        }
    }

    public ScheduleEvent? SelectedEvent
    {
        get => _selectedEvent;
        set
        {
            if (ReferenceEquals(_selectedEvent, value)) return;
            _selectedEvent = value;
            InvalidateVisual();
            SelectedEventChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    static HDevScheduler()
    {
        FocusableProperty.OverrideDefaultValue<HDevScheduler>(true);
    }

    public HDevScheduler()
    {
        ClipToBounds = true;

        Events.CollectionChanged += (s, e) => InvalidateVisual();

        _scrollBar = new ScrollBar
        {
            Orientation = Avalonia.Layout.Orientation.Vertical,
            Width = ScrollBarSize,
            AllowAutoHide = false
        };
        _scrollBar.Scroll += (s, e) =>
        {
            if (_syncingScroll) return;
            _timeOffset = _scrollBar.Value;
            InvalidateVisual();
        };
        VisualChildren.Add(_scrollBar);
        LogicalChildren.Add(_scrollBar);
    }

    /// <summary>Navigation : période précédente/suivante selon la vue</summary>
    public void Navigate(int direction)
    {
        DisplayDate = _viewMode switch
        {
            SchedulerViewMode.Day => _displayDate.AddDays(direction),
            SchedulerViewMode.Month => _displayDate.AddMonths(direction),
            _ => _displayDate.AddDays(7 * direction)
        };
    }

    public void GoToToday() => DisplayDate = DateTime.Today;

    // ═══════════════════════════════════════════════════════════════
    // GÉOMÉTRIE
    // ═══════════════════════════════════════════════════════════════

    private bool IsTimeGridView => _viewMode != SchedulerViewMode.Month;

    private int DayCount => _viewMode switch
    {
        SchedulerViewMode.Day => 1,
        SchedulerViewMode.WorkWeek => 5,
        _ => 7
    };

    private double GridTop => DayHeaderHeight + (IsTimeGridView ? AllDayHeight : 0);
    private double ContentWidth => Math.Max(0, Bounds.Width - ScrollBarSize);
    private double DayColumnWidth => Math.Max(20,
        (ContentWidth - (IsTimeGridView ? TimeGutterWidth : 0)) / DayCount);

    private void UpdateScrollBar()
    {
        _syncingScroll = true;
        if (IsTimeGridView)
        {
            var viewport = Math.Max(0, Bounds.Height - GridTop);
            _scrollBar.IsVisible = true;
            _scrollBar.Minimum = 0;
            _scrollBar.Maximum = Math.Max(0, 24 * HourHeight - viewport);
            _scrollBar.ViewportSize = viewport;
            _timeOffset = Math.Clamp(_timeOffset, 0, Math.Max(0, _scrollBar.Maximum));
            _scrollBar.Value = _timeOffset;
        }
        else
        {
            _scrollBar.IsVisible = false;
        }
        _syncingScroll = false;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _scrollBar.Measure(finalSize);
        _scrollBar.Arrange(new Rect(finalSize.Width - ScrollBarSize, GridTop,
            ScrollBarSize, Math.Max(0, finalSize.Height - GridTop)));
        UpdateScrollBar();
        return finalSize;
    }

    protected override Size MeasureOverride(Size availableSize)
        => new(
            double.IsInfinity(availableSize.Width) ? 700 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 400 : availableSize.Height);

    /// <summary>Date-heure au point donné (grille horaire), snappée au quart d'heure</summary>
    private DateTime? TimeAt(Point pos)
    {
        if (!IsTimeGridView) return MonthDayAt(pos);
        if (pos.X < TimeGutterWidth || pos.Y < GridTop) return null;

        var (start, days) = ScheduleEngine.DayViewRange(_displayDate, DayCount);
        var day = (int)((pos.X - TimeGutterWidth) / DayColumnWidth);
        if (day < 0 || day >= days) return null;

        var minutes = (pos.Y - GridTop + _timeOffset) / HourHeight * 60;
        minutes = Math.Clamp(Math.Round(minutes / SnapMinutes) * SnapMinutes, 0, 24 * 60 - SnapMinutes);
        return start.AddDays(day).AddMinutes(minutes);
    }

    private DateTime? MonthDayAt(Point pos)
    {
        if (pos.Y < DayHeaderHeight) return null;
        var (gridStart, weeks) = ScheduleEngine.MonthViewRange(_displayDate);
        var cellWidth = ContentWidth / 7;
        var cellHeight = (Bounds.Height - DayHeaderHeight) / weeks;
        var col = (int)(pos.X / cellWidth);
        var row = (int)((pos.Y - DayHeaderHeight) / cellHeight);
        if (col is < 0 or > 6 || row < 0 || row >= weeks) return null;
        return gridStart.AddDays(row * 7 + col);
    }

    private (ScheduleOccurrence Occurrence, bool AllDay)? BlockAt(Point pos)
    {
        for (int i = _blocks.Count - 1; i >= 0; i--)
        {
            if (_blocks[i].Rect.Contains(pos))
                return (_blocks[i].Occurrence, _blocks[i].AllDay);
        }
        return null;
    }

    // ═══════════════════════════════════════════════════════════════
    // SOURIS
    // ═══════════════════════════════════════════════════════════════

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var pos = e.GetPosition(this);

        if (_dragKind != DragKind.None && _dragEvent != null)
        {
            UpdateDragGhost(pos);
            InvalidateVisual();
            return;
        }

        var block = BlockAt(pos);
        var hovered = block?.Occurrence.Master;

        // Curseur resize sur le bord bas d'un bloc horaire
        if (block != null && !block.Value.AllDay && !IsReadOnly &&
            !block.Value.Occurrence.Master.IsRecurring &&
            Math.Abs(pos.Y - _blocks.First(b => b.Occurrence.Equals(block.Value.Occurrence)).Rect.Bottom) <= 5)
            Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
        else
            Cursor = Cursor.Default;

        if (!ReferenceEquals(hovered, _hoveredEvent))
        {
            _hoveredEvent = hovered;
            ToolTip.SetTip(this, hovered == null ? null : BuildTooltip(hovered));
            InvalidateVisual();
        }

        base.OnPointerMoved(e);
    }

    private static string BuildTooltip(ScheduleEvent evt)
    {
        var text = evt.AllDay
            ? $"{evt.Subject}\n{evt.Start:d} (journée)"
            : $"{evt.Subject}\n{evt.Start:g} → {evt.End:t}";
        if (!string.IsNullOrEmpty(evt.Location)) text += $"\n📍 {evt.Location}";
        if (evt.IsRecurring) text += "\n↻ récurrent";
        return text;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Focus();
        var pos = e.GetPosition(this);
        var block = BlockAt(pos);

        if (block != null)
        {
            var master = block.Value.Occurrence.Master;
            SelectedEvent = master;

            if (e.ClickCount == 2)
            {
                EventDoubleClicked?.Invoke(this, master);
                e.Handled = true;
                return;
            }

            // Drag (ponctuel uniquement — les récurrents ne se déplacent pas v1)
            if (!IsReadOnly && !master.IsRecurring)
            {
                var rect = _blocks.First(b => b.Occurrence.Equals(block.Value.Occurrence)).Rect;
                _dragKind = !block.Value.AllDay && Math.Abs(pos.Y - rect.Bottom) <= 5
                    ? DragKind.ResizeEnd
                    : DragKind.Move;
                _dragEvent = master;
                _dragOrigin = pos;
                _origStart = master.Start;
                _origEnd = master.End;
                _ghostStart = master.Start;
                _ghostEnd = master.End;
                e.Pointer.Capture(this);
            }

            e.Handled = true;
            return;
        }

        if (e.ClickCount == 2)
        {
            var time = TimeAt(pos);
            if (time != null)
            {
                TimeSlotDoubleClicked?.Invoke(this, time.Value);
                e.Handled = true;
                return;
            }
        }

        // Vue mois : double-clic sur un jour vide déjà géré ; simple clic = désélection
        SelectedEvent = null;
        base.OnPointerPressed(e);
    }

    private void UpdateDragGhost(Point pos)
    {
        if (_dragEvent == null) return;

        if (_viewMode == SchedulerViewMode.Month)
        {
            // Déplacement de jour (heure conservée)
            var day = MonthDayAt(pos);
            if (day != null)
            {
                var delta = day.Value.Date - _origStart.Date;
                _ghostStart = _origStart + delta;
                _ghostEnd = _origEnd + delta;
            }
            return;
        }

        switch (_dragKind)
        {
            case DragKind.Move:
            {
                var time = TimeAt(pos);
                if (time == null) return;
                _ghostStart = time.Value.AddMinutes(-SnapMinutes *
                    Math.Round(_origStart.TimeOfDay.TotalMinutes % SnapMinutes / SnapMinutes));
                _ghostStart = time.Value;
                _ghostEnd = _ghostStart + (_origEnd - _origStart);
                break;
            }
            case DragKind.ResizeEnd:
            {
                var time = TimeAt(pos);
                if (time == null) return;
                var end = time.Value;
                if (end <= _ghostStart) end = _ghostStart.AddMinutes(SnapMinutes);
                _ghostEnd = end;
                break;
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_dragKind != DragKind.None && _dragEvent != null)
        {
            var evt = _dragEvent;
            _dragKind = DragKind.None;
            _dragEvent = null;
            e.Pointer.Capture(null);

            if (_ghostStart != _origStart || _ghostEnd != _origEnd)
            {
                var args = new ScheduleEventChangeEventArgs(evt, _ghostStart, _ghostEnd);
                EventChanging?.Invoke(this, args);
                if (!args.Cancel)
                {
                    evt.Start = args.NewStart;
                    evt.End = args.NewEnd;
                    EventChanged?.Invoke(this, evt);
                }
            }
            InvalidateVisual();
            e.Handled = true;
        }
        base.OnPointerReleased(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (IsTimeGridView)
        {
            _timeOffset = Math.Clamp(
                _timeOffset + (e.Delta.Y > 0 ? -1 : 1) * HourHeight,
                0, Math.Max(0, 24 * HourHeight - (Bounds.Height - GridTop)));
            UpdateScrollBar();
        }
        else
        {
            Navigate(e.Delta.Y > 0 ? -1 : 1);
        }
        InvalidateVisual();
        e.Handled = true;
        base.OnPointerWheelChanged(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape when _dragKind != DragKind.None:
                _dragKind = DragKind.None;
                _dragEvent = null;
                InvalidateVisual();
                e.Handled = true;
                break;
            case Key.Left:
                Navigate(-1);
                e.Handled = true;
                break;
            case Key.Right:
                Navigate(1);
                e.Handled = true;
                break;
            case Key.Home:
                GoToToday();
                e.Handled = true;
                break;
        }
        base.OnKeyDown(e);
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════

    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(new SolidColorBrush(HDevTheme.Background.Panel), bounds);
        _blocks.Clear();

        if (IsTimeGridView)
            RenderTimeGridView(context);
        else
            RenderMonthView(context);
    }

    // ── Vue Jour / Semaine ─────────────────────────────────────────

    private void RenderTimeGridView(DrawingContext context)
    {
        var (start, days) = ScheduleEngine.DayViewRange(_displayDate, DayCount);
        var gridBottom = Bounds.Height;
        var colWidth = DayColumnWidth;

        // En-têtes de jours + bandeau toute-la-journée
        for (int d = 0; d < days; d++)
        {
            var date = start.AddDays(d);
            var x = TimeGutterWidth + d * colWidth;
            var isToday = date == DateTime.Today;

            if (isToday)
                context.FillRectangle(new SolidColorBrush(HDevTheme.WithOpacity(HDevTheme.Accent.Primary, 18)),
                    new Rect(x, 0, colWidth, DayHeaderHeight));

            var header = Text(date.ToString("ddd dd/MM", CultureInfo.CurrentCulture),
                isToday ? HDevTheme.Accent.Primary : HDevTheme.Text.Primary, 12,
                isToday ? FontWeight.SemiBold : FontWeight.Regular);
            context.DrawText(header, Crisp.Snap(new Point(
                x + (colWidth - header.Width) / 2, (DayHeaderHeight - header.Height) / 2)));
        }

        context.FillRectangle(new SolidColorBrush(HDevTheme.Background.Toolbar),
            new Rect(0, DayHeaderHeight, ContentWidth, AllDayHeight));

        // Grille horaire (clippée sous le bandeau)
        using (context.PushClip(new Rect(0, GridTop, ContentWidth, gridBottom - GridTop)))
        {
            var linePen = new Pen(new SolidColorBrush(HDevTheme.Border.Subtle), 1);
            var halfPen = new Pen(new SolidColorBrush(HDevTheme.WithOpacity(HDevTheme.Border.Subtle, 90)), 1);

            // Heures non ouvrées ombrées
            var shade = new SolidColorBrush(HDevTheme.WithOpacity(HDevTheme.Text.Secondary, 10));
            for (int d = 0; d < days; d++)
            {
                var date = start.AddDays(d);
                var x = TimeGutterWidth + d * colWidth;
                var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

                if (isWeekend)
                {
                    context.FillRectangle(shade, new Rect(x, GridTop, colWidth, gridBottom - GridTop));
                }
                else
                {
                    var beforeWork = GridTop + WorkDayStart * HourHeight - _timeOffset;
                    var afterWork = GridTop + WorkDayEnd * HourHeight - _timeOffset;
                    context.FillRectangle(shade, new Rect(x, GridTop, colWidth, Math.Max(0, beforeWork - GridTop)));
                    context.FillRectangle(shade, new Rect(x, afterWork, colWidth, Math.Max(0, gridBottom - afterWork)));
                }
            }

            for (int h = 0; h <= 24; h++)
            {
                var y = GridTop + h * HourHeight - _timeOffset;
                if (y < GridTop - HourHeight || y > gridBottom) continue;

                context.DrawLine(linePen, new Point(TimeGutterWidth, y), new Point(ContentWidth, y));
                if (h < 24)
                    context.DrawLine(halfPen,
                        new Point(TimeGutterWidth, y + HourHeight / 2),
                        new Point(ContentWidth, y + HourHeight / 2));

                var label = Text($"{h:00}:00", HDevTheme.Text.Secondary, 10);
                context.DrawText(label, Crisp.Snap(new Point(
                    TimeGutterWidth - label.Width - 6, y + 2)));
            }

            // Séparateurs de colonnes
            for (int d = 0; d <= days; d++)
            {
                var x = TimeGutterWidth + d * colWidth;
                context.DrawLine(linePen, new Point(x, GridTop), new Point(x, gridBottom));
            }

            // Événements par jour
            var rangeEnd = start.AddDays(days);
            var occurrences = ScheduleEngine.ExpandAll(Events, start, rangeEnd);

            for (int d = 0; d < days; d++)
            {
                var dayStart = start.AddDays(d);
                var dayEnd = dayStart.AddDays(1);
                var dayOccurrences = occurrences
                    .Where(o => !o.AllDay && o.Start < dayEnd && o.End > dayStart)
                    .ToList();

                foreach (var (occ, lane, laneCount) in ScheduleEngine.AssignLanes(dayOccurrences))
                {
                    var visibleStart = occ.Start < dayStart ? dayStart : occ.Start;
                    var visibleEnd = occ.End > dayEnd ? dayEnd : occ.End;

                    var laneWidth = (colWidth - 4) / laneCount;
                    var rect = new Rect(
                        TimeGutterWidth + d * colWidth + 2 + lane * laneWidth,
                        GridTop + visibleStart.TimeOfDay.TotalHours * HourHeight - _timeOffset,
                        Math.Max(8, laneWidth - 2),
                        Math.Max(14, (visibleEnd - visibleStart).TotalHours * HourHeight));

                    RenderEventBlock(context, occ, rect, compact: laneCount > 1);
                    _blocks.Add((rect, occ, false));
                }
            }

            // Ligne « maintenant »
            var today = DateTime.Today;
            if (today >= start && today < rangeEnd)
            {
                var dayIndex = (today - start).Days;
                var y = GridTop + DateTime.Now.TimeOfDay.TotalHours * HourHeight - _timeOffset;
                var x = TimeGutterWidth + dayIndex * colWidth;
                context.DrawLine(new Pen(new SolidColorBrush(HDevTheme.Accent.Error), 1.5),
                    new Point(x, y), new Point(x + colWidth, y));
                context.DrawEllipse(new SolidColorBrush(HDevTheme.Accent.Error), null,
                    new Point(x + 3, y), 3, 3);
            }

            // Fantôme de drag
            if (_dragKind != DragKind.None && _dragEvent != null)
                RenderDragGhost(context, start, days, colWidth, gridBottom);
        }

        // Toute-la-journée (par-dessus, non clippé par la grille)
        RenderAllDayStrip(context, start, days, colWidth);

        context.DrawLine(new Pen(new SolidColorBrush(HDevTheme.Border.Default), 1),
            new Point(0, GridTop - 0.5), new Point(ContentWidth, GridTop - 0.5));
    }

    private void RenderAllDayStrip(DrawingContext context, DateTime start, int days, double colWidth)
    {
        var occurrences = ScheduleEngine.ExpandAll(Events, start, start.AddDays(days))
            .Where(o => o.AllDay).ToList();

        foreach (var occ in occurrences)
        {
            var firstDay = Math.Max(0, (occ.Start.Date - start).Days);
            var lastDay = Math.Min(days - 1, (occ.End.Date > occ.Start.Date
                ? (occ.End.Date - start).Days - (occ.End.TimeOfDay == TimeSpan.Zero ? 1 : 0)
                : firstDay));

            var rect = new Rect(
                TimeGutterWidth + firstDay * colWidth + 2,
                DayHeaderHeight + 3,
                (lastDay - firstDay + 1) * colWidth - 4,
                AllDayHeight - 6);

            RenderEventBlock(context, occ, rect, compact: true);
            _blocks.Add((rect, occ, true));
        }
    }

    private void RenderDragGhost(DrawingContext context, DateTime start, int days,
        double colWidth, double gridBottom)
    {
        var day = (_ghostStart.Date - start).Days;
        if (day < 0 || day >= days) return;

        var rect = new Rect(
            TimeGutterWidth + day * colWidth + 2,
            GridTop + _ghostStart.TimeOfDay.TotalHours * HourHeight - _timeOffset,
            colWidth - 4,
            Math.Max(14, (_ghostEnd - _ghostStart).TotalHours * HourHeight));

        var pen = new Pen(new SolidColorBrush(HDevTheme.Accent.Primary), 1.5)
        {
            DashStyle = new DashStyle(new double[] { 3, 3 }, 0)
        };
        context.DrawRectangle(
            new SolidColorBrush(HDevTheme.WithOpacity(HDevTheme.Accent.Primary, 30)), pen, rect, 4, 4);

        var label = Text($"{_ghostStart:HH:mm} → {_ghostEnd:HH:mm}", HDevTheme.Text.Primary, 11);
        context.DrawText(label, Crisp.Snap(new Point(rect.X + 4, rect.Y - label.Height - 2)));
    }

    // ── Vue Mois ───────────────────────────────────────────────────

    private void RenderMonthView(DrawingContext context)
    {
        var (gridStart, weeks) = ScheduleEngine.MonthViewRange(_displayDate);
        var cellWidth = ContentWidth / 7;
        var cellHeight = (Bounds.Height - DayHeaderHeight) / weeks;
        var linePen = new Pen(new SolidColorBrush(HDevTheme.Border.Subtle), 1);

        // En-têtes des jours de semaine
        for (int d = 0; d < 7; d++)
        {
            var name = gridStart.AddDays(d).ToString("dddd", CultureInfo.CurrentCulture);
            var header = Text(name, HDevTheme.Text.Secondary, 11);
            context.DrawText(header, Crisp.Snap(new Point(
                d * cellWidth + (cellWidth - header.Width) / 2,
                (DayHeaderHeight - header.Height) / 2)));
        }

        var occurrences = ScheduleEngine.ExpandAll(Events, gridStart, gridStart.AddDays(weeks * 7));

        for (int w = 0; w < weeks; w++)
        {
            for (int d = 0; d < 7; d++)
            {
                var date = gridStart.AddDays(w * 7 + d);
                var cell = new Rect(d * cellWidth, DayHeaderHeight + w * cellHeight, cellWidth, cellHeight);
                var isCurrentMonth = date.Month == _displayDate.Month;
                var isToday = date == DateTime.Today;

                if (!isCurrentMonth)
                    context.FillRectangle(new SolidColorBrush(
                        HDevTheme.WithOpacity(HDevTheme.Text.Secondary, 8)), cell);

                context.DrawRectangle(null, linePen, cell);

                // Numéro du jour (pastille accent = aujourd'hui)
                var number = Text(date.Day.ToString(),
                    isToday ? Colors.White : isCurrentMonth ? HDevTheme.Text.Primary : HDevTheme.Text.Secondary,
                    12, isToday ? FontWeight.SemiBold : FontWeight.Regular);
                if (isToday)
                    context.DrawEllipse(new SolidColorBrush(HDevTheme.Accent.Primary), null,
                        new Point(cell.Right - 14, cell.Y + 12), 10, 10);
                context.DrawText(number, Crisp.Snap(new Point(
                    cell.Right - 14 - number.Width / 2, cell.Y + 12 - number.Height / 2)));

                // Chips d'événements du jour
                var dayOccurrences = occurrences
                    .Where(o => o.Start.Date <= date && o.End > date &&
                        (o.Start.Date == date || o.AllDay || o.End.Date >= date))
                    .Where(o => o.Start.Date == date || (o.AllDay && o.Start.Date <= date && o.End.Date >= date))
                    .OrderBy(o => !o.AllDay).ThenBy(o => o.Start)
                    .ToList();

                var maxChips = Math.Max(1, (int)((cellHeight - 26) / 17));
                var y = cell.Y + 24;

                for (int i = 0; i < dayOccurrences.Count && i < maxChips; i++)
                {
                    if (i == maxChips - 1 && dayOccurrences.Count > maxChips)
                    {
                        var more = Text($"+ {dayOccurrences.Count - i} autres", HDevTheme.Text.Secondary, 10);
                        context.DrawText(more, Crisp.Snap(new Point(cell.X + 6, y)));
                        break;
                    }

                    var occ = dayOccurrences[i];
                    var chipRect = new Rect(cell.X + 3, y, cellWidth - 6, 15);
                    RenderEventBlock(context, occ, chipRect, compact: true);
                    _blocks.Add((chipRect, occ, occ.AllDay));
                    y += 17;
                }
            }
        }
    }

    // ── Bloc d'événement (commun) ──────────────────────────────────

    private void RenderEventBlock(DrawingContext context, ScheduleOccurrence occ, Rect rect, bool compact)
    {
        var master = occ.Master;
        var color = master.Color ?? HDevTheme.Accent.Primary;
        var isSelected = ReferenceEquals(master, _selectedEvent);
        var isHovered = ReferenceEquals(master, _hoveredEvent);

        context.DrawRectangle(
            new SolidColorBrush(HDevTheme.WithOpacity(color, isSelected ? (byte)230 : isHovered ? (byte)190 : (byte)160)),
            isSelected ? new Pen(new SolidColorBrush(HDevTheme.Text.Primary), 1.5) : null,
            rect, 4, 4);

        var prefix = master.IsRecurring ? "↻ " : "";
        var timeText = compact || occ.AllDay ? "" : $"{occ.Start:HH:mm} ";
        var label = Text($"{prefix}{timeText}{master.Subject}", Colors.White, compact ? 10 : 11,
            FontWeight.SemiBold);
        label.MaxTextWidth = Math.Max(8, rect.Width - 8);
        label.Trimming = TextTrimming.CharacterEllipsis;
        label.MaxLineCount = Math.Max(1, (int)(rect.Height / 14));

        using (context.PushClip(rect))
        {
            context.DrawText(label, Crisp.Snap(new Point(rect.X + 4, rect.Y + 2)));
        }
    }

    private static FormattedText Text(string text, Color color, double size,
        FontWeight weight = FontWeight.Regular)
        => new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(HDevTheme.Typography.FontFamily, weight: weight), size,
            new SolidColorBrush(color));
}
