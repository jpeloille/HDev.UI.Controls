using Avalonia;
using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

/// <summary>
/// Case à cocher professionnelle style VS2022
/// Avec support indeterminate et animations visuelles
/// </summary>
public class HDevCheckBox : HDevControlBase
{
    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    public static readonly StyledProperty<bool?> IsCheckedProperty =
        AvaloniaProperty.Register<HDevCheckBox, bool?>(nameof(IsChecked), false);
    
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<HDevCheckBox, string>(nameof(Label), "");
    
    public static readonly StyledProperty<bool> IsThreeStateProperty =
        AvaloniaProperty.Register<HDevCheckBox, bool>(nameof(IsThreeState), false);

    public bool? IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }
    
    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }
    
    public bool IsThreeState
    {
        get => GetValue(IsThreeStateProperty);
        set => SetValue(IsThreeStateProperty, value);
    }
    
    // ═══════════════════════════════════════════════════════════════
    // ÉVÉNEMENTS
    // ═══════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Déclenché à chaque changement de IsChecked, quelle qu'en soit la source
    /// (souris, clavier, binding, code)
    /// </summary>
    public event EventHandler<bool?>? CheckedChanged;

    protected override void OnClick()
    {
        if (!IsEnabled) return;

        if (IsThreeState)
        {
            IsChecked = IsChecked switch
            {
                false => true,
                true => null,
                null => false
            };
        }
        else
        {
            IsChecked = !(IsChecked ?? false);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsCheckedProperty)
            CheckedChanged?.Invoke(this, IsChecked);
    }
    
    static HDevCheckBox()
    {
        AffectsRender<HDevCheckBox>(IsCheckedProperty, LabelProperty, IsThreeStateProperty);
        AffectsMeasure<HDevCheckBox>(LabelProperty);
    }

    // ═══════════════════════════════════════════════════════════════
    // MESURE
    // ═══════════════════════════════════════════════════════════════
    
    protected override Size MeasureOverride(Size availableSize)
    {
        var boxSize = 18.0;
        var spacing = 8.0;
        
        if (string.IsNullOrEmpty(Label))
        {
            return new Size(boxSize + 6, boxSize + 6);
        }
        
        var labelText = CreateText(Label, HDevTheme.Text.Primary, HDevTheme.Typography.FontSizeBody);
        return new Size(boxSize + spacing + labelText.Width + 6, Math.Max(boxSize, labelText.Height) + 6);
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════
    
    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);
        var bounds = new Rect(Bounds.Size);
        var boxSize = 16.0;
        var cornerRadius = 3.0;
        
        // Position de la checkbox
        var boxRect = new Rect(3, (bounds.Height - boxSize) / 2, boxSize, boxSize);
        
        // Couleurs selon l'état
        var isChecked = IsChecked == true;
        var isIndeterminate = IsChecked == null;
        
        Color bgColor, borderColor;
        
        if (!IsEnabled)
        {
            bgColor = HDevTheme.Background.ControlDisabled;
            borderColor = HDevTheme.Border.Disabled;
        }
        else if (isChecked || isIndeterminate)
        {
            bgColor = IsPressed ? HDevTheme.Accent.PrimaryPressed :
                      IsHovered ? HDevTheme.Accent.PrimaryHover : 
                      HDevTheme.Accent.Primary;
            borderColor = bgColor;
        }
        else if (IsPressed)
        {
            bgColor = HDevTheme.Background.ControlPressed;
            borderColor = HDevTheme.Border.Pressed;
        }
        else if (IsHovered)
        {
            bgColor = HDevTheme.Background.ControlHover;
            borderColor = HDevTheme.Border.Hover;
        }
        else
        {
            bgColor = HDevTheme.Background.Control;
            borderColor = HDevTheme.Border.Default;
        }
        
        // Fond de la checkbox
        context.DrawRectangle(
            new SolidColorBrush(bgColor), 
            null, 
            boxRect, 
            cornerRadius, cornerRadius);
        
        // Bordure
        var borderPen = new Pen(new SolidColorBrush(borderColor), 1.0);
        context.DrawRectangle(null, borderPen, boxRect.Deflate(0.5), cornerRadius, cornerRadius);
        
        // Coche ou tiret
        if (isChecked || isIndeterminate)
        {
            var markColor = IsEnabled ? HDevTheme.Text.OnAccent : HDevTheme.Text.Disabled;
            var markPen = new Pen(new SolidColorBrush(markColor), 2.0)
            {
                LineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };
            
            if (isChecked)
            {
                // Coche ✓
                var checkGeometry = new StreamGeometry();
                using (var ctx = checkGeometry.Open())
                {
                    ctx.BeginFigure(new Point(boxRect.X + 4, boxRect.Y + boxSize / 2), false);
                    ctx.LineTo(new Point(boxRect.X + boxSize / 2.5, boxRect.Y + boxSize - 4));
                    ctx.LineTo(new Point(boxRect.X + boxSize - 3, boxRect.Y + 4));
                }
                context.DrawGeometry(null, markPen, checkGeometry);
            }
            else
            {
                // Tiret pour indeterminate
                var y = boxRect.Y + boxSize / 2;
                context.DrawLine(markPen,
                    new Point(boxRect.X + 4, y),
                    new Point(boxRect.X + boxSize - 4, y));
            }
        }
        
        // Focus ring
        if (IsFocused && IsEnabled)
        {
            DrawFocusRing(context, boxRect, cornerRadius);
        }
        
        // Label
        if (!string.IsNullOrEmpty(Label))
        {
            var textColor = IsEnabled ? HDevTheme.Text.Primary : HDevTheme.Text.Disabled;
            var labelText = CreateText(Label, textColor, HDevTheme.Typography.FontSizeBody);
            var labelX = boxRect.Right + 8;
            var labelY = (bounds.Height - labelText.Height) / 2;
            context.DrawText(labelText, Crisp.Snap(new Point(labelX, labelY)));
        }
    }
}
