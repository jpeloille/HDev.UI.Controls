using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using HDev.UI.Controls;
using Xunit;

namespace HDev.UI.Controls.Tests;

public class GanttSerializationTests
{
    // Projet exerçant TOUS les champs non-défaut (sinon « round-trip identique »
    // serait vrai par vacuité, moitié du sérialiseur cassée et test vert).
    private static GanttProject RichProject()
    {
        var p = new GanttProject { AutoSchedule = true };
        p.BeginUpdate();

        p.Calendar.WeekendDays.Clear();
        p.Calendar.WeekendDays.Add(DayOfWeek.Friday); // week-end custom (≠ Sam/Dim)
        p.Calendar.WeekendDays.Add(DayOfWeek.Saturday);
        p.Calendar.AddHoliday(new DateTime(2026, 7, 14));

        var etude = p.Add("Étude", new DateTime(2026, 7, 1), new DateTime(2026, 7, 10));
        etude.Id = "etude";
        var cahier = etude.Add("Cahier", new DateTime(2026, 7, 1), new DateTime(2026, 7, 5), 40);
        cahier.Id = "cahier";
        cahier.IsManuallyScheduled = true;
        var maquette = etude.Add("Maquette", new DateTime(2026, 7, 6), new DateTime(2026, 7, 10));
        maquette.Id = "maquette";
        // Dépendance type ≠ FinishToStart avec lag ≠ 0
        maquette.DependsOn(cahier, GanttDependencyType.StartToStart, lagDays: 2);

        var dev = p.Add("Dev", new DateTime(2026, 7, 11), new DateTime(2026, 7, 20));
        dev.Id = "dev";
        dev.IsExpanded = false; // récapitulative repliée
        var jalon = dev.AddMilestone("Go", new DateTime(2026, 7, 20));
        jalon.Id = "jalon";

        p.EndUpdate();
        p.SetBaseline(); // baseline figée sur tout l'arbre
        return p;
    }

    [Fact]
    public void RoundTrip_PreservesEveryField()
    {
        var original = RichProject();
        var back = GanttProject.FromJson(original.ToJson());

        Assert.True(back.AutoSchedule);
        Assert.Equal(
            new[] { DayOfWeek.Friday, DayOfWeek.Saturday }.OrderBy(d => d),
            back.Calendar.WeekendDays.OrderBy(d => d));
        Assert.Contains(new DateTime(2026, 7, 14), back.Calendar.Holidays);
        Assert.False(back.Calendar.IsWorkingDay(new DateTime(2026, 7, 14)));

        var cahier = back.FindById("cahier")!;
        Assert.Equal("Cahier", cahier.Name);
        Assert.Equal(new DateTime(2026, 7, 1), cahier.Start);
        Assert.Equal(new DateTime(2026, 7, 5), cahier.End);
        Assert.Equal(40, cahier.Progress);
        Assert.True(cahier.IsManuallyScheduled);
        Assert.True(cahier.HasBaseline);

        var dev = back.FindById("dev")!;
        Assert.False(dev.IsExpanded);
        Assert.True(dev.IsSummary);

        var jalon = back.FindById("jalon")!;
        Assert.True(jalon.IsMilestone);

        // Dépendance : type + lag + prédécesseur par Id
        var maquette = back.FindById("maquette")!;
        var dep = Assert.Single(maquette.Predecessors);
        Assert.Equal(GanttDependencyType.StartToStart, dep.Type);
        Assert.Equal(2, dep.LagDays);
        Assert.Equal("cahier", dep.Predecessor.Id);
    }

    [Fact]
    public void RoundTrip_TreeStructurePreserved()
    {
        var original = RichProject();
        var back = GanttProject.FromJson(original.ToJson());

        Assert.Equal(original.Tasks.Count, back.Tasks.Count);
        Assert.Equal("Étude", back.Tasks[0].Name);
        Assert.Equal(2, back.Tasks[0].Children.Count);
        Assert.Same(back, back.Tasks[0].Project);          // projet recâblé
        Assert.Same(back.Tasks[0], back.Tasks[0].Children[0].Parent); // parent recâblé
    }

    [Fact]
    public void Idempotent_TwoRoundTripsEqualJson()
    {
        var original = RichProject();
        var json1 = original.ToJson();
        var json2 = GanttProject.FromJson(json1).ToJson();
        Assert.Equal(json1, json2);
    }

    [Fact]
    public void LoadJson_InPlace_KeepsProjectReferenceAndSubscriptions()
    {
        var project = RichProject();
        var fired = 0;
        project.DataChanged += (_, _) => fired++;

        var snapshot = RichProject().ToJson();
        project.LoadJson(snapshot); // recharge en place

        Assert.True(fired > 0);                       // l'abonnement existant a bien tiré
        Assert.NotNull(project.FindById("cahier"));   // contenu remplacé
    }

    [Fact]
    public void Schema_IsStamped_AndMissingSchemaAssumedV1()
    {
        Assert.Contains("\"schema\": 1", RichProject().ToJson());

        // JSON sans champ « schema » (fichier d'avant le versionnage) : supposé v1
        var legacy = """
            { "autoSchedule": false, "calendar": { "weekend": [6, 0], "holidays": [] },
              "tasks": [ { "id": "a", "name": "A", "start": "2026-07-01", "end": "2026-07-02",
                "progress": 0, "milestone": false, "expanded": true, "manual": false,
                "baselineStart": null, "baselineEnd": null, "deps": [], "children": [] } ] }
            """;
        Assert.Equal("A", GanttProject.FromJson(legacy).FindById("a")!.Name);
    }

