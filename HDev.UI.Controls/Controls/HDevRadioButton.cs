using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

/// <summary>
/// Bouton radio professionnel style VS2022
/// Avec cercle parfait et animation de sélection
/// </summary>
public class HDevRadioButton : HDevControlBase
{
    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<HDevRadioButton, bool>(nameof(IsChecked), false);
    
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<HDevRadioButton, string>(nameof(Label), "");
    
    public static readonly StyledProperty<string> GroupNameProperty =
        AvaloniaProperty.Register<HDevRadioButton, string>(nameof(GroupName), "");

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
    
    /// <summary>
    /// Déclenché à chaque changement de IsChecked, quelle qu'en soit la source
    /// (souris, clavier, binding, code)
    /// </summary>
    public event EventHandler<bool>? CheckedChanged;

    protected override void OnClick()
    {
        if (!IsEnabled || IsChecked) return;
        IsChecked = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsCheckedProperty)
        {
            // L'exclusivité du groupe vit ici pour s'appliquer aussi aux
            // changements programmatiques et aux bindings
            if (IsChecked)
            {
                foreach (var radio in GetGroupSiblings())
                    radio.IsChecked = false;
            }

            CheckedChanged?.Invoke(this, IsChecked);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Navigation par flèches au sein du groupe (coche la cible, comme WPF/WinForms)
        if (!e.Handled && IsEnabled &&
            e.Key is Key.Down or Key.Right or Key.Up or Key.Left)
        {
            var group = GetGroupSiblings().Append(this).OrderBy(IndexInParent).ToList();

            if (group.Count > 1)
            {
                var forward = e.Key is Key.Down or Key.Right;
                var index = group.IndexOf(this);
                var next = group[(index + (forward ? 1 : -1) + group.Count) % group.Count];
                next.Focus();
                next.IsChecked = true;
                e.Handled = true;
                return;
            }
        }

        base.OnKeyDown(e);
    }

    /// <summary>
    /// Les autres radios actifs du même groupe : mêmes GroupName et parent visuel.
    /// Un GroupName vide forme le groupe par défaut du conteneur.
    /// </summary>
    private IEnumerable<HDevRadioButton> GetGroupSiblings()
    {
        if (Parent is not Visual visualParent) yield break;

        foreach (var child in visualParent.GetVisualChildren())
        {
            if (child is HDevRadioButton radio && radio != this &&
                radio.GroupName == GroupName && radio.IsEnabled)
            {
                yield return radio;
            }
        }
    }

    private static int IndexInParent(HDevRadioButton radio) =>
        radio.Parent is Visual p ? p.GetVisualChildren().ToList().IndexOf(radio) : -1;
    
    static HDevRadioButton()
    {
        AffectsRender<HDevRadioButton>(IsCheckedProperty, LabelProperty);
        AffectsMeasure<HDevRadioButton>(LabelProperty);
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
        
        var labelText = CreateText(Label, HDevTheme.Text.Primary, HDevTheme.Typography.FontSizeBody);
        return new Size(circleSize + spacing + labelText.Width + 6, Math.Max(circleSize, labelText.Height) + 6);
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════
    
    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);
        var bounds = new Rect(Bounds.Size);
        var circleSize = 16.0;
        var radius = circleSize / 2;
        
        // Centre du cercle
        var center = new Point(3 + radius, bounds.Height / 2);
        
        // Couleurs selon l'état
        Color bgColor, borderColor;
        
        if (!IsEnabled)
        {
            bgColor = HDevTheme.Background.ControlDisabled;
            borderColor = HDevTheme.Border.Disabled;
        }
        else if (IsChecked)
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
            var dotColor = IsEnabled ? HDevTheme.Text.OnAccent : HDevTheme.Text.Disabled;
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
                new SolidColorBrush(HDevTheme.WithOpacity(HDevTheme.Border.FocusOuter, 100)),
                1.5);
            context.DrawEllipse(null, focusPen, center, radius + 3, radius + 3);
        }
        
        // Label
        if (!string.IsNullOrEmpty(Label))
        {
            var textColor = IsEnabled ? HDevTheme.Text.Primary : HDevTheme.Text.Disabled;
            var labelText = CreateText(Label, textColor, HDevTheme.Typography.FontSizeBody);
            var labelX = center.X + radius + 8;
            var labelY = (bounds.Height - labelText.Height) / 2;
            context.DrawText(labelText, Crisp.Snap(new Point(labelX, labelY)));
        }
    }
}
