using System;
using System.Linq;
using ProControls.Controls;
using Xunit;

namespace ProControls.Tests;

public class GanttStructureTests
{
    private static readonly DateTime D0 = new(2026, 7, 1);

    // Plan : A, B(B1, B2), C
    private static GanttProject Plan()
    {
        var p = new GanttProject();
        p.BeginUpdate();
        var a = p.Add("A", D0, D0.AddDays(2));
        a.Id = "A";
        var b = p.Add("B", D0, D0.AddDays(5));
        b.Id = "B";
        var b1 = b.Add("B1", D0, D0.AddDays(2)); b1.Id = "B1";
        var b2 = b.Add("B2", D0.AddDays(3), D0.AddDays(5)); b2.Id = "B2";
        var c = p.Add("C", D0.AddDays(6), D0.AddDays(8));
        c.Id = "C";
        p.EndUpdate();
        return p;
    }

    // Topologie compacte : "A,B[B1,B2],C"
    private static string Describe(GanttProject p) =>
        string.Join(",", p.Tasks.Select(Describe));

    private static string Describe(GanttTask t) =>
        t.Children.Count == 0
            ? t.Name
            : $"{t.Name}[{string.Join(",", t.Children.Select(Describe))}]";

    [Fact]
    public void Plan_BaselineTopology()
        => Assert.Equal("A,B[B1,B2],C", Describe(Plan()));

    // ── Insertion ────────────────────────────────────────────────────────────

    [Fact]
    public void InsertAfter_AddsSiblingRightAfter()
    {
        var p = Plan();
        var created = p.InsertAfter(p.FindById("A")!, "X");
        Assert.Equal("A,X,B[B1,B2],C", Describe(p));
        Assert.Null(created.Parent);
        Assert.Same(p, created.Project);
    }

    [Fact]
    public void InsertAfter_InsideSummary_StaysInSummary()
    {
        var p = Plan();
        p.InsertAfter(p.FindById("B1")!, "X");
        Assert.Equal("A,B[B1,X,B2],C", Describe(p));
        Assert.Same(p.FindById("B"), p.Tasks[1].Children[1].Parent);
    }

    [Fact]
    public void InsertRoot_AppendsAtEnd()
    {
        var p = Plan();
        p.InsertRoot("X");
        Assert.Equal("A,B[B1,B2],C,X", Describe(p));
    }

    // ── Suppression + purge des dépendances (l'invariant) ────────────────────

    [Fact]
    public void Remove_Leaf_DropsIt()
    {
        var p = Plan();
        Assert.True(p.Remove(p.FindById("B1")!));
        Assert.Equal("A,B[B2],C", Describe(p));
        Assert.Null(p.FindById("B1"));
    }

    [Fact]
    public void Remove_Summary_DropsWholeSubtree()
    {
        var p = Plan();
        Assert.True(p.Remove(p.FindById("B")!));
        Assert.Equal("A,C", Describe(p));
        Assert.Null(p.FindById("B1"));
        Assert.Null(p.FindById("B2"));
    }

    [Fact]
    public void Remove_PurgesDependenciesTargetingIt()
    {
        var p = Plan();
        p.FindById("C")!.DependsOn(p.FindById("A")!);
        Assert.Single(p.FindById("C")!.Predecessors);

        p.Remove(p.FindById("A")!);

        // INVARIANT : plus aucune dépendance ne vise une tâche retirée
        Assert.Empty(p.FindById("C")!.Predecessors);
    }

    [Fact]
    public void Remove_Summary_PurgesDependenciesTargetingItsDescendants()
    {
        var p = Plan();
        p.FindById("C")!.DependsOn(p.FindById("B2")!); // vise un ENFANT de B
        Assert.Single(p.FindById("C")!.Predecessors);

        p.Remove(p.FindById("B")!); // supprime le parent

        Assert.Empty(p.FindById("C")!.Predecessors);
        Assert.DoesNotContain(p.AllTasks().SelectMany(t => t.Predecessors),
            d => d.Predecessor.Name is "B" or "B1" or "B2");
    }

    [Fact]
    public void Remove_Absent_ReturnsFalse()
    {
        var p = Plan();
        var orphan = new GanttTask("Orphelin", D0, D0);
        Assert.False(p.Remove(orphan));
    }

    // ── Indentation ──────────────────────────────────────────────────────────

    [Fact]
    public void Indent_MakesPreviousSiblingASummary()
    {
        var p = Plan();
        Assert.True(p.CanIndent(p.FindById("B")!));
        Assert.True(p.Indent(p.FindById("B")!));

        Assert.Equal("A[B[B1,B2]],C", Describe(p));
        var a = p.FindById("A")!;
        Assert.True(a.IsSummary);
        Assert.Same(a, p.FindById("B")!.Parent);
    }

