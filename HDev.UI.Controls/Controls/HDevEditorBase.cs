using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Data;
using Avalonia.Media;
using HDev.UI.Controls.Theme;
using System.ComponentModel;

namespace HDev.UI.Controls;

// ═══════════════════════════════════════════════════════════════════════════
// MOTEUR DE MASQUE
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Moteur de masque de saisie « simple » (type MaskedTextBox WinForms) :
/// 0 = chiffre requis · 9 = chiffre optionnel · L = lettre · A = alphanumérique ·
/// tout autre caractère = littéral auto-inséré (ex : "00/00/0000", "00:00")
/// </summary>
public static class HDevMaskEngine
{
    public static bool IsPlaceholder(char maskChar)
        => maskChar is '0' or '9' or 'L' or 'A';

    public static bool Matches(char maskChar, char input) => maskChar switch
    {
        '0' or '9' => char.IsDigit(input),
        'L' => char.IsLetter(input),
        'A' => char.IsLetterOrDigit(input),
        _ => maskChar == input
    };

    /// <summary>
    /// Tente d'insérer une saisie à la position du caret en respectant le masque.
    /// Les littéraux du masque sont insérés automatiquement.
    /// </summary>
    public static bool TryInsert(string mask, string text, int caret, string input,
        out string newText, out int newCaret)
    {
        newText = text;
        newCaret = caret;

        foreach (var ch in input)
        {
            // Avancer sur les littéraux du masque (auto-insertion)
            while (newCaret < mask.Length && !IsPlaceholder(mask[newCaret]))
            {
                if (newCaret >= newText.Length)
                    newText += mask[newCaret];
                newCaret++;

                // L'utilisateur a tapé le littéral lui-même : consommé
                if (newCaret <= mask.Length && mask[newCaret - 1] == ch)
                    goto nextChar;
            }

            if (newCaret >= mask.Length || !Matches(mask[newCaret], ch))
                return false;

            newText = newCaret < newText.Length
                ? newText.Remove(newCaret, 1).Insert(newCaret, ch.ToString())
                : newText + ch;
            newCaret++;

            nextChar: ;
        }

        return true;
    }

