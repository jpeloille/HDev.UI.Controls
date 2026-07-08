using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using ProControls.Theme;

namespace ProControls.Controls;

public enum ButtonVariant
{
    Primary,    // Fond accent, texte blanc
    Secondary,  // Fond blanc, bordure grise
    Ghost,      // Transparent, bordure au hover
    Danger      // Rouge pour actions destructives
}

public enum ButtonSize
{
    Small,
    Medium,
    Large
}

/// <summary>
/// Bouton professionnel style VS2022
/// Supporte plusieurs variantes et tailles
/// </summary>
public class ProButton : ProControlBase
{
    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<ProButton, string>(nameof(Text), "Button");
    
    public static readonly StyledProperty<ButtonVariant> VariantProperty =
        AvaloniaProperty.Register<ProButton, ButtonVariant>(nameof(Variant), ButtonVariant.Secondary);
    
    public static readonly StyledProperty<ButtonSize> SizeProperty =
        AvaloniaProperty.Register<ProButton, ButtonSize>(nameof(Size), ButtonSize.Medium);

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    
    public ButtonVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }
    
    public ButtonSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }
    
    // ═══════════════════════════════════════════════════════════════
    // ÉVÉNEMENTS
    // ═══════════════════════════════════════════════════════════════
    
    public event EventHandler? Click;
    
    protected override void OnClick()
    {
        Click?.Invoke(this, EventArgs.Empty);
    }
    
    static ProButton()
    {
        AffectsRender<ProButton>(TextProperty, VariantProperty, SizeProperty);
        AffectsMeasure<ProButton>(TextProperty, SizeProperty);
    }

    // ═══════════════════════════════════════════════════════════════
    // MESURE
    // ═══════════════════════════════════════════════════════════════
    
    protected override Size MeasureOverride(Size availableSize)
    {
        var (height, fontSize, paddingH) = Size switch
        {
            ButtonSize.Small => (VS2022Theme.Size.ControlHeightSmall, 11.0, 10.0),
            ButtonSize.Large => (VS2022Theme.Size.ControlHeightLarge, 14.0, 20.0),
            _ => (VS2022Theme.Size.ControlHeightMedium, 13.0, 16.0)
        };
        
        var text = CreateText(Text, VS2022Theme.Text.Primary, fontSize);
        var width = text.Width + paddingH * 2 + 8; // Extra pour marge de manœuvre
        
        return new Size(Math.Max(80, width), height + 8); // +8 pour ombre
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════
    
    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        var buttonRect = bounds.Deflate(new Thickness(4, 4, 4, 5));
        var cornerRadius = VS2022Theme.Size.CornerRadiusSmall;
        
        var (fontSize, _) = Size switch
        {
            ButtonSize.Small => (11.0, FontWeight.Regular),
            ButtonSize.Large => (14.0, FontWeight.Medium),
            _ => (13.0, FontWeight.Regular)
        };
        
        // Récupérer les couleurs selon la variante et l'état
        var (bgColor, borderColor, textColor) = ResolveColors();
        
        // Ombre (sauf ghost et disabled)
        if (IsEnabled && Variant != ButtonVariant.Ghost && !IsPressed)
        {
            DrawShadow(context, buttonRect, cornerRadius, IsHovered ? 1.2 : 0.8);
        }
        
        // Fond
        context.DrawRectangle(
            new SolidColorBrush(bgColor), 
            null, 
            buttonRect, 
            cornerRadius, cornerRadius);
        
        // Bordure
        var borderPen = new Pen(new SolidColorBrush(borderColor), 1.0);
        context.DrawRectangle(null, borderPen, buttonRect.Deflate(0.5), cornerRadius, cornerRadius);
        
        // Inner highlight (sauf pressed et ghost)
        if (IsEnabled && !IsPressed && Variant != ButtonVariant.Ghost)
        {
            DrawInnerHighlight(context, buttonRect, cornerRadius);
        }
        
        // Focus ring
        if (IsFocused && IsEnabled)
        {
            DrawFocusRing(context, buttonRect, cornerRadius);
        }
        
        // Texte
        var formattedText = CreateText(Text, textColor, fontSize, FontWeight.Regular);
        var textOffset = IsPressed ? 0.5 : 0.0;
        DrawTextCentered(context, formattedText, buttonRect.Translate(new Vector(0, textOffset)));
    }
    
    private (Color background, Color border, Color text) ResolveColors()
    {
        return Variant switch
        {
            ButtonVariant.Primary => ResolvePrimaryColors(),
            ButtonVariant.Ghost => ResolveGhostColors(),
            ButtonVariant.Danger => ResolveDangerColors(),
            _ => ResolveSecondaryColors()
        };
    }
    
    private (Color, Color, Color) ResolvePrimaryColors()
    {
        if (!IsEnabled)
            return (VS2022Theme.Disabled.Background, VS2022Theme.Disabled.Border, VS2022Theme.Disabled.Text);
        if (IsPressed)
            return (VS2022Theme.Accent.PrimaryPressed, VS2022Theme.Accent.PrimaryPressed, VS2022Theme.Text.OnAccent);
        if (IsHovered)
            return (VS2022Theme.Accent.PrimaryHover, VS2022Theme.Accent.PrimaryHover, VS2022Theme.Text.OnAccent);
        return (VS2022Theme.Accent.Primary, VS2022Theme.Accent.Primary, VS2022Theme.Text.OnAccent);
    }
    
    private (Color, Color, Color) ResolveSecondaryColors()
    {
        if (!IsEnabled)
            return (VS2022Theme.Background.ControlDisabled, VS2022Theme.Border.Disabled, VS2022Theme.Text.Disabled);
        if (IsPressed)
            return (VS2022Theme.Background.ControlPressed, VS2022Theme.Border.Pressed, VS2022Theme.Text.Primary);
        if (IsHovered)
            return (VS2022Theme.Background.ControlHover, VS2022Theme.Border.Hover, VS2022Theme.Text.Primary);
        return (VS2022Theme.Background.Control, VS2022Theme.Border.Default, VS2022Theme.Text.Primary);
    }
    
    private (Color, Color, Color) ResolveGhostColors()
    {
        if (!IsEnabled)
            return (Colors.Transparent, Colors.Transparent, VS2022Theme.Text.Disabled);
        if (IsPressed)
            return (VS2022Theme.Background.ControlPressed, VS2022Theme.Border.Pressed, VS2022Theme.Text.Primary);
        if (IsHovered)
            return (VS2022Theme.Background.ControlHover, VS2022Theme.Border.Hover, VS2022Theme.Text.Primary);
        return (Colors.Transparent, Colors.Transparent, VS2022Theme.Text.Primary);
    }
    
    private (Color, Color, Color) ResolveDangerColors()
    {
        if (!IsEnabled)
            return (VS2022Theme.Danger.DisabledBackground, VS2022Theme.Danger.DisabledBorder, VS2022Theme.Danger.DisabledText);
        if (IsPressed)
            return (VS2022Theme.Danger.Pressed, VS2022Theme.Danger.Pressed, VS2022Theme.Text.OnAccent);
        if (IsHovered)
            return (VS2022Theme.Danger.Hover, VS2022Theme.Danger.Hover, VS2022Theme.Text.OnAccent);
        return (VS2022Theme.Danger.Default, VS2022Theme.Danger.Default, VS2022Theme.Text.OnAccent);
    }
}
