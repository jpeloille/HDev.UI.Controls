using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

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
public class HDevButton : HDevControlBase
{
    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<HDevButton, string>(nameof(Text), "Button");
    
    public static readonly StyledProperty<ButtonVariant> VariantProperty =
        AvaloniaProperty.Register<HDevButton, ButtonVariant>(nameof(Variant), ButtonVariant.Secondary);
    
    public static readonly StyledProperty<ButtonSize> SizeProperty =
        AvaloniaProperty.Register<HDevButton, ButtonSize>(nameof(Size), ButtonSize.Medium);

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
    
    static HDevButton()
    {
        AffectsRender<HDevButton>(TextProperty, VariantProperty, SizeProperty);
        AffectsMeasure<HDevButton>(TextProperty, SizeProperty);
    }

    // ═══════════════════════════════════════════════════════════════
    // MESURE
    // ═══════════════════════════════════════════════════════════════
    
    protected override Size MeasureOverride(Size availableSize)
    {
        var (height, fontSize, paddingH) = Size switch
        {
            ButtonSize.Small => (HDevTheme.Size.ControlHeightSmall, 12.0, 10.0),
            ButtonSize.Large => (HDevTheme.Size.ControlHeightLarge, 15.0, 20.0),
            _ => (HDevTheme.Size.ControlHeightMedium, 14.0, 16.0)
        };
        
        var text = CreateText(Text, HDevTheme.Text.Primary, fontSize);
        var width = text.Width + paddingH * 2 + 8; // Extra pour marge de manœuvre
        
        return new Size(Math.Max(80, width), height + 8); // +8 pour ombre
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════
    
    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);
        var bounds = new Rect(Bounds.Size);
        var buttonRect = bounds.Deflate(new Thickness(4, 4, 4, 5));
        var cornerRadius = HDevTheme.Size.CornerRadiusSmall;
        
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
            return (HDevTheme.Disabled.Background, HDevTheme.Disabled.Border, HDevTheme.Disabled.Text);
        if (IsPressed)
            return (HDevTheme.Accent.PrimaryPressed, HDevTheme.Accent.PrimaryPressed, HDevTheme.Text.OnAccent);
        if (IsHovered)
            return (HDevTheme.Accent.PrimaryHover, HDevTheme.Accent.PrimaryHover, HDevTheme.Text.OnAccent);
        return (HDevTheme.Accent.Primary, HDevTheme.Accent.Primary, HDevTheme.Text.OnAccent);
    }
    
    private (Color, Color, Color) ResolveSecondaryColors()
    {
        if (!IsEnabled)
            return (HDevTheme.Background.ControlDisabled, HDevTheme.Border.Disabled, HDevTheme.Text.Disabled);
        if (IsPressed)
            return (HDevTheme.Background.ControlPressed, HDevTheme.Border.Pressed, HDevTheme.Text.Primary);
        if (IsHovered)
            return (HDevTheme.Background.ControlHover, HDevTheme.Border.Hover, HDevTheme.Text.Primary);
        return (HDevTheme.Background.Control, HDevTheme.Border.Default, HDevTheme.Text.Primary);
    }
    
    private (Color, Color, Color) ResolveGhostColors()
    {
        if (!IsEnabled)
            return (Colors.Transparent, Colors.Transparent, HDevTheme.Text.Disabled);
        if (IsPressed)
            return (HDevTheme.Background.ControlPressed, HDevTheme.Border.Pressed, HDevTheme.Text.Primary);
        if (IsHovered)
            return (HDevTheme.Background.ControlHover, HDevTheme.Border.Hover, HDevTheme.Text.Primary);
        return (Colors.Transparent, Colors.Transparent, HDevTheme.Text.Primary);
    }
    
    private (Color, Color, Color) ResolveDangerColors()
    {
        if (!IsEnabled)
            return (HDevTheme.Danger.DisabledBackground, HDevTheme.Danger.DisabledBorder, HDevTheme.Danger.DisabledText);
        if (IsPressed)
            return (HDevTheme.Danger.Pressed, HDevTheme.Danger.Pressed, HDevTheme.Text.OnAccent);
        if (IsHovered)
            return (HDevTheme.Danger.Hover, HDevTheme.Danger.Hover, HDevTheme.Text.OnAccent);
        return (HDevTheme.Danger.Default, HDevTheme.Danger.Default, HDevTheme.Text.OnAccent);
    }
}
