using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Metadata;

namespace Spike;

/// Porteur des événements communs, hors de toute hiérarchie (à la manière de Gestures)
public sealed class EditorEvents
{
    private EditorEvents() { }
    public static readonly RoutedEvent<ValueChangedArgs> ValueChangedEvent =
        RoutedEvent.Register<EditorEvents, ValueChangedArgs>("ValueChanged", RoutingStrategies.Bubble);
}

/// Pièce « bouton » : une description, liable, portée par l'éditeur (forme B)
public class EditorButton : StyledElement
{
    public static readonly StyledProperty<string> KindProperty =
        AvaloniaProperty.Register<EditorButton, string>(nameof(Kind), "Custom");
    public static readonly StyledProperty<bool> IsShownProperty =
        AvaloniaProperty.Register<EditorButton, bool>(nameof(IsShown), true);
    public string Kind { get => GetValue(KindProperty); set => SetValue(KindProperty, value); }
    public bool IsShown { get => GetValue(IsShownProperty); set => SetValue(IsShownProperty, value); }
}

/// Éditeur de forme B : boutons et popup sont des pièces, pas des classes
public class SpikeFieldEdit : Editor<string?>
{
    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<SpikeFieldEdit, string?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);
    protected override StyledProperty<string?> ValuePropertyCore => ValueProperty;
    public override string? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public static readonly StyledProperty<Control?> PopupContentProperty =
        AvaloniaProperty.Register<SpikeFieldEdit, Control?>(nameof(PopupContent));
    public Control? PopupContent { get => GetValue(PopupContentProperty); set => SetValue(PopupContentProperty, value); }

    public AvaloniaList<EditorButton> Buttons { get; } = new();
    public Popup Popup { get; }

    public SpikeFieldEdit()
    {
        Buttons.CollectionChanged += (_, e) =>
        {
            if (e.OldItems != null) foreach (EditorButton b in e.OldItems) LogicalChildren.Remove(b);
            if (e.NewItems != null) foreach (EditorButton b in e.NewItems) LogicalChildren.Add(b);
        };
        Popup = new Popup { PlacementTarget = this, IsLightDismissEnabled = false };
        Child = new Panel { Children = { Popup } };
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PopupContentProperty)
            Popup.Child = PopupContent;
    }

    // Règle d'encapsulation : les pièces internes ne font pas sortir leurs ValueChanged
    static SpikeFieldEdit()
    {
        ValueChangedEvent.AddClassHandler<SpikeFieldEdit>((x, e) => { if (!ReferenceEquals(e.Source, x)) e.Handled = true; });
    }
}

/// Bornage : la valeur est ramenée dans [0, 100]
public class SpikeBoundedEdit : Editor<decimal>
{
    public static readonly StyledProperty<decimal> ValueProperty =
        AvaloniaProperty.Register<SpikeBoundedEdit, decimal>(nameof(Value), defaultBindingMode: BindingMode.TwoWay,
            enableDataValidation: true, coerce: (_, v) => Math.Clamp(v, 0m, 100m));
    protected override StyledProperty<decimal> ValuePropertyCore => ValueProperty;
    public override decimal Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
}
