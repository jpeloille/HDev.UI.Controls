using System.Linq;
using ProControls.Controls;
using Xunit;

namespace ProControls.Tests;

public class DockModelTests
{
    private static DockItem Item(string id) => new(id, id);

    // Représentation textuelle compacte de la topologie (pour comparer / round-trip).
    private static string Describe(DockNode? node) => node switch
    {
        null => "∅",
        DockTabGroup g => (g.IsDocumentArea ? "DOC[" : "T[") +
                          string.Join(",", g.Items.Select(i => i.Id)) + "]",
        DockSplit s => (s.Orientation == DockOrientation.Horizontal ? "H(" : "V(") +
                       string.Join(",", s.Children.Select(Describe)) + ")",
        _ => "?"
    };

    // ── Ancrage ──────────────────────────────────────────────────────────────

    [Fact]
    public void Center_OnEmpty_CreatesDocumentRoot()
    {
        var l = new DockLayout();
        l.AddToRegion(Item("doc"), DockRegion.Center);
        Assert.Equal("DOC[doc]", Describe(l.Root));
        Assert.NotNull(l.DocumentArea);
    }

    [Fact]
    public void Center_Twice_StacksAsTabs()
    {
        var l = new DockLayout();
        l.AddToRegion(Item("a"), DockRegion.Center);
        l.AddToRegion(Item("b"), DockRegion.Center);
        Assert.Equal("DOC[a,b]", Describe(l.Root));
        Assert.Equal(1, l.DocumentArea!.SelectedIndex); // dernier ajouté sélectionné
    }

    [Fact]
    public void LeftThenRight_BuildsHorizontalSplitAroundCenter()
    {
        var l = new DockLayout();
        l.AddToRegion(Item("doc"), DockRegion.Center);
        l.AddToRegion(Item("nav"), DockRegion.Left);
        l.AddToRegion(Item("props"), DockRegion.Right);
        Assert.Equal("H(T[nav],DOC[doc],T[props])", Describe(l.Root));
    }

    [Fact]
    public void Bottom_WrapsInVerticalSplit()
    {
        var l = new DockLayout();
        l.AddToRegion(Item("doc"), DockRegion.Center);
        l.AddToRegion(Item("log"), DockRegion.Bottom);
        Assert.Equal("V(DOC[doc],T[log])", Describe(l.Root));
    }

    [Fact]
    public void Proportions_AlwaysSumToOne()
    {
        var l = new DockLayout();
        l.AddToRegion(Item("doc"), DockRegion.Center);
        l.AddToRegion(Item("a"), DockRegion.Left);
        l.AddToRegion(Item("b"), DockRegion.Left);
        var split = Assert.IsType<DockSplit>(l.Root);
        Assert.Equal(1.0, split.Proportions.Sum(), precision: 9);
    }

    // ── InsertRelative ────────────────────────────────────────────────────────

    [Fact]
    public void InsertRelative_SameOrientation_ExtendsParent()
    {
        var l = new DockLayout();
        l.AddToRegion(Item("doc"), DockRegion.Center);
        l.AddToRegion(Item("nav"), DockRegion.Left);
        // Insère à droite de la zone document, orientation horizontale déjà en place.
        l.InsertRelative(l.DocumentArea!, Item("props"), DockSide.Right);
        Assert.Equal("H(T[nav],DOC[doc],T[props])", Describe(l.Root));
    }

    [Fact]
    public void InsertRelative_CrossOrientation_WrapsTarget()
    {
        var l = new DockLayout();
        l.AddToRegion(Item("doc"), DockRegion.Center);
        l.AddToRegion(Item("nav"), DockRegion.Left);
        // Ancre "log" sous la zone document (vertical) au sein d'un split horizontal.
        l.InsertRelative(l.DocumentArea!, Item("log"), DockSide.Bottom);
        Assert.Equal("H(T[nav],V(DOC[doc],T[log]))", Describe(l.Root));
    }

    [Fact]
    public void InsertRelative_Center_AddsTab()
    {
        var l = new DockLayout();
        l.AddToRegion(Item("doc"), DockRegion.Center);
        l.InsertRelative(l.DocumentArea!, Item("doc2"), DockSide.Center);
        Assert.Equal("DOC[doc,doc2]", Describe(l.Root));
    }

    // ── Retrait + élagage (l'invariant) ───────────────────────────────────────

