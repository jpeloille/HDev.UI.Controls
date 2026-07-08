using Avalonia;
using Avalonia.Media;
using Avalonia.VisualTree;
using ProControls.Theme;

namespace ProControls.Controls;

/// <summary>
/// Bouton radio professionnel style VS2022
/// Avec cercle parfait et animation de sélection
/// </summary>
public class ProRadioButton : ProControlBase
{
    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<ProRadioButton, bool>(nameof(IsChecked), false);
    
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ProRadioButton, string>(nameof(Label), "");
    
    public static readonly StyledProperty<string> GroupNameProperty =
        AvaloniaProperty.Register<ProRadioButton, string>(nameof(GroupName), "");

    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }
    
    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }
    
    public string GroupName
    {
        get => GetValue(GroupNameProperty);
        set => SetValue(GroupNameProperty, value);
    }
    
    // ═══════════════════════════════════════════════════════════════
    // ÉVÉNEMENTS
    // ═══════════════════════════════════════════════════════════════
    
    public event EventHandler<bool>? CheckedChanged;
    
    protected override void OnClick()
    {
        if (!IsEnabled || IsChecked) return;
        
        // Décocher les autres radio buttons du même groupe
        if (Parent is Visual visualParent && !string.IsNullOrEmpty(GroupName))
        {
            foreach (var child in visualParent.GetVisualChildren())
            {
                if (child is ProRadioButton radio && radio != this && radio.GroupName == GroupName)
                {
                    radio.IsChecked = false;
                }
            }
        }
        
        IsChecked = true;
        CheckedChanged?.Invoke(this, IsChecked);
    }
    
    static ProRadioButton()
    {
        AffectsRender<ProRadioButton>(IsCheckedProperty, LabelProperty);
        AffectsMeasure<ProRadioButton>(LabelProperty);
    }

    // ═══════════════════════════════════════════════════════════════
    // MESURE
    // ═══════════════════════════════════════════════════════════════
    
    protected override Size MeasureOverride(Size availableSize)
    {
        var circleSize = 18.0;
        var spacing = 8.0;
        
        if (string.IsNullOrEmpty(Label))
        {
            return new Size(circleSize + 6, circleSize + 6);
        }
        
        var labelText = CreateText(Label, VS2022Theme.Text.Primary, 13);
        return new Size(circleSize + spacing + labelText.Width + 6, Math.Max(circleSize, labelText.Height) + 6);
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════
    
    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        var circleSize = 16.0;
        var radius = circleSize / 2;
        
        // Centre du cercle
        var center = new Point(3 + radius, bounds.Height / 2);
        
        // Couleurs selon l'état
        Color bgColor, borderColor;
        
        if (!IsEnabled)
        {
            bgColor = VS2022Theme.Background.ControlDisabled;
            borderColor = VS2022Theme.Border.Disabled;
        }
        else if (IsChecked)
        {
            bgColor = IsPressed ? VS2022Theme.Accent.PrimaryPressed :
                      IsHovered ? VS2022Theme.Accent.PrimaryHover : 
                      VS2022Theme.Accent.Primary;
            borderColor = bgColor;
        }
        else if (IsPressed)
        {
            bgColor = VS2022Theme.Background.ControlPressed;
            borderColor = VS2022Theme.Border.Pressed;
        }
        else if (IsHovered)
        {
            bgColor = VS2022Theme.Background.ControlHover;
            borderColor = VS2022Theme.Border.Hover;
        }
        else
        {
            bgColor = VS2022Theme.Background.Control;
            borderColor = VS2022Theme.Border.Default;
        }
        
        // Ombre subtile
        if (IsEnabled && !IsPressed)
        {
            for (int i = 2; i >= 0; i--)
            {
                var shadowOpacity = (byte)(10 - i * 3);
                var shadowRadius = radius + i * 0.8;
                var shadowCenter = center + new Vector(0, i * 0.3 + 0.5);
                context.DrawEllipse(
                    new SolidColorBrush(Color.FromArgb(shadowOpacity, 0, 0, 0)),
                    null,
                    shadowCenter,
                    shadowRadius, shadowRadius);
            }
        }
        
        // Cercle de fond
        context.DrawEllipse(
            new SolidColorBrush(bgColor),
            null,
            center,
            radius, radius);
        
        // Bordure
        var borderPen = new Pen(new SolidColorBrush(borderColor), 1.0);
        context.DrawEllipse(null, borderPen, center, radius - 0.5, radius - 0.5);
        
        // Point central si sélectionné
        if (IsChecked)
        {
            var dotRadius = 4.0;
            var dotColor = IsEnabled ? VS2022Theme.Text.OnAccent : VS2022Theme.Text.Disabled;
            context.DrawEllipse(
                new SolidColorBrush(dotColor),
                null,
                center,
                dotRadius, dotRadius);
        }
        
        // Inner highlight (arc supérieur)
        if (IsEnabled && !IsPressed && !IsChecked)
        {
            // Dégradé simulé avec un arc
            var highlightGeometry = new StreamGeometry();
            using (var ctx = highlightGeometry.Open())
            {
                ctx.BeginFigure(new Point(center.X - radius + 2, center.Y - 2), true);
                ctx.ArcTo(
                    new Point(center.X + radius - 2, center.Y - 2),
                    new Size(radius - 2, radius - 4),
                    0, false, SweepDirection.Clockwise);
                ctx.LineTo(new Point(center.X + radius - 3, center.Y - 1));
                ctx.ArcTo(
                    new Point(center.X - radius + 3, center.Y - 1),
                    new Size(radius - 3, radius - 5),
                    0, false, SweepDirection.CounterClockwise);
                ctx.EndFigure(true);
            }
            var highlightBrush = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255));
            context.DrawGeometry(highlightBrush, null, highlightGeometry);
        }
        
        // Focus ring
        if (IsFocused && IsEnabled)
        {
            var focusPen = new Pen(
                new SolidColorBrush(VS2022Theme.WithOpacity(VS2022Theme.Border.FocusOuter, 100)),
                1.5);
            context.DrawEllipse(null, focusPen, center, radius + 3, radius + 3);
        }
        
        // Label
        if (!string.IsNullOrEmpty(Label))
        {
            var textColor = IsEnabled ? VS2022Theme.Text.Primary : VS2022Theme.Text.Disabled;
            var labelText = CreateText(Label, textColor, 13);
            var labelX = center.X + radius + 8;
            var labelY = (bounds.Height - labelText.Height) / 2;
            context.DrawText(labelText, new Point(labelX, labelY));
        }
    }
}
