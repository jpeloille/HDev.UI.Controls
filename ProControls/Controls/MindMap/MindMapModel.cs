using Avalonia;
using Avalonia.Media;
using System.Collections.ObjectModel;

namespace ProControls.Controls;

/// <summary>Disposition de la carte</summary>
public enum MindMapLayoutMode
{
    /// <summary>Racine au centre, branches équilibrées gauche/droite (mind map classique)</summary>
    Radial,
    /// <summary>Tout à droite de la racine (organigramme couché)</summary>
    TreeRight,
    /// <summary>Sous la racine, frères ordonnés horizontalement (organigramme vertical)</summary>
    TreeDown
}

/// <summary>
/// Nœud d'une carte mentale : texte, enfants, repli, couleur de branche
/// (posée sur les branches de niveau 1, héritée en dessous)
/// </summary>
public class MindMapNode
{
    private string _text = "";
    private bool _isExpanded = true;

    internal ProMindMap? Owner { get; set; }
    public MindMapNode? Parent { get; internal set; }

    public ObservableCollection<MindMapNode> Children { get; } = new();

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value) return;
            _text = value;
            InvalidateMeasureCache();
            Owner?.OnStructureChanged();
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            Owner?.OnStructureChanged();
        }
    }

    /// <summary>Couleur de branche (niveau 1) ; null = héritée du parent / palette auto</summary>
    public Color? BranchColor { get; set; }

    public object? Tag { get; set; }

    public bool HasChildren => Children.Count > 0;

    /// <summary>Nombre de descendants (indicateur de repli)</summary>
    public int DescendantCount
    {
        get
        {
            var count = 0;
            foreach (var child in Children)
                count += 1 + child.DescendantCount;
            return count;
        }
    }

    // Cache de mesure du texte (milliers de nœuds : mesurer une fois, pas à
    // chaque layout/rendu ; invalidé sur changement de texte ou de variante)
    internal object? CachedTextLayout;
    internal Size CachedSize;
    internal int CacheGeneration = -1;

    internal void InvalidateMeasureCache()
    {
        CachedTextLayout = null;
        CacheGeneration = -1;
    }

    public MindMapNode()
    {
        Children.CollectionChanged += (s, e) =>
        {
            foreach (var child in Children)
            {
                child.Parent = this;
                child.SetOwner(Owner);
            }
            Owner?.OnStructureChanged();
        };
    }

    public MindMapNode(string text) : this() => _text = text;

    /// <summary>Ajoute un enfant (raccourci fluent)</summary>
    public MindMapNode Add(string text)
    {
        var node = new MindMapNode(text);
        Children.Add(node);
        return node;
    }

    internal void SetOwner(ProMindMap? owner)
    {
        Owner = owner;
        foreach (var child in Children)
            child.SetOwner(owner);
    }

    /// <summary>Vrai si other est ce nœud ou un de ses descendants (garde de re-parentage)</summary>
    public bool IsAncestorOf(MindMapNode other)
    {
        var current = other;
        while (current != null)
        {
            if (ReferenceEquals(current, this)) return true;
            current = current.Parent;
        }
        return false;
    }
}

/// <summary>Nœud posé par le moteur de layout (coordonnées monde)</summary>
public readonly record struct MindMapPlacedNode(MindMapNode Node, Rect Bounds, int Depth, bool OnLeft);

/// <summary>
/// Moteur de layout de carte mentale : hauteurs de sous-arbres bottom-up,
/// empilement vertical sans chevauchement, équilibrage gauche/droite par
/// hauteurs (mode radial). Logique pure — le mesureur de nœud est injecté.
/// </summary>
public static class MindMapLayoutEngine
{
    public const double HorizontalGap = 44;
    public const double VerticalGap = 10;

