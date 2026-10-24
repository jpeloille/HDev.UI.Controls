using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using HDev.UI.Controls.Theme;
using System.Globalization;

namespace HDev.UI.Controls;

/// <summary>
/// Alignement du texte sur la grille de PIXELS PHYSIQUES : une origine qui ne tombe
/// pas sur un pixel du device fait baver les glyphes (antialiasing sur deux pixels).
///
/// Arrondir à l'unité logique entière (Math.Round(p.X)) n'est correct qu'à 100 % et
/// 200 % de scaling. Dès que l'échelle est fractionnaire — 125 / 150 / 175 %, typique
/// de Wayland/Linux — une unité logique ≠ un pixel physique : l'origine retombe au
/// milieu d'un pixel et le texte redevient flou. Il faut donc arrondir dans l'espace
/// des pixels physiques : round(p * scale) / scale.
///
/// Le RenderScaling est uniforme sur toute une fenêtre (TopLevel). On le fige une fois
/// en tête de chaque Render via <see cref="BeginFrame"/> ; les appels <see cref="Snap"/>
/// suivants n'ont alors rien à connaître de la hiérarchie. Tant que BeginFrame n'a pas
/// été appelé, le scaling vaut 1.0 (ancien comportement, sans régression).
/// </summary>
internal static class Crisp
{
    [ThreadStatic] private static double _scale;

    private static double Scale => _scale > 0 ? _scale : 1.0;

    /// <summary>Facteur d'échelle DPI du device pour ce Visual (1.0 s'il n'est pas encore attaché).</summary>
    public static double GetRenderScaling(Visual visual)
        => TopLevel.GetTopLevel(visual)?.RenderScaling ?? 1.0;

    /// <summary>
    /// À appeler en tête de chaque override <c>Render(DrawingContext)</c> : fige le
    /// scaling DPI de la fenêtre pour tous les <see cref="Snap"/> du frame. Retourne
    /// l'échelle, utile si le contrôle veut aussi caler des arêtes sur le pixel.
    /// </summary>
    public static double BeginFrame(Visual visual) => _scale = GetRenderScaling(visual);

    /// <summary>Aligne une origine sur la grille de pixels physiques (échelle du frame courant).</summary>
    public static Point Snap(Point p) => Snap(p, Scale);

    /// <summary>Aligne une origine sur la grille de pixels physiques pour une échelle donnée.</summary>
    public static Point Snap(Point p, double scale)
    {
        var s = scale > 0 ? scale : 1.0;
        return new Point(Math.Round(p.X * s) / s, Math.Round(p.Y * s) / s);
    }
}

/// <summary>
/// Classe de base pour tous les contrôles Pro (style Ubuntu 26.04 / Yaru)
/// Gère les états visuels, le hit testing, et fournit des helpers de rendu
/// </summary>
public abstract class HDevControlBase : Control
{
    // ═══════════════════════════════════════════════════════════════
    // ÉTATS VISUELS
    // ═══════════════════════════════════════════════════════════════
    
    private bool _isHovered;
    private bool _isPressed;
    private bool _isFocusedState;
    private bool _isFocusVisible;

    protected bool IsHovered => _isHovered;
    protected bool IsPressed => _isPressed;
    protected new bool IsFocused => _isFocusedState;

    /// <summary>Focus arrivé au clavier : l'anneau de focus se dessine (cf. <see cref="HDevFocus"/>)</summary>
    protected bool IsFocusVisible => _isFocusedState && _isFocusVisible;
    
    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    public static new readonly StyledProperty<bool> IsEnabledProperty =
        InputElement.IsEnabledProperty;

    public new bool IsEnabled
    {
        get => GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    static HDevControlBase()
    {
        AffectsRender<HDevControlBase>(InputElement.IsEnabledProperty);
        FocusableProperty.OverrideDefaultValue<HDevControlBase>(true);

        // Contrôles à hauteur intrinsèque (bouton, case, radio, chip, badge…) : dans une
        // rangée plus haute, ils se centrent au lieu de s'étirer
        VerticalAlignmentProperty.OverrideDefaultValue<HDevControlBase>(VerticalAlignment.Center);

        HDevFocus.EnsureInitialized();
    }

    protected HDevControlBase()
    {
        HDevTheme.Track(this);
        ClipToBounds = false;
    }
    
    // ═══════════════════════════════════════════════════════════════
    // GESTION DES ÉVÉNEMENTS
    // ═══════════════════════════════════════════════════════════════

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        if (!IsEnabled) return;
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
        if (!IsEnabled) return;
        _isPressed = true;
        Focus();
        InvalidateVisual();
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        var wasPressed = _isPressed;
        _isPressed = false;
        InvalidateVisual();
        
        if (wasPressed && _isHovered && IsEnabled)
        {
            OnClick();
        }
        
        base.OnPointerReleased(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || !IsEnabled) return;

        // Socle clavier commun : Espace/Entrée activent le contrôle
        // (feedback pressé au down, OnClick au up, comme à la souris)
        if (e.Key is Key.Space or Key.Enter)
        {
            _isPressed = true;
            InvalidateVisual();
            e.Handled = true;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Handled) return;

        if (e.Key is Key.Space or Key.Enter && _isPressed)
        {
            _isPressed = false;
            InvalidateVisual();

            if (IsEnabled)
                OnClick();

            e.Handled = true;
        }
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        _isFocusedState = true;
        _isFocusVisible = HDevFocus.IsVisible(e);
        InvalidateVisual();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        _isFocusedState = false;
        _isFocusVisible = false;
        InvalidateVisual();
        base.OnLostFocus(e);
    }
    
    protected virtual void OnClick() { }

    /// <summary>
    /// Fige l'état visuel sans souris ni clavier : réservé à la planche visuelle
    /// headless (tests/Gallery), qui montre survol, pression et focus côte à côte
    /// </summary>
    internal void ForceVisualState(bool hovered = false, bool pressed = false, bool focused = false)
    {
        _isHovered = hovered;
        _isPressed = pressed;
        _isFocusedState = focused;
        _isFocusVisible = focused;
        InvalidateVisual();
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS DE RENDU
    // ═══════════════════════════════════════════════════════════════
    
    protected FormattedText CreateText(
        string text, 
        Color color, 
        double fontSize = HDevTheme.Typography.FontSizeBody, 
        FontWeight? weight = null)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(HDevTheme.Typography.FontFamily, FontStyle.Normal, weight ?? FontWeight.Regular),
            fontSize,
            new SolidColorBrush(color));
    }
    
    protected void DrawTextCentered(DrawingContext context, FormattedText text, Rect bounds)
    {
        var origin = Crisp.Snap(new Point(
            bounds.X + (bounds.Width - text.Width) / 2,
            bounds.Y + (bounds.Height - text.Height) / 2));
        context.DrawText(text, origin);
    }

    protected void DrawTextLeft(DrawingContext context, FormattedText text, Rect bounds, double leftPadding = 0)
    {
        var origin = Crisp.Snap(new Point(
            bounds.X + leftPadding,
            bounds.Y + (bounds.Height - text.Height) / 2));
        context.DrawText(text, origin);
    }
    
    /// <summary>Anneau de focus clavier extérieur, commun à la suite (cf. <see cref="HDevFocus"/>)</summary>
    protected void DrawFocusRing(DrawingContext context, Rect rect, double cornerRadius)
        => HDevFocus.DrawRing(context, rect, cornerRadius);
}