    /// <summary>Vrai si tous les emplacements requis (0, L, A) du masque sont remplis</summary>
    public static bool IsComplete(string mask, string text)
    {
        for (int i = 0; i < mask.Length; i++)
        {
            var required = mask[i] is '0' or 'L' or 'A';
            if (required && (i >= text.Length || !Matches(mask[i], text[i])))
                return false;
        }
        return true;
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// VALIDATION
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Arguments de l'événement Validating (annulable, style DevExpress)
/// </summary>
public class HDevValidatingEventArgs : CancelEventArgs
{
    public object? NewValue { get; set; }

    /// <summary>Message d'erreur affiché si Cancel = true</summary>
    public string? ErrorText { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
// SOCLE COMMUN DES ÉDITEURS
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Socle des éditeurs de saisie (équivalent BaseEdit/RepositoryItem) :
/// TextBox interne stylé Yaru, masque de saisie, validation au blur,
/// zone de boutons à droite fournie par l'éditeur dérivé
/// </summary>
public abstract class HDevEditorBase : Border
{
    protected readonly TextBox InnerTextBox;
    private readonly Grid _layoutGrid;
    private bool _isHovered;
    private bool _isFocusedState;
    private bool _suppressMask;

    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<HDevEditorBase, string?>(nameof(Placeholder));

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<HDevEditorBase, bool>(nameof(IsReadOnly));

    public static readonly StyledProperty<string?> MaskProperty =
        AvaloniaProperty.Register<HDevEditorBase, string?>(nameof(Mask));

    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>
    /// Masque de saisie simple (0 = chiffre, 9 = chiffre optionnel, L = lettre,
    /// A = alphanumérique, autres = littéraux). Vide/null = pas de masque.
    /// </summary>
    public string? Mask
    {
        get => GetValue(MaskProperty);
        set => SetValue(MaskProperty, value);
    }

    // Deux sources d'erreur, gardées à part : la saisie (erreur intrinsèque, Validating,
    // SetError) et la liaison (INotifyDataErrorInfo, exception de la source). Une saisie
    // valide efface la première sans toucher à la seconde, que seule la source retire.
    private string? _inputError;
    private string? _bindingError;

    /// <summary>
    /// Erreur affichée (null = valide) : celle de la saisie d'abord, la plus proche de ce que
    /// l'utilisateur voit ; sinon celle de la liaison. Bordure rouge, halo et infobulle.
    /// </summary>
    public string? ErrorText => _inputError ?? _bindingError;

    public bool HasError => ErrorText != null;

    /// <summary>Validation annulable, déclenchée à la perte de focus (style DevExpress)</summary>
    public event EventHandler<HDevValidatingEventArgs>? Validating;

    /// <summary>Déclenché quand le texte change</summary>
    public event EventHandler? TextChanged;

    public string Text
    {
        get => InnerTextBox.Text ?? "";
        set => InnerTextBox.Text = value;
    }

    protected HDevEditorBase()
    {
        CornerRadius = new CornerRadius(HDevTheme.Size.CornerRadiusSmall);
        BorderThickness = new Thickness(1);
        // Hauteur unique des champs : dans une rangée plus haute, l'éditeur se centre
        // (Stretch borné par MaxHeight) au lieu de s'étirer
        MinHeight = HDevTheme.Size.EditorHeight;
        MaxHeight = HDevTheme.Size.EditorHeight;
        MinWidth = 100;
        // Pas de clip sur l'hôte : il couperait le halo de focus (BoxShadow)
        // dessiné à l'extérieur du filet ; c'est la grille interne qui clippe

        InnerTextBox = new TextBox
        {
            Padding = new Thickness(HDevTheme.Size.EditorTextInset, 0),
            Margin = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily(HDevTheme.Typography.FontFamily),
            FontSize = HDevTheme.Typography.FontSizeBody,
            CaretBrush = new SolidColorBrush(HDevTheme.Text.Primary),
            SelectionBrush = new SolidColorBrush(HDevTheme.Background.TextSelection),
            SelectionForegroundBrush = new SolidColorBrush(HDevTheme.Text.Primary)
        };
        HDevInputChrome.Neutralize(InnerTextBox);

        // Masque : filtrer la saisie AVANT le TextBox (tunnel)
        InnerTextBox.AddHandler(TextInputEvent, OnInnerTextInput, RoutingStrategies.Tunnel);
        InnerTextBox.AddHandler(KeyDownEvent, OnInnerKeyDownForMask, RoutingStrategies.Tunnel);
        InnerTextBox.AddHandler(TextBox.PastingFromClipboardEvent, OnInnerPasting);

        InnerTextBox.TextChanged += (s, e) =>
        {
            OnInnerTextChanged();
            TextChanged?.Invoke(this, EventArgs.Empty);
        };

        InnerTextBox.GotFocus += (s, e) => { _isFocusedState = true; UpdateVisualState(); };
        InnerTextBox.LostFocus += (s, e) =>
        {
            _isFocusedState = false;
            Validate();
            UpdateVisualState();
        };

        _layoutGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ClipToBounds = true
        };
        Grid.SetColumn(InnerTextBox, 0);
        _layoutGrid.Children.Add(InnerTextBox);

        Child = _layoutGrid;
        UpdateVisualState();
    }

    static HDevEditorBase()
    {
        PlaceholderProperty.Changed.AddClassHandler<HDevEditorBase>((x, _) =>
            x.InnerTextBox.PlaceholderText = x.Placeholder);
        IsReadOnlyProperty.Changed.AddClassHandler<HDevEditorBase>((x, _) =>
            x.InnerTextBox.IsReadOnly = x.IsReadOnly);
        IsEnabledProperty.Changed.AddClassHandler<HDevEditorBase>((x, _) => x.UpdateVisualState());

        // Encapsulation : les pièces internes d'un éditeur (calendrier, liste, grille d'un popup)
        // ne font pas sortir leurs événements de valeur ; seuls ceux de l'éditeur remontent
        HDevEditor.ValueChangedEvent.AddClassHandler<HDevEditorBase>((x, e) =>
        {
            if (!ReferenceEquals(e.Source, x)) e.Handled = true;
        });
        HDevEditor.ValueChangingEvent.AddClassHandler<HDevEditorBase>((x, e) =>
        {
            if (!ReferenceEquals(e.Source, x)) e.Handled = true;
        });
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        HDevTheme.VariantChanged += OnThemeVariantChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        HDevTheme.VariantChanged -= OnThemeVariantChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e) => RefreshThemeBrushes();

    /// <summary>
    /// Re-pose les brushes construits après un changement de variante ;
    /// les éditeurs à popup surchargent pour re-brusher leur dropdown
    /// </summary>
    protected virtual void RefreshThemeBrushes()
    {
        InnerTextBox.CaretBrush = new SolidColorBrush(HDevTheme.Text.Primary);
        InnerTextBox.SelectionBrush = new SolidColorBrush(HDevTheme.Background.TextSelection);
        InnerTextBox.SelectionForegroundBrush = new SolidColorBrush(HDevTheme.Text.Primary);
        HDevInputChrome.Neutralize(InnerTextBox);
        UpdateVisualState();
    }

    /// <summary>Re-brushe un Border de dropdown (fond, bordure, ombre)</summary>
    protected static void RefreshPopupBorder(Avalonia.Controls.Border border)
    {
        border.Background = new SolidColorBrush(HDevTheme.Background.Panel);
        border.BorderBrush = new SolidColorBrush(HDevTheme.Border.Default);
        border.BoxShadow = new BoxShadows(new BoxShadow
        {
            OffsetX = 0, OffsetY = 2, Blur = 8,
            Color = HDevTheme.Shadow.Color
        });
    }

    /// <summary>Installe la zone de boutons à droite du texte (calendrier, spin...)</summary>
    protected void SetRightElement(Control element)
    {
        Grid.SetColumn(element, 1);
        _layoutGrid.Children.Add(element);
    }

    /// <summary>
    /// Héberge un Popup dans l'arbre logique de l'éditeur (un Popup mesure 0,
    /// son contenu vit en overlay — cf. OverlayPopups)
    /// </summary>
    protected void AttachPopup(Avalonia.Controls.Primitives.Popup popup)
    {
        Grid.SetColumn(popup, 0);
        _layoutGrid.Children.Add(popup);
    }

    /// <summary>Convertit le texte courant en valeur typée (null si invalide) — pour Validating</summary>
    protected virtual object? ConvertTextToValue() => Text;

    /// <summary>Appelé quand le texte interne change (avant TextChanged public)</summary>
    protected virtual void OnInnerTextChanged() { }

    // ═══════════════════════════════════════════════════════════════
    // MASQUE
    // ═══════════════════════════════════════════════════════════════

    private void OnInnerTextInput(object? sender, TextInputEventArgs e)
    {
        var mask = Mask;
        if (string.IsNullOrEmpty(mask) || string.IsNullOrEmpty(e.Text) || _suppressMask)
            return;

        e.Handled = true; // le masque décide de ce qui entre

        var text = InnerTextBox.Text ?? "";
        var caret = InnerTextBox.CaretIndex;

        // Saisie avec sélection : la sélection est remplacée
        if (InnerTextBox.SelectionStart != InnerTextBox.SelectionEnd)
        {
            var selStart = Math.Min(InnerTextBox.SelectionStart, InnerTextBox.SelectionEnd);
            var selEnd = Math.Max(InnerTextBox.SelectionStart, InnerTextBox.SelectionEnd);
            text = text.Remove(selStart, selEnd - selStart);
            caret = selStart;
        }

        if (HDevMaskEngine.TryInsert(mask, text, caret, e.Text, out var newText, out var newCaret))
        {
            _suppressMask = true;
            InnerTextBox.Text = newText;
            PlaceCaret(newCaret);
            _suppressMask = false;
        }
    }

    /// <summary>
    /// Place le curseur et réduit la sélection à lui (constat E0 n° 7). Poser CaretIndex ne
    /// suffit pas : quand le curseur est déjà à cette position, le TextBox ne lève pas de
    /// changement et garde la sélection, qu'un Retour arrière effacerait en entier.
    /// </summary>
    private void PlaceCaret(int index)
    {
        InnerTextBox.CaretIndex = index;
        InnerTextBox.SelectionStart = index;
        InnerTextBox.SelectionEnd = index;
    }

    /// <summary>
    /// Collage (constat E0 n° 6) : le texte du presse-papiers passe par le même chemin que
    /// la frappe, masque et filtres de l'éditeur compris. Il entre en entier ou pas du tout,
    /// comme une frappe en bloc ; en monoligne, seule sa première ligne compte.
    /// </summary>
    private async void OnInnerPasting(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (IsReadOnly || TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard)
            return;

        string? text;
        try { text = await Avalonia.Input.Platform.ClipboardExtensions.TryGetTextAsync(clipboard); }
        catch (Exception) { return; }

        if (!InnerTextBox.AcceptsReturn && text != null)
        {
            var end = text.IndexOfAny(new[] { '\r', '\n' });
            if (end >= 0) text = text[..end];
        }
        if (string.IsNullOrEmpty(text))
            return;

        InnerTextBox.RaiseEvent(new TextInputEventArgs { RoutedEvent = TextInputEvent, Text = text });
    }

    /// <summary>
    /// Suppression sous masque : troncature au placeholder précédent, pour que le
    /// texte reste TOUJOURS un préfixe valide du masque (une suppression en milieu
    /// de texte décalerait toutes les positions et corromprait la saisie suivante)
    /// </summary>
    private void OnInnerKeyDownForMask(object? sender, KeyEventArgs e)
    {
        var mask = Mask;
        if (string.IsNullOrEmpty(mask) || e.Key is not (Key.Back or Key.Delete))
            return;

        var text = InnerTextBox.Text ?? "";
        int cut;

        if (InnerTextBox.SelectionStart != InnerTextBox.SelectionEnd)
            cut = Math.Min(InnerTextBox.SelectionStart, InnerTextBox.SelectionEnd);
        else if (e.Key == Key.Back)
            cut = InnerTextBox.CaretIndex - 1;
        else
            cut = InnerTextBox.CaretIndex;

        if (cut >= text.Length && e.Key == Key.Delete && InnerTextBox.SelectionStart == InnerTextBox.SelectionEnd)
            return; // rien à droite du caret

        if (cut < 0)
            return;

        // Reculer sur les littéraux : on coupe sur un emplacement de saisie
        while (cut > 0 && cut < mask!.Length && !HDevMaskEngine.IsPlaceholder(mask[cut]))
            cut--;

        e.Handled = true;
        _suppressMask = true;
        InnerTextBox.Text = text[..Math.Min(cut, text.Length)];
        PlaceCaret(InnerTextBox.Text?.Length ?? 0);
        _suppressMask = false;
    }

    /// <summary>Vrai si le texte remplit tous les emplacements requis du masque</summary>
    public bool IsMaskComplete
        => string.IsNullOrEmpty(Mask) || HDevMaskEngine.IsComplete(Mask!, Text);

    // ═══════════════════════════════════════════════════════════════
    // VALIDATION
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Force la validation ; retourne false si annulée (erreur affichée)</summary>
    public bool Validate()
    {
        // Erreur intrinsèque de l'éditeur typé (date incomplète, hors bornes...)
        var intrinsic = GetIntrinsicError();
        if (intrinsic != null)
        {
            SetError(intrinsic);
            return false;
        }

        var args = new HDevValidatingEventArgs { NewValue = ConvertTextToValue() };
        Validating?.Invoke(this, args);

        SetError(args.Cancel ? (args.ErrorText ?? "Valeur invalide") : null);
        return !args.Cancel;
    }

    /// <summary>Erreur propre à l'éditeur typé, vérifiée avant Validating (null = OK)</summary>
    protected virtual string? GetIntrinsicError() => null;

    /// <summary>Pose ou efface l'erreur de saisie (bordure rouge + infobulle) ; l'erreur de liaison reste</summary>
    public void SetError(string? errorText)
    {
        _inputError = errorText;
        RefreshError();
    }

    /// <summary>Erreur venue de la liaison de la valeur (validation de données Avalonia)</summary>
    protected void SetBindingError(string? errorText)
    {
        _bindingError = errorText;
        RefreshError();
    }

    private void RefreshError()
    {
        ToolTip.SetTip(this, ErrorText);
        UpdateVisualState();
    }

    // ═══════════════════════════════════════════════════════════════
    // ÉTATS VISUELS (mêmes règles que HDevTextBox)
    // ═══════════════════════════════════════════════════════════════

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        _isHovered = true;
        UpdateVisualState();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _isHovered = false;
        UpdateVisualState();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.Handled)
            InnerTextBox.Focus();
    }

    protected void UpdateVisualState()
        => HDevInputChrome.ApplyState(this, InnerTextBox,
            IsEnabled, HasError, _isFocusedState, _isHovered);

    /// <summary>Fige survol et focus pour la planche visuelle headless (tests/Gallery)</summary>
    internal void ForceVisualState(bool hovered = false, bool focused = false)
    {
        _isHovered = hovered;
        _isFocusedState = focused;
        UpdateVisualState();
    }

    public void FocusEditor() => InnerTextBox.Focus();
    public void SelectAll() => InnerTextBox.SelectAll();
}

// ═══════════════════════════════════════════════════════════════════════════
// COUCHE TYPÉE DU SOCLE (D2)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Couche typée du socle : la valeur <typeparamref name="T"/>, liable dans les deux sens,
/// l'événement commun avec l'origine du changement, l'erreur de liaison et le texte
/// resynchronisé. La StyledProperty est déclarée par l'éditeur concret, avec son
/// accesseur : Avalonia déconseille un propriétaire générique (analyseur AVP1002), et le
/// compilateur XAML lit la propriété sur la classe qui déclare <c>Value</c>.
/// <code>
/// public static readonly StyledProperty&lt;DateTime?&gt; ValueProperty =
///     AvaloniaProperty.Register&lt;HDevDateEdit, DateTime?&gt;(nameof(Value),
///         defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);
/// protected override StyledProperty&lt;DateTime?&gt; ValuePropertyCore => ValueProperty;
/// public override DateTime? Value { get => GetValue(ValueProperty); set => SetValueFromCode(value); }
/// </code>
/// </summary>
public abstract class HDevEditorBase<T> : HDevEditorBase, IHDevEditor
{
    private bool _userCommit;
    private bool _userCommitFromText;

