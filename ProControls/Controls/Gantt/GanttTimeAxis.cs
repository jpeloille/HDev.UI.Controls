using System.Globalization;

namespace ProControls.Controls;

/// <summary>
/// Axe temporel du Gantt : transformation temps↔pixels, zoom continu,
/// graduations à deux niveaux adaptées à la densité. Logique pure (testée).
/// </summary>
public class GanttTimeAxis
{
    private double _pixelsPerDay = 24;

    /// <summary>Origine de l'axe (date affichée au pixel ViewOffset = 0)</summary>
    public DateTime Origin { get; set; } = DateTime.Today;

    /// <summary>Décalage de vue en pixels (scroll horizontal)</summary>
    public double ViewOffset { get; set; }

    public const double MinPixelsPerDay = 0.5;
    public const double MaxPixelsPerDay = 80;

    /// <summary>Zoom : largeur d'un jour en pixels, borné [0,5 ; 80]</summary>
    public double PixelsPerDay
    {
        get => _pixelsPerDay;
        set => _pixelsPerDay = Math.Clamp(value, MinPixelsPerDay, MaxPixelsPerDay);
    }

    /// <summary>Position X (pixels vue) du début du jour donné</summary>
    public double ToX(DateTime date)
        => (date.Date - Origin).TotalDays * _pixelsPerDay - ViewOffset;

    /// <summary>Date au pixel X de la vue</summary>
    public DateTime ToDate(double x)
        => Origin.AddDays((x + ViewOffset) / _pixelsPerDay);

    /// <summary>
    /// Zoom centré : ajuste PixelsPerDay en gardant la date sous pivotX immobile
    /// </summary>
    public void ZoomAt(double pivotX, double factor)
    {
        var pivotDate = ToDate(pivotX);
        PixelsPerDay = _pixelsPerDay * factor;
        // Repositionner pour que pivotDate reste sous pivotX
        ViewOffset = (pivotDate - Origin).TotalDays * _pixelsPerDay - pivotX;
    }

    /// <summary>Fenêtre temporelle visible pour une largeur de vue donnée</summary>
    public (DateTime Start, DateTime End) VisibleRange(double viewWidth)
        => (ToDate(0).Date.AddDays(-1), ToDate(viewWidth).Date.AddDays(1));

    // ═══════════════════════════════════════════════════════════════
    // GRADUATIONS
    // ═══════════════════════════════════════════════════════════════

    public enum TickTier
    {
        /// <summary>Zoom fort : mois en haut, jours en bas</summary>
        MonthsDays,
        /// <summary>Zoom moyen : mois en haut, semaines en bas</summary>
        MonthsWeeks,
        /// <summary>Zoom faible : années en haut, mois en bas</summary>
        YearsMonths
    }

    public TickTier CurrentTier => _pixelsPerDay switch
    {
        >= 14 => TickTier.MonthsDays,
        >= 3 => TickTier.MonthsWeeks,
        _ => TickTier.YearsMonths
    };

    public readonly record struct Tick(DateTime Date, double X, string Label);

    /// <summary>Graduations du niveau bas (jours / semaines / mois selon zoom)</summary>
    public IEnumerable<Tick> MinorTicks(double viewWidth)
    {
        var (start, end) = VisibleRange(viewWidth);

        switch (CurrentTier)
        {
            case TickTier.MonthsDays:
                for (var d = start.Date; d <= end; d = d.AddDays(1))
                    yield return new Tick(d, ToX(d), d.Day.ToString());
                break;

            case TickTier.MonthsWeeks:
                // Semaines : alignées sur lundi
                var monday = start.Date.AddDays(-(((int)start.DayOfWeek + 6) % 7));
                for (var d = monday; d <= end; d = d.AddDays(7))
                    yield return new Tick(d, ToX(d), $"S{ISOWeek(d)}");
                break;

            case TickTier.YearsMonths:
                for (var d = new DateTime(start.Year, start.Month, 1); d <= end; d = d.AddMonths(1))
                    yield return new Tick(d, ToX(d),
                        d.ToString("MMM", CultureInfo.CurrentCulture));
                break;
        }
    }

    /// <summary>Graduations du niveau haut (mois / mois / années selon zoom)</summary>
    public IEnumerable<Tick> MajorTicks(double viewWidth)
    {
        var (start, end) = VisibleRange(viewWidth);

        if (CurrentTier == TickTier.YearsMonths)
        {
            for (var d = new DateTime(start.Year, 1, 1); d <= end; d = d.AddYears(1))
                yield return new Tick(d, ToX(d), d.Year.ToString());
        }
        else
        {
            for (var d = new DateTime(start.Year, start.Month, 1); d <= end; d = d.AddMonths(1))
                yield return new Tick(d, ToX(d),
                    d.ToString("MMMM yyyy", CultureInfo.CurrentCulture));
        }
    }

    private static int ISOWeek(DateTime date)
        => System.Globalization.ISOWeek.GetWeekOfYear(date);
}
