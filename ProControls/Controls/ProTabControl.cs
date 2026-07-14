using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ProControls.Theme;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ProControls.Controls;

/// <summary>
/// Page d'un ProTabControl : en-tête + contenu
/// </summary>
public class ProTabPage
{
    private string _header = "";
    private string? _icon;

    internal ProTabControl? Owner { get; set; }

    public string Header
    {
        get => _header;
        set { _header = value; Owner?.InvalidateVisual(); }
    }

    /// <summary>Icône emoji/unicode optionnelle avant le texte</summary>
    public string? Icon
    {
        get => _icon;
        set { _icon = value; Owner?.InvalidateVisual(); }
    }

    /// <summary>Contenu affiché quand la page est active</summary>
    public Control? Content { get; set; }

    /// <summary>Affiche un bouton × sur l'onglet (et autorise le clic milieu)</summary>
    public bool CanClose { get; set; }

    public object? Tag { get; set; }

    public ProTabPage() { }

    public ProTabPage(string header, Control? content = null, string? icon = null)
    {
        _header = header;
        Content = content;
        _icon = icon;
    }
}

/// <summary>
/// Arguments de fermeture d'onglet (annulable)
/// </summary>
public class ProTabClosingEventArgs : System.ComponentModel.CancelEventArgs
{
    public ProTabPage Page { get; }

    public ProTabClosingEventArgs(ProTabPage page)
    {
        Page = page;
    }
}

/// <summary>
/// Contrôle à onglets (équivalent XtraTabControl) : onglets pilule style Yaru,
/// contenu hébergé, fermeture optionnelle par onglet
/// </summary>
public class ProTabControl : Control
{
    private const double StripHeight = 38;
    private const double TabHeight = 28;
    private const double CloseSize = 16;

    private int _selectedIndex = -1;
    private ProTabPage? _hoveredTab;
    private ProTabPage? _pressedTab;
    private ProTabPage? _closeHoveredTab;
    private Control? _hostedContent;

    public ObservableCollection<ProTabPage> Items { get; } = new();

    /// <summary>Déclenché quand l'onglet sélectionné change</summary>
    public event EventHandler? SelectedIndexChanged;

    /// <summary>Déclenché avant la fermeture d'un onglet (annulable)</summary>
    public event EventHandler<ProTabClosingEventArgs>? TabClosing;

    /// <summary>Déclenché après la fermeture d'un onglet</summary>
    public event EventHandler<ProTabPage>? TabClosed;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var clamped = Items.Count == 0 ? -1 : Math.Clamp(value, 0, Items.Count - 1);
            if (_selectedIndex == clamped) return;
            _selectedIndex = clamped;
            AttachSelectedContent();
            InvalidateVisual();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ProTabPage? SelectedTab
        => _selectedIndex >= 0 && _selectedIndex < Items.Count ? Items[_selectedIndex] : null;

    static ProTabControl()
    {
        FocusableProperty.OverrideDefaultValue<ProTabControl>(true);
    }

    public ProTabControl()
    {
        ClipToBounds = true;

        Items.CollectionChanged += (s, e) =>
        {
            foreach (var page in Items)
                page.Owner = this;

            // Garder une sélection valide (première page par défaut)
            if (Items.Count == 0)
                SelectedIndex = -1;
            else if (_selectedIndex < 0 || _selectedIndex >= Items.Count)
                SelectedIndex = Math.Clamp(_selectedIndex, 0, Items.Count - 1);
            else
                AttachSelectedContent(); // le contenu sélectionné a pu changer d'index

            InvalidateVisual();
        };
    }

    /// <summary>Ajoute une page (raccourci fluent)</summary>
    public ProTabPage AddPage(string header, Control? content = null, string? icon = null)
    {
        var page = new ProTabPage(header, content, icon);
        Items.Add(page);
        return page;
    }

