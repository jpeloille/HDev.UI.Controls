using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using HDev.UI.Controls.Theme;
using System.Globalization;

namespace HDev.UI.Controls;

/// <summary>Arguments de re-parentage d'un nœud (annulable)</summary>
public class MindMapReparentEventArgs : System.ComponentModel.CancelEventArgs
{
    public MindMapNode Node { get; }
    public MindMapNode NewParent { get; }

    public MindMapReparentEventArgs(MindMapNode node, MindMapNode newParent)
    {
        Node = node;
        NewParent = newParent;
    }
}

/// <summary>
/// Carte mentale orientée brainstorming : layout radial équilibré ou arbre à
/// droite, connecteurs Bézier, pan/zoom, culling (milliers de nœuds).
/// Grammaire clavier standard : Tab = enfant, Enter = frère, Suppr = retirer,
/// F2/double-clic = éditer, drag = re-parenter, Ctrl+Z/Y = annuler/rétablir.
/// </summary>
public class HDevMindMap : Control
{
    private const double MaxNodeTextWidth = 220;
    private const double NodePaddingX = 12;
    private const double NodePaddingY = 7;
    private const double MinZoom = 0.15;
    private const double MaxZoom = 2.5;

    private static int _themeGeneration; // bump au changement de variante (caches)

    private MindMapNode _root;
    private MindMapLayoutMode _layoutMode = MindMapLayoutMode.Radial;
    private List<MindMapPlacedNode> _placed = new();
    private readonly Dictionary<MindMapNode, MindMapPlacedNode> _placedByNode = new();
    private bool _layoutDirty = true;

    private Vector _pan;             // translation écran
    private double _zoom = 1;

    private MindMapNode? _selected;
    private MindMapNode? _hovered;

    // Drags
    private enum DragKind { None, Pan, Reparent }
    private DragKind _dragKind;
    private Point _dragOrigin;
    private Vector _panOrigin;
    private MindMapNode? _dragNode;
    private MindMapNode? _dropTarget;
    private bool _dropValid;
    private Point _dragCursor;

    // Édition in-place
    private HDevTextBox? _editor;
    private MindMapNode? _editNode;

    // Undo
    private readonly List<MindMapNode> _undo = new();
    private readonly List<MindMapNode> _redo = new();
    private const int UndoLimit = 100;

    // Palette de couleurs des branches de niveau 1
    private static readonly Color[] BranchPalette =
    {
        Color.Parse("#E95420"), Color.Parse("#1C71D8"), Color.Parse("#26A269"),
        Color.Parse("#A855A0"), Color.Parse("#E5A50A"), Color.Parse("#C01C28"),
        Color.Parse("#0AA1A5"), Color.Parse("#865E3C")
    };

    /// <summary>Déclenché quand la sélection change</summary>
    public event EventHandler? SelectedNodeChanged;

    /// <summary>Avant re-parentage par drag (annulable)</summary>
    public event EventHandler<MindMapReparentEventArgs>? NodeReparenting;

    /// <summary>Après toute mutation de la carte</summary>
    public event EventHandler? MapChanged;

    public bool IsReadOnly { get; set; }

    public MindMapNode Root
    {
        get => _root;
        set
        {
            _root = value ?? new MindMapNode("Idée centrale");
            _root.SetOwner(this);
            _undo.Clear();
            _redo.Clear();
            _selected = _root;
            OnStructureChanged();
            CenterOnRoot();
        }
    }

    public MindMapLayoutMode LayoutMode
    {
        get => _layoutMode;
        set
        {
            if (_layoutMode == value) return;
            _layoutMode = value;
            OnStructureChanged();
        }
    }