    [Fact]
    public void Indent_FirstSibling_Refused()
    {
        var p = Plan();
        Assert.False(p.CanIndent(p.FindById("A")!));
        Assert.False(p.Indent(p.FindById("A")!));
        Assert.Equal("A,B[B1,B2],C", Describe(p));
    }

    [Fact]
    public void Indent_ExpandsNewParent_SoTaskStaysVisible()
    {
        var p = Plan();
        p.FindById("A")!.IsExpanded = false;
        p.Indent(p.FindById("B")!);
        Assert.True(p.FindById("A")!.IsExpanded);
    }

    [Fact]
    public void Indent_RollsUpSummaryDates()
    {
        var p = Plan();
        p.Indent(p.FindById("B")!); // A devient récapitulative de B
        var a = p.FindById("A")!;
        // A couvrait 01→03, B couvre 01→06 : le roll-up doit s'étendre
        Assert.Equal(D0.AddDays(5), a.EffectiveEnd);
    }

    [Fact]
    public void Outdent_MovesAfterFormerParent()
    {
        var p = Plan();
        Assert.True(p.CanOutdent(p.FindById("B1")!));
        Assert.True(p.Outdent(p.FindById("B1")!));

        Assert.Equal("A,B[B2],B1,C", Describe(p));
        Assert.Null(p.FindById("B1")!.Parent);
    }

    [Fact]
    public void Outdent_Root_Refused()
    {
        var p = Plan();
        Assert.False(p.CanOutdent(p.FindById("A")!));
        Assert.False(p.Outdent(p.FindById("A")!));
    }

    [Fact]
    public void Outdent_CarriesItsSubtree()
    {
        var p = Plan();
        p.Indent(p.FindById("B2")!);      // B[B1[B2]]
        Assert.Equal("A,B[B1[B2]],C", Describe(p));
        p.Outdent(p.FindById("B1")!);     // B1 (avec B2) remonte après B
        Assert.Equal("A,B,B1[B2],C", Describe(p));
    }

    [Fact]
    public void IndentThenOutdent_RestoresTopology()
    {
        var p = Plan();
        p.Indent(p.FindById("B")!);
        p.Outdent(p.FindById("B")!);
        Assert.Equal("A,B[B1,B2],C", Describe(p));
    }

    // ── Réordonnancement ─────────────────────────────────────────────────────

    [Fact]
    public void MoveUp_SwapsWithPreviousSibling()
    {
        var p = Plan();
        Assert.True(p.MoveUp(p.FindById("B")!));
        Assert.Equal("B[B1,B2],A,C", Describe(p));
    }

    [Fact]
    public void MoveDown_SwapsWithNextSibling()
    {
        var p = Plan();
        Assert.True(p.MoveDown(p.FindById("A")!));
        Assert.Equal("B[B1,B2],A,C", Describe(p));
    }

    [Fact]
    public void Move_AtBounds_Refused()
    {
        var p = Plan();
        Assert.False(p.CanMoveUp(p.FindById("A")!));
        Assert.False(p.MoveUp(p.FindById("A")!));
        Assert.False(p.CanMoveDown(p.FindById("C")!));
        Assert.False(p.MoveDown(p.FindById("C")!));
        Assert.Equal("A,B[B1,B2],C", Describe(p));
    }

    [Fact]
    public void Move_StaysWithinSiblings()
    {
        var p = Plan();
        Assert.False(p.CanMoveUp(p.FindById("B1")!)); // 1er enfant : pas de sortie du parent
        Assert.True(p.MoveDown(p.FindById("B1")!));
        Assert.Equal("A,B[B2,B1],C", Describe(p));
    }

    // ── Atomicité des notifications ──────────────────────────────────────────

    [Fact]
    public void Operation_RaisesDataChanged_Once()
    {
        var p = Plan();
        var fired = 0;
        p.DataChanged += (_, _) => fired++;

        p.Indent(p.FindById("B")!);

        Assert.Equal(1, fired); // un seul Recalculate malgré remove+add interne
    }

    [Fact]
    public void Operation_InsideAppBeginUpdate_StaysSilent()
    {
        // Scope réentrant : une opération ne doit pas lever un BeginUpdate applicatif
        var p = Plan();
        var fired = 0;
        p.DataChanged += (_, _) => fired++;

        p.BeginUpdate();
        p.Indent(p.FindById("B")!);
        p.MoveUp(p.FindById("C")!);
        Assert.Equal(0, fired); // toujours suspendu
        p.EndUpdate();

        Assert.Equal(1, fired); // une seule notification, au relâchement
    }
}