    /// <summary>Ferme une page (après TabClosing, annulable)</summary>
    public void ClosePage(ProTabPage page)
    {
        if (!Items.Contains(page)) return;

        var args = new ProTabClosingEventArgs(page);
        TabClosing?.Invoke(this, args);
        if (args.Cancel) return;

        var index = Items.IndexOf(page);
        Items.Remove(page);

        if (_selectedIndex >= Items.Count)
            SelectedIndex = Items.Count - 1;
        else if (index <= _selectedIndex)
            AttachSelectedContent();

        TabClosed?.Invoke(this, page);
        InvalidateVisual();
    }

    // ═══════════════════════════════════════════════════════════════
    // CONTENU HÉBERGÉ
    // ═══════════════════════════════════════════════════════════════

    private void AttachSelectedContent()
    {
        var newContent = SelectedTab?.Content;
        if (ReferenceEquals(_hostedContent, newContent)) return;

        if (_hostedContent != null)
        {
            VisualChildren.Remove(_hostedContent);
            LogicalChildren.Remove(_hostedContent);
        }

        _hostedContent = newContent;

        if (_hostedContent != null)
        {
            VisualChildren.Add(_hostedContent);
            LogicalChildren.Add(_hostedContent);
        }

        InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var contentSize = new Size(
            availableSize.Width,
            Math.Max(0, availableSize.Height - StripHeight));
        _hostedContent?.Measure(contentSize);

        var desiredContentHeight = _hostedContent?.DesiredSize.Height ?? 0;
        var height = double.IsInfinity(availableSize.Height)
            ? StripHeight + desiredContentHeight
            : availableSize.Height;

        return new Size(
            double.IsInfinity(availableSize.Width)
                ? Math.Max(_hostedContent?.DesiredSize.Width ?? 0, 200)
                : availableSize.Width,
            height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _hostedContent?.Arrange(new Rect(
            0, StripHeight, finalSize.Width, Math.Max(0, finalSize.Height - StripHeight)));
        return finalSize;
    }

    // ═══════════════════════════════════════════════════════════════
    // LAYOUT DES ONGLETS (partagé rendu / hit-test)
    // ═══════════════════════════════════════════════════════════════

    private IEnumerable<(ProTabPage Page, Rect Rect, Rect CloseRect)> GetTabsLayout()
    {
        double x = 8;
        var y = (StripHeight - TabHeight) / 2;

        foreach (var page in Items)
        {
            var width = MeasureTabWidth(page);
            var rect = new Rect(x, y, width, TabHeight);

            var closeRect = page.CanClose
                ? new Rect(rect.Right - CloseSize - 6, rect.Y + (TabHeight - CloseSize) / 2, CloseSize, CloseSize)
                : default;

            yield return (page, rect, closeRect);
            x += width + 4;
        }
    }

    private double MeasureTabWidth(ProTabPage page)
    {
        var text = CreateText(GetTabText(page), ProTheme.Text.Primary);
        return 12 + text.Width + 12 + (page.CanClose ? CloseSize + 4 : 0);
    }

    private static string GetTabText(ProTabPage page)
        => string.IsNullOrEmpty(page.Icon) ? page.Header : $"{page.Icon} {page.Header}";

    private (ProTabPage? Page, bool OnClose) HitTest(Point pos)
    {
        foreach (var (page, rect, closeRect) in GetTabsLayout())
        {
            if (page.CanClose && closeRect.Contains(pos))
                return (page, true);
            if (rect.Contains(pos))
                return (page, false);
        }
        return (null, false);
    }

    // ═══════════════════════════════════════════════════════════════
    // SOURIS
    // ═══════════════════════════════════════════════════════════════

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var (page, onClose) = HitTest(e.GetPosition(this));
        var closeHovered = onClose ? page : null;

        if (page != _hoveredTab || closeHovered != _closeHoveredTab)
        {
            _hoveredTab = page;
            _closeHoveredTab = closeHovered;
            InvalidateVisual();
        }
        base.OnPointerMoved(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _hoveredTab = null;
        _pressedTab = null;
        _closeHoveredTab = null;
        InvalidateVisual();
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var pos = e.GetPosition(this);
        if (pos.Y > StripHeight)
        {
            base.OnPointerPressed(e);
            return;
        }

        var (page, onClose) = HitTest(pos);
        if (page == null)
        {
            base.OnPointerPressed(e);
            return;
        }

        var point = e.GetCurrentPoint(this);

        // Clic milieu = fermer (si fermable)
        if (point.Properties.IsMiddleButtonPressed)
        {
            if (page.CanClose)
                ClosePage(page);
            e.Handled = true;
            return;
        }

        if (onClose)
        {
            ClosePage(page);
            e.Handled = true;
            return;
        }

        _pressedTab = page;
        Focus();
        SelectedIndex = Items.IndexOf(page);
        e.Handled = true;
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _pressedTab = null;
        InvalidateVisual();
        base.OnPointerReleased(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
                SelectedIndex = Math.Max(0, _selectedIndex - 1);
                e.Handled = true;
                break;
            case Key.Right:
                SelectedIndex = Math.Min(Items.Count - 1, _selectedIndex + 1);
                e.Handled = true;
                break;
        }
        base.OnKeyDown(e);
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);

        // Bandeau des onglets + séparateur bas
        context.FillRectangle(new SolidColorBrush(ProTheme.Background.Toolbar),
            new Rect(0, 0, bounds.Width, StripHeight));
        context.DrawLine(new Pen(new SolidColorBrush(ProTheme.Border.Subtle), 1),
            new Point(0, StripHeight - 0.5), new Point(bounds.Width, StripHeight - 0.5));

        foreach (var (page, rect, closeRect) in GetTabsLayout())
        {
            var isSelected = ReferenceEquals(page, SelectedTab);
            var isHovered = ReferenceEquals(page, _hoveredTab);
            var isPressed = ReferenceEquals(page, _pressedTab);
            var radius = TabHeight / 2;

            // Pilule (cf. onglets du ruban)
            if (isSelected)
            {
                context.DrawRectangle(new SolidColorBrush(ProTheme.Background.Panel),
                    new Pen(new SolidColorBrush(ProTheme.Border.Default), 1),
                    rect.Deflate(0.5), radius, radius);
            }
            else if (isPressed)
            {
                context.DrawRectangle(new SolidColorBrush(ProTheme.Background.ControlPressed),
                    null, rect, radius, radius);
            }
            else if (isHovered)
            {
                context.DrawRectangle(new SolidColorBrush(ProTheme.Background.ControlHover),
                    null, rect, radius, radius);
            }

            var textColor = isSelected ? ProTheme.Text.Primary : ProTheme.Text.Secondary;
            var text = CreateText(GetTabText(page), textColor);
            context.DrawText(text, Crisp.Snap(new Point(
                rect.X + 12,
                rect.Y + (rect.Height - text.Height) / 2)));

            // Bouton fermer ×
            if (page.CanClose)
            {
                if (ReferenceEquals(page, _closeHoveredTab))
                {
                    context.DrawRectangle(new SolidColorBrush(ProTheme.Background.ControlPressed),
                        null, closeRect, CloseSize / 2, CloseSize / 2);
                }

                var pen = new Pen(new SolidColorBrush(textColor), 1.2);
                var c = closeRect.Center;
                context.DrawLine(pen, new Point(c.X - 3, c.Y - 3), new Point(c.X + 3, c.Y + 3));
                context.DrawLine(pen, new Point(c.X - 3, c.Y + 3), new Point(c.X + 3, c.Y - 3));
            }
        }

        // Fond de la zone de contenu
        context.FillRectangle(new SolidColorBrush(ProTheme.Background.Panel),
            new Rect(0, StripHeight, bounds.Width, Math.Max(0, bounds.Height - StripHeight)));
    }

    private static FormattedText CreateText(string text, Color color)
        => new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(ProTheme.Typography.FontFamily), ProTheme.Typography.FontSizeBody,
            new SolidColorBrush(color));
}
