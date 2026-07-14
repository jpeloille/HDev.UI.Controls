using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ProControls.Theme;
using System.Globalization;
using System.Linq;
using Calendar = Avalonia.Controls.Calendar;

namespace ProControls.Controls;

/// <summary>
/// Éditeur de date avec masque et calendrier déroulant (équivalent DateEdit) :
/// saisie masquée selon Format, bouton calendrier, MinDate/MaxDate, validation au blur
/// </summary>
public class ProDateEdit : ProEditorBase
{
    private string _format = "dd/MM/yyyy";
    private DateTime? _value;
    private bool _updatingText;

    private readonly Popup _popup;
    private readonly Border _popupBorder;
    private ProMonthCalendar? _calendar;
    private readonly ProPopupDismissGuard _guard;
    private bool _isPopupOpen;

    public DateTime MinDate { get; set; } = DateTime.MinValue;
    public DateTime MaxDate { get; set; } = DateTime.MaxValue;

    /// <summary>Déclenché quand Value change (saisie complète valide ou calendrier)</summary>
    public event EventHandler? ValueChanged;

    /// <summary>Format d'affichage/saisie (lettres = chiffres du masque), défaut dd/MM/yyyy</summary>
    public string Format
    {
        get => _format;
        set
        {
            _format = value;
            Mask = DeriveMask(value);
            if (_value.HasValue)
                SetTextFromValue();
        }
    }

