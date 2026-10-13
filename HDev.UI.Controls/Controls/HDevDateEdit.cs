using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using HDev.UI.Controls.Theme;
using System.Globalization;
using System.Linq;
using Calendar = Avalonia.Controls.Calendar;

namespace HDev.UI.Controls;

/// <summary>
/// Éditeur de date avec masque et calendrier déroulant :
/// saisie masquée selon Format, bouton calendrier, MinDate/MaxDate, validation au blur.
/// Value est liable dans les deux sens ; l'heure de la valeur n'est ni affichée ni perdue :
/// une date choisie par l'utilisateur garde l'heure de la valeur courante.
/// </summary>
public class HDevDateEdit : HDevEditorBase<DateTime?>
{
    public static readonly StyledProperty<DateTime?> ValueProperty =
        AvaloniaProperty.Register<HDevDateEdit, DateTime?>(nameof(Value),
            defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true, coerce: CoerceThroughChanging);

    private string _format = "dd/MM/yyyy";

    private HDevMonthCalendar? _calendar;

    public DateTime MinDate { get; set; } = DateTime.MinValue;
    public DateTime MaxDate { get; set; } = DateTime.MaxValue;

    /// <summary>Format d'affichage/saisie (lettres = chiffres du masque), défaut dd/MM/yyyy</summary>
    public string Format
    {
        get => _format;
        set
        {
            _format = value;
            Mask = DeriveMask(value);
            if (Value.HasValue)
                ResyncText();
        }
    }

    /// <summary>Valeur typée (null = vide)</summary>
    public DateTime? Value
    {
        get => GetValue(ValueProperty);
        set => SetValueFromCode(value);
    }

    protected override StyledProperty<DateTime?> ValuePropertyCore => ValueProperty;

    public HDevDateEdit()
    {
        Mask = DeriveMask(_format);
        Placeholder = _format.ToLowerInvariant();
        MinWidth = 130;

        // Bouton calendrier : un bouton déroulant du socle, au glyphe de calendrier, que
        // l'application peut retirer ou compléter ; le calendrier est recréé à chaque ouverture
        Buttons.Add(new HDevEditorButton { Kind = HDevEditorButtonKind.DropDown, Icon = HDevEditorIcon.Calendar });
        UseDropDown();
        DropDownOpening += (_, _) => PrepareCalendar();
    }

    private static string DeriveMask(string format)
        => new(format.Select(c => char.IsLetter(c) ? '0' : c).ToArray());

    protected override string FormatValue(DateTime? value)
        => value?.ToString(_format, CultureInfo.CurrentCulture) ?? "";

    /// <summary>La date choisie, à l'heure de la valeur courante (00:00 si vide)</summary>
    private DateTime AtCurrentTime(DateTime date) => date.Date + (Value?.TimeOfDay ?? TimeSpan.Zero);

    protected override void OnInnerTextChanged()
    {
        if (IsUpdatingText) return;

        // Saisie complète et valide -> mise à jour de Value au fil de l'eau
        if (IsMaskComplete &&
            DateTime.TryParseExact(Text, _format, CultureInfo.CurrentCulture,
                DateTimeStyles.None, out var parsed) &&
            parsed >= MinDate && parsed <= MaxDate)
        {
            if (Value?.Date != parsed.Date)
                CommitUserValue(AtCurrentTime(parsed), fromText: true);
        }
        else if (string.IsNullOrEmpty(Text) && Value != null)
        {
            CommitUserValue(null, fromText: true);
        }
    }

    protected override object? ConvertTextToValue() => Value;

    protected override string? GetIntrinsicError()
    {
        if (string.IsNullOrEmpty(Text))
            return null; // vide = null autorisé (la validation métier peut refuser via Validating)

        if (!DateTime.TryParseExact(Text, _format, CultureInfo.CurrentCulture,
                DateTimeStyles.None, out var parsed))
            return $"Date invalide (format {_format.ToLowerInvariant()})";

        if (parsed < MinDate)
            return $"Date antérieure au {MinDate.ToString(_format, CultureInfo.CurrentCulture)}";
        if (parsed > MaxDate)
            return $"Date postérieure au {MaxDate.ToString(_format, CultureInfo.CurrentCulture)}";

        return null;
    }

    // ═══════════════════════════════════════════════════════════════
    // POPUP CALENDRIER
    // ═══════════════════════════════════════════════════════════════

    private void PrepareCalendar()
    {
        // HDevMonthCalendar natif (le Calendar Fluent gardait un état
        // d'interaction périmé à la réouverture — le nôtre est recréé aussi,
        // ceinture et bretelles). Pas de présélection : n'importe quel clic,
        // y compris la date déjà choisie, doit commiter.
        _calendar = new HDevMonthCalendar
        {
            DisplayMonth = Value ?? DateTime.Today,
            MinDate = MinDate,
            MaxDate = MaxDate
        };
        _calendar.ValueChanged += OnCalendarDateSelected;
        DropDown.Content = _calendar;
    }