    public MindMapNode? SelectedNode
    {
        get => _selected;
        set
        {
            if (ReferenceEquals(_selected, value)) return;
            _selected = value;
            InvalidateVisual();
            SelectedNodeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    static HDevMindMap()
    {
        FocusableProperty.OverrideDefaultValue<HDevMindMap>(true);
        HDevTheme.VariantChanged += (s, e) => _themeGeneration++;
    }

    public HDevMindMap()
    {
        ClipToBounds = true;
        _root = new MindMapNode("Idée centrale");
        _root.SetOwner(this);
        _selected = _root;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        HDevTheme.VariantChanged += OnThemeVariantChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        HDevTheme.VariantChanged -= OnThemeVariantChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        _layoutDirty = true; // les caches de texte portent des brushes
        InvalidateVisual();
    }

    internal void OnStructureChanged()
    {
        _layoutDirty = true;
        InvalidateVisual();
        MapChanged?.Invoke(this, EventArgs.Empty);
    }

    // ═══════════════════════════════════════════════════════════════
    // API PUBLIQUE
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Recentre la vue sur la racine</summary>
    public void CenterOnRoot()
    {
        _pan = new Vector(Bounds.Width / 2, Bounds.Height / 2);
        InvalidateVisual();
    }

    /// <summary>Ajuste zoom et pan pour voir toute la carte</summary>
    public void ZoomToFit()
    {
        EnsureLayout();
        if (_placed.Count == 0 || Bounds.Width <= 0) return;

        var world = _placed[0].Bounds;
        foreach (var placed in _placed)
            world = world.Union(placed.Bounds);
        world = world.Inflate(40);

        _zoom = Math.Clamp(Math.Min(Bounds.Width / world.Width, Bounds.Height / world.Height),
            MinZoom, 1.4);
        _pan = new Vector(
            Bounds.Width / 2 - world.Center.X * _zoom,
            Bounds.Height / 2 - world.Center.Y * _zoom);
        InvalidateVisual();
    }

    /// <summary>Exporte la carte entière en PNG (rendu hors écran)</summary>
    public void ExportPng(string path)
    {
        EnsureLayout();
        if (_placed.Count == 0) return;

        var world = _placed[0].Bounds;
        foreach (var placed in _placed)
            world = world.Union(placed.Bounds);
        world = world.Inflate(40);

        var offscreen = new HDevMindMap
        {
            Root = MindMapCloner.Clone(_root),
            LayoutMode = _layoutMode,
            IsReadOnly = true
        };
        var size = new Size(Math.Min(16000, world.Width), Math.Min(16000, world.Height));
        offscreen.Measure(size);
        offscreen.Arrange(new Rect(size));
        offscreen.EnsureLayout();
        offscreen._zoom = 1;
        offscreen._pan = new Vector(-world.X, -world.Y);

        using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(
            new PixelSize((int)size.Width, (int)size.Height), new Vector(96, 96));
        bitmap.Render(offscreen);
        bitmap.Save(path, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }

    // ═══════════════════════════════════════════════════════════════
    // MUTATIONS (undo par instantanés)
    // ═══════════════════════════════════════════════════════════════

    private void PushUndo()
    {
        _undo.Add(MindMapCloner.Clone(_root));
        if (_undo.Count > UndoLimit) _undo.RemoveAt(0);
        _redo.Clear();
    }

    public void Undo()
    {
        if (_undo.Count == 0) return;
        _redo.Add(MindMapCloner.Clone(_root));
        RestoreRoot(_undo[^1]);
        _undo.RemoveAt(_undo.Count - 1);
    }

    public void Redo()
    {
        if (_redo.Count == 0) return;
        _undo.Add(MindMapCloner.Clone(_root));
        RestoreRoot(_redo[^1]);
        _redo.RemoveAt(_redo.Count - 1);
    }

    private void RestoreRoot(MindMapNode root)
    {
        CloseEditor(commit: false);
        _root = root;
        _root.SetOwner(this);
        _selected = _root;
        OnStructureChanged();
    }

    /// <summary>Tab : ajoute un enfant au nœud sélectionné et l'édite</summary>
    public void AddChildToSelected()
    {
        if (IsReadOnly || _selected == null) return;
        PushUndo();
        var node = new MindMapNode("…");
        _selected.IsExpanded = true;
        _selected.Children.Add(node);
        SelectedNode = node;
        BeginEdit(node, selectAll: true);
    }

    /// <summary>Enter : ajoute un frère après le nœud sélectionné et l'édite</summary>
    public void AddSiblingToSelected()
    {
        if (IsReadOnly || _selected?.Parent == null)
        {
            AddChildToSelected(); // sur la racine : Enter = enfant
            return;
        }
        PushUndo();
        var node = new MindMapNode("…");
        var siblings = _selected.Parent.Children;
        siblings.Insert(siblings.IndexOf(_selected) + 1, node);
        SelectedNode = node;
        BeginEdit(node, selectAll: true);
    }

    /// <summary>Suppr : retire le sous-arbre sélectionné</summary>
    public void RemoveSelected()
    {
        if (IsReadOnly || _selected?.Parent == null) return;
        PushUndo();
        var parent = _selected.Parent;
        parent.Children.Remove(_selected);
        SelectedNode = parent;
    }

    // ═══════════════════════════════════════════════════════════════
    // LAYOUT + TRANSFORMATION MONDE ↔ ÉCRAN
    // ═══════════════════════════════════════════════════════════════

    private Size MeasureNode(MindMapNode node)
    {
        if (node.CachedTextLayout is TextLayout && node.CacheGeneration == _themeGeneration)
            return node.CachedSize;

        var layout = new TextLayout(
            string.IsNullOrEmpty(node.Text) ? " " : node.Text,
            new Typeface(HDevTheme.Typography.FontFamily,
                weight: node.Parent == null ? FontWeight.SemiBold : FontWeight.Regular),
            node.Parent == null ? 16 : 13,
            new SolidColorBrush(HDevTheme.Text.Primary),
            textWrapping: TextWrapping.Wrap,
            maxWidth: MaxNodeTextWidth);

        node.CachedTextLayout = layout;
        node.CacheGeneration = _themeGeneration;
        node.CachedSize = new Size(
            Math.Min(MaxNodeTextWidth, layout.WidthIncludingTrailingWhitespace) + NodePaddingX * 2,
            layout.Height + NodePaddingY * 2);
        return node.CachedSize;
    }

    private void EnsureLayout()
    {
        if (!_layoutDirty) return;
        _placed = MindMapLayoutEngine.Layout(_root, _layoutMode, MeasureNode);
        _placedByNode.Clear();
        foreach (var placed in _placed)
            _placedByNode[placed.Node] = placed;
        _layoutDirty = false;
    }

    private Point ToScreen(Point world) => new(world.X * _zoom + _pan.X, world.Y * _zoom + _pan.Y);
    private Point ToWorld(Point screen) => new((screen.X - _pan.X) / _zoom, (screen.Y - _pan.Y) / _zoom);
    private Rect ToScreen(Rect world) => new(ToScreen(world.TopLeft),
        new Size(world.Width * _zoom, world.Height * _zoom));

    private MindMapNode? NodeAt(Point screenPoint)
    {
        EnsureLayout();
        var world = ToWorld(screenPoint);
        // Parcours inverse : les enfants dessinés après les parents priment
        for (int i = _placed.Count - 1; i >= 0; i--)
        {
            if (_placed[i].Bounds.Inflate(2).Contains(world))
                return _placed[i].Node;
        }
        return null;
    }

    /// <summary>Centre écran + rayon de la pastille plier/déplier d'un nœud</summary>
    private (Point Center, double Radius) ToggleGeometry(MindMapPlacedNode placed)
    {
        var screen = ToScreen(placed.Bounds);

        if (_layoutMode == MindMapLayoutMode.TreeDown)
            return (new Point(screen.Center.X, screen.Bottom + 10 * _zoom), Math.Max(7, 9 * _zoom));

        var cx = placed.OnLeft ? screen.X - 10 * _zoom : screen.Right + 10 * _zoom;
        return (new Point(cx, screen.Center.Y), Math.Max(7, 9 * _zoom));
    }

    /// <summary>
    /// Centre écran + rayon du bouton « + » (ajout d'enfant) : badge posé sur
    /// le COIN bas-extérieur de la pilule — les connecteurs partent du milieu
    /// du bord, les coins sont libres
    /// </summary>
    private (Point Center, double Radius) PlusGeometry(MindMapPlacedNode placed)
    {
        var screen = ToScreen(placed.Bounds);
        var r = Math.Max(7, 8.5 * _zoom);

        var x = _layoutMode != MindMapLayoutMode.TreeDown && placed.OnLeft
            ? screen.X - 1
            : screen.Right + 1;
        return (new Point(x, screen.Bottom + 1), r);
    }

    /// <summary>Vrai si le « + » du nœud sélectionné est sous le curseur</summary>
    private bool PlusAt(Point screenPoint)
    {
        if (_selected == null || IsReadOnly) return false;
        EnsureLayout();
        if (!_placedByNode.TryGetValue(_selected, out var placed)) return false;

        var (center, radius) = PlusGeometry(placed);
        var dx = screenPoint.X - center.X;
        var dy = screenPoint.Y - center.Y;
        return dx * dx + dy * dy <= (radius + 3) * (radius + 3);
    }

    /// <summary>
    /// Centre + rayon du « × » de suppression : badge posé sur le COIN
    /// haut-extérieur de la pilule (même côté que le « + », à l'opposé vertical)
    /// </summary>
    private (Point Center, double Radius) DeleteGeometry(MindMapPlacedNode placed)
    {
        var screen = ToScreen(placed.Bounds);
        var x = _layoutMode != MindMapLayoutMode.TreeDown && placed.OnLeft
            ? screen.X - 1
            : screen.Right + 1;
        return (new Point(x, screen.Y - 1), Math.Max(6.5, 7.5 * _zoom));
    }

    /// <summary>Vrai si le « × » du nœud sélectionné est sous le curseur (racine exclue)</summary>
    private bool DeleteAt(Point screenPoint)
    {
        if (_selected?.Parent == null || IsReadOnly) return false;
        EnsureLayout();
        if (!_placedByNode.TryGetValue(_selected, out var placed)) return false;

        var (center, radius) = DeleteGeometry(placed);
        var dx = screenPoint.X - center.X;
        var dy = screenPoint.Y - center.Y;
        return dx * dx + dy * dy <= (radius + 3) * (radius + 3);
    }

    /// <summary>Nœud dont la pastille plier/déplier est sous le curseur</summary>
    private MindMapNode? ToggleAt(Point screenPoint)
    {
        EnsureLayout();
        foreach (var placed in _placed)
        {
            if (!placed.Node.HasChildren) continue;
            var (center, radius) = ToggleGeometry(placed);
            var dx = screenPoint.X - center.X;
            var dy = screenPoint.Y - center.Y;
            if (dx * dx + dy * dy <= (radius + 3) * (radius + 3))
                return placed.Node;
        }
        return null;
    }

    /// <summary>Couleur de branche : posée au niveau 1, héritée en dessous</summary>
    private Color BranchColorOf(MindMapNode node)
    {
        var current = node;
        while (current.Parent != null && current.Parent.Parent != null)
            current = current.Parent;

        if (current.Parent == null)
            return HDevTheme.Accent.Primary; // racine

        if (current.BranchColor is { } explicitColor)
            return explicitColor;

        var index = current.Parent.Children.IndexOf(current);
        return BranchPalette[Math.Max(0, index) % BranchPalette.Length];
    }

    protected override Size MeasureOverride(Size availableSize)
        => new(
            double.IsInfinity(availableSize.Width) ? 800 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 500 : availableSize.Height);

    // ═══════════════════════════════════════════════════════════════
    // SOURIS
    // ═══════════════════════════════════════════════════════════════

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Focus();
        CloseEditor(commit: true);
        var pos = e.GetPosition(this);

        // « × » du nœud sélectionné : suppression du sous-arbre
        if (DeleteAt(pos))
        {
            RemoveSelected();
            e.Handled = true;
            return;
        }

        // « + » du nœud sélectionné : ajout d'un enfant (édition immédiate)
        if (PlusAt(pos))
        {
            AddChildToSelected();
            e.Handled = true;
            return;
        }

        // Pastille plier/déplier : bascule sans changer la sélection
        var toggle = ToggleAt(pos);
        if (toggle != null)
        {
            toggle.IsExpanded = !toggle.IsExpanded;
            e.Handled = true;
            return;
        }

        var node = NodeAt(pos);

        if (node != null)
        {
            SelectedNode = node;

            // Clic droit : menu contextuel avec toute la grammaire
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                if (!IsReadOnly)
                {
                    var menu = new HDevContextMenu();
                    menu.Add("Ajouter un enfant", "➕", "Tab", AddChildToSelected);
                    if (node.Parent != null)
                        menu.Add("Ajouter un frère", null, "Enter", AddSiblingToSelected);
                    menu.Add("Renommer", "✏️", "F2", () => BeginEdit(node, selectAll: true));
                    if (node.HasChildren)
                        menu.Add(node.IsExpanded ? "Replier" : "Déplier", null, "Espace",
                            () => node.IsExpanded = !node.IsExpanded);
                    if (node.Parent != null)
                    {
                        menu.AddSeparator();
                        menu.Add("Supprimer le sous-arbre", "🗑", "Suppr", RemoveSelected);
                    }
                    menu.Show(this, pos);
                }
                e.Handled = true;
                return;
            }

            if (e.ClickCount == 2)
            {
                if (!IsReadOnly) BeginEdit(node, selectAll: true);
                e.Handled = true;
                return;
            }

            // Drag potentiel : re-parentage (racine exclue)
            if (!IsReadOnly && node.Parent != null)
            {
                _dragKind = DragKind.Reparent;
                _dragNode = node;
                _dragOrigin = pos;
                _dragCursor = pos;
                _dropTarget = null;
                e.Pointer.Capture(this);
            }
        }
        else
        {
            // Pan du fond
            _dragKind = DragKind.Pan;
            _dragOrigin = pos;
            _panOrigin = _pan;
            e.Pointer.Capture(this);
        }

        e.Handled = true;
        base.OnPointerPressed(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var pos = e.GetPosition(this);

        switch (_dragKind)
        {
            case DragKind.Pan:
                _pan = _panOrigin + (pos - _dragOrigin);
                InvalidateVisual();
                return;

            case DragKind.Reparent:
                _dragCursor = pos;
                var target = NodeAt(pos);
                _dropTarget = target != null && !ReferenceEquals(target, _dragNode) ? target : null;
                _dropValid = _dropTarget != null && _dragNode != null
                    && !_dragNode.IsAncestorOf(_dropTarget)
                    && !ReferenceEquals(_dropTarget, _dragNode.Parent);
                InvalidateVisual();
                return;
        }

        var hovered = NodeAt(pos);
        var onAffordance = PlusAt(pos) || DeleteAt(pos) || ToggleAt(pos) != null;
        if (!ReferenceEquals(hovered, _hovered))
        {
            _hovered = hovered;
            InvalidateVisual();
        }
        Cursor = hovered != null || onAffordance
            ? new Cursor(StandardCursorType.Hand)
            : Cursor.Default;
        base.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_dragKind == DragKind.Reparent && _dragNode != null)
        {
            // Petit déplacement = simple clic (pas de re-parentage)
            var moved = Math.Abs(_dragCursor.X - _dragOrigin.X) > 6 ||
                        Math.Abs(_dragCursor.Y - _dragOrigin.Y) > 6;

            if (moved && _dropValid && _dropTarget != null)
            {
                var args = new MindMapReparentEventArgs(_dragNode, _dropTarget);
                NodeReparenting?.Invoke(this, args);
                if (!args.Cancel)
                {
                    PushUndo();
                    _dragNode.Parent!.Children.Remove(_dragNode);
                    _dropTarget.IsExpanded = true;
                    _dropTarget.Children.Add(_dragNode);
                }
            }
        }

        _dragKind = DragKind.None;
        _dragNode = null;
        _dropTarget = null;
        e.Pointer.Capture(null);
        InvalidateVisual();
        base.OnPointerReleased(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        var pos = e.GetPosition(this);

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            // Zoom centré sur le curseur
            var worldBefore = ToWorld(pos);
            _zoom = Math.Clamp(_zoom * (e.Delta.Y > 0 ? 1.15 : 0.87), MinZoom, MaxZoom);
            _pan = new Vector(pos.X - worldBefore.X * _zoom, pos.Y - worldBefore.Y * _zoom);
        }
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            _pan += new Vector((e.Delta.Y > 0 ? 1 : -1) * 60, 0);
        }
        else
        {
            _pan += new Vector(0, (e.Delta.Y > 0 ? 1 : -1) * 60);
        }

        InvalidateVisual();
        e.Handled = true;
        base.OnPointerWheelChanged(e);
    }