    /// <summary>Valeur typée (null = vide)</summary>
    public DateTime? Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value?.Date;
            SetTextFromValue();
            SetError(null);
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ProDateEdit()
    {
        Mask = DeriveMask(_format);
        Placeholder = _format.ToLowerInvariant();
        MinWidth = 130;

        // Bouton calendrier
        var button = new ProEditorGlyphButton { Glyph = "📅", GlyphSize = 13 };
        button.Click += (s, e) => TogglePopup();
        SetRightElement(button);

        // Conteneur du calendrier (le Calendar lui-même est recréé à chaque ouverture)
        _popupBorder = new Border
        {
            Background = new SolidColorBrush(ProTheme.Background.Panel),
            BorderBrush = new SolidColorBrush(ProTheme.Border.Default),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(ProTheme.Size.CornerRadiusMedium),
            Padding = new Thickness(4),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0, OffsetY = 2, Blur = 8,
                Color = ProTheme.Shadow.Color
            })
        };

        _popup = new Popup
        {
            Child = _popupBorder,
            PlacementTarget = this,
            Placement = PlacementMode.BottomEdgeAlignedLeft,
            // Garde maison (cf. ProComboBox) : le light dismiss ferme au moindre
            // rebond d'activation de fenêtre sous GNOME/XWayland
            IsLightDismissEnabled = false,
            VerticalOffset = 2
        };
        _guard = new ProPopupDismissGuard(() => _isPopupOpen, ClosePopup);
        _popup.Opened += (s, e) => _guard.Install(this);
        _popup.Closed += (s, e) => { _guard.Remove(); _isPopupOpen = false; };
        AttachPopup(_popup);

        // Navigation clavier : Alt+Bas ouvre le calendrier, Échap ferme
        InnerTextBox.AddHandler(KeyDownEvent, OnEditorKeyDown, RoutingStrategies.Tunnel);
    }

    private static string DeriveMask(string format)
        => new(format.Select(c => char.IsLetter(c) ? '0' : c).ToArray());

    protected override void RefreshThemeBrushes()
    {
        base.RefreshThemeBrushes();
        RefreshPopupBorder(_popupBorder); // le calendrier lui-même est recréé à l'ouverture
    }

    private void SetTextFromValue()
    {
        _updatingText = true;
        Text = _value?.ToString(_format, CultureInfo.CurrentCulture) ?? "";
        _updatingText = false;
    }

    protected override void OnInnerTextChanged()
    {
        if (_updatingText) return;

        // Saisie complète et valide -> mise à jour de Value au fil de l'eau
        if (IsMaskComplete &&
            DateTime.TryParseExact(Text, _format, CultureInfo.CurrentCulture,
                DateTimeStyles.None, out var parsed) &&
            parsed >= MinDate && parsed <= MaxDate)
        {
            if (_value != parsed.Date)
            {
                _value = parsed.Date;
                SetError(null);
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        else if (string.IsNullOrEmpty(Text) && _value != null)
        {
            _value = null;
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override object? ConvertTextToValue() => _value;

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

    private void TogglePopup()
    {
        if (_isPopupOpen)
            ClosePopup();
        else if (!_guard.JustClosed) // le garde vient de fermer sur ce même clic
            OpenPopup();
    }

    private void OpenPopup()
    {
        if (!IsEnabled || IsReadOnly) return;

        // ProMonthCalendar natif (le Calendar Fluent gardait un état
        // d'interaction périmé à la réouverture — le nôtre est recréé aussi,
        // ceinture et bretelles). Pas de présélection : n'importe quel clic,
        // y compris la date déjà choisie, doit commiter.
        _calendar = new ProMonthCalendar
        {
            DisplayMonth = _value ?? DateTime.Today,
            MinDate = MinDate,
            MaxDate = MaxDate
        };
        _calendar.SelectedDateChanged += OnCalendarDateSelected;
        _popupBorder.Child = _calendar;

        _isPopupOpen = true;
        _popup.IsOpen = true;
    }

    private void ClosePopup()
    {
        _isPopupOpen = false;
        _popup.IsOpen = false;
    }

    private void OnCalendarDateSelected(object? sender, EventArgs e)
    {
        if (_calendar?.SelectedDate is { } date)
        {
            Value = date;
            ClosePopup();
            FocusEditor();
        }
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down when e.KeyModifiers.HasFlag(KeyModifiers.Alt):
            case Key.F4:
                TogglePopup();
                e.Handled = true;
                break;

            case Key.Escape when _isPopupOpen:
                ClosePopup();
                e.Handled = true;
                break;
        }
    }
}

/// <summary>
/// Éditeur d'heure avec masque HH:mm et boutons spin (équivalent TimeEdit) :
/// flèches ±1 minute, PageUp/PageDown ±1 heure, molette
/// </summary>
public class ProTimeEdit : ProEditorBase
{
    private TimeSpan? _value;
    private bool _updatingText;

    /// <summary>Déclenché quand Value change</summary>
    public event EventHandler? ValueChanged;

    /// <summary>Valeur typée (null = vide), bornée sur 24 h</summary>
    public TimeSpan? Value
    {
        get => _value;
        set
        {
            var normalized = value.HasValue ? Normalize(value.Value) : (TimeSpan?)null;
            if (_value == normalized) return;
            _value = normalized;
            SetTextFromValue();
            SetError(null);
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ProTimeEdit()
    {
        Mask = "00:00";
        Placeholder = "hh:mm";
        MinWidth = 80;
        Width = 90;

        var spin = new ProSpinButtons();
        spin.UpClicked += (s, e) => Adjust(TimeSpan.FromMinutes(1));
        spin.DownClicked += (s, e) => Adjust(TimeSpan.FromMinutes(-1));
        SetRightElement(spin);

        InnerTextBox.AddHandler(KeyDownEvent, OnEditorKeyDown, RoutingStrategies.Tunnel);
    }

    private static TimeSpan Normalize(TimeSpan t)
    {
        // Ramener dans [0, 24h) en préservant les enchaînements 23:59 + 1 min -> 00:00
        var minutes = ((int)t.TotalMinutes % (24 * 60) + 24 * 60) % (24 * 60);
        return TimeSpan.FromMinutes(minutes);
    }

    private void SetTextFromValue()
    {
        _updatingText = true;
        Text = _value.HasValue ? $"{_value.Value.Hours:00}:{_value.Value.Minutes:00}" : "";
        _updatingText = false;
    }

    protected override void OnInnerTextChanged()
    {
        if (_updatingText) return;

        if (IsMaskComplete && TryParseTime(Text, out var parsed))
        {
            if (_value != parsed)
            {
                _value = parsed;
                SetError(null);
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        else if (string.IsNullOrEmpty(Text) && _value != null)
        {
            _value = null;
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
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

    protected override object? ConvertTextToValue() => _value;

    protected override string? GetIntrinsicError()
    {
        if (string.IsNullOrEmpty(Text))
            return null;

        return TryParseTime(Text, out _) ? null : "Heure invalide (hh:mm, 00:00 à 23:59)";
    }

    private void Adjust(TimeSpan delta)
    {
        if (!IsEnabled || IsReadOnly) return;
        Value = (_value ?? TimeSpan.Zero) + delta;
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
