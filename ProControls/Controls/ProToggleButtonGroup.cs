using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ProControls.Theme;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace ProControls.Controls;

/// <summary>Mode de sélection du groupe segmenté</summary>
public enum ToggleGroupSelectionMode
{
    /// <summary>Exactement un segment actif (exclusif, façon RadioGroup)</summary>
    Single,
    /// <summary>Chaque segment bascule indépendamment</summary>
    Multiple
}

/// <summary>Segment d'un ProToggleButtonGroup</summary>
public class ProToggleButtonItem
{
    private string _text = "";
    private bool _isSelected;

    internal ProToggleButtonGroup? Owner { get; set; }

    public string Text
    {
        get => _text;
        set { _text = value; Owner?.Refresh(); }
    }

    /// <summary>Icône emoji/unicode optionnelle (seule ou avant le texte)</summary>
    public string? Icon { get; set; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            if (Owner != null) Owner.SetSelected(this, value);
            else _isSelected = value;
        }
    }

    internal void SetSelectedSilent(bool value) => _isSelected = value;

    public bool IsEnabled { get; set; } = true;

    public object? Tag { get; set; }

    public ProToggleButtonItem() { }

    public ProToggleButtonItem(string text, string? icon = null)
    {
        _text = text;
        Icon = icon;
    }
}

/// <summary>
/// Groupe de boutons à bascule (segmented control, équivalent de la barre
/// Jour/Semaine/Mois d'un agenda) : segments côte à côte dans une pilule,
/// exclusif (Single) ou cumulable (Multiple). Clavier : ←→ (Single = change
/// la sélection ; Multiple = déplace le focus), Espace/Entrée bascule.
/// </summary>
public class ProToggleButtonGroup : Control
{
    private const double GroupHeight = 30;

    private ProToggleButtonItem? _hoveredItem;
    private ProToggleButtonItem? _pressedItem;
    private int _focusIndex = -1;

    public ObservableCollection<ProToggleButtonItem> Items { get; } = new();

    public ToggleGroupSelectionMode SelectionMode { get; set; } = ToggleGroupSelectionMode.Single;

    /// <summary>Sélection modifiée (clic ou clavier)</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>Index du segment actif (mode Single ; -1 si aucun)</summary>
    public int SelectedIndex
    {
        get
        {
            for (int i = 0; i < Items.Count; i++)
                if (Items[i].IsSelected) return i;
            return -1;
        }
        set
        {
            if (value < 0 || value >= Items.Count) return;
            SetSelected(Items[value], true);
        }
    }

    /// <summary>Segments actifs (utile en mode Multiple)</summary>
    public IEnumerable<ProToggleButtonItem> SelectedItems => Items.Where(i => i.IsSelected);

    static ProToggleButtonGroup()
    {
        FocusableProperty.OverrideDefaultValue<ProToggleButtonGroup>(true);
    }

    public ProToggleButtonGroup()
    {
        Items.CollectionChanged += (s, e) =>
        {
            foreach (var item in Items)
                item.Owner = this;
            Refresh();
        };
    }

    /// <summary>Ajoute un segment (raccourci fluent)</summary>
    public ProToggleButtonItem Add(string text, string? icon = null, bool isSelected = false)
    {
        var item = new ProToggleButtonItem(text, icon);
        Items.Add(item);
        if (isSelected) SetSelected(item, true);
        return item;
    }

    internal void Refresh()
    {
        InvalidateMeasure();
        InvalidateVisual();
    }

