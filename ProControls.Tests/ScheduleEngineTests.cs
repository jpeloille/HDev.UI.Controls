using ProControls.Controls;
using Xunit;

namespace ProControls.Tests;

public class ScheduleEngineTests
{
    // Repère : lundi 13/07/2026
    private static readonly DateTime Mon = new(2026, 7, 13);

    private static ScheduleEvent At(int day, int hour, double durationHours = 1)
        => new("evt", Mon.AddDays(day).AddHours(hour), Mon.AddDays(day).AddHours(hour + durationHours));

    // ── Ponctuels ──────────────────────────────────────────────────

    [Fact]
    public void SingleEvent_InRange_YieldsOneOccurrence()
    {
        var occurrences = ScheduleEngine.Expand(At(0, 9), Mon, Mon.AddDays(7)).ToList();
        Assert.Single(occurrences);
        Assert.Equal(Mon.AddHours(9), occurrences[0].Start);
    }

    [Fact]
    public void SingleEvent_OutOfRange_YieldsNothing()
        => Assert.Empty(ScheduleEngine.Expand(At(10, 9), Mon, Mon.AddDays(7)));

    [Fact]
    public void SingleEvent_StraddlingRangeEdge_IsIncluded()
    {
        var evt = new ScheduleEvent("nuit", Mon.AddHours(-2), Mon.AddHours(2));
        Assert.Single(ScheduleEngine.Expand(evt, Mon, Mon.AddDays(1)));
    }

    // ── Récurrences ────────────────────────────────────────────────

    [Fact]
    public void Daily_YieldsEveryDay()
    {
        var evt = At(0, 9);
        evt.Recurrence.Kind = ScheduleRecurrenceKind.Daily;
        var occurrences = ScheduleEngine.Expand(evt, Mon, Mon.AddDays(7)).ToList();
        Assert.Equal(7, occurrences.Count);
        Assert.All(occurrences, o => Assert.Equal(9, o.Start.Hour));
    }

    [Fact]
    public void Daily_WithInterval2_SkipsAlternateDays()
    {
        var evt = At(0, 9);
        evt.Recurrence.Kind = ScheduleRecurrenceKind.Daily;
        evt.Recurrence.Interval = 2;
        Assert.Equal(4, ScheduleEngine.Expand(evt, Mon, Mon.AddDays(7)).Count()); // j0 j2 j4 j6
    }

    [Fact]
    public void Daily_RespectsUntil()
    {
        var evt = At(0, 9);
        evt.Recurrence.Kind = ScheduleRecurrenceKind.Daily;
        evt.Recurrence.Until = Mon.AddDays(2); // inclus
        Assert.Equal(3, ScheduleEngine.Expand(evt, Mon, Mon.AddDays(30)).Count());
    }

    [Fact]
    public void Weekly_OnSelectedDays()
    {
        var evt = At(0, 14);
        evt.Recurrence.Kind = ScheduleRecurrenceKind.Weekly;
        evt.Recurrence.DaysOfWeek.Add(DayOfWeek.Monday);
        evt.Recurrence.DaysOfWeek.Add(DayOfWeek.Thursday);

        var occurrences = ScheduleEngine.Expand(evt, Mon, Mon.AddDays(14)).ToList();
        Assert.Equal(4, occurrences.Count); // lun+jeu × 2 semaines
        Assert.All(occurrences, o =>
            Assert.True(o.Start.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Thursday));
    }

    [Fact]
    public void Weekly_DoesNotYieldBeforeFirstOccurrence()
    {
        // Départ mercredi, jours lun+mer : le lundi de la même semaine ne sort pas
        var evt = new ScheduleEvent("x", Mon.AddDays(2).AddHours(10), Mon.AddDays(2).AddHours(11));
        evt.Recurrence.Kind = ScheduleRecurrenceKind.Weekly;
        evt.Recurrence.DaysOfWeek.Add(DayOfWeek.Monday);
        evt.Recurrence.DaysOfWeek.Add(DayOfWeek.Wednesday);

        var occurrences = ScheduleEngine.Expand(evt, Mon, Mon.AddDays(7)).ToList();
        Assert.Single(occurrences); // seulement le mercredi
        Assert.Equal(DayOfWeek.Wednesday, occurrences[0].Start.DayOfWeek);
    }

    [Fact]
    public void Monthly_OnDayOfMonth_SkipsShortMonths()
    {
        var evt = new ScheduleEvent("paie", new DateTime(2026, 1, 31, 9, 0, 0),
            new DateTime(2026, 1, 31, 10, 0, 0));
        evt.Recurrence.Kind = ScheduleRecurrenceKind.Monthly;

        var occurrences = ScheduleEngine.Expand(evt,
            new DateTime(2026, 1, 1), new DateTime(2026, 5, 1)).ToList();

        // janvier, mars (pas de 31 février ni avril... avril a 30 jours)
        Assert.Equal(2, occurrences.Count);
        Assert.All(occurrences, o => Assert.Equal(31, o.Start.Day));
    }