    /// <summary>La StyledProperty <c>Value</c> de l'éditeur concret</summary>
    protected abstract StyledProperty<T> ValuePropertyCore { get; }

    /// <summary>Valeur typée ; l'éditeur concret la déclare avec <see cref="SetValueFromCode"/></summary>
    public abstract T Value { get; set; }

    /// <summary>Texte affiché pour une valeur (culture courante)</summary>
    protected abstract string FormatValue(T value);

    /// <summary>Vrai pendant que le socle réécrit le texte depuis la valeur</summary>
    protected bool IsUpdatingText { get; private set; }

    public object? EditValue
    {
        get => GetValue(ValuePropertyCore);
        set => Value = value is null ? default! : (T)value;
    }

    public event EventHandler<HDevValueChangedEventArgs>? ValueChanged
    {
        add => AddHandler(HDevEditor.ValueChangedEvent, value);
        remove => RemoveHandler(HDevEditor.ValueChangedEvent, value);
    }

    public event EventHandler<HDevValueChangingEventArgs>? ValueChanging
    {
        add => AddHandler(HDevEditor.ValueChangingEvent, value);
        remove => RemoveHandler(HDevEditor.ValueChangingEvent, value);
    }

    /// <summary>
    /// Setter des <c>Value</c> concrètes : reposer la valeur courante réaffiche son texte et
    /// efface l'erreur de saisie (constat E0 n° 5), sans lever d'événement
    /// </summary>
    protected void SetValueFromCode(T value)
    {
        if (EqualityComparer<T>.Default.Equals(value, GetValue(ValuePropertyCore)))
            ResyncText();
        else
            SetValue(ValuePropertyCore, value);
    }

