using Avalonia.Data;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

/// <summary>
/// Bouton radio (style Yaru) : exclusivité et flèches partagées avec
/// <see cref="HDevRadioCard"/> par <see cref="HDevRadioGroup"/>
/// </summary>
public class HDevRadioButton : HDevControlBase, IHDevEditor
{
    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<HDevRadioButton, bool>(nameof(IsChecked), false,
            defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);
    
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
    
    protected override void OnClick()
    {
        if (!IsEnabled || IsChecked || IsReadOnly) return;
        Core.CommitUser(true);
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
                foreach (var radio in HDevRadioGroup.Siblings(this, r => r.GroupName))
                    radio.SetCurrentValue(IsCheckedProperty, false);
            }

            Core.OnPropertyChanged(change);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Navigation par flèches au sein du groupe (coche la cible, comme WPF/WinForms)
        if (!e.Handled && IsEnabled && !IsReadOnly && HDevRadioGroup.Navigate(this, e.Key, r => r.GroupName) is { } next)
        {
            next.Core.CommitUser(true);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }
    

    // ═══════════════════════════════════════════════════════════════
    // CONTRAT DE VALEUR (IHDevEditor, D2)
    // ═══════════════════════════════════════════════════════════════

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<HDevRadioButton, bool>(nameof(IsReadOnly));

    /// <summary>Lecture seule : la valeur s'affiche, l'utilisateur ne la change pas</summary>
    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    private HDevValueCore<bool>? _core;
    private HDevValueCore<bool> Core => _core ??= new HDevValueCore<bool>(this, IsCheckedProperty);

    /// <summary>La valeur, non typée (<c>bool</c>, celle de <see cref="IsChecked"/>)</summary>
    public object? EditValue
    {
        get => IsChecked;
        set => IsChecked = value is true;
    }

    /// <summary>Erreur de la liaison de la valeur (null = valide)</summary>
    public string? ErrorText => Core.ErrorText;

    /// <summary>Vrai si la valeur est valide (aucune erreur de liaison)</summary>
    public bool Validate() => ErrorText == null;

    public void FocusEditor() => Focus();

    /// <summary>Changement de valeur, au code comme à l'utilisateur (événement commun, routé)</summary>
    public event EventHandler<HDevValueChangedEventArgs>? ValueChanged
    {
        add => AddHandler(HDevEditor.ValueChangedEvent, value);
        remove => RemoveHandler(HDevEditor.ValueChangedEvent, value);
    }

    /// <summary>Avant un changement de l'utilisateur ; annulable</summary>
    public event EventHandler<HDevValueChangingEventArgs>? ValueChanging
    {
        add => AddHandler(HDevEditor.ValueChangingEvent, value);
        remove => RemoveHandler(HDevEditor.ValueChangingEvent, value);
    }

    protected override void UpdateDataValidation(AvaloniaProperty property, Avalonia.Data.BindingValueType state, Exception? error)
    {
        base.UpdateDataValidation(property, state, error);
        Core.OnDataValidation(property, error);
    }

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
        if (IsFocusVisible && IsEnabled)
            HDevFocus.DrawEllipseRing(context, center, radius);
        
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
