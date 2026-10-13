using Avalonia.Data;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using HDev.UI.Controls.Theme;
using System.Globalization;

namespace HDev.UI.Controls;

/// <summary>
/// Calendrier mensuel natif (navigateur de dates) : sélection, pastille
/// aujourd'hui, jours en gras (BoldDates : jours à événements), bornes.
/// Remplace le Calendar Fluent (souveraineté) — utilisé par HDevDateEdit.
/// </summary>
public class HDevMonthCalendar : Control, IHDevEditor
{
    private const double HeaderHeight = 32;
    private const double WeekDayHeight = 22;
    private const double CellSize = 30;

    private DateTime _displayMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime? _hoveredDate;
    private bool _hoverPrev, _hoverNext;

    public static readonly StyledProperty<DateTime?> SelectedDateProperty =
        AvaloniaProperty.Register<HDevMonthCalendar, DateTime?>(nameof(SelectedDate),
            defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);

    /// <summary>Jours marqués en gras (jours portant des événements)</summary>
    public HashSet<DateTime> BoldDates { get; } = new();

    public DateTime MinDate { get; set; } = DateTime.MinValue;
    public DateTime MaxDate { get; set; } = DateTime.MaxValue;

    /// <summary>
    /// Jour choisi, liable dans les deux sens (nom d'Avalonia). Une date posée par le code garde
    /// son heure, qui n'est pas affichée ; un jour choisi par l'utilisateur est à minuit.
    /// </summary>
    public DateTime? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    private DateTime? SelectedDay => SelectedDate?.Date;

    /// <summary>Mois affiché (le 1er du mois)</summary>
    public DateTime DisplayMonth
    {
        get => _displayMonth;
        set
        {
            var month = new DateTime(value.Year, value.Month, 1);
            if (_displayMonth == month) return;
            _displayMonth = month;
            InvalidateVisual();
        }
    }

