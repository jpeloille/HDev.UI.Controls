using Avalonia;
using Avalonia.Media;
using ProControls.Theme;

namespace ProControls.Controls;

public enum BadgeVariant
{
    Default,
    Primary,
    Success,
    Warning,
    Error,
    Info
}

public enum BadgeSize
{
    Small,
    Medium,
    Large
}

/// <summary>
/// Badge/étiquette de statut style VS2022
/// Pour afficher des labels, compteurs, ou indicateurs
/// </summary>
public class ProBadge : ProControlBase
{
    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<ProBadge, string>(nameof(Text), "Badge");
    
    public static readonly StyledProperty<BadgeVariant> VariantProperty =
        AvaloniaProperty.Register<ProBadge, BadgeVariant>(nameof(Variant), BadgeVariant.Default);
    
    public static readonly StyledProperty<BadgeSize> SizeProperty =
        AvaloniaProperty.Register<ProBadge, BadgeSize>(nameof(Size), BadgeSize.Medium);
    
    public static readonly StyledProperty<bool> IsOutlinedProperty =
        AvaloniaProperty.Register<ProBadge, bool>(nameof(IsOutlined), false);
    
    public static readonly StyledProperty<bool> IsPillProperty =
        AvaloniaProperty.Register<ProBadge, bool>(nameof(IsPill), true);

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    
    public BadgeVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }
    
    public BadgeSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }
    
    public bool IsOutlined
    {
        get => GetValue(IsOutlinedProperty);
        set => SetValue(IsOutlinedProperty, value);
    }
    
    public bool IsPill
    {
        get => GetValue(IsPillProperty);
        set => SetValue(IsPillProperty, value);
    }
    
    static ProBadge()
    {
        AffectsRender<ProBadge>(TextProperty, VariantProperty, SizeProperty, IsOutlinedProperty, IsPillProperty);
        AffectsMeasure<ProBadge>(TextProperty, SizeProperty);
    }
    
    public ProBadge()
    {
        Focusable = false;
    }

    // ═══════════════════════════════════════════════════════════════
    // MESURE
    // ═══════════════════════════════════════════════════════════════
    
    protected override Size MeasureOverride(Size availableSize)
    {
        var (fontSize, paddingH, paddingV) = Size switch
        {
            BadgeSize.Small => (10.0, 6.0, 2.0),
            BadgeSize.Large => (13.0, 12.0, 5.0),
            _ => (11.0, 8.0, 3.0)
        };
        
        var text = CreateText(Text, VS2022Theme.Text.Primary, fontSize, FontWeight.Medium);
        return new Size(text.Width + paddingH * 2, text.Height + paddingV * 2);
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════
    
    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        
        var (fontSize, _, _) = Size switch
        {
            BadgeSize.Small => (10.0, 6.0, 2.0),
            BadgeSize.Large => (13.0, 12.0, 5.0),
            _ => (11.0, 8.0, 3.0)
        };
        
        var cornerRadius = IsPill ? bounds.Height / 2 : VS2022Theme.Size.CornerRadiusSmall;
        
        // Couleurs selon la variante
        var (bgColor, textColor, borderColor) = ResolveColors();
        
        if (IsOutlined)
        {
            // Version outline : fond transparent, bordure colorée
            context.DrawRectangle(
                new SolidColorBrush(VS2022Theme.WithOpacity(bgColor, 20)),
                null,
                bounds,
                cornerRadius, cornerRadius);
            
            var borderPen = new Pen(new SolidColorBrush(borderColor), 1.0);
            context.DrawRectangle(null, borderPen, bounds.Deflate(0.5), cornerRadius, cornerRadius);
        }
        else
        {
            // Version pleine
            context.DrawRectangle(
                new SolidColorBrush(bgColor),
                null,
                bounds,
                cornerRadius, cornerRadius);
        }
        
        // Texte
        var formattedText = CreateText(Text, IsOutlined ? borderColor : textColor, fontSize, FontWeight.Medium);
        DrawTextCentered(context, formattedText, bounds);
    }
    
    private (Color background, Color text, Color border) ResolveColors()
    {
        return Variant switch
        {
            BadgeVariant.Primary => (
                VS2022Theme.Accent.Primary,
                VS2022Theme.Text.OnAccent,
                VS2022Theme.Accent.Primary
            ),
            BadgeVariant.Success => (
                VS2022Theme.Accent.Success,
                VS2022Theme.Text.OnAccent,
                VS2022Theme.Accent.Success
            ),
            BadgeVariant.Warning => (
                VS2022Theme.Accent.Warning,
                VS2022Theme.Text.OnAccent,
                VS2022Theme.Accent.Warning
            ),
            BadgeVariant.Error => (
                VS2022Theme.Accent.Error,
                VS2022Theme.Text.OnAccent,
                VS2022Theme.Accent.Error
            ),
            BadgeVariant.Info => (
                VS2022Theme.Accent.Primary,
                VS2022Theme.Text.OnAccent,
                VS2022Theme.Accent.Primary
            ),
            _ => (
                VS2022Theme.Background.Toolbar,
                VS2022Theme.Text.Primary,
                VS2022Theme.Border.Default
            )
        };
    }
}
