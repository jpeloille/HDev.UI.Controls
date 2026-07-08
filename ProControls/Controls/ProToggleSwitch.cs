using Avalonia;
using Avalonia.Media;
using ProControls.Theme;

namespace ProControls.Controls;

/// <summary>
/// Toggle switch moderne style VS2022/Windows 11
/// Avec animation fluide du curseur
/// </summary>
public class ProToggleSwitch : ProControlBase
{
    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    public static readonly StyledProperty<bool> IsOnProperty =
        AvaloniaProperty.Register<ProToggleSwitch, bool>(nameof(IsOn), false);
    
    public static readonly StyledProperty<string> OnLabelProperty =
        AvaloniaProperty.Register<ProToggleSwitch, string>(nameof(OnLabel), "On");
    
    public static readonly StyledProperty<string> OffLabelProperty =
        AvaloniaProperty.Register<ProToggleSwitch, string>(nameof(OffLabel), "Off");
    
    public static readonly StyledProperty<bool> ShowLabelsProperty =
        AvaloniaProperty.Register<ProToggleSwitch, bool>(nameof(ShowLabels), false);

    public bool IsOn
    {
        get => GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }
    
    public string OnLabel
    {
        get => GetValue(OnLabelProperty);
        set => SetValue(OnLabelProperty, value);
    }
    
    public string OffLabel
    {
        get => GetValue(OffLabelProperty);
        set => SetValue(OffLabelProperty, value);
    }
    
    public bool ShowLabels
    {
        get => GetValue(ShowLabelsProperty);
        set => SetValue(ShowLabelsProperty, value);
    }
    
    // ═══════════════════════════════════════════════════════════════
    // ÉVÉNEMENTS
    // ═══════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Déclenché à chaque changement de IsOn, quelle qu'en soit la source
    /// (souris, clavier, binding, code)
    /// </summary>
    public event EventHandler<bool>? Toggled;

