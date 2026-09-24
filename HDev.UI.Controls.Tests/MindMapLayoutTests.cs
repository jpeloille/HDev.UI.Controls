using Avalonia;
using HDev.UI.Controls;
using Xunit;

namespace HDev.UI.Controls.Tests;

public class MindMapLayoutTests
{
    // Mesureur stub : tous les nœuds 100×30
    private static Size Fixed(MindMapNode _) => new(100, 30);

    private static MindMapNode Tree(int level1, int childrenEach = 0)
    {
        var root = new MindMapNode("racine");
        for (int i = 0; i < level1; i++)
        {
            var branch = root.Add($"B{i}");
            for (int j = 0; j < childrenEach; j++)
                branch.Add($"B{i}.{j}");
        }
        return root;
    }

    [Fact]
    public void Root_IsCenteredOnOrigin()
    {
        var placed = MindMapLayoutEngine.Layout(Tree(0), MindMapLayoutMode.Radial, Fixed);
        Assert.Single(placed);
        Assert.Equal(0, placed[0].Bounds.Center.X, precision: 5);
        Assert.Equal(0, placed[0].Bounds.Center.Y, precision: 5);
    }

    [Fact]
    public void TreeRight_AllNodesRightOfRoot()
    {
        var placed = MindMapLayoutEngine.Layout(Tree(4, 3), MindMapLayoutMode.TreeRight, Fixed);
        var root = placed[0];
        foreach (var node in placed.Skip(1))
        {
            Assert.True(node.Bounds.X > root.Bounds.Right);
            Assert.False(node.OnLeft);
        }
    }

    [Fact]
    public void Radial_UsesBothSides()
    {
        var placed = MindMapLayoutEngine.Layout(Tree(6), MindMapLayoutMode.Radial, Fixed);
        Assert.Contains(placed.Skip(1), p => p.OnLeft);
        Assert.Contains(placed.Skip(1), p => !p.OnLeft);
    }

    [Fact]
    public void Radial_SidesAreBalanced()
    {
        // 8 branches égales : 4 de chaque côté
        var placed = MindMapLayoutEngine.Layout(Tree(8), MindMapLayoutMode.Radial, Fixed);
        var level1 = placed.Where(p => p.Depth == 1).ToList();
        Assert.Equal(4, level1.Count(p => p.OnLeft));
        Assert.Equal(4, level1.Count(p => !p.OnLeft));
    }

    [Fact]
    public void Siblings_DoNotOverlapVertically()
    {
        var placed = MindMapLayoutEngine.Layout(Tree(5, 4), MindMapLayoutMode.TreeRight, Fixed);

        // Tout couple de nœuds de même profondeur et même côté : pas de chevauchement
        var groups = placed.Skip(1).GroupBy(p => (p.Depth, p.OnLeft, p.Node.Parent));
        foreach (var group in groups)
        {
            var sorted = group.OrderBy(p => p.Bounds.Y).ToList();
            for (int i = 1; i < sorted.Count; i++)
                Assert.True(sorted[i].Bounds.Y >= sorted[i - 1].Bounds.Bottom,
                    $"chevauchement entre {sorted[i - 1].Node.Text} et {sorted[i].Node.Text}");
        }
    }

    [Fact]
    public void Children_AreCenteredOnParent()
    {
        var root = new MindMapNode("r");
        var branch = root.Add("b");
        branch.Add("c1");
        branch.Add("c2");
        branch.Add("c3");

        var placed = MindMapLayoutEngine.Layout(root, MindMapLayoutMode.TreeRight, Fixed);
        var parent = placed.Single(p => p.Node == branch);
        var children = placed.Where(p => p.Node.Parent == branch).ToList();

        var childrenCenter = (children.Min(c => c.Bounds.Y) + children.Max(c => c.Bounds.Bottom)) / 2;
        Assert.Equal(parent.Bounds.Center.Y, childrenCenter, precision: 3);
    }

