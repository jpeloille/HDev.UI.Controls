using HDev.UI.Controls;
using Xunit;

namespace HDev.UI.Controls.Tests;

public class RosterTimeAxisTests
{
    private static RosterTimeAxis Axis(double pixelsPerHour = 60)
        => new() { PixelsPerHour = pixelsPerHour };

    // ── Ce que l'axe du Gantt ne sait pas faire ────────────────────

    [Fact]
    public void ToX_DistinguishesHoursWithinTheSameDay()
    {
        // La raison d'être du type : GanttTimeAxis.ToX tronque par .Date et rendrait 0 ici.
        var axis = Axis();

        Assert.Equal(480, axis.ToX(TimeSpan.FromHours(8)));
        Assert.Equal(870, axis.ToX(TimeSpan.FromHours(14.5)));
    }

    [Fact]
    public void PixelsPerDay_HoldsAFullRosterDay()
        => Assert.Equal(1440, Axis().PixelsPerDay);

    // ── Transformation ─────────────────────────────────────────────

    [Fact]
    public void ToOffset_IsTheInverseOfToX()
    {
        var axis = Axis(45);
        axis.ViewOffset = 137;

        var offset = TimeSpan.FromMinutes(500);

        Assert.Equal(offset.TotalMinutes, axis.ToOffset(axis.ToX(offset)).TotalMinutes, 6);
    }

    [Fact]
    public void ViewOffset_ShiftsX()
    {
        var axis = Axis();
        axis.ViewOffset = 120;

        Assert.Equal(360, axis.ToX(TimeSpan.FromHours(8)));
    }

    // ── Zoom ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(0.01, RosterTimeAxis.MinPixelsPerHour)]
    [InlineData(10000, RosterTimeAxis.MaxPixelsPerHour)]
    public void PixelsPerHour_IsClamped(double requested, double expected)
        => Assert.Equal(expected, Axis(requested).PixelsPerHour);

    [Fact]
    public void ZoomAt_KeepsThePivotInstantUnderThePivotPixel()
    {
        var axis = Axis();
        const double pivotX = 300;
        var before = axis.ToOffset(pivotX);

        axis.ZoomAt(pivotX, 1.25);

        Assert.Equal(before.TotalMinutes, axis.ToOffset(pivotX).TotalMinutes, 6);
    }

    // ── Graduations ────────────────────────────────────────────────

    [Theory]
    [InlineData(180, RosterTimeAxis.TickTier.DaysQuarters)]
    [InlineData(60, RosterTimeAxis.TickTier.DaysHours)]
    [InlineData(10, RosterTimeAxis.TickTier.DaysThreeHours)]
    [InlineData(3, RosterTimeAxis.TickTier.Days)]
    public void CurrentTier_FollowsZoom(double pixelsPerHour, RosterTimeAxis.TickTier expected)
        => Assert.Equal(expected, Axis(pixelsPerHour).CurrentTier);

    [Fact]
    public void MinorTicks_AtHourTier_YieldsEveryHourOfTheDay()
    {
        var ticks = Axis().MinorTicks(1440, RosterEngine.Day).ToList();

        Assert.Equal(25, ticks.Count); // 00 à 24 inclus, comme la frise d'origine
        Assert.Equal("00", ticks[0].Label);
        Assert.Equal("08", ticks[8].Label);
    }

    [Fact]
    public void MinorTicks_AreBoundedToTheLaneSpan()
    {
        var ticks = Axis().MinorTicks(5000, RosterEngine.Day).ToList();

        Assert.All(ticks, t => Assert.InRange(t.Offset, TimeSpan.Zero, RosterEngine.Day));
    }

    [Fact]
    public void MinorTicks_NeverYieldNegativeOffsets()
    {
        var axis = Axis();
        axis.ViewOffset = -500;

        Assert.All(axis.MinorTicks(1440, RosterEngine.Day), t => Assert.True(t.Offset >= TimeSpan.Zero));
    }

    [Fact]
    public void MajorTicks_YieldOneEntryPerDay()
    {
        var ticks = Axis(6).MajorTicks(2000, TimeSpan.FromDays(5)).ToList();

        Assert.Equal(6, ticks.Count); // J+0 à J+5
        Assert.Equal("J+0", ticks[0].Label);
    }

    [Fact]
    public void LabelFormatter_OverridesTheDefault()
    {
        var axis = Axis();
        axis.LabelFormatter = o => $"{o.TotalHours:0}h";

        Assert.Equal("8h", axis.MinorTicks(1440, RosterEngine.Day).ElementAt(8).Label);
    }
}
