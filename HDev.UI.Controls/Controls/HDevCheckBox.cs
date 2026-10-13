using Avalonia.Data;
using Avalonia;
using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

/// <summary>
/// Case à cocher professionnelle style VS2022
/// Avec support indeterminate et animations visuelles
/// </summary>
public class HDevCheckBox : HDevControlBase, IHDevEditor
{
    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    public static readonly StyledProperty<bool?> IsCheckedProperty =
        AvaloniaProperty.Register<HDevCheckBox, bool?>(nameof(IsChecked), false,
            defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);
    
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
    
    protected override void OnClick()
    {
        if (!IsEnabled || IsReadOnly) return;

        Core.CommitUser(IsThreeState
            ? IsChecked switch
            {
                false => true,
                true => null,
                null => false
            }
            : !(IsChecked ?? false));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsCheckedProperty)
            Core.OnPropertyChanged(change);
    }
    

    // ═══════════════════════════════════════════════════════════════
    // CONTRAT DE VALEUR (IHDevEditor, D2)
    // ═══════════════════════════════════════════════════════════════

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<HDevCheckBox, bool>(nameof(IsReadOnly));

    /// <summary>Lecture seule : la valeur s'affiche, l'utilisateur ne la change pas</summary>
    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    private HDevValueCore<bool?>? _core;
    private HDevValueCore<bool?> Core => _core ??= new HDevValueCore<bool?>(this, IsCheckedProperty);

    /// <summary>La valeur, non typée (<c>bool?</c>, celle de <see cref="IsChecked"/>)</summary>
    public object? EditValue
    {
        get => IsChecked;
        set => IsChecked = (bool?)value;
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
            return new Size(boxSize + 6, Math.Max(boxSize + 6, HDevTheme.Size.EditorHeight));
        }
        
        var labelText = CreateText(Label, HDevTheme.Text.Primary, HDevTheme.Typography.FontSizeBody);
        // Hauteur de ligne des champs : une case à cocher s'aligne sur le
        // rythme d'un formulaire (Début : [x] [date] [heure])
        return new Size(boxSize + spacing + labelText.Width + 6,
            Math.Max(Math.Max(boxSize, labelText.Height) + 6, HDevTheme.Size.EditorHeight));
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
        if (IsFocusVisible && IsEnabled)
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