    [Fact]
    public void CollapsedBranch_HidesDescendants()
    {
        var root = Tree(3, 5);
        root.Children[0].IsExpanded = false;

        var placed = MindMapLayoutEngine.Layout(root, MindMapLayoutMode.Radial, Fixed);
        Assert.DoesNotContain(placed, p => p.Node.Parent == root.Children[0]);
        Assert.Contains(placed, p => p.Node == root.Children[0]); // le nœud replié reste
    }

    [Fact]
    public void SubtreeHeight_LeafIsOwnHeight_ParentIsStack()
    {
        var root = new MindMapNode("r");
        var branch = root.Add("b");
        branch.Add("c1");
        branch.Add("c2");

        Assert.Equal(30, MindMapLayoutEngine.SubtreeHeight(root.Children[0].Children[0], Fixed));
        // 2 enfants de 30 + 1 gap
        Assert.Equal(30 * 2 + MindMapLayoutEngine.VerticalGap,
            MindMapLayoutEngine.SubtreeHeight(branch, Fixed));
    }

    [Fact]
    public void TreeDown_AllNodesBelowRoot_NoHorizontalOverlap()
    {
        var placed = MindMapLayoutEngine.Layout(Tree(4, 3), MindMapLayoutMode.TreeDown, Fixed);
        var root = placed[0];

        foreach (var node in placed.Skip(1))
            Assert.True(node.Bounds.Y > root.Bounds.Bottom);

        // Frères d'un même parent : pas de chevauchement horizontal
        var groups = placed.Skip(1).GroupBy(p => p.Node.Parent);
        foreach (var group in groups)
        {
            var sorted = group.OrderBy(p => p.Bounds.X).ToList();
            for (int i = 1; i < sorted.Count; i++)
                Assert.True(sorted[i].Bounds.X >= sorted[i - 1].Bounds.Right);
        }
    }

    [Fact]
    public void TreeDown_ChildrenCenteredUnderParent()
    {
        var root = new MindMapNode("r");
        var branch = root.Add("b");
        branch.Add("c1");
        branch.Add("c2");
        branch.Add("c3");

        var placed = MindMapLayoutEngine.Layout(root, MindMapLayoutMode.TreeDown, Fixed);
        var parent = placed.Single(p => p.Node == branch);
        var children = placed.Where(p => p.Node.Parent == branch).ToList();

        var center = (children.Min(c => c.Bounds.X) + children.Max(c => c.Bounds.Right)) / 2;
        Assert.Equal(parent.Bounds.Center.X, center, precision: 3);
    }

    [Fact]
    public void Layout_IsDeterministic()
    {
        var root = Tree(5, 3);
        var a = MindMapLayoutEngine.Layout(root, MindMapLayoutMode.Radial, Fixed);
        var b = MindMapLayoutEngine.Layout(root, MindMapLayoutMode.Radial, Fixed);

        Assert.Equal(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++)
            Assert.Equal(a[i].Bounds, b[i].Bounds);
    }

    [Fact]
    public void IsAncestorOf_GuardsReparenting()
    {
        var root = new MindMapNode("r");
        var branch = root.Add("b");
        var leaf = branch.Add("l");

        Assert.True(branch.IsAncestorOf(leaf));
        Assert.True(branch.IsAncestorOf(branch));
        Assert.False(leaf.IsAncestorOf(branch));
        Assert.False(branch.IsAncestorOf(root));
    }

    [Fact]
    public void DescendantCount_IsRecursive()
    {
        var root = Tree(3, 2); // 3 branches × (1 + 2) = 9
        Assert.Equal(9, root.DescendantCount);
    }

    [Fact]
    public void LargeTree_LaysOutQuickly()
    {
        // 2000+ nœuds : le layout doit rester O(n·profondeur) praticable
        var root = new MindMapNode("r");
        for (int i = 0; i < 10; i++)
        {
            var b = root.Add($"B{i}");
            for (int j = 0; j < 20; j++)
            {
                var c = b.Add($"C{j}");
                for (int k = 0; k < 10; k++)
                    c.Add($"L{k}");
            }
        }

        var placed = MindMapLayoutEngine.Layout(root, MindMapLayoutMode.Radial, Fixed);
        Assert.Equal(1 + 10 + 200 + 2000, placed.Count);
    }
}