    /// <summary>Applique la politique de sélection (exclusivité en Single)</summary>
    internal void SetSelected(ProToggleButtonItem item, bool selected)
    {
        if (item.IsSelected == selected) return;

        if (SelectionMode == ToggleGroupSelectionMode.Single)
        {
            // Exclusif : on ne désélectionne pas le segment actif au clic
            if (!selected) return;
            foreach (var other in Items)
                if (!ReferenceEquals(other, item))
                    other.SetSelectedSilent(false);
        }

        item.SetSelectedSilent(selected);
        InvalidateVisual();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    // ═══════════════════════════════════════════════════════════════
    // LAYOUT (partagé rendu / hit-test)
    // ═══════════════════════════════════════════════════════════════

    private string DisplayText(ProToggleButtonItem item)
        => string.IsNullOrEmpty(item.Icon) ? item.Text
            : string.IsNullOrEmpty(item.Text) ? item.Icon! : $"{item.Icon} {item.Text}";

    private double SegmentWidth(ProToggleButtonItem item)
        => 14 + CreateText(DisplayText(item), ProTheme.Text.Primary).Width + 14;

    private IEnumerable<(ProToggleButtonItem Item, Rect Rect)> SegmentsLayout()
    {
        double x = 0;
        foreach (var item in Items)
        {
            var width = SegmentWidth(item);
            yield return (item, new Rect(x, 0, width, GroupHeight));
            x += width;
        }
    }

    private ProToggleButtonItem? HitTestSegment(Point pos)
    {
        foreach (var (item, rect) in SegmentsLayout())
            if (rect.Contains(pos))
                return item;
        return null;
    }

    protected override Size MeasureOverride(Size availableSize)
        => new(Items.Sum(SegmentWidth), GroupHeight);

    // ═══════════════════════════════════════════════════════════════
    // SOURIS / CLAVIER
    // ═══════════════════════════════════════════════════════════════

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var item = HitTestSegment(e.GetPosition(this));
        if (item != _hoveredItem)
        {
            _hoveredItem = item;
            InvalidateVisual();
        }
        base.OnPointerMoved(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _hoveredItem = null;
        _pressedItem = null;
        InvalidateVisual();
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var item = HitTestSegment(e.GetPosition(this));
        if (item is { IsEnabled: true })
        {
            Focus();
            _pressedItem = item;
            _focusIndex = Items.IndexOf(item);
            Toggle(item);
            InvalidateVisual();
            e.Handled = true;
            return;
        }
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _pressedItem = null;
        InvalidateVisual();
        base.OnPointerReleased(e);
    }

    private void Toggle(ProToggleButtonItem item)
    {
        if (SelectionMode == ToggleGroupSelectionMode.Single)
            SetSelected(item, true);
        else
            SetSelected(item, !item.IsSelected);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (Items.Count == 0)
        {
            base.OnKeyDown(e);
            return;
        }

        switch (e.Key)
        {
            case Key.Left:
            case Key.Right:
                var delta = e.Key == Key.Right ? 1 : -1;
                var start = _focusIndex >= 0 ? _focusIndex : SelectedIndex;
                var next = NextEnabled(start, delta);
                if (next >= 0)
                {
                    _focusIndex = next;
                    // Single : les flèches CHANGENT la sélection (idiome RadioGroup)
                    if (SelectionMode == ToggleGroupSelectionMode.Single)
                        SetSelected(Items[next], true);
                    InvalidateVisual();
                }
                e.Handled = true;
                break;

            case Key.Space:
            case Key.Enter:
                if (_focusIndex >= 0 && _focusIndex < Items.Count && Items[_focusIndex].IsEnabled)
                {
                    Toggle(Items[_focusIndex]);
                    e.Handled = true;
                }
                break;
        }
        base.OnKeyDown(e);
    }

    private int NextEnabled(int from, int delta)
    {
        var i = from;
        for (int step = 0; step < Items.Count; step++)
        {
            i += delta;
            if (i < 0 || i >= Items.Count) return from >= 0 ? from : -1;
            if (Items[i].IsEnabled) return i;
        }
        return -1;
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        if (_focusIndex < 0) _focusIndex = Math.Max(0, SelectedIndex);
        InvalidateVisual();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        InvalidateVisual();
        base.OnLostFocus(e);
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════

    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);
        var bounds = new Rect(0, 0, Items.Sum(SegmentWidth), GroupHeight);
        if (bounds.Width <= 0) return;
        var radius = GroupHeight / 2;

        // Enveloppe pilule
        context.DrawRectangle(new SolidColorBrush(ProTheme.Background.Toolbar),
            new Pen(new SolidColorBrush(ProTheme.Border.Default), 1),
            bounds.Deflate(0.5), radius, radius);

        // Segments (clip à la pilule pour que les extrémités restent rondes)
        using (context.PushGeometryClip(new RectangleGeometry(bounds, radius, radius)))
        {
            var index = 0;
            foreach (var (item, rect) in SegmentsLayout())
            {
                var isSelected = item.IsSelected;
                var isHovered = ReferenceEquals(item, _hoveredItem) && item.IsEnabled;
                var isPressed = ReferenceEquals(item, _pressedItem);

                if (isSelected)
                {
                    context.FillRectangle(new SolidColorBrush(ProTheme.Accent.Primary), rect);
                }
                else if (isPressed)
                {
                    context.FillRectangle(new SolidColorBrush(ProTheme.Background.ControlPressed), rect);
                }
                else if (isHovered)
                {
                    context.FillRectangle(new SolidColorBrush(ProTheme.Background.ControlHover), rect);
                }

                // Séparateur entre segments (pas après le dernier, pas contre un actif)
                if (index < Items.Count - 1 && !isSelected && !Items[index + 1].IsSelected)
                {
                    context.DrawLine(new Pen(new SolidColorBrush(ProTheme.Border.Subtle), 1),
                        new Point(rect.Right - 0.5, rect.Y + 6),
                        new Point(rect.Right - 0.5, rect.Bottom - 6));
                }

                var textColor = !item.IsEnabled ? ProTheme.Text.Disabled
                    : isSelected ? ProTheme.Text.OnAccent
                    : ProTheme.Text.Primary;
                var text = CreateText(DisplayText(item), textColor,
                    isSelected ? FontWeight.SemiBold : FontWeight.Regular);
                context.DrawText(text, Crisp.Snap(new Point(
                    rect.X + (rect.Width - text.Width) / 2,
                    rect.Y + (rect.Height - text.Height) / 2)));

                // Focus clavier sur le segment courant
                if (IsFocused && index == _focusIndex)
                {
                    context.DrawRectangle(null,
                        new Pen(new SolidColorBrush(ProTheme.WithOpacity(ProTheme.Border.FocusOuter, 130)), 1.5),
                        rect.Deflate(2), radius - 2, radius - 2);
                }

                index++;
            }
        }
    }

    private static FormattedText CreateText(string text, Color color, FontWeight weight = FontWeight.Regular)
        => new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(ProTheme.Typography.FontFamily, FontStyle.Normal, weight),
            ProTheme.Typography.FontSizeBody - 1, new SolidColorBrush(color));
}
