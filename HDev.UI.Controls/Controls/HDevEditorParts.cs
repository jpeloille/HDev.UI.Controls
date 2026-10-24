using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using HDev.UI.Controls.Theme;
using System.Globalization;

namespace HDev.UI.Controls;

/// <summary>
/// Habillage commun des champs de saisie (HDevTextBox, HDevEditorBase, combo
/// éditable) : un seul jeu d'états, et neutralisation du chrome Fluent du
/// TextBox interne, qui sinon repeint son propre fond (blanc au focus, gris
/// foncé désactivé) et un soulignement bleu à l'intérieur de notre bordure
/// </summary>
internal static class HDevInputChrome
{
    private static readonly string[] TransparentKeys =
    {
        "TextControlBackground", "TextControlBackgroundPointerOver",
        "TextControlBackgroundFocused", "TextControlBackgroundDisabled",
        "TextControlBorderBrush", "TextControlBorderBrushPointerOver",
        "TextControlBorderBrushFocused", "TextControlBorderBrushDisabled"
    };

    /// <summary>
    /// Prépare un TextBox interne : aucun fond/bordure propre, pas de hauteur
    /// minimale Fluent (32), retrait texte commun. À rappeler au changement de
    /// variante (couleurs de placeholder et de texte désactivé).
    /// </summary>
    public static void Neutralize(TextBox tb)
    {
        foreach (var key in TransparentKeys)
            tb.Resources[key] = Brushes.Transparent;
        tb.Resources["TextControlBorderThemeThickness"] = new Thickness(0);
        tb.Resources["TextControlBorderThemeThicknessFocused"] = new Thickness(0);

        var placeholder = new SolidColorBrush(HDevTheme.Text.Placeholder);
        tb.Resources["TextControlPlaceholderForeground"] = placeholder;
        tb.Resources["TextControlPlaceholderForegroundPointerOver"] = placeholder;
        tb.Resources["TextControlPlaceholderForegroundFocused"] = placeholder;
        tb.Resources["TextControlPlaceholderForegroundDisabled"] =
            new SolidColorBrush(HDevTheme.Disabled.Text);
        tb.Resources["TextControlForegroundDisabled"] = new SolidColorBrush(HDevTheme.Disabled.Text);

        tb.BorderThickness = new Thickness(0);
        tb.Background = Brushes.Transparent;
        tb.MinHeight = 0;
    }

    /// <summary>
    /// Applique l'état visuel au Border hôte. Bordure toujours à 1 px : l'accent
    /// du focus/de l'erreur passe par la couleur et un halo extérieur, jamais par
    /// l'épaisseur (1 → 1,5 décalait le texte d'un demi-pixel et le floutait)
    /// </summary>
    public static void ApplyState(Avalonia.Controls.Border host, TextBox inner,
        bool isEnabled, bool hasError, bool isFocused, bool isHovered, bool isReadOnly = false)
    {
        Color bg, border;
        Color? halo = null;

        if (!isEnabled)
        {
            bg = HDevTheme.Disabled.Background;
            border = HDevTheme.Disabled.Border;
        }
        else if (hasError)
        {
            bg = HDevTheme.Background.Error;
            border = HDevTheme.Accent.Error;
            halo = ErrorHalo;
        }
        else if (isReadOnly)
        {
            // Lecture seule : ni grisé comme un désactivé, ni halo — le fond PanelAlt le distingue d'un champ éditable
            bg = HDevTheme.Background.PanelAlt;
            border = isFocused ? HDevTheme.Border.Focused : HDevTheme.Border.Input;
        }
        else if (isFocused)
        {
            bg = HDevTheme.Background.Input;
            border = HDevTheme.Border.Focused;
            halo = FocusHalo;
        }
        else
        {
            bg = HDevTheme.Background.Input;
            border = isHovered ? HDevTheme.Border.InputHover : HDevTheme.Border.Input;
        }

        host.Background = new SolidColorBrush(bg);
        host.BorderBrush = new SolidColorBrush(border);
        host.BorderThickness = new Thickness(HDevTheme.Size.BorderThin);
        host.BoxShadow = halo is { } h
            ? new BoxShadows(new BoxShadow { Blur = 0, Spread = 2, Color = h })
            : new BoxShadows();

        inner.Foreground = new SolidColorBrush(isEnabled ? HDevTheme.Text.Primary : HDevTheme.Disabled.Text);
    }

    /// <summary>Halo de 2 px des champs au focus et en erreur (BoxShadow ou dessin)</summary>
    public static Color FocusHalo => HDevTheme.WithOpacity(HDevTheme.Border.Focused, 110);
    public static Color ErrorHalo => HDevTheme.WithOpacity(HDevTheme.Accent.Error, 90);

