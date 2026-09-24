using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

/// <summary>
/// Volet ancrable : identité stable + contenu Avalonia. Le contenu n'est PAS
/// sérialisé (cf. DockLayout) ; l'app le réassocie par <see cref="Id"/> au load.
/// </summary>
public sealed class HDevDockPanel
{
    public string Id { get; }
    public string Title { get; set; }
    public string? Icon { get; set; }
    public bool CanClose { get; set; }
    public Control? Content { get; set; }

    public HDevDockPanel(string id, string title, Control? content = null,
        string? icon = null, bool canClose = true)
    {
        Id = id;
        Title = title;
        Content = content;
        Icon = icon;
        CanClose = canClose;
    }
}

/// <summary>
/// Gestionnaire de docking (phase 1) : construit l'arbre visuel depuis un
/// <see cref="DockLayout"/> pur — splits redimensionnables (Grid + GridSplitter
/// Avalonia) et groupes d'onglets (DockTabStrip custom). Réarrangement par
/// drag = phase 2 ; détachement (float) = phase 3, tout dans l'OverlayLayer.
/// </summary>
public sealed class HDevDockManager : Decorator
{
    private readonly Dictionary<string, HDevDockPanel> _panels = new();
    private readonly List<DockTabStrip> _strips = new();
    private readonly List<Border> _hosts = new();
    private readonly List<(Grid Grid, DockSplit Split)> _grids = new();
    private readonly List<GridSplitter> _splitters = new();

    private DockLayout _layout = new();

    /// <summary>Arbre de docking sous-jacent (moteur pur).</summary>
    public DockLayout Layout => _layout;

    /// <summary>Notifié après fermeture d'un volet (par l'onglet ×).</summary>
    public event EventHandler<HDevDockPanel>? PanelClosed;

