using Avalonia;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Globalization;

namespace HDev.UI.Controls;

/// <summary>
/// Éditeur numérique avec boutons spin : Increment, Minimum et Maximum, Decimals, molette et
/// flèches, format de la culture. <see cref="Value"/> est liable dans les deux sens ; une valeur
/// hors bornes est ramenée à la borne, y compris dans la source liée.
/// </summary>
public class HDevSpinEdit : HDevEditorBase<decimal>
{
    public static readonly StyledProperty<decimal> ValueProperty =
        AvaloniaProperty.Register<HDevSpinEdit, decimal>(nameof(Value),
            defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true, coerce: CoerceThroughChanging);

    public static readonly StyledProperty<decimal> MinimumProperty =
        AvaloniaProperty.Register<HDevSpinEdit, decimal>(nameof(Minimum), decimal.MinValue);

    public static readonly StyledProperty<decimal> MaximumProperty =
        AvaloniaProperty.Register<HDevSpinEdit, decimal>(nameof(Maximum), decimal.MaxValue);

    public decimal Increment { get; set; } = 1;

    /// <summary>Borne basse de la valeur (bornage : la valeur et sa source y sont ramenées)</summary>
    public decimal Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>Borne haute de la valeur (bornage : la valeur et sa source y sont ramenées)</summary>
    public decimal Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    private int _decimals;

    /// <summary>Nombre de décimales affichées (0 = entier)</summary>
    public int Decimals
    {
        get => _decimals;
        set { _decimals = Math.Max(0, value); ResyncText(); }
    }

    /// <summary>Valeur, bornée à [Minimum, Maximum]</summary>
    public decimal Value
    {
        get => GetValue(ValueProperty);
        set => SetValueFromCode(value);
    }

    protected override StyledProperty<decimal> ValuePropertyCore => ValueProperty;

    static HDevSpinEdit()
    {
        // Des bornes qui changent ramènent la valeur dans le nouvel intervalle. La valeur courante
        // est reposée, et pas recoercée depuis la valeur d'origine : une valeur substituée par
        // ValueChanging ne revient pas à celle qui avait été demandée
        MinimumProperty.Changed.AddClassHandler<HDevSpinEdit>((x, _) => x.SetCurrentValue(ValueProperty, x.Value));
        MaximumProperty.Changed.AddClassHandler<HDevSpinEdit>((x, _) => x.SetCurrentValue(ValueProperty, x.Value));
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

        ResyncText();
    }

    /// <summary>Bornage à [Minimum, Maximum]</summary>
    protected override decimal OnCoerceValue(decimal value)
        => Math.Clamp(value, Minimum, Math.Max(Minimum, Maximum));

    private string Format(decimal value) => value.ToString($"F{_decimals}", CultureInfo.CurrentCulture);

    protected override string FormatValue(decimal value) => Format(value);

    protected override void OnInnerTextChanged()
    {
        if (IsUpdatingText) return;

        if (decimal.TryParse(Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
            && parsed >= Minimum && parsed <= Maximum)
            CommitUserValue(parsed, fromText: true);
    }

    protected override object? ConvertTextToValue() => Value;

    protected override string? GetIntrinsicError()
    {
        if (string.IsNullOrEmpty(Text))
            return null;

        if (!decimal.TryParse(Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed))
            return "Nombre invalide";

        if (parsed < Minimum)
            return $"Minimum : {Format(Minimum)}";
        if (parsed > Maximum)
            return $"Maximum : {Format(Maximum)}";

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
                || (ch == '-' && Minimum < 0 && caret == 0 && !text.StartsWith('-'));

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
        CommitUserValue(OnCoerceValue(Value + delta));
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
            ResyncText();
        base.OnLostFocus(e);
    }
}