    protected override void OnClick()
    {
        if (!IsEnabled) return;
        IsOn = !IsOn;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsOnProperty)
            Toggled?.Invoke(this, IsOn);
    }
    
    static ProToggleSwitch()
    {
        AffectsRender<ProToggleSwitch>(IsOnProperty, OnLabelProperty, OffLabelProperty, ShowLabelsProperty);
        AffectsMeasure<ProToggleSwitch>(ShowLabelsProperty, OnLabelProperty, OffLabelProperty);
    }

    // ═══════════════════════════════════════════════════════════════
    // MESURE
    // ═══════════════════════════════════════════════════════════════
    
    protected override Size MeasureOverride(Size availableSize)
    {
        var trackWidth = 44.0;
        var trackHeight = 22.0;
        
        if (!ShowLabels)
        {
            return new Size(trackWidth + 6, trackHeight + 6);
        }
        
        var label = IsOn ? OnLabel : OffLabel;
        var labelText = CreateText(label, VS2022Theme.Text.Primary, 13);
        return new Size(trackWidth + 10 + labelText.Width + 6, Math.Max(trackHeight, labelText.Height) + 6);
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════
    
    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        var trackWidth = 40.0;
        var trackHeight = 20.0;
        var thumbSize = 14.0;
        var trackRadius = trackHeight / 2;
        var thumbRadius = thumbSize / 2;
        
        // Position du track
        var trackRect = new Rect(3, (bounds.Height - trackHeight) / 2, trackWidth, trackHeight);
        
        // Couleurs selon l'état
        Color trackColor, thumbColor, borderColor;
        
        if (!IsEnabled)
        {
            trackColor = VS2022Theme.Background.ControlDisabled;
            thumbColor = VS2022Theme.Border.Disabled;
            borderColor = VS2022Theme.Border.Disabled;
        }
        else if (IsOn)
        {
            trackColor = IsPressed ? VS2022Theme.Accent.PrimaryPressed :
                        IsHovered ? VS2022Theme.Accent.PrimaryHover :
                        VS2022Theme.Accent.Primary;
            thumbColor = Colors.White;
            borderColor = trackColor;
        }
        else
        {
            trackColor = IsPressed ? VS2022Theme.Background.ControlPressed :
                        IsHovered ? VS2022Theme.Background.ControlHover :
                        VS2022Theme.Background.Control;
            thumbColor = IsHovered ? VS2022Theme.Text.Secondary : VS2022Theme.Text.Tertiary;
            borderColor = IsHovered ? VS2022Theme.Border.Hover : VS2022Theme.Border.Default;
        }
        
        // Ombre du track
        if (IsEnabled)
        {
            for (int i = 2; i >= 0; i--)
            {
                var shadowOpacity = (byte)(8 - i * 2);
                var shadowRect = trackRect.Inflate(i * 0.5).Translate(new Vector(0, i * 0.3 + 0.5));
                context.DrawRectangle(
                    new SolidColorBrush(Color.FromArgb(shadowOpacity, 0, 0, 0)),
                    null,
                    shadowRect,
                    trackRadius + i * 0.5, trackRadius + i * 0.5);
            }
        }
        
        // Track (piste)
        context.DrawRectangle(
            new SolidColorBrush(trackColor),
            null,
            trackRect,
            trackRadius, trackRadius);
        
        // Bordure du track
        var trackBorderPen = new Pen(new SolidColorBrush(borderColor), 1.0);
        context.DrawRectangle(null, trackBorderPen, trackRect.Deflate(0.5), trackRadius, trackRadius);
        
        // Position du thumb (curseur)
        var thumbPadding = 3.0;
        var thumbX = IsOn 
            ? trackRect.Right - thumbRadius - thumbPadding
            : trackRect.Left + thumbRadius + thumbPadding;
        var thumbCenter = new Point(thumbX, trackRect.Center.Y);
        
        // Agrandissement du thumb au hover/press
        var currentThumbRadius = IsPressed ? thumbRadius + 1 : (IsHovered ? thumbRadius + 0.5 : thumbRadius);
        
        // Ombre du thumb
        if (IsEnabled)
        {
            for (int i = 2; i >= 0; i--)
            {
                var shadowOpacity = (byte)(20 - i * 5);
                var shadowCenter = thumbCenter + new Vector(0, i * 0.4);
                context.DrawEllipse(
                    new SolidColorBrush(Color.FromArgb(shadowOpacity, 0, 0, 0)),
                    null,
                    shadowCenter,
                    currentThumbRadius + i * 0.5, currentThumbRadius + i * 0.5);
            }
        }
        
        // Thumb (curseur)
        context.DrawEllipse(
            new SolidColorBrush(thumbColor),
            null,
            thumbCenter,
            currentThumbRadius, currentThumbRadius);
        
        // Bordure du thumb (si off)
        if (!IsOn && IsEnabled)
        {
            var thumbBorderPen = new Pen(new SolidColorBrush(VS2022Theme.Border.Strong), 1.0);
            context.DrawEllipse(null, thumbBorderPen, thumbCenter, currentThumbRadius - 0.5, currentThumbRadius - 0.5);
        }
        
        // Highlight sur le thumb
        if (IsEnabled && !IsPressed)
        {
            var highlightCenter = thumbCenter - new Vector(0, currentThumbRadius * 0.3);
            var highlightBrush = new RadialGradientBrush
            {
                Center = new RelativePoint(0.5, 0.3, RelativeUnit.Relative),
                GradientOrigin = new RelativePoint(0.5, 0.2, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(50, 255, 255, 255), 0),
                    new GradientStop(Color.FromArgb(0, 255, 255, 255), 1)
                }
            };
            context.DrawEllipse(highlightBrush, null, thumbCenter, currentThumbRadius, currentThumbRadius);
        }
        
        // Focus ring
        if (IsFocused && IsEnabled)
        {
            var focusRect = trackRect.Inflate(3);
            var focusPen = new Pen(
                new SolidColorBrush(VS2022Theme.WithOpacity(VS2022Theme.Border.FocusOuter, 100)),
                1.5);
            context.DrawRectangle(null, focusPen, focusRect, trackRadius + 3, trackRadius + 3);
        }
        
        // Label
        if (ShowLabels)
        {
            var label = IsOn ? OnLabel : OffLabel;
            var textColor = IsEnabled ? VS2022Theme.Text.Primary : VS2022Theme.Text.Disabled;
            var labelText = CreateText(label, textColor, 13);
            var labelX = trackRect.Right + 10;
            var labelY = (bounds.Height - labelText.Height) / 2;
            context.DrawText(labelText, new Point(labelX, labelY));
        }
    }
}
