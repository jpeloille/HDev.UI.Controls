using ProControls.Controls;
using Xunit;

namespace ProControls.Tests;

public class GanttCalendarTests
{
    // Repères 2026 : le 13/07/2026 est un lundi
    private static readonly DateTime Monday = new(2026, 7, 13);

    [Fact]
    public void Weekend_IsNotWorkingDay()
    {
        var calendar = new GanttCalendar();
        Assert.True(calendar.IsWorkingDay(Monday));
        Assert.False(calendar.IsWorkingDay(Monday.AddDays(5)));  // samedi
        Assert.False(calendar.IsWorkingDay(Monday.AddDays(6)));  // dimanche
    }

    [Fact]
    public void Holiday_IsNotWorkingDay()
    {
        var calendar = new GanttCalendar();
        calendar.AddHoliday(new DateTime(2026, 7, 14)); // mardi férié
        Assert.False(calendar.IsWorkingDay(new DateTime(2026, 7, 14)));
    }

    [Fact]
    public void CountWorkingDays_FullWeek_Is5()
    {
        var calendar = new GanttCalendar();
        Assert.Equal(5, calendar.CountWorkingDays(Monday, Monday.AddDays(6)));
    }

    [Fact]
    public void CountWorkingDays_ExcludesHolidays()
    {
        var calendar = new GanttCalendar();
        calendar.AddHoliday(Monday.AddDays(1));
        Assert.Equal(4, calendar.CountWorkingDays(Monday, Monday.AddDays(6)));
    }

    [Fact]
    public void CountWorkingDays_EndBeforeStart_IsZero()
    {
        var calendar = new GanttCalendar();
        Assert.Equal(0, calendar.CountWorkingDays(Monday, Monday.AddDays(-1)));
    }

    [Fact]
    public void AddWorkingDays_SkipsWeekend()
    {
        var calendar = new GanttCalendar();
        // lundi + 4 ouvrés = vendredi ; + 5 ouvrés = lundi suivant
        Assert.Equal(Monday.AddDays(4), calendar.AddWorkingDays(Monday, 4));
        Assert.Equal(Monday.AddDays(7), calendar.AddWorkingDays(Monday, 5));
    }

    [Fact]
    public void AddWorkingDays_Zero_FromWeekend_MovesToNextWorkingDay()
    {
        var calendar = new GanttCalendar();
        var saturday = Monday.AddDays(5);
        Assert.Equal(Monday.AddDays(7), calendar.AddWorkingDays(saturday, 0));
    }
}

public class GanttRollUpTests
{
    private static readonly DateTime D0 = new(2026, 7, 13); // lundi

    [Fact]
    public void Leaf_EffectiveValues_MirrorOwnValues()
    {
        var project = new GanttProject();
        var task = project.Add("T", D0, D0.AddDays(4), progress: 30);

        Assert.Equal(D0, task.EffectiveStart);
        Assert.Equal(D0.AddDays(4), task.EffectiveEnd);
        Assert.Equal(30, task.EffectiveProgress);
    }

    [Fact]
    public void Summary_SpansChildren()
    {
        var project = new GanttProject();
        var summary = project.Add("Phase", D0, D0); // dates ignorées (roll-up)
        summary.Add("A", D0, D0.AddDays(2));
        summary.Add("B", D0.AddDays(7), D0.AddDays(11));

        Assert.True(summary.IsSummary);
        Assert.Equal(D0, summary.EffectiveStart);
        Assert.Equal(D0.AddDays(11), summary.EffectiveEnd);
    }

    [Fact]
    public void Summary_Progress_IsWeightedByWorkingDuration()
    {
        var project = new GanttProject();
        var summary = project.Add("Phase", D0, D0);
        summary.Add("Courte", D0, D0, progress: 100);              // 1 jour ouvré
        summary.Add("Longue", D0.AddDays(1), D0.AddDays(4), 0);    // 4 jours ouvrés

        // (100×1 + 0×4) / 5 = 20
        Assert.Equal(20, summary.EffectiveProgress, precision: 5);
    }

    [Fact]
    public void RollUp_IsRecursive_OverThreeLevels()
    {
        var project = new GanttProject();
        var root = project.Add("Racine", D0, D0);
        var phase = root.Add("Phase", D0, D0);
        phase.Add("Feuille", D0.AddDays(3), D0.AddDays(9));

        Assert.Equal(D0.AddDays(3), root.EffectiveStart);
        Assert.Equal(D0.AddDays(9), root.EffectiveEnd);
    }

