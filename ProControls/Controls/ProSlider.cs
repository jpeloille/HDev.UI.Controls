using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using ProControls.Theme;

namespace ProControls.Controls;

/// <summary>
/// Slider professionnel style VS2022
/// Avec track, thumb, et affichage optionnel de la valeur
/// </summary>
public class ProSlider : ProControlBase
{
    private bool _isDragging;
    
    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<ProSlider, double>(nameof(Value), 50);
    
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<ProSlider, double>(nameof(Minimum), 0);
    
    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<ProSlider, double>(nameof(Maximum), 100);
    
    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<ProSlider, double>(nameof(Step), 1);
    
    public static readonly StyledProperty<bool> ShowValueProperty =
        AvaloniaProperty.Register<ProSlider, bool>(nameof(ShowValue), false);
    
    public static readonly StyledProperty<bool> ShowTicksProperty =
        AvaloniaProperty.Register<ProSlider, bool>(nameof(ShowTicks), false);

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
    
    public double Step
    {
        get => GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }
    
    public bool ShowValue
    {
        get => GetValue(ShowValueProperty);
        set => SetValue(ShowValueProperty, value);
    }
    
    public bool ShowTicks
    {
        get => GetValue(ShowTicksProperty);
        set => SetValue(ShowTicksProperty, value);
    }
    