    // ═══════════════════════════════════════════════════════════════
    // CLAVIER (grammaire brainstorming)
    // ═══════════════════════════════════════════════════════════════

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var handled = true;

        switch (e.Key)
        {
            case Key.Tab: AddChildToSelected(); break;
            case Key.Enter: AddSiblingToSelected(); break;
            case Key.Delete or Key.Back: RemoveSelected(); break;
            case Key.F2 when _selected != null && !IsReadOnly:
                BeginEdit(_selected, selectAll: true);
                break;

            case Key.Space when _selected is { HasChildren: true }:
                _selected.IsExpanded = !_selected.IsExpanded;
                break;

            case Key.Z when ctrl: Undo(); break;
            case Key.Y when ctrl: Redo(); break;
            case Key.Home: CenterOnRoot(); break;

            // TreeDown : l'axe parent/enfant est vertical, les frères horizontaux
            case Key.Left when _layoutMode == MindMapLayoutMode.TreeDown: NavigateSibling(-1); break;
            case Key.Right when _layoutMode == MindMapLayoutMode.TreeDown: NavigateSibling(1); break;
            case Key.Up when _layoutMode == MindMapLayoutMode.TreeDown: NavigateToParent(); break;
            case Key.Down when _layoutMode == MindMapLayoutMode.TreeDown: NavigateToChild(); break;

            case Key.Left: NavigateHorizontal(toLeft: true); break;
            case Key.Right: NavigateHorizontal(toLeft: false); break;
            case Key.Up: NavigateSibling(-1); break;
            case Key.Down: NavigateSibling(1); break;

            default: handled = false; break;
        }

