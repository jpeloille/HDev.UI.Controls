using Avalonia;
using Avalonia.Media;
using ProControls.Theme;

namespace ProControls.Controls;

/// <summary>
/// Case à cocher professionnelle style VS2022
/// Avec support indeterminate et animations visuelles
/// </summary>
public class ProCheckBox : ProControlBase
{
    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    public static readonly StyledProperty<bool?> IsCheckedProperty =
        AvaloniaProperty.Register<ProCheckBox, bool?>(nameof(IsChecked), false);
    
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ProCheckBox, string>(nameof(Label), "");
    
    public static readonly StyledProperty<bool> IsThreeStateProperty =
        AvaloniaProperty.Register<ProCheckBox, bool>(nameof(IsThreeState), false);

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
    
    static ProCheckBox()
    {
        AffectsRender<ProCheckBox>(IsCheckedProperty, LabelProperty, IsThreeStateProperty);
        AffectsMeasure<ProCheckBox>(LabelProperty);
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
        
        var labelText = CreateText(Label, ProTheme.Text.Primary, ProTheme.Typography.FontSizeBody);
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
            bgColor = ProTheme.Background.ControlDisabled;
            borderColor = ProTheme.Border.Disabled;
        }
        else if (isChecked || isIndeterminate)
        {
            bgColor = IsPressed ? ProTheme.Accent.PrimaryPressed :
                      IsHovered ? ProTheme.Accent.PrimaryHover : 
                      ProTheme.Accent.Primary;
            borderColor = bgColor;
        }
        else if (IsPressed)
        {
            bgColor = ProTheme.Background.ControlPressed;
            borderColor = ProTheme.Border.Pressed;
        }
        else if (IsHovered)
        {
            bgColor = ProTheme.Background.ControlHover;
            borderColor = ProTheme.Border.Hover;
        }
        else
        {
            bgColor = ProTheme.Background.Control;
            borderColor = ProTheme.Border.Default;
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
            var markColor = IsEnabled ? ProTheme.Text.OnAccent : ProTheme.Text.Disabled;
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
            var textColor = IsEnabled ? ProTheme.Text.Primary : ProTheme.Text.Disabled;
            var labelText = CreateText(Label, textColor, ProTheme.Typography.FontSizeBody);
            var labelX = boxRect.Right + 8;
            var labelY = (bounds.Height - labelText.Height) / 2;
            context.DrawText(labelText, Crisp.Snap(new Point(labelX, labelY)));
        }
    }
}