    private void OnCalendarDateSelected(object? sender, HDevValueChangedEventArgs e)
    {
        // Encapsulation : le calendrier vit dans le popup, hors de l'arbre de l'éditeur ; son
        // événement de valeur s'arrête ici, seul celui de l'éditeur remonte
        e.Handled = true;
        if (_calendar?.SelectedDate is { } date)
        {
            CommitUserValue(AtCurrentTime(date));
            DropDown.Close(HDevDropDownCloseReason.Accepted);
            FocusEditor();
        }
    }
}

/// <summary>
/// Éditeur d'heure avec masque HH:mm et boutons spin : flèches ±1 minute, PageUp/PageDown
/// ±1 heure, molette. <see cref="Value"/> est liable dans les deux sens ; c'est une heure du
/// jour, ramenée dans [00:00, 24:00) (23:59 + 1 min donne 00:00), y compris dans la source
/// liée. Les secondes d'une valeur posée par le code ne sont ni affichées ni perdues.
/// </summary>
public class HDevTimeEdit : HDevEditorBase<TimeSpan?>
{
    public static readonly StyledProperty<TimeSpan?> ValueProperty =
        AvaloniaProperty.Register<HDevTimeEdit, TimeSpan?>(nameof(Value),
            defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true, coerce: CoerceThroughChanging);

    /// <summary>Valeur typée (null = vide), heure du jour</summary>
    public TimeSpan? Value
    {
        get => GetValue(ValueProperty);
        set => SetValueFromCode(value);
    }

    protected override StyledProperty<TimeSpan?> ValuePropertyCore => ValueProperty;

    public HDevTimeEdit()
    {
        Mask = "00:00";
        Placeholder = "hh:mm";
        MinWidth = 80;
        Width = 90;

        var spin = new HDevSpinButtons();
        spin.UpClicked += (s, e) => Adjust(TimeSpan.FromMinutes(1));
        spin.DownClicked += (s, e) => Adjust(TimeSpan.FromMinutes(-1));
        SetRightElement(spin);

        InnerTextBox.AddHandler(KeyDownEvent, OnEditorKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>Heure du jour : ramenée dans [0, 24 h)</summary>
    protected override TimeSpan? OnCoerceValue(TimeSpan? value) => value.HasValue ? Normalize(value.Value) : null;

    /// <summary>Ramène dans [0, 24 h), en gardant les enchaînements 23:59 + 1 min -> 00:00</summary>
    private static TimeSpan Normalize(TimeSpan t)
    {
        var ticks = t.Ticks % TimeSpan.TicksPerDay;
        return TimeSpan.FromTicks(ticks < 0 ? ticks + TimeSpan.TicksPerDay : ticks);
    }

    protected override string FormatValue(TimeSpan? value)
        => value is { } v ? $"{v.Hours:00}:{v.Minutes:00}" : "";

    protected override void OnInnerTextChanged()
    {
        if (IsUpdatingText) return;

        if (IsMaskComplete && TryParseTime(Text, out var parsed))
            CommitUserValue(parsed, fromText: true);
        else if (string.IsNullOrEmpty(Text) && Value != null)
            CommitUserValue(null, fromText: true);
    }

    private static bool TryParseTime(string text, out TimeSpan value)
    {
        value = default;
        var parts = text.Split(':');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var h) || !int.TryParse(parts[1], out var m) ||
            h > 23 || m > 59)
            return false;

        value = new TimeSpan(h, m, 0);
        return true;
    }

    protected override object? ConvertTextToValue() => Value;

    protected override string? GetIntrinsicError()
    {
        if (string.IsNullOrEmpty(Text))
            return null;

        return TryParseTime(Text, out _) ? null : "Heure invalide (hh:mm, 00:00 à 23:59)";
    }

    private void Adjust(TimeSpan delta)
    {
        if (!IsEnabled || IsReadOnly) return;
        CommitUserValue(Normalize((Value ?? TimeSpan.Zero) + delta));
        FocusEditor();
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
                Adjust(TimeSpan.FromMinutes(1));
                e.Handled = true;
                break;
            case Key.Down:
                Adjust(TimeSpan.FromMinutes(-1));
                e.Handled = true;
                break;
            case Key.PageUp:
                Adjust(TimeSpan.FromHours(1));
                e.Handled = true;
                break;
            case Key.PageDown:
                Adjust(TimeSpan.FromHours(-1));
                e.Handled = true;
                break;
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (IsEnabled && !IsReadOnly && InnerTextBox.IsFocused)
        {
            Adjust(TimeSpan.FromMinutes(e.Delta.Y > 0 ? 1 : -1));
            e.Handled = true;
        }
        base.OnPointerWheelChanged(e);
    }
}
