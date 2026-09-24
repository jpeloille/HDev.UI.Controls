using Avalonia;
using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

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
public class HDevBadge : HDevControlBase
{
    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<HDevBadge, string>(nameof(Text), "Badge");
    
    public static readonly StyledProperty<BadgeVariant> VariantProperty =
        AvaloniaProperty.Register<HDevBadge, BadgeVariant>(nameof(Variant), BadgeVariant.Default);
    
    public static readonly StyledProperty<BadgeSize> SizeProperty =
        AvaloniaProperty.Register<HDevBadge, BadgeSize>(nameof(Size), BadgeSize.Medium);
    
    public static readonly StyledProperty<bool> IsOutlinedProperty =
        AvaloniaProperty.Register<HDevBadge, bool>(nameof(IsOutlined), false);
    
    public static readonly StyledProperty<bool> IsPillProperty =
        AvaloniaProperty.Register<HDevBadge, bool>(nameof(IsPill), true);

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
    
    static HDevBadge()
    {
        AffectsRender<HDevBadge>(TextProperty, VariantProperty, SizeProperty, IsOutlinedProperty, IsPillProperty);
        AffectsMeasure<HDevBadge>(TextProperty, SizeProperty);
    }
    
    public HDevBadge()
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
        
        var text = CreateText(Text, HDevTheme.Text.Primary, fontSize, FontWeight.Medium);
        return new Size(text.Width + paddingH * 2, text.Height + paddingV * 2);
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════
    
    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);
        var bounds = new Rect(Bounds.Size);
        
        var (fontSize, _, _) = Size switch
        {
            BadgeSize.Small => (10.0, 6.0, 2.0),
            BadgeSize.Large => (13.0, 12.0, 5.0),
            _ => (11.0, 8.0, 3.0)
        };
        
        var cornerRadius = IsPill ? bounds.Height / 2 : HDevTheme.Size.CornerRadiusSmall;
        
        // Couleurs selon la variante
        var (bgColor, textColor, borderColor) = ResolveColors();
        
        if (IsOutlined)
        {
            // Version outline : fond transparent, bordure colorée
            context.DrawRectangle(
                new SolidColorBrush(HDevTheme.WithOpacity(bgColor, 20)),
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
                HDevTheme.Accent.Primary,
                HDevTheme.Text.OnAccent,
                HDevTheme.Accent.Primary
            ),
            BadgeVariant.Success => (
                HDevTheme.Accent.Success,
                HDevTheme.Text.OnAccent,
                HDevTheme.Accent.Success
            ),
            BadgeVariant.Warning => (
                HDevTheme.Accent.Warning,
                HDevTheme.Text.OnAccent,
                HDevTheme.Accent.Warning
            ),
            BadgeVariant.Error => (
                HDevTheme.Accent.Error,
                HDevTheme.Text.OnAccent,
                HDevTheme.Accent.Error
            ),
            BadgeVariant.Info => (
                HDevTheme.Accent.Primary,
                HDevTheme.Text.OnAccent,
                HDevTheme.Accent.Primary
            ),
            _ => (
                HDevTheme.Background.Toolbar,
                HDevTheme.Text.Primary,
                HDevTheme.Border.Default
            )
        };
    }
}
