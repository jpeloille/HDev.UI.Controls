using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using ProControls.Theme;
using System.Globalization;

namespace ProControls.Controls;

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
public abstract class ProControlBase : Control
{
    // ═══════════════════════════════════════════════════════════════
    // ÉTATS VISUELS
    // ═══════════════════════════════════════════════════════════════
    
    private bool _isHovered;
    private bool _isPressed;
    private bool _isFocusedState;
    
    protected bool IsHovered => _isHovered;
    protected bool IsPressed => _isPressed;
    protected new bool IsFocused => _isFocusedState;
    
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

    static ProControlBase()
    {
        AffectsRender<ProControlBase>(InputElement.IsEnabledProperty);
        FocusableProperty.OverrideDefaultValue<ProControlBase>(true);
    }

    protected ProControlBase()
    {
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
        InvalidateVisual();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        _isFocusedState = false;
        InvalidateVisual();
        base.OnLostFocus(e);
    }
    
    protected virtual void OnClick() { }
    
    // ═══════════════════════════════════════════════════════════════
    // HELPERS DE RENDU
    // ═══════════════════════════════════════════════════════════════
    
    protected FormattedText CreateText(
        string text, 
        Color color, 
        double fontSize = ProTheme.Typography.FontSizeBody, 
        FontWeight? weight = null)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(ProTheme.Typography.FontFamily, FontStyle.Normal, weight ?? FontWeight.Regular),
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
    
    protected static LinearGradientBrush CreateVerticalGradient(Color top, Color bottom)
    {
        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(top, 0),
                new GradientStop(bottom, 1)
            }
        };
    }
    
    protected void DrawRoundedRect(
        DrawingContext context, 
        Rect rect, 
        Color? fill, 
        Color? stroke, 
        double cornerRadius = 2,
        double strokeThickness = 1)
    {
        var fillBrush = fill.HasValue ? new SolidColorBrush(fill.Value) : null;
        var strokePen = stroke.HasValue ? new Pen(new SolidColorBrush(stroke.Value), strokeThickness) : null;
        
        context.DrawRectangle(fillBrush, strokePen, rect, cornerRadius, cornerRadius);
    }
    
    protected void DrawShadow(DrawingContext context, Rect rect, double cornerRadius, double intensity = 1.0)
    {
        var layers = 3;
        for (int i = layers; i >= 0; i--)
        {
            var opacity = (byte)(15 * intensity - i * 4);
            var expand = i * 1.0;
            var offset = i * 0.5 + 1;
            var shadowRect = rect.Inflate(expand).Translate(new Vector(0, offset));
            var shadowBrush = new SolidColorBrush(Color.FromArgb(opacity, 0, 0, 0));
            context.DrawRectangle(shadowBrush, null, shadowRect, cornerRadius + expand, cornerRadius + expand);
        }
    }
    
    protected void DrawFocusRing(DrawingContext context, Rect rect, double cornerRadius)
    {
        var focusRect = rect.Inflate(2);
        var focusPen = new Pen(
            new SolidColorBrush(ProTheme.WithOpacity(ProTheme.Border.FocusOuter, 100)), 
            1.5);
        context.DrawRectangle(null, focusPen, focusRect, cornerRadius + 2, cornerRadius + 2);
    }
    
    protected void DrawInnerHighlight(DrawingContext context, Rect rect, double cornerRadius)
    {
        var innerRect = rect.Deflate(1);
        var highlightBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
        var highlightPen = new Pen(highlightBrush, 1.0);
        context.DrawRectangle(null, highlightPen, innerRect.Deflate(0.5), cornerRadius - 0.5, cornerRadius - 0.5);
    }
    
    // ═══════════════════════════════════════════════════════════════
    // ÉTAT VISUEL RÉSOLU
    // ═══════════════════════════════════════════════════════════════
    
    protected (Color background, Color border, Color text) ResolveVisualState()
    {
        if (!IsEnabled)
        {
            return (
                ProTheme.Background.ControlDisabled,
                ProTheme.Border.Disabled,
                ProTheme.Text.Disabled
            );
        }
        
        if (IsPressed)
        {
            return (
                ProTheme.Background.ControlPressed,
                ProTheme.Border.Pressed,
                ProTheme.Text.Primary
            );
        }
        
        if (IsHovered)
        {
            return (
                ProTheme.Background.ControlHover,
                ProTheme.Border.Hover,
                ProTheme.Text.Primary
            );
        }
        
        return (
            ProTheme.Background.Control,
            ProTheme.Border.Default,
            ProTheme.Text.Primary
        );
    }
}