    /// <summary>
    /// Pose l'arbre : la racine est centrée sur (0,0). measure fournit la
    /// taille de chaque nœud (texte mesuré côté UI, stub côté tests).
    /// </summary>
    public static List<MindMapPlacedNode> Layout(MindMapNode root,
        MindMapLayoutMode mode, Func<MindMapNode, Size> measure)
    {
        var result = new List<MindMapPlacedNode>();
        var rootSize = measure(root);
        var rootRect = new Rect(-rootSize.Width / 2, -rootSize.Height / 2,
            rootSize.Width, rootSize.Height);
        result.Add(new MindMapPlacedNode(root, rootRect, 0, false));

        if (!root.IsExpanded || root.Children.Count == 0)
            return result;

        if (mode == MindMapLayoutMode.TreeRight)
        {
            LayoutSide(result, root.Children.ToList(), rootRect, measure, onLeft: false, depth: 1);
        }
        else if (mode == MindMapLayoutMode.TreeDown)
        {
            LayoutDown(result, root.Children.ToList(), rootRect, measure, depth: 1);
        }
        else
        {
            // Répartition équilibrée par hauteurs de sous-arbres (greedy sur
            // les plus hauts d'abord, en préservant l'ordre d'origine par côté)
            var heights = root.Children.ToDictionary(c => c, c => SubtreeHeight(c, measure));
            var ordered = root.Children.OrderByDescending(c => heights[c]).ToList();

            double leftTotal = 0, rightTotal = 0;
            var side = new Dictionary<MindMapNode, bool>(); // true = gauche
            foreach (var child in ordered)
            {
                var toLeft = leftTotal < rightTotal;
                side[child] = toLeft;
                if (toLeft) leftTotal += heights[child] + VerticalGap;
                else rightTotal += heights[child] + VerticalGap;
            }

            var rightChildren = root.Children.Where(c => !side[c]).ToList();
            var leftChildren = root.Children.Where(c => side[c]).ToList();

            LayoutSide(result, rightChildren, rootRect, measure, onLeft: false, depth: 1);
            LayoutSide(result, leftChildren, rootRect, measure, onLeft: true, depth: 1);
        }

        return result;
    }

    /// <summary>Hauteur totale du sous-arbre (nœud + enfants dépliés empilés)</summary>
    public static double SubtreeHeight(MindMapNode node, Func<MindMapNode, Size> measure)
    {
        var own = measure(node).Height;
        if (!node.IsExpanded || node.Children.Count == 0)
            return own;

        double children = 0;
        foreach (var child in node.Children)
            children += SubtreeHeight(child, measure) + VerticalGap;
        children -= VerticalGap;

        return Math.Max(own, children);
    }

    /// <summary>Largeur totale du sous-arbre (mode TreeDown : frères côte à côte)</summary>
    public static double SubtreeWidth(MindMapNode node, Func<MindMapNode, Size> measure)
    {
        var own = measure(node).Width;
        if (!node.IsExpanded || node.Children.Count == 0)
            return own;

        double children = 0;
        foreach (var child in node.Children)
            children += SubtreeWidth(child, measure) + SiblingGap;
        children -= SiblingGap;

        return Math.Max(own, children);
    }

    public const double SiblingGap = 16;
    public const double LevelGap = 44;

    private static void LayoutDown(List<MindMapPlacedNode> result,
        List<MindMapNode> children, Rect parentRect,
        Func<MindMapNode, Size> measure, int depth)
    {
        if (children.Count == 0) return;

        double total = 0;
        foreach (var child in children)
            total += SubtreeWidth(child, measure) + SiblingGap;
        total -= SiblingGap;

        var x = parentRect.Center.X - total / 2;
        var y = parentRect.Bottom + LevelGap;

        foreach (var child in children)
        {
            var width = SubtreeWidth(child, measure);
            var size = measure(child);

            var rect = new Rect(x + width / 2 - size.Width / 2, y, size.Width, size.Height);
            result.Add(new MindMapPlacedNode(child, rect, depth, false));

            if (child.IsExpanded && child.Children.Count > 0)
                LayoutDown(result, child.Children.ToList(), rect, measure, depth + 1);

            x += width + SiblingGap;
        }
    }

    private static void LayoutSide(List<MindMapPlacedNode> result,
        List<MindMapNode> children, Rect parentRect,
        Func<MindMapNode, Size> measure, bool onLeft, int depth)
    {
        if (children.Count == 0) return;

        double total = 0;
        foreach (var child in children)
            total += SubtreeHeight(child, measure) + VerticalGap;
        total -= VerticalGap;

        var y = parentRect.Center.Y - total / 2;

        foreach (var child in children)
        {
            var height = SubtreeHeight(child, measure);
            var size = measure(child);

            var x = onLeft
                ? parentRect.X - HorizontalGap - size.Width
                : parentRect.Right + HorizontalGap;

            var rect = new Rect(x, y + height / 2 - size.Height / 2, size.Width, size.Height);
            result.Add(new MindMapPlacedNode(child, rect, depth, onLeft));

            if (child.IsExpanded && child.Children.Count > 0)
                LayoutSide(result, child.Children.ToList(), rect, measure, onLeft, depth + 1);

            y += height + VerticalGap;
        }
    }
}

/// <summary>Clone profond (instantanés d'undo du ProMindMap)</summary>
internal static class MindMapCloner
{
    public static MindMapNode Clone(MindMapNode node)
    {
        var clone = new MindMapNode(node.Text)
        {
            BranchColor = node.BranchColor,
            Tag = node.Tag,
            IsExpanded = node.IsExpanded
        };
        foreach (var child in node.Children)
            clone.Children.Add(Clone(child));
        return clone;
    }
}
