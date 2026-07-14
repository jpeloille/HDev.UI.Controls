using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ProControls.Theme;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ProControls.Controls;

/// <summary>
/// Nœud d'un ProTreeView
/// </summary>
public class ProTreeNode
{
    private string _text = "";
    private string? _icon;
    private bool _isExpanded;

    internal ProTreeView? Owner { get; set; }
    public ProTreeNode? Parent { get; internal set; }

    public ObservableCollection<ProTreeNode> Children { get; } = new();

    public string Text
    {
        get => _text;
        set { _text = value; Owner?.InvalidateVisual(); }
    }

    /// <summary>Icône emoji/unicode optionnelle</summary>
    public string? Icon
    {
        get => _icon;
        set { _icon = value; Owner?.InvalidateVisual(); }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            Owner?.OnNodeExpandChanged(this);
        }
    }

    public bool HasChildren => Children.Count > 0;

    public object? Tag { get; set; }

    public ProTreeNode()
    {
        Children.CollectionChanged += (s, e) =>
        {
            foreach (var child in Children)
            {
                child.Parent = this;
                child.SetOwner(Owner);
            }
            Owner?.RefreshLayout();
        };
    }

    public ProTreeNode(string text, string? icon = null) : this()
    {
        _text = text;
        _icon = icon;
    }

    /// <summary>Ajoute un enfant (raccourci fluent)</summary>
    public ProTreeNode Add(string text, string? icon = null)
    {
        var node = new ProTreeNode(text, icon);
        Children.Add(node);
        return node;
    }

    internal void SetOwner(ProTreeView? owner)
    {
        Owner = owner;
        foreach (var child in Children)
            child.SetOwner(owner);
    }
}

/// <summary>
/// Arborescence rendue custom (équivalent TreeView/TreeList sans colonnes) :
/// expand/collapse, sélection, navigation clavier complète.
/// Hauteur au contenu : placer dans un ScrollViewer pour défiler.
/// </summary>
public class ProTreeView : Control
{
    private const double RowHeight = 26;
    private const double IndentWidth = 18;

    private ProTreeNode? _selectedNode;
    private ProTreeNode? _hoveredNode;
    private List<(ProTreeNode Node, int Depth)> _visibleRows = new();

    public ObservableCollection<ProTreeNode> Nodes { get; } = new();

    /// <summary>Déclenché quand la sélection change</summary>
    public event EventHandler? SelectedNodeChanged;

    /// <summary>Déclenché quand un nœud est développé</summary>
    public event EventHandler<ProTreeNode>? NodeExpanded;

    /// <summary>Déclenché quand un nœud est replié</summary>
    public event EventHandler<ProTreeNode>? NodeCollapsed;

    /// <summary>Déclenché au double-clic sur un nœud</summary>
    public event EventHandler<ProTreeNode>? NodeDoubleClicked;

    public ProTreeNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (ReferenceEquals(_selectedNode, value)) return;
            _selectedNode = value;
            InvalidateVisual();
            SelectedNodeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    static ProTreeView()
    {
        FocusableProperty.OverrideDefaultValue<ProTreeView>(true);
    }

    public ProTreeView()
    {
        ClipToBounds = true;

        Nodes.CollectionChanged += (s, e) =>
        {
            foreach (var node in Nodes)
            {
                node.Parent = null;
                node.SetOwner(this);
            }
            RefreshLayout();
        };
    }

    /// <summary>Ajoute un nœud racine (raccourci fluent)</summary>
    public ProTreeNode Add(string text, string? icon = null)
    {
        var node = new ProTreeNode(text, icon);
        Nodes.Add(node);
        return node;
    }

    public void ExpandAll() => SetExpandedRecursive(Nodes, true);
    public void CollapseAll() => SetExpandedRecursive(Nodes, false);

    private static void SetExpandedRecursive(IEnumerable<ProTreeNode> nodes, bool expanded)
    {
        foreach (var node in nodes)
        {
            if (node.HasChildren)
            {
                node.IsExpanded = expanded;
                SetExpandedRecursive(node.Children, expanded);
            }
        }
    }

    internal void OnNodeExpandChanged(ProTreeNode node)
    {
        RefreshLayout();
        if (node.IsExpanded)
            NodeExpanded?.Invoke(this, node);
        else
            NodeCollapsed?.Invoke(this, node);
    }

    // ═══════════════════════════════════════════════════════════════
    // APLATISSEMENT DES NŒUDS VISIBLES
    // ═══════════════════════════════════════════════════════════════