    /// <summary>
    /// Pose une valeur venue de l'utilisateur : ValueChanging (annulable), puis SetCurrentValue,
    /// qui garde les liaisons en place et met la source à jour. Faux si un abonné a annulé :
    /// le texte revient alors à la valeur courante.
    /// </summary>
    /// <param name="fromText">Vrai si la valeur vient du texte tapé, qui reste alors tel quel</param>
    protected bool CommitUserValue(T value, bool fromText = false)
    {
        var current = GetValue(ValuePropertyCore);
        if (EqualityComparer<T>.Default.Equals(value, current))
            return true;

        var changing = new HDevValueChangingEventArgs(current, value);
        RaiseEvent(changing);
        if (changing.Cancel)
        {
            ResyncText();
            return false;
        }

        _userCommit = true;
        _userCommitFromText = fromText;
        try { SetCurrentValue(ValuePropertyCore, value); }
        finally { _userCommit = false; _userCommitFromText = false; }
        return true;
    }

    /// <summary>Réaffiche la valeur courante et efface l'erreur de saisie</summary>
    protected void ResyncText()
    {
        IsUpdatingText = true;
        try { Text = FormatValue(GetValue(ValuePropertyCore)); }
        finally { IsUpdatingText = false; }
        SetError(null);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != ValuePropertyCore)
            return;

        // Le texte suit la valeur, sauf quand l'utilisateur vient de la taper
        if (_userCommit && _userCommitFromText)
            SetError(null);
        else
            ResyncText();

        RaiseEvent(new HDevValueChangedEventArgs(change.OldValue, change.NewValue,
            _userCommit ? HDevValueOrigin.User : HDevValueOrigin.Code));
    }

    protected override void UpdateDataValidation(AvaloniaProperty property, BindingValueType state, Exception? error)
    {
        base.UpdateDataValidation(property, state, error);
        if (property == ValuePropertyCore)
            SetBindingError(error is AggregateException many
                ? string.Join(Environment.NewLine, many.InnerExceptions.Select(x => x.Message))
                : error?.Message);
    }
}