    [Fact]
    public void ExpandAll_MergesAndSorts()
    {
        var a = At(1, 10);
        var b = At(0, 9);
        var list = ScheduleEngine.ExpandAll(new[] { a, b }, Mon, Mon.AddDays(7));
        Assert.Equal(2, list.Count);
        Assert.Equal(b, list[0].Master);
    }

    // ── Couloirs ───────────────────────────────────────────────────

    [Fact]
    public void NonOverlapping_AllInLaneZero()
    {
        var occurrences = ScheduleEngine.ExpandAll(
            new[] { At(0, 9), At(0, 11), At(0, 14) }, Mon, Mon.AddDays(1));
        var lanes = ScheduleEngine.AssignLanes(occurrences);

        Assert.All(lanes, l => Assert.Equal(0, l.Lane));
        Assert.All(lanes, l => Assert.Equal(1, l.LaneCount));
    }

    [Fact]
    public void TwoOverlapping_SideBySide()
    {
        var occurrences = ScheduleEngine.ExpandAll(
            new[] { At(0, 9, 2), At(0, 10, 2) }, Mon, Mon.AddDays(1));
        var lanes = ScheduleEngine.AssignLanes(occurrences);

        Assert.Equal(new[] { 0, 1 }, lanes.Select(l => l.Lane).OrderBy(x => x).ToArray());
        Assert.All(lanes, l => Assert.Equal(2, l.LaneCount));
    }

    [Fact]
    public void LaneIsReused_AfterEventEnds()
    {
        // A 9-10, B 9:30-10:30, C 10-11 : C réutilise le couloir de A
        var occurrences = ScheduleEngine.ExpandAll(
            new[] { At(0, 9, 1), At(0, 9.5 == 9.5 ? 9 : 9, 0), At(0, 10, 1) }
                .Where(e => e.Duration > TimeSpan.Zero).ToArray(),
            Mon, Mon.AddDays(1));

        var a = At(0, 9, 1);
        var b = new ScheduleEvent("b", Mon.AddHours(9.5), Mon.AddHours(10.5));
        var c = At(0, 10, 1);
        var all = ScheduleEngine.ExpandAll(new[] { a, b, c }, Mon, Mon.AddDays(1));
        var lanes = ScheduleEngine.AssignLanes(all);

        var laneOfA = lanes.Single(l => l.Occurrence.Master == a).Lane;
        var laneOfC = lanes.Single(l => l.Occurrence.Master == c).Lane;
        Assert.Equal(laneOfA, laneOfC);
        Assert.All(lanes, l => Assert.Equal(2, l.LaneCount));
    }

    [Fact]
    public void SeparateClusters_DoNotShareLaneCount()
    {
        // Matin : 2 chevauchants ; après-midi : 1 seul → LaneCount 2 puis 1
        var all = ScheduleEngine.ExpandAll(
            new[] { At(0, 9, 2), At(0, 10, 2), At(0, 15, 1) }, Mon, Mon.AddDays(1));
        var lanes = ScheduleEngine.AssignLanes(all);

        Assert.Equal(2, lanes.Single(l => l.Occurrence.Start.Hour == 15).LaneCount == 1
            ? 2 : 0); // l'isolé a LaneCount 1
        Assert.Equal(1, lanes.Single(l => l.Occurrence.Start.Hour == 15).LaneCount);
    }

    // ── Plages de vues ─────────────────────────────────────────────

    [Fact]
    public void StartOfWeek_IsMonday()
    {
        Assert.Equal(Mon, ScheduleEngine.StartOfWeek(Mon));
        Assert.Equal(Mon, ScheduleEngine.StartOfWeek(Mon.AddDays(3)));
        Assert.Equal(Mon, ScheduleEngine.StartOfWeek(Mon.AddDays(6)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(7)]
    public void DayViewRange_Counts(int days)
    {
        var (start, count) = ScheduleEngine.DayViewRange(Mon.AddDays(2), days);
        Assert.Equal(days, count);
        Assert.Equal(days == 1 ? Mon.AddDays(2) : Mon, start);
    }

    [Fact]
    public void MonthViewRange_StartsMondayBeforeFirst_CoversMonth()
    {
        // Juillet 2026 : le 1er est un mercredi → grille depuis lundi 29/06
        var (gridStart, weeks) = ScheduleEngine.MonthViewRange(new DateTime(2026, 7, 15));
        Assert.Equal(new DateTime(2026, 6, 29), gridStart);
        Assert.True(gridStart.AddDays(weeks * 7) >= new DateTime(2026, 7, 31));
    }
}
