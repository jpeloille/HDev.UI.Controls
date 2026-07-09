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
            ButtonSize.Small => (ProTheme.Size.ControlHeightSmall, 12.0, 10.0),
            ButtonSize.Large => (ProTheme.Size.ControlHeightLarge, 15.0, 20.0),
            _ => (ProTheme.Size.ControlHeightMedium, 14.0, 16.0)
        };
        
        var text = CreateText(Text, ProTheme.Text.Primary, fontSize);
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
        var cornerRadius = ProTheme.Size.CornerRadiusSmall;
        
        var (fontSize, _) = Size switch
        {
            ButtonSize.Small => (12.0, FontWeight.Regular),
            ButtonSize.Large => (15.0, FontWeight.Medium),
            _ => (14.0, FontWeight.Regular)
        };
        
        // Récupérer les couleurs selon la variante et l'état
        var (bgColor, borderColor, textColor) = ResolveColors();
        
        // Fond
        context.DrawRectangle(
            new SolidColorBrush(bgColor), 
            null, 
            buttonRect, 
            cornerRadius, cornerRadius);
        
        // Bordure
        var borderPen = new Pen(new SolidColorBrush(borderColor), 1.0);
        context.DrawRectangle(null, borderPen, buttonRect.Deflate(0.5), cornerRadius, cornerRadius);
        
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
            return (ProTheme.Disabled.Background, ProTheme.Disabled.Border, ProTheme.Disabled.Text);
        if (IsPressed)
            return (ProTheme.Accent.PrimaryPressed, ProTheme.Accent.PrimaryPressed, ProTheme.Text.OnAccent);
        if (IsHovered)
            return (ProTheme.Accent.PrimaryHover, ProTheme.Accent.PrimaryHover, ProTheme.Text.OnAccent);
        return (ProTheme.Accent.Primary, ProTheme.Accent.Primary, ProTheme.Text.OnAccent);
    }
    
    private (Color, Color, Color) ResolveSecondaryColors()
    {
        if (!IsEnabled)
            return (ProTheme.Background.ControlDisabled, ProTheme.Border.Disabled, ProTheme.Text.Disabled);
        if (IsPressed)
            return (ProTheme.Background.ControlPressed, ProTheme.Border.Pressed, ProTheme.Text.Primary);
        if (IsHovered)
            return (ProTheme.Background.ControlHover, ProTheme.Border.Hover, ProTheme.Text.Primary);
        return (ProTheme.Background.Control, ProTheme.Border.Default, ProTheme.Text.Primary);
    }
    
    private (Color, Color, Color) ResolveGhostColors()
    {
        if (!IsEnabled)
            return (Colors.Transparent, Colors.Transparent, ProTheme.Text.Disabled);
        if (IsPressed)
            return (ProTheme.Background.ControlPressed, ProTheme.Border.Pressed, ProTheme.Text.Primary);
        if (IsHovered)
            return (ProTheme.Background.ControlHover, ProTheme.Border.Hover, ProTheme.Text.Primary);
        return (Colors.Transparent, Colors.Transparent, ProTheme.Text.Primary);
    }
    
    private (Color, Color, Color) ResolveDangerColors()
    {
        if (!IsEnabled)
            return (ProTheme.Danger.DisabledBackground, ProTheme.Danger.DisabledBorder, ProTheme.Danger.DisabledText);
        if (IsPressed)
            return (ProTheme.Danger.Pressed, ProTheme.Danger.Pressed, ProTheme.Text.OnAccent);
        if (IsHovered)
            return (ProTheme.Danger.Hover, ProTheme.Danger.Hover, ProTheme.Text.OnAccent);
        return (ProTheme.Danger.Default, ProTheme.Danger.Default, ProTheme.Text.OnAccent);
    }
}
