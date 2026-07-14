using ProControls.Controls;
using Xunit;

namespace ProControls.Tests;

public class GanttSchedulerTests
{
    // Repère : lundi 13/07/2026
    private static readonly DateTime Mon = new(2026, 7, 13);

    private static GanttProject NewProject(bool autoSchedule = true)
        => new() { AutoSchedule = autoSchedule };

    // ── Propagation FS ─────────────────────────────────────────────

    [Fact]
    public void FS_SuccessorStartsNextWorkingDay()
    {
        var project = NewProject();
        var a = project.Add("A", Mon, Mon.AddDays(2));            // lun→mer
        var b = project.Add("B", Mon, Mon.AddDays(1));            // durée 2 j ouvrés
        b.DependsOn(a);

        Assert.Equal(Mon.AddDays(3), b.Start);                     // jeudi
        Assert.Equal(Mon.AddDays(4), b.End);                       // vendredi (durée préservée)
    }

    [Fact]
    public void FS_SkipsWeekend()
    {
        var project = NewProject();
        var a = project.Add("A", Mon, Mon.AddDays(4));             // lun→ven
        var b = project.Add("B", Mon, Mon);                        // 1 j
        b.DependsOn(a);

        Assert.Equal(Mon.AddDays(7), b.Start);                     // lundi suivant
    }

    [Fact]
    public void FS_WithLag_AddsWorkingDays()
    {
        var project = NewProject();
        var a = project.Add("A", Mon, Mon);                        // lundi
        var b = project.Add("B", Mon, Mon);
        b.DependsOn(a, lagDays: 2);

        // fin lundi -> +1+2 = jeudi
        Assert.Equal(Mon.AddDays(3), b.Start);
    }

    [Fact]
    public void FS_ChainPropagatesTransitively()
    {
        var project = NewProject();
        var a = project.Add("A", Mon, Mon.AddDays(1));
        var b = project.Add("B", Mon, Mon.AddDays(1));
        var c = project.Add("C", Mon, Mon.AddDays(1));
        b.DependsOn(a);
        c.DependsOn(b);

        // A: lun-mar ; B: mer-jeu ; C: ven-lun
        Assert.Equal(Mon.AddDays(2), b.Start);
        Assert.Equal(Mon.AddDays(4), c.Start);
        Assert.Equal(Mon.AddDays(7), c.End);                       // saute le week-end
    }

    [Fact]
    public void MultiplePredecessors_LatestWins()
    {
        var project = NewProject();
        var a = project.Add("A", Mon, Mon.AddDays(1));             // finit mardi
        var b = project.Add("B", Mon, Mon.AddDays(8));             // finit mardi suivant
        var c = project.Add("C", Mon, Mon);
        c.DependsOn(a);
        c.DependsOn(b);

        Assert.Equal(Mon.AddDays(9), c.Start);                     // après B
    }

    // ── Autres types de liens ──────────────────────────────────────

    [Fact]
    public void SS_AlignsStarts()
    {
        var project = NewProject();
        var a = project.Add("A", Mon.AddDays(7), Mon.AddDays(10));
        var b = project.Add("B", Mon, Mon.AddDays(1));
        b.DependsOn(a, GanttDependencyType.StartToStart);

        Assert.Equal(a.Start, b.Start);
    }

    [Fact]
    public void FF_AlignsEnds_PreservingDuration()
    {
        var project = NewProject();
        var a = project.Add("A", Mon, Mon.AddDays(4));             // finit vendredi
        var b = project.Add("B", Mon, Mon.AddDays(1));             // 2 j ouvrés
        b.DependsOn(a, GanttDependencyType.FinishToFinish);

        Assert.Equal(Mon.AddDays(4), b.End);                       // vendredi
        Assert.Equal(Mon.AddDays(3), b.Start);                     // jeudi
    }

    [Fact]
    public void Milestone_IsScheduledAtConstraint()
    {
        var project = NewProject();
        var a = project.Add("A", Mon, Mon.AddDays(2));
        var jalon = project.Add("J", Mon, Mon);
        jalon.IsMilestone = true;
        jalon.DependsOn(a);

        Assert.Equal(Mon.AddDays(3), jalon.Start);
        Assert.Equal(jalon.Start, jalon.End);
    }

    // ── Modes ──────────────────────────────────────────────────────

    [Fact]
    public void AutoScheduleOff_DatesUntouched()
    {
        var project = NewProject(autoSchedule: false);
        var a = project.Add("A", Mon, Mon.AddDays(2));
        var b = project.Add("B", Mon, Mon.AddDays(1));
        b.DependsOn(a);

        Assert.Equal(Mon, b.Start);                                 // pas bougé
    }

