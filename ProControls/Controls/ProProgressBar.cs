using Avalonia;
using Avalonia.Media;
using ProControls.Theme;

namespace ProControls.Controls;

public enum ProgressVariant
{
    Default,
    Success,
    Warning,
    Error
}

/// <summary>
/// Barre de progression professionnelle style VS2022
/// Avec variantes de couleur et mode indéterminé
/// </summary>
public class ProProgressBar : ProControlBase
{
    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<ProProgressBar, double>(nameof(Value), 0);
    
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<ProProgressBar, double>(nameof(Minimum), 0);
    
    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<ProProgressBar, double>(nameof(Maximum), 100);
    
    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<ProProgressBar, bool>(nameof(IsIndeterminate), false);
    
    public static readonly StyledProperty<bool> ShowValueProperty =
        AvaloniaProperty.Register<ProProgressBar, bool>(nameof(ShowValue), false);
    
    public static readonly StyledProperty<ProgressVariant> VariantProperty =
        AvaloniaProperty.Register<ProProgressBar, ProgressVariant>(nameof(Variant), ProgressVariant.Default);

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, Math.Clamp(value, Minimum, Maximum));
    }
    
    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }
    
    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }
    
    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }
    
    public bool ShowValue
    {
        get => GetValue(ShowValueProperty);
        set => SetValue(ShowValueProperty, value);
    }
    
    public ProgressVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }
    
    static ProProgressBar()
    {
        AffectsRender<ProProgressBar>(ValueProperty, MinimumProperty, MaximumProperty, 
            IsIndeterminateProperty, ShowValueProperty, VariantProperty);
    }
    
    public ProProgressBar()
    {
        Focusable = false;
    }

    // ═══════════════════════════════════════════════════════════════
    // MESURE
    // ═══════════════════════════════════════════════════════════════
    
    protected override Size MeasureOverride(Size availableSize)
    {
        var height = ShowValue ? 24.0 : 8.0;
        return new Size(Math.Min(200, availableSize.Width), height);
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════
    
    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        var trackHeight = ShowValue ? 6.0 : 4.0;
        var cornerRadius = trackHeight / 2;
        
        // Position du track
        var trackY = ShowValue ? bounds.Height - trackHeight - 2 : (bounds.Height - trackHeight) / 2;
        var trackRect = new Rect(2, trackY, bounds.Width - 4, trackHeight);
        
        // Couleur selon la variante
        var progressColor = Variant switch
        {
            ProgressVariant.Success => VS2022Theme.Accent.Success,
            ProgressVariant.Warning => VS2022Theme.Accent.Warning,
            ProgressVariant.Error => VS2022Theme.Accent.Error,
            _ => VS2022Theme.Accent.Primary
        };
        
        // Track (fond)
        var trackColor = VS2022Theme.Background.ControlDisabled;
        context.DrawRectangle(
            new SolidColorBrush(trackColor),
            null,
            trackRect,
            cornerRadius, cornerRadius);
        
        // Bordure du track
        var trackBorderPen = new Pen(new SolidColorBrush(VS2022Theme.Border.Subtle), 0.5);
        context.DrawRectangle(null, trackBorderPen, trackRect.Deflate(0.25), cornerRadius, cornerRadius);
        
        // Progression
        var progress = (Value - Minimum) / (Maximum - Minimum);
        progress = Math.Clamp(progress, 0, 1);
        
        if (progress > 0 || IsIndeterminate)
        {
            using (context.PushClip(new RoundedRect(trackRect, cornerRadius)))
            {
                Rect progressRect;
                
                if (IsIndeterminate)
                {
                    // Animation simulée (en production, utiliser une vraie animation)
                    var animProgress = (DateTime.Now.Millisecond / 1000.0);
                    var barWidth = trackRect.Width * 0.3;
                    var barX = trackRect.X + (trackRect.Width + barWidth) * animProgress - barWidth;
                    progressRect = new Rect(barX, trackRect.Y, barWidth, trackRect.Height);
                }
                else
                {
                    var progressWidth = trackRect.Width * progress;
                    progressRect = new Rect(trackRect.X, trackRect.Y, progressWidth, trackRect.Height);
                }
                
                // Dégradé pour la barre de progression
                var progressBrush = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(VS2022Theme.Lerp(progressColor, Colors.White, 0.2), 0),
                        new GradientStop(progressColor, 0.5),
                        new GradientStop(VS2022Theme.Lerp(progressColor, Colors.Black, 0.1), 1)
                    }
                };
                
                context.DrawRectangle(progressBrush, null, progressRect, cornerRadius, cornerRadius);
                
                // Highlight
                var highlightRect = new Rect(progressRect.X, progressRect.Y, progressRect.Width, progressRect.Height / 2);
                var highlightBrush = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.FromArgb(60, 255, 255, 255), 0),
                        new GradientStop(Color.FromArgb(0, 255, 255, 255), 1)
                    }
                };
                context.DrawRectangle(highlightBrush, null, highlightRect);
            }
        }
        
        // Valeur textuelle
        if (ShowValue && !IsIndeterminate)
        {
            var percentage = (int)(progress * 100);
            var valueText = CreateText($"{percentage}%", VS2022Theme.Text.Secondary, 11);
            var textX = (bounds.Width - valueText.Width) / 2;
            context.DrawText(valueText, new Point(textX, 0));
        }
    }
}