    [Fact]
    public void Schema_FromFuture_Refused()
    {
        var future = RichProject().ToJson()
            .Replace("\"schema\": 1", $"\"schema\": {GanttProject.SchemaVersion + 1}");
        Assert.Throws<NotSupportedException>(() => GanttProject.FromJson(future));
    }

    [Fact]
    public void UndoRedo_ViaHistoryAndLoadJson_ResetsThenReappliesMutation()
    {
        // Compose GanttHistory + LoadJson comme le fait HDevGantt (hors GUI) :
        // une mutation faite EN PLACE doit être RÉELLEMENT effacée par l'undo.
        var project = RichProject();
        var history = new GanttHistory();

        history.Record(project.ToJson());          // snapshot AVANT mutation

        var jalon = project.FindById("jalon")!;
        Assert.Empty(jalon.Predecessors);
        jalon.DependsOn(project.FindById("cahier")!); // mutation en place
        Assert.Single(project.FindById("jalon")!.Predecessors);

        // Undo → l'état revient : la dépendance ajoutée a disparu
        var restored = history.Undo(project.ToJson());
        Assert.NotNull(restored);
        project.LoadJson(restored!);
        Assert.Empty(project.FindById("jalon")!.Predecessors);

        // Redo → la mutation est réappliquée
        var redone = history.Redo(project.ToJson());
        Assert.NotNull(redone);
        project.LoadJson(redone!);
        Assert.Single(project.FindById("jalon")!.Predecessors);
    }

    [Fact]
    public void Dates_ParseUnderNonInvariantCulture()
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
            var back = GanttProject.FromJson(RichProject().ToJson());
            Assert.Equal(new DateTime(2026, 7, 1), back.FindById("cahier")!.Start);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Fact]
    public void StoredCycle_ReclassifiedNotThrown()
    {
        // Construit un JSON avec un cycle a→b→a ; le load ne doit pas lever
        // (câblage direct) et Recalculate doit le reclasser en CyclicTasks.
        var json = """
            {
              "autoSchedule": true,
              "calendar": { "weekend": [6, 0], "holidays": [] },
              "tasks": [
                { "id": "a", "name": "A", "start": "2026-07-01", "end": "2026-07-02",
                  "progress": 0, "milestone": false, "expanded": true, "manual": false,
                  "baselineStart": null, "baselineEnd": null,
                  "deps": [ { "pred": "b", "type": "FinishToStart", "lag": 0 } ], "children": [] },
                { "id": "b", "name": "B", "start": "2026-07-03", "end": "2026-07-04",
                  "progress": 0, "milestone": false, "expanded": true, "manual": false,
                  "baselineStart": null, "baselineEnd": null,
                  "deps": [ { "pred": "a", "type": "FinishToStart", "lag": 0 } ], "children": [] }
              ]
            }
            """;
        var project = GanttProject.FromJson(json);
        Assert.True(project.HasCycle);
    }
}

public class GanttHistoryTests
{
    [Fact]
    public void Record_ThenUndo_ReturnsPreviousState()
    {
        var h = new GanttHistory();
        Assert.False(h.CanUndo);
        h.Record("s0");
        Assert.True(h.CanUndo);
        Assert.Equal("s0", h.Undo("s1"));
        Assert.False(h.CanUndo);
        Assert.True(h.CanRedo);
    }

    [Fact]
    public void Redo_RestoresForwardState()
    {
        var h = new GanttHistory();
        h.Record("s0");
        var undone = h.Undo("s1");  // undo → s0, redo garde s1
        Assert.Equal("s0", undone);
        Assert.Equal("s1", h.Redo("s0")); // redo → s1
        Assert.False(h.CanRedo);
        Assert.True(h.CanUndo);
    }

    [Fact]
    public void Record_ClearsRedoStack()
    {
        var h = new GanttHistory();
        h.Record("s0");
        h.Undo("s1");
        Assert.True(h.CanRedo);
        h.Record("s1"); // nouvelle branche → redo purgé
        Assert.False(h.CanRedo);
    }

    [Fact]
    public void Record_IgnoresDuplicateTop()
    {
        var h = new GanttHistory();
        h.Record("same");
        h.Record("same"); // no-op / double-capture
        h.Undo("cur");
        Assert.False(h.CanUndo); // une seule entrée avait été poussée
    }

    [Fact]
    public void Capacity_DropsOldest()
    {
        var h = new GanttHistory(capacity: 2);
        h.Record("a");
        h.Record("b");
        h.Record("c"); // "a" évincé
        Assert.Equal("c", h.Undo("d"));
        Assert.Equal("b", h.Undo("c"));
        Assert.False(h.CanUndo); // "a" a bien été évincé
    }

    [Fact]
    public void Undo_Empty_ReturnsNull()
    {
        var h = new GanttHistory();
        Assert.Null(h.Undo("cur"));
    }
}