    public HDevDockManager()
    {
        _layout.Changed += Rebuild;
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Enregistre un volet et l'ancre dans une zone.</summary>
    public HDevDockPanel AddPanel(HDevDockPanel panel, DockRegion region)
    {
        _panels[panel.Id] = panel;
        _layout.AddToRegion(
            new DockItem(panel.Id, panel.Title, panel.Icon, panel.CanClose), region);
        return panel;
    }

    /// <summary>Ferme (retire) un volet par Id.</summary>
    public void ClosePanel(string id)
    {
        if (!_panels.TryGetValue(id, out var panel)) return;
        if (_layout.Remove(id))
        {
            _panels.Remove(id);
            PanelClosed?.Invoke(this, panel);
        }
    }

    /// <summary>Sérialise la topologie + l'identité (pas le contenu).</summary>
    public string SaveLayout()
    {
        CaptureProportions();
        return _layout.ToJson();
    }

    /// <summary>
    /// Recharge une topologie. Réassocie chaque item au volet enregistré par Id ;
    /// les items sans volet connu sont ignorés à la construction.
    /// </summary>
    public void LoadLayout(string json)
    {
        _layout.Changed -= Rebuild;
        _layout = DockLayout.FromJson(json);
        _layout.Changed += Rebuild;
        Rebuild();
    }

    // ── Construction du visuel ────────────────────────────────────────────────

    private void Rebuild()
    {
        // Détacher l'ancien contenu AVANT de reconstruire : un Control ne peut avoir
        // qu'un parent logique. On libère les hôtes puis on jette l'ancien arbre.
        foreach (var s in _strips) s.Detach();
        foreach (var h in _hosts) h.Child = null;
        Child = null;
        _strips.Clear();
        _hosts.Clear();
        _grids.Clear();
        _splitters.Clear();

        Child = _layout.Root != null ? Build(_layout.Root) : BuildPlaceholder();
    }

    private Control Build(DockNode node) => node switch
    {
        DockTabGroup g => BuildGroup(g),
        DockSplit s => BuildSplit(s),
        _ => BuildPlaceholder(),
    };

    private Control BuildGroup(DockTabGroup group)
    {
        var strip = new DockTabStrip(group);
        _strips.Add(strip);

        var host = new Border { Background = new SolidColorBrush(HDevTheme.Background.Panel) };
        _hosts.Add(host);

        void ShowSelected()
        {
            var item = group.Selected;
            host.Child = item != null && _panels.TryGetValue(item.Id, out var p) ? p.Content : null;
        }

        strip.SelectionChanged += (_, _) => ShowSelected();
        strip.CloseRequested += (_, item) => ClosePanel(item.Id);
        ShowSelected();

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        Grid.SetRow(strip, 0);
        Grid.SetRow(host, 1);
        grid.Children.Add(strip);
        grid.Children.Add(host);
        return grid;
    }

    private Control BuildSplit(DockSplit split)
    {
        var horizontal = split.Orientation == DockOrientation.Horizontal;
        var grid = new Grid();
        _grids.Add((grid, split));

        // Colonnes/lignes : enfant, splitter, enfant, splitter, … (enfants aux index pairs).
        for (int i = 0; i < split.Children.Count; i++)
        {
            if (i > 0)
                AddTrack(grid, horizontal, new GridLength(DockTabStrip.SplitterThickness, GridUnitType.Pixel));
            AddTrack(grid, horizontal, new GridLength(split.Proportions[i], GridUnitType.Star));
        }

        for (int i = 0; i < split.Children.Count; i++)
        {
            var trackIndex = i * 2; // saute les pistes de splitter
            var childControl = Build(split.Children[i]);
            Place(childControl, horizontal, trackIndex);
            grid.Children.Add(childControl);

            if (i > 0)
            {
                var splitter = new GridSplitter
                {
                    Background = new SolidColorBrush(HDevTheme.Border.Subtle),
                    ResizeBehavior = GridResizeBehavior.PreviousAndNext,
                    ResizeDirection = horizontal
                        ? GridResizeDirection.Columns
                        : GridResizeDirection.Rows,
                };
                _splitters.Add(splitter);
                Place(splitter, horizontal, trackIndex - 1);
                grid.Children.Add(splitter);
            }
        }
        return grid;
    }

    private static void AddTrack(Grid grid, bool horizontal, GridLength length)
    {
        if (horizontal) grid.ColumnDefinitions.Add(new ColumnDefinition(length));
        else grid.RowDefinitions.Add(new RowDefinition(length));
    }

    private static void Place(Control control, bool horizontal, int index)
    {
        if (horizontal) Grid.SetColumn(control, index);
        else Grid.SetRow(control, index);
    }

    private static Control BuildPlaceholder() => new Border
    {
        Background = new SolidColorBrush(HDevTheme.Background.Panel),
    };

    /// <summary>
    /// Relit les proportions manipulées par les GridSplitter dans le modèle.
    /// On lit la taille RÉELLEMENT mise en page (ActualWidth/ActualHeight), pas la
    /// valeur étoile : robuste que le splitter laisse de l'étoile ou passe au pixel.
    /// </summary>
    private void CaptureProportions()
    {
        foreach (var (grid, split) in _grids)
        {
            var horizontal = split.Orientation == DockOrientation.Horizontal;
            var sizes = new List<double>();
            for (int i = 0; i < split.Children.Count; i++)
            {
                var track = i * 2;
                sizes.Add(horizontal
                    ? grid.ColumnDefinitions[track].ActualWidth
                    : grid.RowDefinitions[track].ActualHeight);
            }
            // Pas encore mis en page (tout à zéro) : garder les proportions du modèle.
            if (sizes.Sum() <= 0) continue;
            split.Proportions.Clear();
            split.Proportions.AddRange(sizes);
            split.Normalize();
        }
    }

    // ── Cycle de vie / thème ──────────────────────────────────────────────────

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Child == null) Rebuild();
        HDevTheme.VariantChanged += OnThemeVariantChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        HDevTheme.VariantChanged -= OnThemeVariantChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        foreach (var splitter in _splitters)
            splitter.Background = new SolidColorBrush(HDevTheme.Border.Subtle);
        foreach (var host in _hosts)
            host.Background = new SolidColorBrush(HDevTheme.Background.Panel);
        foreach (var strip in _strips)
            strip.InvalidateVisual();
    }
}

/// <summary>
/// Bandeau d'onglets d'un groupe docké : reprend l'idiome pilule de HDevTabControl
/// mais dédié au docking (sélection, fermeture ×). Le drag-réordonnance viendra
/// en phase 2 — d'où une classe distincte plutôt qu'une greffe sur HDevTabControl.
/// </summary>
internal sealed class DockTabStrip : Control
{
    public const double StripHeight = 32;
    public const double SplitterThickness = 6;
    private const double TabHeight = 24;
    private const double CloseSize = 14;

    private readonly DockTabGroup _group;
    private DockItem? _hovered;
    private DockItem? _closeHovered;

    public event EventHandler? SelectionChanged;
    public event EventHandler<DockItem>? CloseRequested;

    public DockTabStrip(DockTabGroup group)
    {
        _group = group;
        Height = StripHeight;
        ClipToBounds = true;
    }

    public void Detach()
    {
        SelectionChanged = null;
        CloseRequested = null;
    }

    // ── Layout des onglets (partagé rendu / hit-test) ─────────────────────────

