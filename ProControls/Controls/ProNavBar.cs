using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ProControls.Theme;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ProControls.Controls;

/// <summary>Module du rail de navigation</summary>
public class ProNavBarItem
{
    private string? _badge;

    internal ProNavBar? Owner { get; set; }

    public string Icon { get; set; } = "";
    public string Label { get; set; } = "";

    /// <summary>Pastille (compteur non-lus...) ; null = masquée</summary>
    public string? Badge
    {
        get => _badge;
        set { _badge = value; Owner?.InvalidateVisual(); }
    }

    public object? Tag { get; set; }

    public ProNavBarItem() { }

    public ProNavBarItem(string icon, string label)
    {
        Icon = icon;
        Label = label;
    }
}

/// <summary>
/// Rail vertical de modules (équivalent barre de navigation Outlook :
/// Courrier / Calendrier / Contacts / Tâches) — icône + label + badge,
/// mode compact (icônes seules)
/// </summary>
public class ProNavBar : Control
{
    private const double ItemHeight = 56;
    private const double CompactWidth = 52;
    private const double WideWidth = 96;

    private int _selectedIndex;
    private int _hoveredIndex = -1;
    private bool _isCompact;

    public ObservableCollection<ProNavBarItem> Items { get; } = new();

    /// <summary>Déclenché quand le module sélectionné change</summary>
    public event EventHandler? SelectedIndexChanged;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var clamped = Items.Count == 0 ? 0 : Math.Clamp(value, 0, Items.Count - 1);
            if (_selectedIndex == clamped) return;
            _selectedIndex = clamped;
            InvalidateVisual();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ProNavBarItem? SelectedItem
        => _selectedIndex >= 0 && _selectedIndex < Items.Count ? Items[_selectedIndex] : null;

    /// <summary>Icônes seules (rail étroit)</summary>
    public bool IsCompact
    {
        get => _isCompact;
        set
        {
            if (_isCompact == value) return;
            _isCompact = value;
            Width = value ? CompactWidth : WideWidth;
            InvalidateVisual();
        }
    }

    static ProNavBar()
    {
        FocusableProperty.OverrideDefaultValue<ProNavBar>(true);
    }

    public ProNavBar()
    {
        Width = WideWidth;
        ClipToBounds = true;

        Items.CollectionChanged += (s, e) =>
        {
            foreach (var item in Items)
                item.Owner = this;
            InvalidateVisual();
        };
    }

    /// <summary>Ajoute un module (raccourci fluent)</summary>
    public ProNavBarItem Add(string icon, string label)
    {
        var item = new ProNavBarItem(icon, label);
        Items.Add(item);
        return item;
    }

    private int IndexAt(Point pos)
    {
        var index = (int)(pos.Y / ItemHeight);
        return index >= 0 && index < Items.Count ? index : -1;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var index = IndexAt(e.GetPosition(this));
        if (index != _hoveredIndex)
        {
            _hoveredIndex = index;
            ToolTip.SetTip(this, _isCompact && index >= 0 ? Items[index].Label : null);
            InvalidateVisual();
        }
        base.OnPointerMoved(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _hoveredIndex = -1;
        InvalidateVisual();
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Focus();
        var index = IndexAt(e.GetPosition(this));
        if (index >= 0)
        {
            SelectedIndex = index;
            e.Handled = true;
        }
        base.OnPointerPressed(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
                SelectedIndex = Math.Max(0, _selectedIndex - 1);
                e.Handled = true;
                break;
            case Key.Down:
                SelectedIndex = Math.Min(Items.Count - 1, _selectedIndex + 1);
                e.Handled = true;
                break;
        }
        base.OnKeyDown(e);
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(new SolidColorBrush(ProTheme.Background.Toolbar), bounds);
        context.DrawLine(new Pen(new SolidColorBrush(ProTheme.Border.Subtle), 1),
            new Point(bounds.Width - 0.5, 0), new Point(bounds.Width - 0.5, bounds.Height));

        for (int i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            var rect = new Rect(0, i * ItemHeight, bounds.Width, ItemHeight);
            var isSelected = i == _selectedIndex;
            var isHovered = i == _hoveredIndex;

            if (isSelected)
            {
                context.FillRectangle(new SolidColorBrush(
                    ProTheme.WithOpacity(ProTheme.Accent.Primary, 26)), rect);
                context.FillRectangle(new SolidColorBrush(ProTheme.Accent.Primary),
                    new Rect(0, rect.Y + 10, 3, ItemHeight - 20));
            }
            else if (isHovered)
            {
                context.FillRectangle(new SolidColorBrush(ProTheme.Background.ControlHover), rect);
            }

            var iconColor = isSelected ? ProTheme.Accent.Primary : ProTheme.Text.Secondary;
            var icon = Text(item.Icon, iconColor, 20);
            var iconY = _isCompact
                ? rect.Y + (ItemHeight - icon.Height) / 2
                : rect.Y + 8;
            context.DrawText(icon, Crisp.Snap(new Point(
                (bounds.Width - icon.Width) / 2, iconY)));

            if (!_isCompact)
            {
                var label = Text(item.Label, iconColor, 11,
                    isSelected ? FontWeight.SemiBold : FontWeight.Regular);
                context.DrawText(label, Crisp.Snap(new Point(
                    (bounds.Width - label.Width) / 2, rect.Y + ItemHeight - label.Height - 6)));
            }

            // Badge
            if (!string.IsNullOrEmpty(item.Badge))
            {
                var badge = Text(item.Badge, Colors.White, 9, FontWeight.SemiBold);
                var badgeWidth = Math.Max(15, badge.Width + 8);
                var badgeRect = new Rect(
                    bounds.Width / 2 + 6, rect.Y + 5, badgeWidth, 14);
                context.DrawRectangle(new SolidColorBrush(ProTheme.Accent.Error), null,
                    badgeRect, 7, 7);
                context.DrawText(badge, Crisp.Snap(new Point(
                    badgeRect.Center.X - badge.Width / 2, badgeRect.Center.Y - badge.Height / 2)));
            }
        }
    }

    private static FormattedText Text(string text, Color color, double size,
        FontWeight weight = FontWeight.Regular)
        => new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(ProTheme.Typography.FontFamily, weight: weight), size,
            new SolidColorBrush(color));
}