    static HDevMonthCalendar()
    {
        FocusableProperty.OverrideDefaultValue<HDevMonthCalendar>(true);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != SelectedDateProperty) return;
        // Le mois affiché suit le jour choisi
        if (SelectedDate is { } date)
            _displayMonth = new DateTime(date.Year, date.Month, 1);
        InvalidateVisual();
        Core.OnPropertyChanged(change);
    }

    // ═══════════════════════════════════════════════════════════════
    // CONTRAT DE VALEUR (IHDevEditor, D2) : le calendrier reste hors du socle
    // ═══════════════════════════════════════════════════════════════

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<HDevMonthCalendar, bool>(nameof(IsReadOnly));

    /// <summary>Lecture seule : on navigue de mois en mois, on ne choisit pas de jour</summary>
    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    private HDevValueCore<DateTime?>? _core;
    private HDevValueCore<DateTime?> Core => _core ??= new HDevValueCore<DateTime?>(this, SelectedDateProperty);

    /// <summary>La valeur, non typée (<c>DateTime?</c>, celle de <see cref="SelectedDate"/>)</summary>
    public object? EditValue
    {
        get => SelectedDate;
        set => SelectedDate = (DateTime?)value;
    }

    /// <summary>Erreur de la liaison de la valeur (null = valide)</summary>
    public string? ErrorText => Core.ErrorText;

    /// <summary>Vrai si la valeur est valide (aucune erreur de liaison)</summary>
    public bool Validate() => ErrorText == null;

    public void FocusEditor() => Focus();

    /// <summary>Changement du jour choisi, au code comme à l'utilisateur (événement commun, routé)</summary>
    public event EventHandler<HDevValueChangedEventArgs>? ValueChanged
    {
        add => AddHandler(HDevEditor.ValueChangedEvent, value);
        remove => RemoveHandler(HDevEditor.ValueChangedEvent, value);
    }

    /// <summary>Avant un choix de l'utilisateur ; annulable</summary>
    public event EventHandler<HDevValueChangingEventArgs>? ValueChanging
    {
        add => AddHandler(HDevEditor.ValueChangingEvent, value);
        remove => RemoveHandler(HDevEditor.ValueChangingEvent, value);
    }

    protected override void UpdateDataValidation(AvaloniaProperty property, BindingValueType state, Exception? error)
    {
        base.UpdateDataValidation(property, state, error);
        Core.OnDataValidation(property, error);
    }

    /// <summary>Un jour choisi par l'utilisateur (clic, clavier)</summary>
    private void SelectByUser(DateTime date)
    {
        if (IsReadOnly) return;
        Core.CommitUser(date.Date);
    }

    public HDevMonthCalendar()
    {
        Width = 7 * CellSize + 12;
        Height = HeaderHeight + WeekDayHeight + 6 * CellSize + 8;
    }

    // ═══════════════════════════════════════════════════════════════
    // GÉOMÉTRIE
    // ═══════════════════════════════════════════════════════════════

    private DateTime GridStart => ScheduleEngine.StartOfWeek(_displayMonth);

    private Rect PrevRect => new(6, 6, 22, HeaderHeight - 12);
    private Rect NextRect => new(Bounds.Width - 28, 6, 22, HeaderHeight - 12);

    private Rect CellRect(int week, int day)
        => new(6 + day * CellSize, HeaderHeight + WeekDayHeight + week * CellSize, CellSize, CellSize);

    private DateTime? DateAt(Point pos)
    {
        var day = (int)((pos.X - 6) / CellSize);
        var week = (int)((pos.Y - HeaderHeight - WeekDayHeight) / CellSize);
        if (day is < 0 or > 6 || week is < 0 or > 5) return null;
        if (pos.Y < HeaderHeight + WeekDayHeight) return null;
        return GridStart.AddDays(week * 7 + day);
    }

    // ═══════════════════════════════════════════════════════════════
    // INTERACTIONS
    // ═══════════════════════════════════════════════════════════════

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var pos = e.GetPosition(this);
        var date = DateAt(pos);
        var prev = PrevRect.Contains(pos);
        var next = NextRect.Contains(pos);

        if (date != _hoveredDate || prev != _hoverPrev || next != _hoverNext)
        {
            _hoveredDate = date;
            _hoverPrev = prev;
            _hoverNext = next;
            InvalidateVisual();
        }
        base.OnPointerMoved(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _hoveredDate = null;
        _hoverPrev = _hoverNext = false;
        InvalidateVisual();
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Focus();
        var pos = e.GetPosition(this);

        if (PrevRect.Contains(pos))
        {
            DisplayMonth = _displayMonth.AddMonths(-1);
            e.Handled = true;
            return;
        }
        if (NextRect.Contains(pos))
        {
            DisplayMonth = _displayMonth.AddMonths(1);
            e.Handled = true;
            return;
        }

        var date = DateAt(pos);
        if (date != null && date >= MinDate && date <= MaxDate)
        {
            SelectByUser(date.Value);
            e.Handled = true;
        }
        base.OnPointerPressed(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        DisplayMonth = _displayMonth.AddMonths(e.Delta.Y > 0 ? -1 : 1);
        e.Handled = true;
        base.OnPointerWheelChanged(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var current = SelectedDay ?? DateTime.Today;
        DateTime? target = e.Key switch
        {
            Key.Left => current.AddDays(-1),
            Key.Right => current.AddDays(1),
            Key.Up => current.AddDays(-7),
            Key.Down => current.AddDays(7),
            Key.PageUp => current.AddMonths(-1),
            Key.PageDown => current.AddMonths(1),
            Key.Home => DateTime.Today,
            _ => null
        };

        if (target != null && target >= MinDate && target <= MaxDate && !IsReadOnly)
        {
            SelectByUser(target.Value);
            e.Handled = true;
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

        // En-tête : ◀ mois année ▶
        var title = Text(_displayMonth.ToString("MMMM yyyy", CultureInfo.CurrentCulture),
            HDevTheme.Text.Primary, 13, FontWeight.SemiBold);
        context.DrawText(title, Crisp.Snap(new Point(
            (bounds.Width - title.Width) / 2, (HeaderHeight - title.Height) / 2)));

        DrawNavArrow(context, PrevRect, _hoverPrev, left: true);
        DrawNavArrow(context, NextRect, _hoverNext, left: false);

        // Jours de semaine
        for (int d = 0; d < 7; d++)
        {
            var name = GridStart.AddDays(d).ToString("ddd", CultureInfo.CurrentCulture);
            if (name.Length > 2) name = name[..2];
            var label = Text(name, HDevTheme.Text.Secondary, 10);
            var cell = CellRect(0, d);
            context.DrawText(label, Crisp.Snap(new Point(
                cell.X + (CellSize - label.Width) / 2, HeaderHeight + 4)));
        }

        // Grille 6 semaines
        for (int w = 0; w < 6; w++)
        {
            for (int d = 0; d < 7; d++)
            {
                var date = GridStart.AddDays(w * 7 + d);
                var cell = CellRect(w, d);
                var center = cell.Center;

                var isCurrentMonth = date.Month == _displayMonth.Month;
                var isToday = date == DateTime.Today;
                var isSelected = date == SelectedDay;
                var isHovered = date == _hoveredDate;
                var isDisabled = date < MinDate || date > MaxDate;

                if (isSelected)
                    context.DrawEllipse(new SolidColorBrush(HDevTheme.Accent.Fill), null,
                        center, CellSize / 2 - 3, CellSize / 2 - 3);
                else if (isToday)
                    context.DrawEllipse(null, new Pen(new SolidColorBrush(HDevTheme.Accent.Primary), 1.4),
                        center, CellSize / 2 - 3, CellSize / 2 - 3);
                else if (isHovered && !isDisabled)
                    context.DrawEllipse(new SolidColorBrush(HDevTheme.Background.ControlHover), null,
                        center, CellSize / 2 - 3, CellSize / 2 - 3);

                var color = isSelected ? HDevTheme.Text.OnAccent
                    : isDisabled ? HDevTheme.Text.Disabled
                    : isCurrentMonth ? HDevTheme.Text.Primary
                    : HDevTheme.Text.Secondary;

                var bold = BoldDates.Contains(date.Date) || isSelected;
                var number = Text(date.Day.ToString(), color, 12,
                    bold ? FontWeight.Bold : FontWeight.Regular);
                context.DrawText(number, Crisp.Snap(new Point(
                    center.X - number.Width / 2, center.Y - number.Height / 2)));
            }
        }
    }

    private void DrawNavArrow(DrawingContext context, Rect rect, bool hovered, bool left)
    {
        if (hovered)
            context.DrawRectangle(new SolidColorBrush(HDevTheme.Background.ControlHover),
                null, rect, 4, 4);

        var pen = new Pen(new SolidColorBrush(HDevTheme.Text.Secondary), 1.5);
        var c = rect.Center;
        var dx = left ? 2.0 : -2.0;
        context.DrawLine(pen, new Point(c.X + dx, c.Y - 4), new Point(c.X - dx, c.Y));
        context.DrawLine(pen, new Point(c.X - dx, c.Y), new Point(c.X + dx, c.Y + 4));
    }

    private static FormattedText Text(string text, Color color, double size,
        FontWeight weight = FontWeight.Regular)
        => new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(HDevTheme.Typography.FontFamily, weight: weight), size,
            new SolidColorBrush(color));
}