    [Fact]
    public void MutatingChildDate_UpdatesSummary()
    {
        var project = new GanttProject();
        var summary = project.Add("Phase", D0, D0);
        var child = summary.Add("A", D0, D0.AddDays(2));

        child.End = D0.AddDays(20);

        Assert.Equal(D0.AddDays(20), summary.EffectiveEnd);
    }

    [Fact]
    public void BeginEndUpdate_DefersRecalculation()
    {
        var project = new GanttProject();
        var changes = 0;
        project.DataChanged += (s, e) => changes++;

        project.BeginUpdate();
        var summary = project.Add("Phase", D0, D0);
        for (int i = 0; i < 100; i++)
            summary.Add($"T{i}", D0.AddDays(i), D0.AddDays(i + 1));
        project.EndUpdate();

        Assert.Equal(1, changes);
        Assert.Equal(D0.AddDays(100), summary.EffectiveEnd);
    }

    [Fact]
    public void GetBounds_CoversAllTasks()
    {
        var project = new GanttProject();
        project.Add("A", D0, D0.AddDays(5));
        project.Add("B", D0.AddDays(-3), D0.AddDays(2));

        var (start, end) = project.GetBounds();
        Assert.Equal(D0.AddDays(-3), start);
        Assert.Equal(D0.AddDays(5), end);
    }

    [Fact]
    public void AllTasks_IsDepthFirstPrefixOrder()
    {
        var project = new GanttProject();
        var a = project.Add("A", D0, D0);
        a.Add("A1", D0, D0);
        project.Add("B", D0, D0);

        Assert.Equal(new[] { "A", "A1", "B" },
            project.AllTasks().Select(t => t.Name).ToArray());
    }
}

public class GanttTimeAxisTests
{
    private static readonly DateTime D0 = new(2026, 7, 13);

    [Fact]
    public void ToX_And_ToDate_RoundTrip()
    {
        var axis = new GanttTimeAxis { Origin = D0, PixelsPerDay = 20 };

        Assert.Equal(0, axis.ToX(D0));
        Assert.Equal(200, axis.ToX(D0.AddDays(10)));
        Assert.Equal(D0.AddDays(10), axis.ToDate(200).Date);
    }

    [Fact]
    public void ViewOffset_ShiftsView()
    {
        var axis = new GanttTimeAxis { Origin = D0, PixelsPerDay = 20, ViewOffset = 100 };
        Assert.Equal(-100, axis.ToX(D0));
    }

    [Fact]
    public void PixelsPerDay_IsClamped()
    {
        var axis = new GanttTimeAxis { PixelsPerDay = 1000 };
        Assert.Equal(GanttTimeAxis.MaxPixelsPerDay, axis.PixelsPerDay);
        axis.PixelsPerDay = 0.01;
        Assert.Equal(GanttTimeAxis.MinPixelsPerDay, axis.PixelsPerDay);
    }

    [Fact]
    public void ZoomAt_KeepsPivotDateStationary()
    {
        var axis = new GanttTimeAxis { Origin = D0, PixelsPerDay = 10 };
        var pivotX = 300.0;
        var dateBefore = axis.ToDate(pivotX);

        axis.ZoomAt(pivotX, 2.0);

        // Tolérance < 1 seconde sur la date pivot
        Assert.True(Math.Abs((dateBefore - axis.ToDate(pivotX)).TotalSeconds) < 1);
        Assert.Equal(20, axis.PixelsPerDay);
    }

    [Theory]
    [InlineData(20, GanttTimeAxis.TickTier.MonthsDays)]
    [InlineData(5, GanttTimeAxis.TickTier.MonthsWeeks)]
    [InlineData(1, GanttTimeAxis.TickTier.YearsMonths)]
    public void CurrentTier_DependsOnZoom(double ppd, GanttTimeAxis.TickTier expected)
        => Assert.Equal(expected, new GanttTimeAxis { PixelsPerDay = ppd }.CurrentTier);

    [Fact]
    public void MinorTicks_MonthsDays_AreDaily()
    {
        var axis = new GanttTimeAxis { Origin = D0, PixelsPerDay = 20 };
        var ticks = axis.MinorTicks(200).ToList();

        // ~10 jours visibles + marges
        Assert.InRange(ticks.Count, 10, 14);
        Assert.Equal(1, (ticks[1].Date - ticks[0].Date).Days);
    }

    [Fact]
    public void MinorTicks_Weeks_AreMondayAligned()
    {
        var axis = new GanttTimeAxis { Origin = D0, PixelsPerDay = 5 };
        foreach (var tick in axis.MinorTicks(400))
            Assert.Equal(DayOfWeek.Monday, tick.Date.DayOfWeek);
    }
}
