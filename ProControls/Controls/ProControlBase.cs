using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ProControls.Theme;
using System.Globalization;

namespace ProControls.Controls;

/// <summary>
/// Classe de base pour tous les contrôles Pro VS2022
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

    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        _isFocusedState = true;
        InvalidateVisual();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
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
        double fontSize = 13, 
        FontWeight? weight = null)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(VS2022Theme.Typography.FontFamily, FontStyle.Normal, weight ?? FontWeight.Regular),
            fontSize,
            new SolidColorBrush(color));
    }
    
    protected void DrawTextCentered(DrawingContext context, FormattedText text, Rect bounds)
    {
        var origin = new Point(
            bounds.X + (bounds.Width - text.Width) / 2,
            bounds.Y + (bounds.Height - text.Height) / 2);
        context.DrawText(text, origin);
    }
    
    protected void DrawTextLeft(DrawingContext context, FormattedText text, Rect bounds, double leftPadding = 0)
    {
        var origin = new Point(
            bounds.X + leftPadding,
            bounds.Y + (bounds.Height - text.Height) / 2);
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
            new SolidColorBrush(VS2022Theme.WithOpacity(VS2022Theme.Border.FocusOuter, 100)), 
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
                VS2022Theme.Background.ControlDisabled,
                VS2022Theme.Border.Disabled,
                VS2022Theme.Text.Disabled
            );
        }
        
        if (IsPressed)
        {
            return (
                VS2022Theme.Background.ControlPressed,
                VS2022Theme.Border.Pressed,
                VS2022Theme.Text.Primary
            );
        }
        
        if (IsHovered)
        {
            return (
                VS2022Theme.Background.ControlHover,
                VS2022Theme.Border.Hover,
                VS2022Theme.Text.Primary
            );
        }
        
        return (
            VS2022Theme.Background.Control,
            VS2022Theme.Border.Default,
            VS2022Theme.Text.Primary
        );
    }
}
