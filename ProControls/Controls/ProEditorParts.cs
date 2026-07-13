using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ProControls.Theme;
using System.Globalization;

namespace ProControls.Controls;

/// <summary>
/// Petit bouton à glyphe pour la zone droite des éditeurs (calendrier, effacer...)
/// </summary>
public class ProEditorGlyphButton : Control
{
    private bool _isHovered;
    private bool _isPressed;

    public string Glyph { get; set; } = "";
    public double GlyphSize { get; set; } = 14;

    public event EventHandler? Click;

    public ProEditorGlyphButton()
    {
        Width = 26;
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        _isHovered = true;
        InvalidateVisual();
        base.OnPointerEntered(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _isHovered = false;
        _isPressed = false;
        InvalidateVisual();
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _isPressed = true;
        InvalidateVisual();
        e.Handled = true;
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_isPressed && _isHovered)
            Click?.Invoke(this, EventArgs.Empty);
        _isPressed = false;
        InvalidateVisual();
        e.Handled = true;
        base.OnPointerReleased(e);
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);

        if (_isHovered)
        {
            var bg = _isPressed ? ProTheme.Background.ControlPressed : ProTheme.Background.ControlHover;
            context.FillRectangle(new SolidColorBrush(bg),
                bounds.Deflate(new Thickness(2, 3)), 4);
        }

        var text = new FormattedText(Glyph, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(ProTheme.Typography.FontFamily), GlyphSize,
            new SolidColorBrush(ProTheme.Text.Secondary));
        context.DrawText(text, Crisp.Snap(new Point(
            (bounds.Width - text.Width) / 2,
            (bounds.Height - text.Height) / 2)));
    }
}

/// <summary>
/// Boutons spin (▲/▼) empilés pour les éditeurs numériques
/// </summary>
public class ProSpinButtons : Control
{
    private bool _upHovered;
    private bool _downHovered;
    private bool _pressed;

    public event EventHandler? UpClicked;
    public event EventHandler? DownClicked;

    public ProSpinButtons()
    {
        Width = 20;
    }

    private bool IsUpZone(Point p) => p.Y < Bounds.Height / 2;

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var p = e.GetPosition(this);
        var up = IsUpZone(p);
        if (up != _upHovered || up == _downHovered)
        {
            _upHovered = up;
            _downHovered = !up;
            InvalidateVisual();
        }
        base.OnPointerMoved(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _upHovered = _downHovered = _pressed = false;
        InvalidateVisual();
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _pressed = true;
        if (IsUpZone(e.GetPosition(this)))
            UpClicked?.Invoke(this, EventArgs.Empty);
        else
            DownClicked?.Invoke(this, EventArgs.Empty);
        InvalidateVisual();
        e.Handled = true;
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _pressed = false;
        InvalidateVisual();
        e.Handled = true;
        base.OnPointerReleased(e);
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        var half = bounds.Height / 2;
        var upRect = new Rect(2, 2, bounds.Width - 4, half - 3);
        var downRect = new Rect(2, half + 1, bounds.Width - 4, half - 3);

        if (_upHovered)
            context.FillRectangle(new SolidColorBrush(
                _pressed ? ProTheme.Background.ControlPressed : ProTheme.Background.ControlHover), upRect, 3);
        if (_downHovered)
            context.FillRectangle(new SolidColorBrush(
                _pressed ? ProTheme.Background.ControlPressed : ProTheme.Background.ControlHover), downRect, 3);

        var pen = new Pen(new SolidColorBrush(ProTheme.Text.Secondary), 1.2);
        var cx = bounds.Width / 2;

        // ▲
        var uy = upRect.Center.Y;
        context.DrawLine(pen, new Point(cx - 3.5, uy + 1.5), new Point(cx, uy - 1.5));
        context.DrawLine(pen, new Point(cx, uy - 1.5), new Point(cx + 3.5, uy + 1.5));

        // ▼
        var dy = downRect.Center.Y;
        context.DrawLine(pen, new Point(cx - 3.5, dy - 1.5), new Point(cx, dy + 1.5));
        context.DrawLine(pen, new Point(cx, dy + 1.5), new Point(cx + 3.5, dy - 1.5));
    }
}

/// <summary>
/// Garde de fermeture de popup factorisée (cf. ProMenuPopup/ProComboBox) :
/// clic extérieur + désactivation de fenêtre avec délai de grâce 250 ms.
/// Remplace le light dismiss Avalonia, instable sous GNOME/XWayland.
/// </summary>
internal sealed class ProPopupDismissGuard
{
    private readonly Func<bool> _isOpen;
    private readonly Action _close;
    private TopLevel? _root;

    /// <summary>Instant de la dernière fermeture par clic extérieur (anti-réouverture)</summary>
    public DateTime LastOutsideClose { get; private set; }

    public ProPopupDismissGuard(Func<bool> isOpen, Action close)
    {
        _isOpen = isOpen;
        _close = close;
    }

    public void Install(Visual anchor)
    {
        _root = TopLevel.GetTopLevel(anchor);
        if (_root == null) return;

        _root.AddHandler(InputElement.PointerPressedEvent, OnRootPointerPressed,
            RoutingStrategies.Tunnel, handledEventsToo: true);
        if (_root is Window w)
            w.Deactivated += OnRootDeactivated;
    }

    public void Remove()
    {
        if (_root == null) return;
        _root.RemoveHandler(InputElement.PointerPressedEvent, OnRootPointerPressed);
        if (_root is Window w)
            w.Deactivated -= OnRootDeactivated;
        _root = null;
    }

    /// <summary>Vrai si une fermeture par le garde date de moins de 250 ms (même clic)</summary>
    public bool JustClosed
        => (DateTime.UtcNow - LastOutsideClose).TotalMilliseconds <= 250;

    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Visual v && ProMenuPopup.IsInsidePopupHost(v)) return;

        LastOutsideClose = DateTime.UtcNow;
        _close();
    }

    private void OnRootDeactivated(object? sender, EventArgs e)
    {
        // Certains bureaux (GNOME/XWayland) font rebondir l'activation de la
        // fenêtre : ne fermer que si elle reste inactive 250 ms
        Avalonia.Threading.DispatcherTimer.RunOnce(() =>
        {
            if (_isOpen() && _root is Window { IsActive: false })
                _close();
        }, TimeSpan.FromMilliseconds(250));
    }
}