    private IEnumerable<(DockItem Item, Rect Rect, Rect CloseRect)> TabsLayout()
    {
        double x = 6;
        var y = (StripHeight - TabHeight) / 2;
        foreach (var item in _group.Items)
        {
            var width = MeasureTabWidth(item);
            var rect = new Rect(x, y, width, TabHeight);
            var closeRect = item.CanClose
                ? new Rect(rect.Right - CloseSize - 5, rect.Y + (TabHeight - CloseSize) / 2, CloseSize, CloseSize)
                : default;
            yield return (item, rect, closeRect);
            x += width + 3;
        }
    }

    private double MeasureTabWidth(DockItem item)
        => 10 + CreateText(TabText(item), HDevTheme.Text.Primary).Width + 10 + (item.CanClose ? CloseSize + 3 : 0);

    private static string TabText(DockItem item)
        => string.IsNullOrEmpty(item.Icon) ? item.Title : $"{item.Icon} {item.Title}";

    private (DockItem? Item, bool OnClose) HitTest(Point pos)
    {
        foreach (var (item, rect, closeRect) in TabsLayout())
        {
            if (item.CanClose && closeRect.Contains(pos)) return (item, true);
            if (rect.Contains(pos)) return (item, false);
        }
        return (null, false);
    }

    // ── Souris ────────────────────────────────────────────────────────────────

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var (item, onClose) = HitTest(e.GetPosition(this));
        var closeHovered = onClose ? item : null;
        if (item != _hovered || closeHovered != _closeHovered)
        {
            _hovered = item;
            _closeHovered = closeHovered;
            InvalidateVisual();
        }
        base.OnPointerMoved(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _hovered = null;
        _closeHovered = null;
        InvalidateVisual();
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var pos = e.GetPosition(this);
        var (item, onClose) = HitTest(pos);
        if (item == null) { base.OnPointerPressed(e); return; }

        var point = e.GetCurrentPoint(this);
        if (onClose || (point.Properties.IsMiddleButtonPressed && item.CanClose))
        {
            CloseRequested?.Invoke(this, item);
            e.Handled = true;
            return;
        }

        var idx = _group.Items.IndexOf(item);
        if (idx != _group.SelectedIndex)
        {
            _group.SelectedIndex = idx;
            InvalidateVisual();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
        e.Handled = true;
        base.OnPointerPressed(e);
    }

    // ── Rendu ─────────────────────────────────────────────────────────────────

    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);
        var bounds = new Rect(Bounds.Size);

        context.FillRectangle(new SolidColorBrush(HDevTheme.Background.Toolbar),
            new Rect(0, 0, bounds.Width, StripHeight));
        context.DrawLine(new Pen(new SolidColorBrush(HDevTheme.Border.Subtle), 1),
            new Point(0, StripHeight - 0.5), new Point(bounds.Width, StripHeight - 0.5));

        foreach (var (item, rect, closeRect) in TabsLayout())
        {
            var isSelected = ReferenceEquals(item, _group.Selected);
            var isHovered = ReferenceEquals(item, _hovered);
            var radius = TabHeight / 2;

            if (isSelected)
                context.DrawRectangle(new SolidColorBrush(HDevTheme.Background.Panel),
                    new Pen(new SolidColorBrush(HDevTheme.Border.Default), 1),
                    rect.Deflate(0.5), radius, radius);
            else if (isHovered)
                context.DrawRectangle(new SolidColorBrush(HDevTheme.Background.ControlHover),
                    null, rect, radius, radius);

            var textColor = isSelected ? HDevTheme.Text.Primary : HDevTheme.Text.Secondary;
            var text = CreateText(TabText(item), textColor);
            context.DrawText(text, Crisp.Snap(new Point(rect.X + 10, rect.Y + (rect.Height - text.Height) / 2)));

            if (item.CanClose)
            {
                if (ReferenceEquals(item, _closeHovered))
                    context.DrawRectangle(new SolidColorBrush(HDevTheme.Background.ControlPressed),
                        null, closeRect, CloseSize / 2, CloseSize / 2);
                var pen = new Pen(new SolidColorBrush(textColor), 1.2);
                var c = closeRect.Center;
                context.DrawLine(pen, new Point(c.X - 3, c.Y - 3), new Point(c.X + 3, c.Y + 3));
                context.DrawLine(pen, new Point(c.X - 3, c.Y + 3), new Point(c.X + 3, c.Y - 3));
            }
        }
    }

    private static FormattedText CreateText(string text, Color color)
        => new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(HDevTheme.Typography.FontFamily), HDevTheme.Typography.FontSizeBody,
            new SolidColorBrush(color));
}