        if (handled) e.Handled = true;
        base.OnKeyDown(e);
    }

    private void NavigateHorizontal(bool toLeft)
    {
        if (_selected == null) return;
        EnsureLayout();

        var onLeft = _placedByNode.TryGetValue(_selected, out var placed) && placed.OnLeft;

        // Vers l'extérieur = premier enfant ; vers l'intérieur = parent
        var outward = _selected.Parent == null
            ? toLeft // depuis la racine : gauche va vers les branches gauches
            : (onLeft ? toLeft : !toLeft);

        if (outward)
        {
            if (_selected.IsExpanded && _selected.Children.Count > 0)
            {
                // Depuis la racine, choisir un enfant du bon côté
                var candidates = _selected.Parent == null
                    ? _selected.Children.Where(c =>
                        _placedByNode.TryGetValue(c, out var p) && p.OnLeft == toLeft).ToList()
                    : _selected.Children.ToList();
                if (candidates.Count > 0)
                    SelectedNode = candidates[candidates.Count / 2];
            }
        }
        else if (_selected.Parent != null)
        {
            SelectedNode = _selected.Parent;
        }
    }

    private void NavigateSibling(int delta)
    {
        if (_selected?.Parent == null) return;
        var siblings = _selected.Parent.Children;
        var index = siblings.IndexOf(_selected) + delta;
        if (index >= 0 && index < siblings.Count)
            SelectedNode = siblings[index];
    }

    private void NavigateToParent()
    {
        if (_selected?.Parent != null)
            SelectedNode = _selected.Parent;
    }

    private void NavigateToChild()
    {
        if (_selected is { IsExpanded: true, Children.Count: > 0 })
            SelectedNode = _selected.Children[_selected.Children.Count / 2];
    }

    // ═══════════════════════════════════════════════════════════════
    // ÉDITION IN-PLACE
    // ═══════════════════════════════════════════════════════════════

    private void BeginEdit(MindMapNode node, bool selectAll)
    {
        CloseEditor(commit: true);
        EnsureLayout();
        if (!_placedByNode.TryGetValue(node, out var placed)) return;

        _editNode = node;
        _editor = new HDevTextBox { Text = node.Text, MinHeight = 0 };

        var screen = ToScreen(placed.Bounds).Inflate(4);
        _editor.Width = Math.Max(120, screen.Width);
        VisualChildren.Add(_editor);
        LogicalChildren.Add(_editor);
        _editor.Measure(new Size(_editor.Width, 60));
        _editor.Arrange(new Rect(screen.X, screen.Y, _editor.Width, Math.Max(30, screen.Height)));

        _editor.AddHandler(KeyDownEvent, (s, args) =>
        {
            if (args.Key == Key.Enter)
            {
                CloseEditor(commit: true);
                Focus();
                args.Handled = true;
            }
            else if (args.Key == Key.Escape)
            {
                CloseEditor(commit: false);
                Focus();
                args.Handled = true;
            }
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel);

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _editor?.FocusTextBox();
            if (selectAll) _editor?.SelectAll();
        }, Avalonia.Threading.DispatcherPriority.Loaded);
    }

    private void CloseEditor(bool commit)
    {
        if (_editor == null || _editNode == null) return;

        var editor = _editor;
        var node = _editNode;
        _editor = null;
        _editNode = null;

        if (commit && !string.IsNullOrWhiteSpace(editor.Text) && editor.Text != node.Text)
        {
            PushUndo();
            node.Text = editor.Text!.Trim();
        }

        VisualChildren.Remove(editor);
        LogicalChildren.Remove(editor);
        InvalidateVisual();
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU (culling : seuls les éléments croisant la vue sont dessinés)
    // ═══════════════════════════════════════════════════════════════

    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(new SolidColorBrush(HDevTheme.Background.Window), bounds);

        EnsureLayout();
        var viewport = bounds.Inflate(60);

        // Connecteurs Bézier (sous les nœuds)
        foreach (var placed in _placed)
        {
            if (placed.Node.Parent == null) continue;
            if (!_placedByNode.TryGetValue(placed.Node.Parent, out var parentPlaced)) continue;

            Point from, to;
            if (_layoutMode == MindMapLayoutMode.TreeDown)
            {
                // Vertical : bas du parent → haut de l'enfant
                from = ToScreen(new Point(parentPlaced.Bounds.Center.X, parentPlaced.Bounds.Bottom));
                to = ToScreen(new Point(placed.Bounds.Center.X, placed.Bounds.Y));
            }
            else
            {
                from = ToScreen(placed.OnLeft ? parentPlaced.Bounds.TopLeft + new Vector(0, parentPlaced.Bounds.Height / 2)
                    : parentPlaced.Bounds.TopRight + new Vector(0, parentPlaced.Bounds.Height / 2));
                to = ToScreen(placed.OnLeft ? placed.Bounds.TopRight + new Vector(0, placed.Bounds.Height / 2)
                    : placed.Bounds.TopLeft + new Vector(0, placed.Bounds.Height / 2));
            }

            // Culling grossier du segment
            var edgeBox = new Rect(
                Math.Min(from.X, to.X), Math.Min(from.Y, to.Y),
                Math.Abs(to.X - from.X) + 1, Math.Abs(to.Y - from.Y) + 1);
            if (!viewport.Intersects(edgeBox)) continue;

            var color = BranchColorOf(placed.Node);
            var geometry = new StreamGeometry();
            using (var g = geometry.Open())
            {
                g.BeginFigure(from, false);
                if (_layoutMode == MindMapLayoutMode.TreeDown)
                {
                    var midY = (from.Y + to.Y) / 2;
                    g.CubicBezierTo(new Point(from.X, midY), new Point(to.X, midY), to);
                }
                else
                {
                    var midX = (from.X + to.X) / 2;
                    g.CubicBezierTo(new Point(midX, from.Y), new Point(midX, to.Y), to);
                }
                g.EndFigure(false);
            }
            context.DrawGeometry(null,
                new Pen(new SolidColorBrush(HDevTheme.WithOpacity(color, 170)),
                    Math.Max(1.2, (3.2 - placed.Depth * 0.6) * _zoom)),
                geometry);
        }

        // Nœuds
        foreach (var placed in _placed)
        {
            var screen = ToScreen(placed.Bounds);
            if (!viewport.Intersects(screen)) continue;

            RenderNode(context, placed, screen);
        }

        // Affordances du nœud sélectionné : « + » (ajout) et « × » (suppression)
        if (_selected != null && !IsReadOnly && _dragKind == DragKind.None &&
            _placedByNode.TryGetValue(_selected, out var selectedPlaced))
        {
            var color = BranchColorOf(_selected);

            var (plusCenter, plusR) = PlusGeometry(selectedPlaced);
            context.DrawEllipse(new SolidColorBrush(HDevTheme.Background.Panel),
                new Pen(new SolidColorBrush(color), 1.6), plusCenter, plusR, plusR);
            var plusPen = new Pen(new SolidColorBrush(color), 1.8);
            var arm = plusR * 0.45;
            context.DrawLine(plusPen, new Point(plusCenter.X - arm, plusCenter.Y), new Point(plusCenter.X + arm, plusCenter.Y));
            context.DrawLine(plusPen, new Point(plusCenter.X, plusCenter.Y - arm), new Point(plusCenter.X, plusCenter.Y + arm));

            // « × » de suppression (racine exclue), coin haut-droit
            if (_selected.Parent != null)
            {
                var (delCenter, delR) = DeleteGeometry(selectedPlaced);
                var errorColor = HDevTheme.Accent.Error;
                context.DrawEllipse(new SolidColorBrush(HDevTheme.Background.Panel),
                    new Pen(new SolidColorBrush(errorColor), 1.4), delCenter, delR, delR);
                var delPen = new Pen(new SolidColorBrush(errorColor), 1.6);
                var cross = delR * 0.4;
                context.DrawLine(delPen,
                    new Point(delCenter.X - cross, delCenter.Y - cross),
                    new Point(delCenter.X + cross, delCenter.Y + cross));
                context.DrawLine(delPen,
                    new Point(delCenter.X - cross, delCenter.Y + cross),
                    new Point(delCenter.X + cross, delCenter.Y - cross));
            }
        }

        // Ligne élastique du re-parentage
        if (_dragKind == DragKind.Reparent && _dragNode != null &&
            _placedByNode.TryGetValue(_dragNode, out var dragPlaced))
        {
            var from = ToScreen(dragPlaced.Bounds.Center);
            var color = _dropTarget == null ? HDevTheme.Text.Secondary
                : _dropValid ? HDevTheme.Accent.Success : HDevTheme.Accent.Error;
            context.DrawLine(
                new Pen(new SolidColorBrush(color), 1.6)
                {
                    DashStyle = new DashStyle(new double[] { 4, 3 }, 0)
                },
                from, _dragCursor);

            if (_dropTarget != null && _placedByNode.TryGetValue(_dropTarget, out var targetPlaced))
            {
                context.DrawRectangle(null, new Pen(new SolidColorBrush(color), 2),
                    ToScreen(targetPlaced.Bounds).Inflate(3), 8, 8);
            }
        }
    }

    private void RenderNode(DrawingContext context, MindMapPlacedNode placed, Rect screen)
    {
        var node = placed.Node;
        var isRoot = node.Parent == null;
        var isSelected = ReferenceEquals(node, _selected);
        var isHovered = ReferenceEquals(node, _hovered);
        var color = BranchColorOf(node);
        var radius = screen.Height / 2;

        // Fond : racine pleine accent ; branches = teinte de leur couleur
        Color fill = isRoot
            ? HDevTheme.Accent.Primary
            : HDevTheme.WithOpacity(color, isSelected ? (byte)70 : isHovered ? (byte)55 : (byte)38);
        var borderPen = isSelected
            ? new Pen(new SolidColorBrush(color), 2.2)
            : new Pen(new SolidColorBrush(HDevTheme.WithOpacity(color, 150)), 1.2);

        context.DrawRectangle(new SolidColorBrush(fill), borderPen, screen, radius, radius);

        // Texte (cache, redessiné à l'échelle via transformation)
        if (node.CachedTextLayout is TextLayout layout)
        {
            using (context.PushTransform(Matrix.CreateScale(_zoom, _zoom) *
                Matrix.CreateTranslation(screen.X + NodePaddingX * _zoom, screen.Y + NodePaddingY * _zoom)))
            {
                layout.Draw(context, default);
            }
        }

        // Racine : texte blanc par-dessus (le cache est en Text.Primary)
        if (isRoot)
        {
            var rootText = new FormattedText(node.Text, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(HDevTheme.Typography.FontFamily, weight: FontWeight.SemiBold),
                16 * _zoom, new SolidColorBrush(Colors.White));
            context.DrawRectangle(new SolidColorBrush(HDevTheme.Accent.Primary), null,
                screen.Deflate(1), radius, radius);
            context.DrawText(rootText, Crisp.Snap(new Point(
                screen.X + NodePaddingX * _zoom, screen.Y + NodePaddingY * _zoom)));
        }

        // Pastille plier/déplier (cliquable) : repliée = compte de descendants,
        // dépliée = « − » discret
        if (node.HasChildren)
        {
            var (center, r) = ToggleGeometry(placed);

            if (!node.IsExpanded)
            {
                var badge = new FormattedText(node.DescendantCount.ToString(),
                    CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    new Typeface(HDevTheme.Typography.FontFamily),
                    Math.Max(8, 10 * _zoom), new SolidColorBrush(Colors.White));
                r = Math.Max(r, badge.Width / 2 + 5);
                context.DrawEllipse(new SolidColorBrush(color), null, center, r, r);
                context.DrawText(badge, Crisp.Snap(new Point(
                    center.X - badge.Width / 2, center.Y - badge.Height / 2)));
            }
            else
            {
                context.DrawEllipse(new SolidColorBrush(HDevTheme.Background.Panel),
                    new Pen(new SolidColorBrush(HDevTheme.WithOpacity(color, 170)), 1.2),
                    center, r * 0.75, r * 0.75);
                context.DrawLine(new Pen(new SolidColorBrush(color), 1.6),
                    new Point(center.X - r * 0.38, center.Y),
                    new Point(center.X + r * 0.38, center.Y));
            }
        }
    }
}