    [Fact]
    public void ManuallyScheduledTask_IsNotMoved()
    {
        var project = NewProject();
        var a = project.Add("A", Mon, Mon.AddDays(2));
        var b = project.Add("B", Mon, Mon.AddDays(1));
        b.IsManuallyScheduled = true;
        b.DependsOn(a);

        Assert.Equal(Mon, b.Start);
    }

    [Fact]
    public void TogglingAutoSchedule_Reschedules()
    {
        var project = NewProject(autoSchedule: false);
        var a = project.Add("A", Mon, Mon.AddDays(2));
        var b = project.Add("B", Mon, Mon);
        b.DependsOn(a);
        Assert.Equal(Mon, b.Start);

        project.AutoSchedule = true;
        Assert.Equal(Mon.AddDays(3), b.Start);
    }

    // ── Cycles ─────────────────────────────────────────────────────

    [Fact]
    public void DependsOn_Self_Throws()
    {
        var project = NewProject();
        var a = project.Add("A", Mon, Mon);
        Assert.Throws<InvalidOperationException>(() => a.DependsOn(a));
    }

    [Fact]
    public void DependsOn_DirectCycle_Throws()
    {
        var project = NewProject();
        var a = project.Add("A", Mon, Mon);
        var b = project.Add("B", Mon, Mon);
        b.DependsOn(a);
        Assert.Throws<InvalidOperationException>(() => a.DependsOn(b));
    }

    [Fact]
    public void DependsOn_TransitiveCycle_Throws()
    {
        var project = NewProject();
        var a = project.Add("A", Mon, Mon);
        var b = project.Add("B", Mon, Mon);
        var c = project.Add("C", Mon, Mon);
        b.DependsOn(a);
        c.DependsOn(b);
        Assert.Throws<InvalidOperationException>(() => a.DependsOn(c));
    }

    [Fact]
    public void WouldCreateCycle_FalseForValidLink()
    {
        var project = NewProject();
        var a = project.Add("A", Mon, Mon);
        var b = project.Add("B", Mon, Mon);
        b.DependsOn(a);

        var c = project.Add("C", Mon, Mon);
        Assert.False(GanttScheduler.WouldCreateCycle(b, c));
    }

    // ── Chemin critique ────────────────────────────────────────────

    [Fact]
    public void Diamond_LongBranchIsCritical_ShortHasFloat()
    {
        // A -> B(long) -> D ; A -> C(court) -> D
        var project = NewProject();
        var a = project.Add("A", Mon, Mon.AddDays(1));              // 2 j
        var b = project.Add("B", Mon, Mon);
        var c = project.Add("C", Mon, Mon);
        var d = project.Add("D", Mon, Mon);
        b.DependsOn(a);
        c.DependsOn(a);
        d.DependsOn(b);
        d.DependsOn(c);

        b.End = b.Start.AddDays(8);                                 // longue (~7 j ouvrés)
        // c reste 1 jour -> marge

        Assert.True(a.IsCritical);
        Assert.True(b.IsCritical);
        Assert.True(d.IsCritical);
        Assert.False(c.IsCritical);
        Assert.True(c.TotalFloatDays > 0);
        Assert.Equal(0, b.TotalFloatDays);
    }

    [Fact]
    public void IndependentEarlyTask_HasFloat()
    {
        var project = NewProject();
        project.Add("Long", Mon, Mon.AddDays(18));
        var small = project.Add("Petit", Mon, Mon);                 // finit très tôt

        Assert.False(small.IsCritical);
        Assert.True(small.TotalFloatDays > 0);
    }

    [Fact]
    public void Summary_IsCritical_WhenAnyChildIs()
    {
        var project = NewProject();
        var phase = project.Add("Phase", Mon, Mon);
        var t1 = phase.Add("T1", Mon, Mon.AddDays(4));
        var t2 = phase.Add("T2", Mon, Mon);                         // marge

        Assert.True(t1.IsCritical);
        Assert.False(t2.IsCritical);
        Assert.True(phase.IsCritical);
    }

    [Fact]
    public void CriticalPath_RespectsManualGaps()
    {
        // B posée manuellement bien après la fin de A : A gagne de la marge
        var project = NewProject(autoSchedule: false);
        var a = project.Add("A", Mon, Mon.AddDays(1));
        var b = project.Add("B", Mon.AddDays(14), Mon.AddDays(15));
        b.DependsOn(a);

        Assert.False(a.IsCritical);
        Assert.True(a.TotalFloatDays > 0);
        Assert.True(b.IsCritical);
    }
}
