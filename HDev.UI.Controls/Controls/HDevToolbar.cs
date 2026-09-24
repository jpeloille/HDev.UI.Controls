using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using HDev.UI.Controls.Theme;
using System.Collections.ObjectModel;
using System.Globalization;

namespace HDev.UI.Controls;

/// <summary>
/// Classe de base pour les éléments de la barre d'outils
/// </summary>
public abstract class HDevToolbarItem
{
    internal HDevToolbar? Owner { get; set; }

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
public class HDevToolbarButton : HDevToolbarItem
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

    // État visuel (géré par HDevToolbar)
    internal bool IsHovered { get; set; }
    internal bool IsPressed { get; set; }

    public HDevToolbarButton() { }

    public HDevToolbarButton(string? icon, string? label = null, Action? onClick = null)
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
public class HDevToolbarSeparator : HDevToolbarItem
{
}

/// <summary>
/// Barre d'outils horizontale (équivalent Bars simple) : boutons icône/texte,
/// toggles, séparateurs
/// </summary>
public class HDevToolbar : Control
{
    private const double BarHeight = 34;
    private const double ButtonHeight = 26;
    private const double IconOnlyWidth = 28;

    private HDevToolbarButton? _hoveredButton;
    private HDevToolbarButton? _pressedButton;

    public ObservableCollection<HDevToolbarItem> Items { get; } = new();

    public HDevToolbar()
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
    public HDevToolbarButton AddButton(string? icon, string? label = null, Action? onClick = null)
    {
        var button = new HDevToolbarButton(icon, label, onClick);
        Items.Add(button);
        return button;
    }

    /// <summary>Ajoute un bouton toggle (raccourci fluent)</summary>
    public HDevToolbarButton AddToggle(string? icon, string? label = null, bool isChecked = false)
    {
        var button = new HDevToolbarButton(icon, label) { IsToggle = true, IsChecked = isChecked };
        Items.Add(button);
        return button;
    }

    /// <summary>Ajoute un séparateur vertical</summary>
    public void AddSeparator() => Items.Add(new HDevToolbarSeparator());

    protected override Size MeasureOverride(Size availableSize)
        => new(availableSize.Width, BarHeight);

    // ═══════════════════════════════════════════════════════════════
    // LAYOUT (partagé entre rendu et hit-test)
    // ═══════════════════════════════════════════════════════════════

    private IEnumerable<(HDevToolbarItem Item, Rect Rect)> GetItemsLayout()
    {
        double x = 4;
        var y = (BarHeight - ButtonHeight) / 2;

        foreach (var item in Items)
        {
            if (!item.IsVisible)
                continue;

            if (item is HDevToolbarSeparator)
            {
                yield return (item, new Rect(x + 3, 0, 1, BarHeight));
                x += 9;
            }
            else if (item is HDevToolbarButton btn)
            {
                var width = MeasureButtonWidth(btn);
                yield return (btn, new Rect(x, y, width, ButtonHeight));
                x += width + 2;
            }
        }
    }

    private double MeasureButtonWidth(HDevToolbarButton btn)
    {
        if (string.IsNullOrEmpty(btn.Label))
            return IconOnlyWidth;

        var text = CreateText(btn.Label, HDevTheme.Text.Primary);
        var iconWidth = string.IsNullOrEmpty(btn.Icon) ? 0 : 20;
        return 8 + iconWidth + text.Width + 8;
    }

    private HDevToolbarButton? GetButtonAtPosition(Point pos)
    {
        foreach (var (item, rect) in GetItemsLayout())
        {
            if (item is HDevToolbarButton btn && rect.Contains(pos))
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
        Crisp.BeginFrame(this);
        var bounds = new Rect(Bounds.Size);

        // Fond + bordure inférieure
        context.FillRectangle(new SolidColorBrush(HDevTheme.Background.Toolbar), bounds);
        context.DrawLine(new Pen(new SolidColorBrush(HDevTheme.Border.Subtle), 1),
            new Point(0, bounds.Height - 0.5), new Point(bounds.Width, bounds.Height - 0.5));

        foreach (var (item, rect) in GetItemsLayout())
        {
            if (item is HDevToolbarSeparator)
            {
                context.DrawLine(new Pen(new SolidColorBrush(HDevTheme.Border.Subtle), 1),
                    new Point(rect.X + 0.5, 8), new Point(rect.X + 0.5, BarHeight - 8));
            }
            else if (item is HDevToolbarButton btn)
            {
                RenderButton(context, btn, rect);
            }
        }
    }

    private void RenderButton(DrawingContext context, HDevToolbarButton btn, Rect rect)
    {
        var radius = HDevTheme.Size.CornerRadiusSmall;

        // Fond : pressed > checked > hover
        if (btn.IsPressed && btn.IsHovered)
        {
            context.FillRectangle(new SolidColorBrush(HDevTheme.Background.ControlPressed), rect, (float)radius);
        }
        else if (btn.IsChecked)
        {
            context.FillRectangle(new SolidColorBrush(
                HDevTheme.WithOpacity(HDevTheme.Accent.Primary, 30)), rect, (float)radius);
            context.DrawRectangle(null,
                new Pen(new SolidColorBrush(HDevTheme.WithOpacity(HDevTheme.Accent.Primary, 90)), 1),
                rect.Deflate(0.5), radius, radius);
        }
        else if (btn.IsHovered && btn.IsEnabled)
        {
            context.FillRectangle(new SolidColorBrush(HDevTheme.Background.ControlHover), rect, (float)radius);
        }

        var textColor = btn.IsEnabled ? HDevTheme.Text.Primary : HDevTheme.Text.Disabled;
        double contentX = rect.X + (string.IsNullOrEmpty(btn.Label) ? 0 : 8);

        if (!string.IsNullOrEmpty(btn.Icon))
        {
            var icon = CreateText(btn.Icon, textColor);
            var iconX = string.IsNullOrEmpty(btn.Label)
                ? rect.X + (rect.Width - icon.Width) / 2
                : contentX;
            context.DrawText(icon, Crisp.Snap(new Point(iconX, rect.Y + (rect.Height - icon.Height) / 2)));
            contentX += 20;
        }

        if (!string.IsNullOrEmpty(btn.Label))
        {
            var text = CreateText(btn.Label, textColor);
            context.DrawText(text, Crisp.Snap(new Point(contentX, rect.Y + (rect.Height - text.Height) / 2)));
        }
    }

    private static FormattedText CreateText(string text, Color color)
        => new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(HDevTheme.Typography.FontFamily), 13, new SolidColorBrush(color));
}
