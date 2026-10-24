using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

public enum ButtonVariant
{
    Primary,        // Fond accent (Accent.Fill), texte blanc
    Secondary,      // Fond gris (Background.Control), bordure grise
    Ghost,          // Transparent, bordure au hover
    Danger,         // Rouge pour actions destructives
    Outline,        // Fond panneau, bordure grise, texte primaire (« Annuler »)
    OutlineAccent   // Fond panneau, bordure et texte accent (action secondaire)
}

public enum ButtonSize
{
    Small,
    Medium,
    Large
}

/// <summary>
/// Bouton texte (style Yaru / libadwaita) : variantes et tailles 28 / 34 / 42 px
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

    /// <summary>Graisse du libellé (Regular par défaut ; SemiBold pour une action appuyée)</summary>
    public static readonly StyledProperty<FontWeight> TextWeightProperty =
        AvaloniaProperty.Register<HDevButton, FontWeight>(nameof(TextWeight), FontWeight.Regular);

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

    public FontWeight TextWeight
    {
        get => GetValue(TextWeightProperty);
        set => SetValue(TextWeightProperty, value);
    }
    
    // ═══════════════════════════════════════════════════════════════
    // ÉVÉNEMENTS
    // ═══════════════════════════════════════════════════════════════
    
    public event EventHandler? Click;

    protected override void OnClick()
    {
        Click?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Activation par une technologie d'assistance (cf. <see cref="HDevButtonAutomationPeer"/>)</summary>
    internal void PerformClick()
    {
        if (IsEnabled)
            OnClick();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new HDevButtonAutomationPeer(this);
    
    static HDevButton()
    {
        // Hauteur intrinsèque : ne s'étire pas dans une rangée plus haute
        VerticalAlignmentProperty.OverrideDefaultValue<HDevButton>(Avalonia.Layout.VerticalAlignment.Center);
        AffectsRender<HDevButton>(TextProperty, VariantProperty, SizeProperty, TextWeightProperty);
        AffectsMeasure<HDevButton>(TextProperty, SizeProperty, TextWeightProperty);
    }

    // ═══════════════════════════════════════════════════════════════
    // MESURE
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Largeur visible minimale d'un bouton texte</summary>
    private const double MinFaceWidth = 72;

    /// <summary>Hauteur, corps et retrait horizontal du libellé pour la taille courante</summary>
    private protected (double Height, double FontSize, double PaddingH) Metrics => Size switch
    {
        ButtonSize.Small => (HDevTheme.Size.ControlHeightSmall, HDevTheme.Typography.FontSizeCaption, 10.0),
        ButtonSize.Large => (HDevTheme.Size.ControlHeightLarge, 15.0, 20.0),
        _ => (HDevTheme.Size.ControlHeightMedium, HDevTheme.Typography.FontSizeBody, 16.0)
    };

    // Aucune marge réservée autour du bouton : son bord visible est son bord de layout,
    // et il s'aligne sur un champ ou un autre bouton posé à côté
    protected override Size MeasureOverride(Size availableSize)
    {
        var (height, fontSize, paddingH) = Metrics;
        var text = CreateText(Text, HDevTheme.Text.Primary, fontSize, TextWeight);
        return new Size(Math.Max(MinFaceWidth, Math.Ceiling(text.Width + paddingH * 2)), height);
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Face du bouton : la hauteur de sa taille, centrée dans les bornes si on l'étire
    /// (une hauteur explicite, elle, est honorée)
    /// </summary>
    private protected Rect FaceRect(double width)
    {
        var height = double.IsNaN(Height) ? Math.Min(Metrics.Height, Bounds.Height) : Bounds.Height;
        return new Rect(
            Math.Round((Bounds.Width - width) / 2),
            Math.Round((Bounds.Height - height) / 2),
            width, height);
    }

    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);
        var buttonRect = FaceRect(Bounds.Width);
        var cornerRadius = HDevTheme.Size.CornerRadiusSmall;
        var (bgColor, borderColor, textColor) = ResolveColors();

        context.DrawRectangle(new SolidColorBrush(bgColor), null, buttonRect, cornerRadius, cornerRadius);
        var borderPen = new Pen(new SolidColorBrush(borderColor), 1.0);
        context.DrawRectangle(null, borderPen, buttonRect.Deflate(0.5), cornerRadius, cornerRadius);

        if (IsFocusVisible && IsEnabled)
            DrawFocusRing(context, buttonRect, cornerRadius);

        var formattedText = CreateText(Text, textColor, Metrics.FontSize, TextWeight);
        var textOffset = IsPressed ? 0.5 : 0.0;
        DrawTextCentered(context, formattedText, buttonRect.Translate(new Vector(0, textOffset)));
    }
    
    /// <summary>Couleurs de la variante pour l'état courant (partagé avec <see cref="HDevIconButton"/>)</summary>
    protected (Color background, Color border, Color text) ResolveColors()
    {
        return Variant switch
        {
            ButtonVariant.Primary => ResolvePrimaryColors(),
            ButtonVariant.Ghost => ResolveGhostColors(),
            ButtonVariant.Danger => ResolveDangerColors(),
            ButtonVariant.Outline => ResolveOutlineColors(),
            ButtonVariant.OutlineAccent => ResolveOutlineAccentColors(),
            _ => ResolveSecondaryColors()
        };
    }
    
    private (Color, Color, Color) ResolvePrimaryColors()
    {
        // Accent.Fill et non Accent.Primary : le libellé blanc garde ≥ 4,5:1 à tous les états
        if (!IsEnabled)
            return (HDevTheme.Disabled.Background, HDevTheme.Disabled.Border, HDevTheme.Disabled.Text);
        if (IsPressed)
            return (HDevTheme.Accent.FillPressed, HDevTheme.Accent.FillPressed, HDevTheme.Text.OnAccent);
        if (IsHovered)
            return (HDevTheme.Accent.FillHover, HDevTheme.Accent.FillHover, HDevTheme.Text.OnAccent);
        return (HDevTheme.Accent.Fill, HDevTheme.Accent.Fill, HDevTheme.Text.OnAccent);
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

    private (Color, Color, Color) ResolveOutlineColors()
    {
        // Fond panneau (blanc en clair) et non Background.Control : c'est ce qui
        // distingue Outline de Secondary, dont le fond est gris dans les palettes
        if (!IsEnabled)
            return (HDevTheme.Background.Panel, HDevTheme.Border.Disabled, HDevTheme.Text.Disabled);
        if (IsPressed)
            return (HDevTheme.Background.ControlPressed, HDevTheme.Border.Pressed, HDevTheme.Text.Primary);
        if (IsHovered)
            return (HDevTheme.Background.ControlHover, HDevTheme.Border.Hover, HDevTheme.Text.Primary);
        return (HDevTheme.Background.Panel, HDevTheme.Border.Default, HDevTheme.Text.Primary);
    }

    private (Color, Color, Color) ResolveOutlineAccentColors()
    {
        var panel = HDevTheme.Background.Panel;
        if (!IsEnabled)
            return (panel, HDevTheme.Border.Disabled, HDevTheme.Text.Disabled);

        var (background, border) = IsPressed
            ? (HDevTheme.Lerp(panel, HDevTheme.Accent.Primary, 0.16), HDevTheme.Accent.PrimaryPressed)
            : IsHovered
                ? (HDevTheme.Lerp(panel, HDevTheme.Accent.Primary, 0.08), HDevTheme.Accent.PrimaryHover)
                : (panel, HDevTheme.Accent.Primary);

        // Le libellé accent reste ≥ 4,5:1 sur le fond de l'état, teinté compris
        return (background, border, HDevTheme.EnsureContrast(HDevTheme.Accent.Foreground, background));
    }
}

/// <summary>
/// Bouton exposé aux technologies d'assistance (AT-SPI sous Linux) : rôle Bouton,
/// nom et activation. Nom : AutomationProperties.Name, sinon le libellé, sinon
/// l'infobulle — c'est ce qui nomme un <see cref="HDevIconButton"/>.
/// </summary>
internal sealed class HDevButtonAutomationPeer : ControlAutomationPeer, IInvokeProvider
{
    public HDevButtonAutomationPeer(HDevButton owner) : base(owner) { }

    private HDevButton Button => (HDevButton)Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

    protected override string? GetNameCore()
    {
        var name = base.GetNameCore();
        if (!string.IsNullOrWhiteSpace(name)) return name;
        if (!string.IsNullOrWhiteSpace(Button.Text)) return Button.Text;
        return ToolTip.GetTip(Button) as string;
    }

    protected override bool IsContentElementCore() => true;
    protected override bool IsControlElementCore() => true;

    public void Invoke() => Button.PerformClick();
}
