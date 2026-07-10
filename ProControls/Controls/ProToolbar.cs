using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ProControls.Theme;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ProControls.Controls;

/// <summary>
/// Classe de base pour les éléments de la barre d'outils
/// </summary>
public abstract class ProToolbarItem
{
    internal ProToolbar? Owner { get; set; }

    private bool _isEnabled = true;
    private bool _isVisible = true;

    public bool IsEnabled
    {
        get => _isEnabled;
        set { _isEnabled = value; Owner?.InvalidateVisual(); }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set { _isVisible = value; Owner?.InvalidateVisual(); }
    }
}

/// <summary>
/// Bouton de barre d'outils : icône, label optionnel, mode toggle
/// </summary>
public class ProToolbarButton : ProToolbarItem
{
    private string? _icon;
    private string? _label;
    private bool _isChecked;

    public string? Icon
    {
        get => _icon;
        set { _icon = value; Owner?.InvalidateVisual(); }
    }

    /// <summary>Label affiché à côté de l'icône (bouton icône seule si null)</summary>
    public string? Label
    {
        get => _label;
        set { _label = value; Owner?.InvalidateVisual(); }
    }

    public string? Tooltip { get; set; }

    /// <summary>Bouton à bascule : le clic inverse IsChecked</summary>
    public bool IsToggle { get; set; }

    public bool IsChecked
    {
        get => _isChecked;
        set { _isChecked = value; Owner?.InvalidateVisual(); }
    }

    public event EventHandler? Click;

    // État visuel (géré par ProToolbar)
    internal bool IsHovered { get; set; }
    internal bool IsPressed { get; set; }

    public ProToolbarButton() { }

    public ProToolbarButton(string? icon, string? label = null, Action? onClick = null)
    {
        _icon = icon;
        _label = label;
        if (onClick != null)
            Click += (s, e) => onClick();
    }