    [Fact]
    public void Remove_LastTabOfGroup_CollapsesSingleChildSplit()
    {
        var l = new DockLayout();
        l.AddToRegion(Item("doc"), DockRegion.Center);
        l.AddToRegion(Item("nav"), DockRegion.Left);
        Assert.Equal("H(T[nav],DOC[doc])", Describe(l.Root));

        l.Remove("nav"); // le groupe "nav" se vide → split mono-enfant effondré
        Assert.Equal("DOC[doc]", Describe(l.Root));
    }

    [Fact]
    public void Remove_TabInStack_KeepsGroup()
    {
        var l = new DockLayout();
        l.AddToRegion(Item("a"), DockRegion.Center);
        l.AddToRegion(Item("b"), DockRegion.Center);
        l.Remove("a");
        Assert.Equal("DOC[b]", Describe(l.Root));
    }

    [Fact]
    public void Remove_EmptiesTree_RootBecomesNull()
    {
        var l = new DockLayout();
        l.AddToRegion(Item("doc"), DockRegion.Center);
        l.Remove("doc");
        Assert.Equal("∅", Describe(l.Root));
        Assert.False(l.Remove("doc")); // déjà absent
    }

    [Fact]
    public void Prune_FlattensNestedSameOrientationSplits()
    {
        // Construit H(nav, H(doc, props)) via insertions, puis vérifie l'aplatissement.
        var l = new DockLayout();
        l.AddToRegion(Item("doc"), DockRegion.Center);
        l.InsertRelative(l.DocumentArea!, Item("props"), DockSide.Right); // H(doc, props)
        // Ancre "nav" à gauche : étend le split horizontal existant, pas d'imbrication.
        l.AddToRegion(Item("nav"), DockRegion.Left);
        Assert.Equal("H(T[nav],DOC[doc],T[props])", Describe(l.Root));

        // Retirer "doc" ne doit laisser aucun split dégénéré.
        l.Remove("props");
        l.Remove("doc");
        Assert.Equal("T[nav]", Describe(l.Root));
    }

    [Fact]
    public void Prune_NestedCrossThenCollapse_NoDegenerateNodes()
    {
        var l = new DockLayout();
        l.AddToRegion(Item("doc"), DockRegion.Center);
        l.AddToRegion(Item("nav"), DockRegion.Left);
        l.InsertRelative(l.DocumentArea!, Item("log"), DockSide.Bottom);
        Assert.Equal("H(T[nav],V(DOC[doc],T[log]))", Describe(l.Root));

        l.Remove("log"); // V mono-enfant effondré → H(nav, doc)
        Assert.Equal("H(T[nav],DOC[doc])", Describe(l.Root));
    }

    // ── Round-trip sérialisation ──────────────────────────────────────────────

    [Fact]
    public void Serialize_RoundTrip_PreservesTopology()
    {
        var l = new DockLayout();
        l.AddToRegion(Item("doc"), DockRegion.Center);
        l.AddToRegion(Item("doc2"), DockRegion.Center);
        l.AddToRegion(Item("nav"), DockRegion.Left);
        l.AddToRegion(Item("props"), DockRegion.Right);
        l.AddToRegion(Item("log"), DockRegion.Bottom);
        l.DocumentArea!.SelectedIndex = 1;

        var json = l.ToJson();
        var back = DockLayout.FromJson(json);

        Assert.Equal(Describe(l.Root), Describe(back.Root));
    }

    [Fact]
    public void Serialize_RoundTrip_PreservesProportionsAndSelection()
    {
        var l = new DockLayout();
        l.AddToRegion(Item("doc"), DockRegion.Center);
        l.AddToRegion(Item("nav"), DockRegion.Left);
        l.DocumentArea!.IsDocumentArea = true;

        var origSplit = (DockSplit)l.Root!;
        var json = l.ToJson();
        var back = DockLayout.FromJson(json);
        var backSplit = (DockSplit)back.Root!;

        Assert.Equal(origSplit.Proportions[0], backSplit.Proportions[0], precision: 9);
        Assert.True(back.DocumentArea!.IsDocumentArea);
    }

    [Fact]
    public void Serialize_ParentPointers_RestoredOnDeserialize()
    {
        var l = new DockLayout();
        l.AddToRegion(Item("doc"), DockRegion.Center);
        l.AddToRegion(Item("nav"), DockRegion.Left);

        var back = DockLayout.FromJson(l.ToJson());
        var split = (DockSplit)back.Root!;
        Assert.Null(split.Parent);
        Assert.All(split.Children, c => Assert.Same(split, c.Parent));
        // Les items pointent vers leur groupe (réassociation contenu par Id ensuite).
        Assert.All(back.AllItems(), i => Assert.NotNull(back.Find(i.Id)));
    }
}