    internal void RefreshLayout()
    {
        _visibleRows = new List<(ProTreeNode, int)>();
        Flatten(Nodes, 0);
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void Flatten(IEnumerable<ProTreeNode> nodes, int depth)
    {
        foreach (var node in nodes)
        {
            _visibleRows.Add((node, depth));
            if (node.IsExpanded && node.HasChildren)
                Flatten(node.Children, depth + 1);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 240 : availableSize.Width;
        return new Size(width, Math.Max(RowHeight, _visibleRows.Count * RowHeight));
    }

    private (ProTreeNode Node, int Depth, Rect Rect)? RowAt(Point pos)
    {
        var index = (int)(pos.Y / RowHeight);
        if (index < 0 || index >= _visibleRows.Count) return null;

        var (node, depth) = _visibleRows[index];
        return (node, depth, new Rect(0, index * RowHeight, Bounds.Width, RowHeight));
    }

    private static Rect ChevronRect(int depth, double rowY)
        => new(4 + depth * IndentWidth, rowY + (RowHeight - 14) / 2, 14, 14);

    // ═══════════════════════════════════════════════════════════════
    // SOURIS
    // ═══════════════════════════════════════════════════════════════

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var row = RowAt(e.GetPosition(this));
        var node = row?.Node;
        if (!ReferenceEquals(node, _hoveredNode))
        {
            _hoveredNode = node;
            InvalidateVisual();
        }
        base.OnPointerMoved(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _hoveredNode = null;
        InvalidateVisual();
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var pos = e.GetPosition(this);
        var row = RowAt(pos);
        if (row == null)
        {
            base.OnPointerPressed(e);
            return;
        }

        Focus();
        var (node, depth, rect) = row.Value;

        // Clic sur le chevron : toggle sans changer la sélection
        if (node.HasChildren && ChevronRect(depth, rect.Y).Inflate(2).Contains(pos))
        {
            node.IsExpanded = !node.IsExpanded;
            e.Handled = true;
            return;
        }

        SelectedNode = node;

        if (e.ClickCount == 2)
        {
            if (node.HasChildren)
                node.IsExpanded = !node.IsExpanded;
            NodeDoubleClicked?.Invoke(this, node);
        }

        e.Handled = true;
        base.OnPointerPressed(e);
    }

    // ═══════════════════════════════════════════════════════════════
    // CLAVIER
    // ═══════════════════════════════════════════════════════════════

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_visibleRows.Count == 0)
        {
            base.OnKeyDown(e);
            return;
        }

        var index = _selectedNode != null
            ? _visibleRows.FindIndex(r => ReferenceEquals(r.Node, _selectedNode))
            : -1;

        switch (e.Key)
        {
            case Key.Up:
                SelectAt(Math.Max(0, index - 1));
                e.Handled = true;
                break;

            case Key.Down:
                SelectAt(Math.Min(_visibleRows.Count - 1, index + 1));
                e.Handled = true;
                break;

            case Key.Home:
                SelectAt(0);
                e.Handled = true;
                break;

            case Key.End:
                SelectAt(_visibleRows.Count - 1);
                e.Handled = true;
                break;

            case Key.Right:
                if (_selectedNode is { HasChildren: true })
                {
                    if (!_selectedNode.IsExpanded)
                        _selectedNode.IsExpanded = true;
                    else
                        SelectedNode = _selectedNode.Children[0];
                }
                e.Handled = true;
                break;

            case Key.Left:
                if (_selectedNode is { HasChildren: true, IsExpanded: true })
                    _selectedNode.IsExpanded = false;
                else if (_selectedNode?.Parent != null)
                    SelectedNode = _selectedNode.Parent;
                e.Handled = true;
                break;

            case Key.Enter:
                if (_selectedNode != null)
                {
                    if (_selectedNode.HasChildren)
                        _selectedNode.IsExpanded = !_selectedNode.IsExpanded;
                    NodeDoubleClicked?.Invoke(this, _selectedNode);
                }
                e.Handled = true;
                break;
        }

        base.OnKeyDown(e);
    }

    private void SelectAt(int index)
    {
        if (index >= 0 && index < _visibleRows.Count)
            SelectedNode = _visibleRows[index].Node;
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(new SolidColorBrush(ProTheme.Background.Panel), bounds);

        for (int i = 0; i < _visibleRows.Count; i++)
        {
            var (node, depth) = _visibleRows[i];
            var rowRect = new Rect(0, i * RowHeight, bounds.Width, RowHeight);
            var isSelected = ReferenceEquals(node, _selectedNode);
            var isHovered = ReferenceEquals(node, _hoveredNode);

            // Fond de ligne
            if (isSelected)
            {
                context.FillRectangle(new SolidColorBrush(
                    ProTheme.WithOpacity(ProTheme.Accent.Primary, 30)), rowRect.Deflate(new Thickness(2, 1)), 4);
            }
            else if (isHovered)
            {
                context.FillRectangle(new SolidColorBrush(ProTheme.Background.ControlHover),
                    rowRect.Deflate(new Thickness(2, 1)), 4);
            }

            var x = 4 + depth * IndentWidth;

            // Chevron
            if (node.HasChildren)
            {
                var chevron = ChevronRect(depth, rowRect.Y);
                var pen = new Pen(new SolidColorBrush(ProTheme.Text.Secondary), 1.4);
                var c = chevron.Center;
                if (node.IsExpanded)
                {
                    // ▼
                    context.DrawLine(pen, new Point(c.X - 3.5, c.Y - 1.5), new Point(c.X, c.Y + 2));
                    context.DrawLine(pen, new Point(c.X, c.Y + 2), new Point(c.X + 3.5, c.Y - 1.5));
                }
                else
                {
                    // ▶
                    context.DrawLine(pen, new Point(c.X - 1.5, c.Y - 3.5), new Point(c.X + 2, c.Y));
                    context.DrawLine(pen, new Point(c.X + 2, c.Y), new Point(c.X - 1.5, c.Y + 3.5));
                }
            }

            x += 18;

            // Icône + texte
            var label = string.IsNullOrEmpty(node.Icon) ? node.Text : $"{node.Icon} {node.Text}";
            var text = new FormattedText(label, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, new Typeface(ProTheme.Typography.FontFamily),
                ProTheme.Typography.FontSizeBody,
                new SolidColorBrush(isSelected ? ProTheme.Text.Primary : ProTheme.Text.Primary));

            context.DrawText(text, Crisp.Snap(new Point(
                x, rowRect.Y + (RowHeight - text.Height) / 2)));
        }
    }
}
