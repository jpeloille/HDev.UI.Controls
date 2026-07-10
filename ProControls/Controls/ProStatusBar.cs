using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ProControls.Theme;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ProControls.Controls;

/// <summary>
/// Alignement d'un panneau de la barre de statut
/// </summary>
public enum ProStatusBarPanelAlignment
{
    Left,
    Right
}

/// <summary>
/// Panneau de la barre de statut (texte + icône optionnelle)
/// </summary>
public class ProStatusBarPanel
{
    private string _text = "";
    private string? _icon;

    internal ProStatusBar? Owner { get; set; }

    public string Text
    {
        get => _text;
        set { _text = value; Owner?.InvalidateVisual(); }
    }

    /// <summary>Icône emoji/unicode optionnelle affichée avant le texte</summary>
    public string? Icon
    {
        get => _icon;
        set { _icon = value; Owner?.InvalidateVisual(); }
    }

    public ProStatusBarPanelAlignment Alignment { get; set; } = ProStatusBarPanelAlignment.Left;

    /// <summary>Séparateur vertical au lieu d'un panneau de texte</summary>
    public bool IsSeparator { get; set; }

    /// <summary>Déclenché au clic sur le panneau</summary>
    public event EventHandler? Click;

    // État visuel (géré par ProStatusBar)
    internal bool IsHovered { get; set; }

    internal bool IsClickable => Click != null;

    internal void RaiseClick() => Click?.Invoke(this, EventArgs.Empty);

    public static ProStatusBarPanel Separator(ProStatusBarPanelAlignment alignment = ProStatusBarPanelAlignment.Left)
        => new() { IsSeparator = true, Alignment = alignment };
}

/// <summary>
/// Barre de statut en bas de fenêtre (équivalent RibbonStatusBar simple) :
/// panneaux de texte alignés à gauche/droite, séparateurs, panneaux cliquables
/// </summary>
public class ProStatusBar : Control
{
    private const double BarHeight = 26;
    private const double PanelPaddingX = 10;

    private ProStatusBarPanel? _hoveredPanel;
    private ProStatusBarPanel? _pressedPanel;

    public ObservableCollection<ProStatusBarPanel> Items { get; } = new();

    public ProStatusBar()
    {
        Height = BarHeight;
        ClipToBounds = true;

        Items.CollectionChanged += (s, e) =>
        {
            foreach (var item in Items)
                item.Owner = this;
            InvalidateVisual();
        };
    }

    /// <summary>Ajoute un panneau de texte (raccourci fluent)</summary>
    public ProStatusBarPanel AddPanel(string text,
        ProStatusBarPanelAlignment alignment = ProStatusBarPanelAlignment.Left, string? icon = null)
    {
        var panel = new ProStatusBarPanel { Text = text, Alignment = alignment, Icon = icon };
        Items.Add(panel);
        return panel;
    }

    /// <summary>Ajoute un séparateur vertical</summary>
    public void AddSeparator(ProStatusBarPanelAlignment alignment = ProStatusBarPanelAlignment.Left)
        => Items.Add(ProStatusBarPanel.Separator(alignment));

    protected override Size MeasureOverride(Size availableSize)
        => new(availableSize.Width, BarHeight);

    // ═══════════════════════════════════════════════════════════════
    // LAYOUT (partagé entre rendu et hit-test)
    // ═══════════════════════════════════════════════════════════════

    private IEnumerable<(ProStatusBarPanel Panel, Rect Rect)> GetPanelsLayout()
    {
        double leftX = 4;
        double rightX = Bounds.Width - 4;

        foreach (var panel in Items)
        {
            if (panel.Alignment == ProStatusBarPanelAlignment.Left)
            {
                var width = MeasurePanelWidth(panel);
                yield return (panel, new Rect(leftX, 0, width, BarHeight));
                leftX += width;
            }
        }

        // Les panneaux de droite sont posés de droite à gauche, dans l'ordre de la collection
        for (int i = Items.Count - 1; i >= 0; i--)
        {
            var panel = Items[i];
            if (panel.Alignment == ProStatusBarPanelAlignment.Right)
            {
                var width = MeasurePanelWidth(panel);
                rightX -= width;
                yield return (panel, new Rect(rightX, 0, width, BarHeight));
            }
        }
    }

    private double MeasurePanelWidth(ProStatusBarPanel panel)
    {
        if (panel.IsSeparator)
            return 9;

        var text = CreateText(GetPanelText(panel), ProTheme.Text.Secondary);
        return text.Width + PanelPaddingX * 2;
    }

    private static string GetPanelText(ProStatusBarPanel panel)
        => string.IsNullOrEmpty(panel.Icon) ? panel.Text : $"{panel.Icon} {panel.Text}";

    private ProStatusBarPanel? GetPanelAtPosition(Point pos)
    {
        foreach (var (panel, rect) in GetPanelsLayout())
        {
            if (!panel.IsSeparator && rect.Contains(pos))
                return panel;
        }
        return null;
    }

    // ═══════════════════════════════════════════════════════════════
    // SOURIS
    // ═══════════════════════════════════════════════════════════════

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var panel = GetPanelAtPosition(e.GetPosition(this));
        if (panel != _hoveredPanel)
        {
            if (_hoveredPanel != null) _hoveredPanel.IsHovered = false;
            _hoveredPanel = panel;
            if (_hoveredPanel != null) _hoveredPanel.IsHovered = true;
            InvalidateVisual();
        }
        base.OnPointerMoved(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        if (_hoveredPanel != null) _hoveredPanel.IsHovered = false;
        _hoveredPanel = null;
        _pressedPanel = null;
        InvalidateVisual();
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var panel = GetPanelAtPosition(e.GetPosition(this));
        if (panel is { IsClickable: true })
        {
            _pressedPanel = panel;
            e.Handled = true;
        }
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_pressedPanel != null)
        {
            var released = GetPanelAtPosition(e.GetPosition(this));
            var panel = _pressedPanel;
            _pressedPanel = null;

            if (released == panel)
            {
                panel.RaiseClick();
                e.Handled = true;
            }
        }
        base.OnPointerReleased(e);
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);

        // Fond + bordure supérieure
        context.FillRectangle(new SolidColorBrush(ProTheme.Background.Toolbar), bounds);
        context.DrawLine(new Pen(new SolidColorBrush(ProTheme.Border.Subtle), 1),
            new Point(0, 0.5), new Point(bounds.Width, 0.5));

        foreach (var (panel, rect) in GetPanelsLayout())
        {
            if (panel.IsSeparator)
            {
                context.DrawLine(new Pen(new SolidColorBrush(ProTheme.Border.Subtle), 1),
                    new Point(rect.Center.X, 6), new Point(rect.Center.X, BarHeight - 6));
                continue;
            }

            // Survol d'un panneau cliquable
            if (panel.IsHovered && panel.IsClickable)
            {
                context.FillRectangle(new SolidColorBrush(ProTheme.Background.ControlHover),
                    rect.Deflate(new Thickness(2, 3)), 4);
            }

            var text = CreateText(GetPanelText(panel), ProTheme.Text.Secondary);
            context.DrawText(text, new Point(
                rect.X + PanelPaddingX,
                (BarHeight - text.Height) / 2));
        }
    }

    private static FormattedText CreateText(string text, Color color)
        => new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(ProTheme.Typography.FontFamily), 12, new SolidColorBrush(color));
}
