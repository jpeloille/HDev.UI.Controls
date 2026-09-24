using Avalonia.Input;
using Avalonia.Interactivity;
using System.Globalization;
using System.Linq;

namespace HDev.UI.Controls;

/// <summary>
/// Éditeur numérique avec boutons spin (équivalent SpinEdit) :
/// Increment, MinValue/MaxValue, Decimals, molette + flèches, format culture
/// </summary>
public class HDevSpinEdit : HDevEditorBase
{
    private decimal _value;
    private bool _updatingText;

    public decimal Increment { get; set; } = 1;
    public decimal MinValue { get; set; } = decimal.MinValue;
    public decimal MaxValue { get; set; } = decimal.MaxValue;

    private int _decimals;

    /// <summary>Nombre de décimales affichées (0 = entier)</summary>
    public int Decimals
    {
        get => _decimals;
        set { _decimals = Math.Max(0, value); SetTextFromValue(); }
    }

    /// <summary>Déclenché quand Value change</summary>
    public event EventHandler? ValueChanged;

    public decimal Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, MinValue, MaxValue);
            if (_value == clamped) return;
            _value = clamped;
            SetTextFromValue();
            SetError(null);
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public HDevSpinEdit()
    {
        MinWidth = 90;
        Width = 110;

        var spin = new HDevSpinButtons();
        spin.UpClicked += (s, e) => Adjust(Increment);
        spin.DownClicked += (s, e) => Adjust(-Increment);
        SetRightElement(spin);

        // Filtre numérique : chiffres, séparateur décimal, signe en tête
        InnerTextBox.AddHandler(TextInputEvent, OnNumericInput, RoutingStrategies.Tunnel);
        InnerTextBox.AddHandler(KeyDownEvent, OnEditorKeyDown, RoutingStrategies.Tunnel);

        SetTextFromValue();
    }

    private void SetTextFromValue()
    {
        _updatingText = true;
        Text = _value.ToString($"F{_decimals}", CultureInfo.CurrentCulture);
        _updatingText = false;
    }

    protected override void OnInnerTextChanged()
    {
        if (_updatingText) return;

        if (decimal.TryParse(Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
            && parsed >= MinValue && parsed <= MaxValue)
        {
            if (_value != parsed)
            {
                _value = parsed;
                SetError(null);
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    protected override object? ConvertTextToValue() => _value;

    protected override string? GetIntrinsicError()
    {
        if (string.IsNullOrEmpty(Text))
            return null;

        if (!decimal.TryParse(Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed))
            return "Nombre invalide";

        if (parsed < MinValue)
            return $"Minimum : {MinValue.ToString($"F{_decimals}", CultureInfo.CurrentCulture)}";
        if (parsed > MaxValue)
            return $"Maximum : {MaxValue.ToString($"F{_decimals}", CultureInfo.CurrentCulture)}";

        return null;
    }

    private void OnNumericInput(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text))
            return;

        var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        var text = InnerTextBox.Text ?? "";
        var caret = InnerTextBox.CaretIndex;

        // La sélection va être remplacée : l'exclure des contrôles d'unicité/position
        if (InnerTextBox.SelectionStart != InnerTextBox.SelectionEnd)
        {
            var selStart = Math.Min(InnerTextBox.SelectionStart, InnerTextBox.SelectionEnd);
            var selEnd = Math.Max(InnerTextBox.SelectionStart, InnerTextBox.SelectionEnd);
            text = text.Remove(selStart, selEnd - selStart);
            caret = selStart;
        }

        foreach (var ch in e.Text)
        {
            var ok = char.IsDigit(ch)
                || (ch.ToString() == separator && _decimals > 0 && !text.Contains(separator))
                || (ch == '-' && MinValue < 0 && caret == 0 && !text.StartsWith('-'));

            if (!ok)
            {
                e.Handled = true;
                return;
            }
        }
    }

    private void Adjust(decimal delta)
    {
        if (!IsEnabled || IsReadOnly) return;
        Value = Math.Clamp(_value + delta, MinValue, MaxValue);
        FocusEditor();
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
                Adjust(Increment);
                e.Handled = true;
                break;
            case Key.Down:
                Adjust(-Increment);
                e.Handled = true;
                break;
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (IsEnabled && !IsReadOnly && InnerTextBox.IsFocused)
        {
            Adjust(e.Delta.Y > 0 ? Increment : -Increment);
            e.Handled = true;
        }
        base.OnPointerWheelChanged(e);
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        // Reformater proprement à la sortie (ex : "5," -> "5,00")
        if (ErrorText == null)
            SetTextFromValue();
        base.OnLostFocus(e);
    }
}
