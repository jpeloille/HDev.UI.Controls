using System.Collections;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace Spike;

public enum ValueOrigin { Code, User }

public sealed class ValueChangedArgs : RoutedEventArgs
{
    public ValueChangedArgs(RoutedEvent e, object? oldValue, object? newValue, ValueOrigin origin) : base(e)
    { OldValue = oldValue; NewValue = newValue; Origin = origin; }
    public object? OldValue { get; }
    public object? NewValue { get; }
    public ValueOrigin Origin { get; }
}

/// Socle non générique : porte l'événement routé commun et l'erreur de liaison
public abstract class EditorBase : Border
{
    public static readonly RoutedEvent<ValueChangedArgs> ValueChangedEvent =
        RoutedEvent.Register<EditorBase, ValueChangedArgs>("ValueChanged", RoutingStrategies.Bubble);

    public event EventHandler<ValueChangedArgs>? ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    public string? BindingError { get; protected set; }
}


/// Couche générique SANS propriété : la logique typée, la propriété vient de la classe concrète
public abstract class Editor<T> : EditorBase
{
    protected abstract StyledProperty<T> ValuePropertyCore { get; }
    public abstract T Value { get; set; }

    private bool _userCommit;

    public void CommitFromUser(T value)
    {
        _userCommit = true;
        try { SetCurrentValue(ValuePropertyCore, value); }
        finally { _userCommit = false; }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValuePropertyCore)
            RaiseEvent(new ValueChangedArgs(ValueChangedEvent, change.OldValue, change.NewValue,
                _userCommit ? ValueOrigin.User : ValueOrigin.Code));
    }

    protected override void UpdateDataValidation(AvaloniaProperty property, BindingValueType state, Exception? error)
    {
        base.UpdateDataValidation(property, state, error);
        if (property == ValuePropertyCore)
            BindingError = error?.Message;
    }
}

public class SpikeDateEdit : Editor<DateTime?>
{
    public static readonly StyledProperty<DateTime?> ValueProperty = AvaloniaProperty.Register<SpikeDateEdit, DateTime?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);
    protected override StyledProperty<DateTime?> ValuePropertyCore => ValueProperty;
    public override DateTime? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
}

public class SpikeSpinEdit : Editor<decimal>
{
    public static readonly StyledProperty<decimal> ValueProperty = AvaloniaProperty.Register<SpikeSpinEdit, decimal>(nameof(Value), defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);
    protected override StyledProperty<decimal> ValuePropertyCore => ValueProperty;
    public override decimal Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
}

public class SpikeObjEdit : Editor<object?>
{
    public static readonly StyledProperty<object?> ValueProperty = AvaloniaProperty.Register<SpikeObjEdit, object?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);
    protected override StyledProperty<object?> ValuePropertyCore => ValueProperty;
    public override object? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
}

public class SpikeVm : INotifyPropertyChanged, INotifyDataErrorInfo
{
    private DateTime? _date;
    private decimal _amount;
    private string? _dateError;

    public DateTime? Date
    {
        get => _date;
        set { _date = value; Raise(nameof(Date)); }
    }
    private string? _name = "Alpha";
    public string? Name { get => _name; set { _name = value; Raise(nameof(Name)); } }
    private bool _canClear;
    public bool CanClear { get => _canClear; set { _canClear = value; Raise(nameof(CanClear)); } }
    private decimal _percent;
    public decimal Percent { get => _percent; set { _percent = value; Raise(nameof(Percent)); } }
    private DateOnly? _birth;
    public DateOnly? Birth
    {
        get => _birth;
        set { _birth = value; Raise(nameof(Birth)); }
    }
    public decimal Amount
    {
        get => _amount;
        set { _amount = value; Raise(nameof(Amount)); }
    }

    public void SetDateError(string? error)
    {
        _dateError = error;
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Date)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    public bool HasErrors => _dateError != null;
    public IEnumerable GetErrors(string? propertyName)
        => propertyName == nameof(Date) && _dateError != null ? new[] { _dateError } : Array.Empty<string>();
    private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