    /// <summary>
    /// Même halo que la BoxShadow (spread 2, sans flou) pour un champ dessiné à la main
    /// (HDevComboBox, HDevLookUpEdit) : 2 px tout autour, à l'extérieur du filet
    /// </summary>
    public static void DrawHalo(DrawingContext context, Rect bounds, double cornerRadius, Color halo)
        => context.DrawRectangle(null, new Pen(new SolidColorBrush(halo), 2),
            bounds.Inflate(1), cornerRadius + 1, cornerRadius + 1);
}

/// <summary>
/// Petit bouton à glyphe pour la zone droite des éditeurs (calendrier, effacer...)
/// </summary>
public class HDevEditorGlyphButton : Control
{
    private bool _isHovered;
    private bool _isPressed;

    public string Glyph { get; set; } = "";
    public double GlyphSize { get; set; } = 14;

    /// <summary>Icône dessinée en traits fins (prioritaire sur <see cref="Glyph"/>)</summary>
    public HDevIcon Icon { get; set; }

    /// <summary>Côté de la boîte de l'icône ; 0 = taille d'origine du glyphe</summary>
    public double IconSize { get; set; }

    public event EventHandler? Click;

    static HDevEditorGlyphButton()
    {
        // Grisé quand l'éditeur hôte est désactivé
        AffectsRender<HDevEditorGlyphButton>(IsEffectivelyEnabledProperty);
    }

    public HDevEditorGlyphButton()
    {
        HDevTheme.Track(this);
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
        Crisp.BeginFrame(this);
        var bounds = new Rect(Bounds.Size);

        if (_isHovered)
        {
            var bg = _isPressed ? HDevTheme.Background.ControlPressed : HDevTheme.Background.ControlHover;
            context.FillRectangle(new SolidColorBrush(bg),
                bounds.Deflate(new Thickness(2, 3)), 4);
        }

        var color = IsEffectivelyEnabled ? HDevTheme.Text.Secondary : HDevTheme.Disabled.Text;

        if (Icon != HDevIcon.None)
        {
            HDevIcons.Draw(context, Icon, bounds.Center, IconSize > 0 ? IconSize : HDevIcons.NaturalSize(Icon), color);
            return;
        }

        var text = new FormattedText(Glyph, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(HDevTheme.Typography.FontFamily), GlyphSize,
            new SolidColorBrush(color));
        context.DrawText(text, Crisp.Snap(new Point(
            (bounds.Width - text.Width) / 2,
            (bounds.Height - text.Height) / 2)));
    }
}

/// <summary>
/// Boutons spin (▲/▼) empilés pour les éditeurs numériques
/// </summary>
public class HDevSpinButtons : Control
{
    private bool _upHovered;
    private bool _downHovered;
    private bool _pressed;

    public event EventHandler? UpClicked;
    public event EventHandler? DownClicked;

    static HDevSpinButtons()
    {
        // Grisé quand l'éditeur hôte est désactivé
        AffectsRender<HDevSpinButtons>(IsEffectivelyEnabledProperty);
    }

    public HDevSpinButtons()
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
        Crisp.BeginFrame(this);
        var bounds = new Rect(Bounds.Size);
        var half = bounds.Height / 2;
        var upRect = new Rect(2, 2, bounds.Width - 4, half - 3);
        var downRect = new Rect(2, half + 1, bounds.Width - 4, half - 3);

        if (_upHovered)
            context.FillRectangle(new SolidColorBrush(
                _pressed ? HDevTheme.Background.ControlPressed : HDevTheme.Background.ControlHover), upRect, 3);
        if (_downHovered)
            context.FillRectangle(new SolidColorBrush(
                _pressed ? HDevTheme.Background.ControlPressed : HDevTheme.Background.ControlHover), downRect, 3);

        var pen = new Pen(new SolidColorBrush(
            IsEffectivelyEnabled ? HDevTheme.Text.Secondary : HDevTheme.Disabled.Text), 1.2);
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
/// Garde de fermeture de popup factorisée (cf. HDevMenuPopup/HDevComboBox) :
/// clic extérieur + désactivation de fenêtre avec délai de grâce 250 ms.
/// Remplace le light dismiss Avalonia, instable sous GNOME/XWayland.
/// </summary>
internal sealed class HDevPopupDismissGuard
{
    private readonly Func<bool> _isOpen;
    private readonly Action _close;
    private TopLevel? _root;

    /// <summary>Instant de la dernière fermeture par clic extérieur (anti-réouverture)</summary>
    public DateTime LastOutsideClose { get; private set; }

    public HDevPopupDismissGuard(Func<bool> isOpen, Action close)
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
        if (e.Source is Visual v && HDevMenuPopup.IsInsidePopupHost(v)) return;

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