    internal void RaiseClick()
    {
        if (!IsEnabled) return;

        if (IsToggle)
            IsChecked = !IsChecked;

        Click?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Séparateur vertical de barre d'outils
/// </summary>
public class ProToolbarSeparator : ProToolbarItem
{
}

/// <summary>
/// Barre d'outils horizontale (équivalent Bars simple) : boutons icône/texte,
/// toggles, séparateurs
/// </summary>
public class ProToolbar : Control
{
    private const double BarHeight = 34;
    private const double ButtonHeight = 26;
    private const double IconOnlyWidth = 28;

    private ProToolbarButton? _hoveredButton;
    private ProToolbarButton? _pressedButton;

    public ObservableCollection<ProToolbarItem> Items { get; } = new();

    public ProToolbar()
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

    /// <summary>Ajoute un bouton (raccourci fluent)</summary>
    public ProToolbarButton AddButton(string? icon, string? label = null, Action? onClick = null)
    {
        var button = new ProToolbarButton(icon, label, onClick);
        Items.Add(button);
        return button;
    }

    /// <summary>Ajoute un bouton toggle (raccourci fluent)</summary>
    public ProToolbarButton AddToggle(string? icon, string? label = null, bool isChecked = false)
    {
        var button = new ProToolbarButton(icon, label) { IsToggle = true, IsChecked = isChecked };
        Items.Add(button);
        return button;
    }

    /// <summary>Ajoute un séparateur vertical</summary>
    public void AddSeparator() => Items.Add(new ProToolbarSeparator());

    protected override Size MeasureOverride(Size availableSize)
        => new(availableSize.Width, BarHeight);

    // ═══════════════════════════════════════════════════════════════
    // LAYOUT (partagé entre rendu et hit-test)
    // ═══════════════════════════════════════════════════════════════

    private IEnumerable<(ProToolbarItem Item, Rect Rect)> GetItemsLayout()
    {
        double x = 4;
        var y = (BarHeight - ButtonHeight) / 2;

        foreach (var item in Items)
        {
            if (!item.IsVisible)
                continue;

            if (item is ProToolbarSeparator)
            {
                yield return (item, new Rect(x + 3, 0, 1, BarHeight));
                x += 9;
            }
            else if (item is ProToolbarButton btn)
            {
                var width = MeasureButtonWidth(btn);
                yield return (btn, new Rect(x, y, width, ButtonHeight));
                x += width + 2;
            }
        }
    }

    private double MeasureButtonWidth(ProToolbarButton btn)
    {
        if (string.IsNullOrEmpty(btn.Label))
            return IconOnlyWidth;

        var text = CreateText(btn.Label, ProTheme.Text.Primary);
        var iconWidth = string.IsNullOrEmpty(btn.Icon) ? 0 : 20;
        return 8 + iconWidth + text.Width + 8;
    }

    private ProToolbarButton? GetButtonAtPosition(Point pos)
    {
        foreach (var (item, rect) in GetItemsLayout())
        {
            if (item is ProToolbarButton btn && rect.Contains(pos))
                return btn;
        }
        return null;
    }

    // ═══════════════════════════════════════════════════════════════
    // SOURIS
    // ═══════════════════════════════════════════════════════════════

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var btn = GetButtonAtPosition(e.GetPosition(this));
        if (btn != _hoveredButton)
        {
            if (_hoveredButton != null) _hoveredButton.IsHovered = false;
            _hoveredButton = btn;
            if (_hoveredButton != null) _hoveredButton.IsHovered = true;
            InvalidateVisual();
        }
        base.OnPointerMoved(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        if (_hoveredButton != null) _hoveredButton.IsHovered = false;
        if (_pressedButton != null) _pressedButton.IsPressed = false;
        _hoveredButton = null;
        _pressedButton = null;
        InvalidateVisual();
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var btn = GetButtonAtPosition(e.GetPosition(this));
        if (btn is { IsEnabled: true })
        {
            _pressedButton = btn;
            btn.IsPressed = true;
            InvalidateVisual();
            e.Handled = true;
        }
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_pressedButton != null)
        {
            var released = GetButtonAtPosition(e.GetPosition(this));
            var btn = _pressedButton;
            btn.IsPressed = false;
            _pressedButton = null;
            InvalidateVisual();

            if (released == btn)
            {
                btn.RaiseClick();
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

        // Fond + bordure inférieure
        context.FillRectangle(new SolidColorBrush(ProTheme.Background.Toolbar), bounds);
        context.DrawLine(new Pen(new SolidColorBrush(ProTheme.Border.Subtle), 1),
            new Point(0, bounds.Height - 0.5), new Point(bounds.Width, bounds.Height - 0.5));

        foreach (var (item, rect) in GetItemsLayout())
        {
            if (item is ProToolbarSeparator)
            {
                context.DrawLine(new Pen(new SolidColorBrush(ProTheme.Border.Subtle), 1),
                    new Point(rect.X + 0.5, 8), new Point(rect.X + 0.5, BarHeight - 8));
            }
            else if (item is ProToolbarButton btn)
            {
                RenderButton(context, btn, rect);
            }
        }
    }

    private void RenderButton(DrawingContext context, ProToolbarButton btn, Rect rect)
    {
        var radius = ProTheme.Size.CornerRadiusSmall;

        // Fond : pressed > checked > hover
        if (btn.IsPressed && btn.IsHovered)
        {
            context.FillRectangle(new SolidColorBrush(ProTheme.Background.ControlPressed), rect, (float)radius);
        }
        else if (btn.IsChecked)
        {
            context.FillRectangle(new SolidColorBrush(
                ProTheme.WithOpacity(ProTheme.Accent.Primary, 30)), rect, (float)radius);
            context.DrawRectangle(null,
                new Pen(new SolidColorBrush(ProTheme.WithOpacity(ProTheme.Accent.Primary, 90)), 1),
                rect.Deflate(0.5), radius, radius);
        }
        else if (btn.IsHovered && btn.IsEnabled)
        {
            context.FillRectangle(new SolidColorBrush(ProTheme.Background.ControlHover), rect, (float)radius);
        }

        var textColor = btn.IsEnabled ? ProTheme.Text.Primary : ProTheme.Text.Disabled;
        double contentX = rect.X + (string.IsNullOrEmpty(btn.Label) ? 0 : 8);

        if (!string.IsNullOrEmpty(btn.Icon))
        {
            var icon = CreateText(btn.Icon, textColor);
            var iconX = string.IsNullOrEmpty(btn.Label)
                ? rect.X + (rect.Width - icon.Width) / 2
                : contentX;
            context.DrawText(icon, new Point(iconX, rect.Y + (rect.Height - icon.Height) / 2));
            contentX += 20;
        }

        if (!string.IsNullOrEmpty(btn.Label))
        {
            var text = CreateText(btn.Label, textColor);
            context.DrawText(text, new Point(contentX, rect.Y + (rect.Height - text.Height) / 2));
        }
    }

    private static FormattedText CreateText(string text, Color color)
        => new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(ProTheme.Typography.FontFamily), 13, new SolidColorBrush(color));
}
