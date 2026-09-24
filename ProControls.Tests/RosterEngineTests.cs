using ProControls.Controls;
using Xunit;

namespace ProControls.Tests;

public class RosterEngineTests
{
    // Repère : jeudi 17/09/2026, minuit
    private static readonly DateTime J = new(2026, 9, 17);

    private static DateTime At(double hours) => J.AddHours(hours);

    // ── Split : le cas nominal ─────────────────────────────────────

    [Fact]
    public void Split_WithinOneWindow_YieldsOneSegment()
    {
        var segments = RosterEngine.Split(At(8), At(17), J, RosterEngine.Day).ToList();

        Assert.Single(segments);
        Assert.Equal(new RosterSegment(At(8), At(17), 0), segments[0]);
    }

    [Fact]
    public void Split_EndingExactlyOnBoundary_DoesNotYieldEmptyTrailingSegment()
    {
        var segments = RosterEngine.Split(At(20), At(24), J, RosterEngine.Day).ToList();

        Assert.Single(segments);
        Assert.Equal(At(24), segments[0].End);
    }

    // ── Split : le franchissement de minuit, la décision du 17/09/2026 ──

    [Fact]
    public void Split_AcrossMidnight_YieldsTwoSegmentsOnConsecutiveLanes()
    {
        // Un service de nuit 22 h → 06 h. Organizer le tronquait à 24 h ; on le coupe.
        var segments = RosterEngine.Split(At(22), At(30), J, RosterEngine.Day).ToList();

        Assert.Equal(2, segments.Count);
        Assert.Equal(new RosterSegment(At(22), At(24), 0), segments[0]);
        Assert.Equal(new RosterSegment(At(24), At(30), 1), segments[1]);
    }

    [Fact]
    public void Split_AcrossMidnight_PreservesTotalDuration()
    {
        var total = RosterEngine
            .Split(At(22), At(30), J, RosterEngine.Day)
            .Aggregate(TimeSpan.Zero, (sum, s) => sum + (s.End - s.Start));

        Assert.Equal(TimeSpan.FromHours(8), total);
    }

    [Fact]
    public void Split_OverSeveralDays_YieldsOneSegmentPerLane()
    {
        var segments = RosterEngine.Split(At(10), At(58), J, RosterEngine.Day).ToList();

        Assert.Equal(3, segments.Count);
        Assert.Equal([0, 1, 2], segments.Select(s => s.LaneOffset));
    }

    [Fact]
    public void Split_StartingBeforeWindow_YieldsNegativeOffset()
    {
        // TimeSpan divise vers zéro : sans plancher explicite, on obtiendrait 0 au lieu de -1.
        var segments = RosterEngine.Split(At(-3), At(2), J, RosterEngine.Day).ToList();

        Assert.Equal(2, segments.Count);
        Assert.Equal(-1, segments[0].LaneOffset);
        Assert.Equal(0, segments[1].LaneOffset);
    }

    // ── Split : ce qui ne doit rien produire ───────────────────────

    [Theory]
    [InlineData(8, 8)]
    [InlineData(17, 8)]
    public void Split_EmptyOrInvertedInterval_YieldsNothing(double start, double end)
        => Assert.Empty(RosterEngine.Split(At(start), At(end), J, RosterEngine.Day));

    [Fact]
    public void Split_NonPositiveWindow_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => RosterEngine.Split(At(0), At(1), J, TimeSpan.Zero).ToList());

    // ── Clip ───────────────────────────────────────────────────────

    [Fact]
    public void Clip_WithinWindow_IsUnchanged()
    {
        var clipped = RosterEngine.Clip(At(8), At(17), J, RosterEngine.Day);

        Assert.NotNull(clipped);
        Assert.Equal(At(8), clipped!.Value.Start);
        Assert.Equal(At(17), clipped.Value.End);
    }

    [Fact]
    public void Clip_OverflowingBothEnds_IsBoundedToWindow()
    {
        var clipped = RosterEngine.Clip(At(-5), At(30), J, RosterEngine.Day);

        Assert.NotNull(clipped);
        Assert.Equal(J, clipped!.Value.Start);
        Assert.Equal(At(24), clipped.Value.End);
    }

    [Fact]
    public void Clip_OutsideWindow_IsNull()
        => Assert.Null(RosterEngine.Clip(At(30), At(34), J, RosterEngine.Day));

    [Fact]
    public void Clip_TouchingBoundaryOnly_IsNull()
        => Assert.Null(RosterEngine.Clip(At(24), At(30), J, RosterEngine.Day));

    // ── BuildDailyLanes : les journées vides ───────────────────────

    [Fact]
    public void BuildDailyLanes_MaterializesEveryDay_IncludingEmptyOnes()
    {
        var lanes = RosterEngine.BuildDailyLanes(J, 5, d => d.ToString("dd/MM"));

        Assert.Equal(5, lanes.Count);
        Assert.Equal(J, lanes[0].WindowStart);
        Assert.Equal(J.AddDays(4), lanes[4].WindowStart);
    }

    [Fact]
    public void BuildDailyLanes_WindowStartsAreContiguous()
    {
        var lanes = RosterEngine.BuildDailyLanes(J, 10, d => d.ToString("dd/MM"));

        for (var i = 1; i < lanes.Count; i++)
        {
            Assert.Equal(RosterEngine.Day, lanes[i].WindowStart - lanes[i - 1].WindowStart);
        }
    }

    [Fact]
    public void BuildDailyLanes_IdsAreStableAndDistinct()
    {
        var lanes = RosterEngine.BuildDailyLanes(J, 30, d => d.ToString("dd/MM"));

        Assert.Equal(30, lanes.Select(l => l.Id).Distinct().Count());
        Assert.Equal("2026-09-17", lanes[0].Id);
    }

    [Fact]
    public void BuildDailyLanes_AppliesLabels()
    {
        var lanes = RosterEngine.BuildDailyLanes(J, 2, d => d.ToString("dd/MM"), d => d.DayOfWeek.ToString());

        Assert.Equal("17/09", lanes[0].Label);
        Assert.Equal("Thursday", lanes[0].SubLabel);
    }

    [Fact]
    public void BuildDailyLanes_IgnoresTimeOfDayInFirstDay()
        => Assert.Equal(J, RosterEngine.BuildDailyLanes(At(14.5), 1, _ => "")[0].WindowStart);

    [Fact]
    public void BuildDailyLanes_ZeroDays_IsEmpty()
        => Assert.Empty(RosterEngine.BuildDailyLanes(J, 0, _ => ""));

    [Fact]
    public void BuildDailyLanes_NegativeCount_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => RosterEngine.BuildDailyLanes(J, -1, _ => ""));

    // ── Le modèle notifie ──────────────────────────────────────────

    [Fact]
    public void Model_Touch_RaisesChanged()
    {
        var model = new RosterModel();
        var raised = 0;
        model.Changed += (_, _) => raised++;

        model.Lanes.Add(new RosterLane("l1", "Voie", J));
        model.Touch();

        Assert.Equal(1, raised);
    }
}