    // ═══════════════════════════════════════════════════════════════
    // ÉVÉNEMENTS
    // ═══════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Déclenché à chaque changement de Value, quelle qu'en soit la source
    /// (souris, clavier, binding, code)
    /// </summary>
    public event EventHandler<double>? ValueChanged;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty)
            ValueChanged?.Invoke(this, Value);
    }

    static ProSlider()
    {
        AffectsRender<ProSlider>(ValueProperty, MinimumProperty, MaximumProperty, 
            ShowValueProperty, ShowTicksProperty);
    }
    
    public ProSlider()
    {
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    // ═══════════════════════════════════════════════════════════════
    // MESURE
    // ═══════════════════════════════════════════════════════════════
    
    protected override Size MeasureOverride(Size availableSize)
    {
        var height = ShowValue ? 36.0 : 24.0;
        if (ShowTicks) height += 10;
        return new Size(Math.Min(200, availableSize.Width), height);
    }

    // ═══════════════════════════════════════════════════════════════
    // INTERACTION
    // ═══════════════════════════════════════════════════════════════
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _isDragging = true;
        UpdateValueFromPointer(e.GetPosition(this));
        e.Pointer.Capture(this);
    }
    
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDragging)
        {
            UpdateValueFromPointer(e.GetPosition(this));
        }
    }
    
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _isDragging = false;
        e.Pointer.Capture(null);
        base.OnPointerReleased(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!e.Handled && IsEnabled)
        {
            var step = Step > 0 ? Step : (Maximum - Minimum) / 100;
            double? newValue = e.Key switch
            {
                Key.Left or Key.Down => Value - step,
                Key.Right or Key.Up => Value + step,
                Key.PageDown => Value - step * 10,
                Key.PageUp => Value + step * 10,
                Key.Home => Minimum,
                Key.End => Maximum,
                _ => null
            };

            if (newValue.HasValue)
            {
                Value = Math.Clamp(newValue.Value, Minimum, Maximum);
                e.Handled = true;
                return;
            }
        }

        base.OnKeyDown(e);
    }

    private void UpdateValueFromPointer(Point pos)
    {
        var trackRect = GetTrackRect();
        var relativeX = Math.Clamp(pos.X - trackRect.X, 0, trackRect.Width);
        var ratio = relativeX / trackRect.Width;
        var newValue = Minimum + ratio * (Maximum - Minimum);
        
        // Arrondir au step
        if (Step > 0)
        {
            newValue = Math.Round(newValue / Step) * Step;
        }
        
        newValue = Math.Clamp(newValue, Minimum, Maximum);

        if (Math.Abs(newValue - Value) > 0.001)
        {
            Value = newValue;
        }
    }
    
    private Rect GetTrackRect()
    {
        var bounds = new Rect(Bounds.Size);
        var thumbRadius = 8.0;
        var trackHeight = 4.0;
        var trackY = ShowValue ? 20 : (bounds.Height - trackHeight) / 2;
        return new Rect(thumbRadius + 4, trackY, bounds.Width - thumbRadius * 2 - 8, trackHeight);
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════
    
    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        var trackRect = GetTrackRect();
        var trackRadius = trackRect.Height / 2;
        var thumbRadius = (_isDragging || IsPressed) ? 9.0 : (IsHovered ? 8.5 : 8.0);
        
        // Valeur textuelle
        if (ShowValue)
        {
            var valueStr = Step >= 1 ? ((int)Value).ToString() : Value.ToString("F1");
            var valueText = CreateText(valueStr, ProTheme.Text.Secondary, 11);
            var textX = (bounds.Width - valueText.Width) / 2;
            context.DrawText(valueText, Crisp.Snap(new Point(textX, 2)));
        }
        
        // Track (fond)
        context.DrawRectangle(
            new SolidColorBrush(ProTheme.Background.ControlDisabled),
            null,
            trackRect,
            trackRadius, trackRadius);
        
        // Track rempli (partie active)
        var progress = (Value - Minimum) / (Maximum - Minimum);
        var filledWidth = trackRect.Width * progress;
        var filledRect = new Rect(trackRect.X, trackRect.Y, filledWidth, trackRect.Height);
        
        var fillColor = !IsEnabled ? ProTheme.Border.Disabled :
                        (_isDragging ? ProTheme.Accent.PrimaryPressed : ProTheme.Accent.Primary);
        
        context.DrawRectangle(
            new SolidColorBrush(fillColor),
            null,
            filledRect,
            trackRadius, trackRadius);
        
        // Ticks
        if (ShowTicks && Step > 0)
        {
            var tickY = trackRect.Bottom + 4;
            var tickPen = new Pen(new SolidColorBrush(ProTheme.Border.Default), 1);
            var tickCount = (int)((Maximum - Minimum) / Step);
            
            for (int i = 0; i <= tickCount; i++)
            {
                var tickX = trackRect.X + (trackRect.Width * i / tickCount);
                context.DrawLine(tickPen, new Point(tickX, tickY), new Point(tickX, tickY + 4));
            }
        }
        
        // Thumb (curseur)
        var thumbX = trackRect.X + filledWidth;
        var thumbY = trackRect.Center.Y;
        var thumbCenter = new Point(thumbX, thumbY);
        
        // Ombre du thumb
        if (IsEnabled)
        {
            for (int i = 3; i >= 0; i--)
            {
                var shadowOpacity = (byte)(20 - i * 5);
                var shadowCenter = thumbCenter + new Vector(0, i * 0.4);
                context.DrawEllipse(
                    new SolidColorBrush(Color.FromArgb(shadowOpacity, 0, 0, 0)),
                    null,
                    shadowCenter,
                    thumbRadius + i * 0.6, thumbRadius + i * 0.6);
            }
        }
        
        // Thumb
        var thumbColor = !IsEnabled ? ProTheme.Background.ControlDisabled : Colors.White;
        context.DrawEllipse(
            new SolidColorBrush(thumbColor),
            null,
            thumbCenter,
            thumbRadius, thumbRadius);
        
        // Bordure du thumb
        var thumbBorderColor = !IsEnabled ? ProTheme.Border.Disabled :
                              (_isDragging ? ProTheme.Accent.PrimaryPressed :
                               IsHovered ? ProTheme.Accent.PrimaryHover : ProTheme.Accent.Primary);
        var thumbBorderPen = new Pen(new SolidColorBrush(thumbBorderColor), 2);
        context.DrawEllipse(null, thumbBorderPen, thumbCenter, thumbRadius - 1, thumbRadius - 1);
        
        // Point central du thumb
        if (_isDragging || IsHovered)
        {
            context.DrawEllipse(
                new SolidColorBrush(thumbBorderColor),
                null,
                thumbCenter,
                3, 3);
        }
        
        // Focus ring
        if (IsFocused && IsEnabled)
        {
            var focusPen = new Pen(
                new SolidColorBrush(ProTheme.WithOpacity(ProTheme.Border.FocusOuter, 100)),
                1.5);
            context.DrawEllipse(null, focusPen, thumbCenter, thumbRadius + 4, thumbRadius + 4);
        }
    }
}
