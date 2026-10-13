using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Data;
using Avalonia.Media;
using HDev.UI.Controls.Theme;
using System.ComponentModel;
using System.Collections.Specialized;
using Avalonia.Collections;

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
/// Arguments de l'événement Validating (annulable)
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
/// Socle commun des éditeurs de saisie :
/// TextBox interne stylé Yaru, masque de saisie, validation au blur,
/// zone de boutons à droite fournie par l'éditeur dérivé
/// </summary>
public abstract class HDevEditorBase : Border
{
    protected readonly TextBox InnerTextBox;
    private readonly Grid _layoutGrid;

    /// <summary>Zone de droite : éléments propres à l'éditeur (calendrier, spin…), puis boutons</summary>
    private readonly StackPanel _rightZone;

    /// <summary>Les boutons d'éditeur (<see cref="Buttons"/>), au bout de la zone de droite</summary>
    private readonly StackPanel _buttonZone;

    private HDevEditorDropDown? _dropDown;
    private bool _isHovered;
    private bool _isFocusedState;
    private bool _suppressMask;

    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<HDevEditorBase, string?>(nameof(Placeholder));

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<HDevEditorBase, bool>(nameof(IsReadOnly));

    public static readonly StyledProperty<string?> MaskProperty =
        AvaloniaProperty.Register<HDevEditorBase, string?>(nameof(Mask));

    public static readonly StyledProperty<bool> IsMultilineProperty =
        AvaloniaProperty.Register<HDevEditorBase, bool>(nameof(IsMultiline));

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
    /// Multiligne : Entrée insère un saut de ligne, le texte s'enroule et défile. La hauteur
    /// n'est fixée qu'en monoligne (<c>Size.EditorHeight</c>) ; en multiligne, le champ part de
    /// trois lignes et suit la place que lui donne la mise en page.
    /// </summary>
    public bool IsMultiline
    {
        get => GetValue(IsMultilineProperty);
        set => SetValue(IsMultilineProperty, value);
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

    /// <summary>Validation annulable, déclenchée à la perte de focus</summary>
    public event EventHandler<HDevValidatingEventArgs>? Validating;

    /// <summary>Déclenché quand le texte change (après le changement, comme celui d'Avalonia)</summary>
    public event EventHandler<TextChangedEventArgs>? TextChanged;

    /// <summary>
    /// Texte affiché. Un éditeur dont la valeur est le texte (D3) le surcharge pour le porter
    /// par sa StyledProperty liable (<see cref="HDevButtonEdit.TextProperty"/>) : il n'existe
    /// qu'une propriété <c>Text</c> par éditeur. Le socle lit et écrit, lui, le champ interne.
    /// </summary>
    public virtual string Text
    {
        get => InnerText;
        set => InnerText = value;
    }

    /// <summary>Le texte du champ interne, tel qu'il est affiché et saisi</summary>
    private protected string InnerText
    {
        get => InnerTextBox.Text ?? "";
        set => InnerTextBox.Text = value;
    }

    private readonly Queue<bool> _textOrigins = new();

    /// <summary>
    /// Vrai pendant que le socle réécrit le texte depuis la valeur (<see cref="SetTextFromValue"/>),
    /// et pendant le <see cref="OnInnerTextChanged"/> et le <see cref="TextChanged"/> qui en
    /// découlent, même levés plus tard : ce texte ne vient pas de l'utilisateur.
    /// </summary>
    protected bool IsUpdatingText { get; private set; }

    /// <summary>Écrit le texte affiché pour la valeur, sans qu'il passe pour une saisie</summary>
    private protected void SetTextFromValue(string text)
    {
        var outer = IsUpdatingText;
        IsUpdatingText = true;
        try { InnerText = text; }
        finally { IsUpdatingText = outer; }
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

        // Avalonia lève TextChanged au tour suivant du répartiteur (TextChanging, lui, est
        // synchrone) : un drapeau posé autour d'une écriture du texte est déjà retombé quand
        // TextChanged arrive. L'origine de chaque changement est donc relevée à TextChanging,
        // puis rendue à son TextChanged, dans le même ordre (APP-09, constat E0 n° 3).
        InnerTextBox.TextChanging += (s, e) => _textOrigins.Enqueue(IsUpdatingText);
        InnerTextBox.TextChanged += (s, e) =>
        {
            var outer = IsUpdatingText;
            IsUpdatingText = _textOrigins.Count > 0 && _textOrigins.Dequeue();
            try
            {
                OnInnerTextChanged();
                TextChanged?.Invoke(this, e);
            }
            finally { IsUpdatingText = outer; }
        };

        InnerTextBox.GotFocus += (s, e) => { _isFocusedState = true; UpdateVisualState(); };
        InnerTextBox.LostFocus += (s, e) =>
        {
            _isFocusedState = false;
            // Liste de suggestions : le focus qui quitte le champ, et pas vers le popup, la
            // ferme comme un clic extérieur (APP-10)
            if (CloseDropDownOnFocusLoss && _dropDown is { IsOpen: true } dropDown
                && !dropDown.Contains(e.NewFocusedElement))
                dropDown.Close(HDevDropDownCloseReason.LightDismiss);
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
        _rightZone = new StackPanel { Orientation = Orientation.Horizontal };
        _buttonZone = new StackPanel { Orientation = Orientation.Horizontal };
        _rightZone.Children.Add(_buttonZone);
        Grid.SetColumn(_rightZone, 1);
        _layoutGrid.Children.Add(_rightZone);
        Buttons.CollectionChanged += OnButtonsChanged;

        // Popup du socle : F4 et Alt+↓ ouvrent (bascule), Alt+↑ et Échap ferment
        InnerTextBox.AddHandler(KeyDownEvent, OnInnerKeyDownForDropDown, RoutingStrategies.Tunnel);

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
        IsMultilineProperty.Changed.AddClassHandler<HDevEditorBase>((x, _) => x.ApplyLineMode());
        IsDropDownOpenProperty.Changed.AddClassHandler<HDevEditorBase>((x, _) => x.OnIsDropDownOpenChanged());
        DropDownContentProperty.Changed.AddClassHandler<HDevEditorBase>((x, _) => x.DropDown.Content = x.DropDownContent);

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
        _dropDown?.Close(HDevDropDownCloseReason.Canceled, force: true);
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
        _dropDown?.RefreshTheme();
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

    /// <summary>Monoligne (hauteur fixe, texte centré) ou multiligne (hauteur libre, texte en haut)</summary>
    private void ApplyLineMode()
    {
        var multiline = IsMultiline;
        InnerTextBox.AcceptsReturn = multiline;
        InnerTextBox.TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap;
        InnerTextBox.VerticalAlignment = multiline ? VerticalAlignment.Stretch : VerticalAlignment.Center;
        InnerTextBox.VerticalContentAlignment = multiline ? VerticalAlignment.Top : VerticalAlignment.Center;
        InnerTextBox.Padding = new Thickness(HDevTheme.Size.EditorTextInset, multiline ? 5 : 0);
        MinHeight = multiline ? HDevTheme.Size.EditorHeight * 3 : HDevTheme.Size.EditorHeight;
        MaxHeight = multiline ? double.PositiveInfinity : HDevTheme.Size.EditorHeight;

        // Les éléments de droite restent sur la première ligne, à la hauteur d'un champ
        _rightZone.VerticalAlignment = multiline ? VerticalAlignment.Top : VerticalAlignment.Stretch;
        _rightZone.Height = multiline ? HDevTheme.Size.EditorHeight - 2 : double.NaN;
    }

    /// <summary>Ajoute un élément à la zone de droite du texte (calendrier, spin...), de gauche à droite</summary>
    protected void SetRightElement(Control element) => _rightZone.Children.Insert(_rightZone.Children.Count - 1, element);

    // ═══════════════════════════════════════════════════════════════
    // POPUP DU SOCLE
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Le popup du socle, créé à la première demande et rattaché à l'éditeur</summary>
    internal HDevEditorDropDown DropDown
    {
        get
        {
            if (_dropDown == null)
            {
                _dropDown = new HDevEditorDropDown(this);
                WireDropDown(_dropDown);
                AttachPopup(_dropDown.Popup);
            }
            return _dropDown;
        }
    }

    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        AvaloniaProperty.Register<HDevEditorBase, bool>(nameof(IsDropDownOpen),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<Control?> DropDownContentProperty =
        AvaloniaProperty.Register<HDevEditorBase, Control?>(nameof(DropDownContent));

    /// <summary>Popup ouvert ; liable dans les deux sens (F4, Alt+↓, bouton, code)</summary>
    public bool IsDropDownOpen
    {
        get => GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>
    /// Contenu libre du popup, sur n'importe quel éditeur : il s'ouvre sous le champ (au-dessus
    /// faute de place), se ferme au clic extérieur, à Échap, Alt+↑ ou F4
    /// </summary>
    public Control? DropDownContent
    {
        get => GetValue(DropDownContentProperty);
        set => SetValue(DropDownContentProperty, value);
    }

    /// <summary>Avant l'ouverture du popup ; <c>Cancel</c> l'empêche</summary>
    public event EventHandler<CancelEventArgs>? DropDownOpening;

    public event EventHandler? DropDownOpened;

    /// <summary>Avant la fermeture du popup ; <c>Cancel</c> le garde ouvert, <c>IsValueAccepted</c> dit si le choix vaut</summary>
    public event EventHandler<HDevDropDownClosingEventArgs>? DropDownClosing;

    public event EventHandler<HDevDropDownClosedEventArgs>? DropDownClosed;

    /// <summary>Vrai si l'éditeur a un popup (sans le créer)</summary>
    internal bool HasDropDown => _dropDown != null;

    private void OnInnerKeyDownForDropDown(object? sender, KeyEventArgs e)
    {
        if (!e.Handled && ActiveDropDown?.HandleKey(e) == true)
            e.Handled = true;
    }

    /// <summary>
    /// Le popup que le clavier pilote : celui qui existe, ou celui qu'ouvrirait un bouton
    /// déroulant. Sans l'un ni l'autre, F4 et Alt+↓ restent au champ.
    /// </summary>
    private HDevEditorDropDown? ActiveDropDown
        => _dropDown ?? (Buttons.Any(b => b.Kind == HDevEditorButtonKind.DropDown) ? DropDown : null);

    /// <summary>L'éditeur a un popup dès sa construction (calendrier de DateEdit…)</summary>
    internal void UseDropDown() => _ = DropDown;

    /// <summary>
    /// Vrai : le popup se ferme (<see cref="HDevDropDownCloseReason.LightDismiss"/>) quand le
    /// focus quitte le champ ailleurs que vers lui. Pour les listes de suggestions, qui suivent
    /// la frappe : elles n'ont plus de sens hors du champ.
    /// </summary>
    private protected bool CloseDropDownOnFocusLoss { get; set; }

    private bool _syncingDropDown;
    private bool _closingIsValueAccepted;

    /// <summary>Branche la pièce popup sur les propriétés et événements publics du socle</summary>
    private void WireDropDown(HDevEditorDropDown dropDown)
    {
        dropDown.Opening = () =>
        {
            var args = new CancelEventArgs();
            DropDownOpening?.Invoke(this, args);
            return !args.Cancel;
        };
        dropDown.Opened = () =>
        {
            SyncIsDropDownOpen(true);
            DropDownOpened?.Invoke(this, EventArgs.Empty);
        };
        dropDown.Closing = reason =>
        {
            var args = new HDevDropDownClosingEventArgs(reason);
            DropDownClosing?.Invoke(this, args);
            _closingIsValueAccepted = args.IsValueAccepted;
            return !args.Cancel;
        };
        dropDown.Closed = reason =>
        {
            SyncIsDropDownOpen(false);
            DropDownClosed?.Invoke(this, new HDevDropDownClosedEventArgs(reason, _closingIsValueAccepted));
        };
    }

    private void SyncIsDropDownOpen(bool open)
    {
        _syncingDropDown = true;
        try { SetCurrentValue(IsDropDownOpenProperty, open); }
        finally { _syncingDropDown = false; }
    }

    private void OnIsDropDownOpenChanged()
    {
        if (_syncingDropDown) return;
        var wanted = IsDropDownOpen;
        // Ouverture annulée (DropDownOpening, lecture seule) ou fermeture refusée (DropDownClosing) :
        // la propriété revient à l'état réel du popup
        var done = wanted ? DropDown.Open() : _dropDown?.Close(HDevDropDownCloseReason.Canceled) ?? true;
        if (!done || (_dropDown?.IsOpen ?? false) != wanted)
            SyncIsDropDownOpen(_dropDown?.IsOpen ?? false);
    }

    // ═══════════════════════════════════════════════════════════════
    // BOUTONS D'ÉDITEUR
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Boutons d'éditeur, à droite du champ, après les éléments propres à l'éditeur.
    /// Un bouton <see cref="HDevEditorButtonKind.DropDown"/> bascule le popup ;
    /// <see cref="HDevEditorButtonKind.Clear"/> vide la valeur, sauf si <see cref="HDevEditorButton.Click"/>
    /// est marqué traité ; les autres types ne font que lever <see cref="HDevEditorButton.Click"/>,
    /// routé : l'éditeur ou le formulaire l'écoute par <c>AddHandler(HDevEditorButton.ClickEvent, …)</c>.
    /// </summary>
    public AvaloniaList<HDevEditorButton> Buttons { get; } = new();

    private void OnButtonsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (HDevEditorButton b in e.OldItems) { b.Owner = null; _buttonZone.Children.Remove(b); }
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var b in _buttonZone.Children.OfType<HDevEditorButton>()) b.Owner = null;
            _buttonZone.Children.Clear();
        }
        // Ordre de la collection, toujours
        foreach (var b in Buttons)
        {
            b.Owner = this;
            _buttonZone.Children.Remove(b);
        }
        _buttonZone.Children.AddRange(Buttons);
    }

    /// <summary>Après le Click routé d'un bouton : la commande, puis l'action par défaut si Click n'est pas traité</summary>
    internal void OnEditorButtonClick(HDevEditorButton button, bool handled)
    {
        if (button.Command is { } command && command.CanExecute(button.CommandParameter))
            command.Execute(button.CommandParameter);

        if (handled) return;
        switch (button.Kind)
        {
            case HDevEditorButtonKind.DropDown:
                // Le bouton n'est pas focusable : sans le focus, Échap n'atteindrait pas le popup
                FocusEditor();
                DropDown.Toggle();
                break;
            case HDevEditorButtonKind.Clear when !IsReadOnly && IsEffectivelyEnabled:
                ClearByUser();
                FocusEditor();
                break;
        }
    }

    /// <summary>Le bouton Effacer : vider, comme si l'utilisateur effaçait tout le texte</summary>
    private protected virtual void ClearByUser() => InnerText = "";

    // ═══════════════════════════════════════════════════════════════
    // ZONE DE VALEUR
    // ═══════════════════════════════════════════════════════════════

    private Control? _valuePresenter;

    private protected bool HasValuePresenter => _valuePresenter != null;

    /// <summary>
    /// Remplace le champ texte par un présentateur fourni par l'éditeur (jetons, texte non
    /// éditable). L'éditeur prend alors lui-même le focus et le clavier du popup ; un clic sur
    /// la zone de valeur bascule le popup s'il y en a un. Null : retour au champ texte.
    /// </summary>
    protected void SetValuePresenter(Control? presenter)
    {
        if (_valuePresenter != null)
            _layoutGrid.Children.Remove(_valuePresenter);

        _valuePresenter = presenter;
        InnerTextBox.IsVisible = presenter == null;
        Focusable = presenter != null;
        if (presenter != null)
        {
            Grid.SetColumn(presenter, 0);
            _layoutGrid.Children.Insert(0, presenter);
        }
    }

    /// <summary>
    /// Zone de valeur composée : le champ texte du socle est déplacé, en dernier, dans un panneau
    /// fourni par l'éditeur (les jetons de TokenEdit, puis la saisie), qui prend sa place. Le
    /// champ garde tout ce que le socle lui attache : focus, clavier du popup, masque, validation.
    /// </summary>
    private protected void HostTextBoxIn(Panel panel)
    {
        _layoutGrid.Children.Remove(InnerTextBox);
        panel.Children.Add(InnerTextBox);
        Grid.SetColumn(panel, 0);
        _layoutGrid.Children.Insert(0, panel);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_valuePresenter != null && !e.Handled && ActiveDropDown?.HandleKey(e) == true)
            e.Handled = true;
        base.OnKeyDown(e);
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        if (_valuePresenter != null && ReferenceEquals(e.Source, this)) { _isFocusedState = true; UpdateVisualState(); }
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        if (_valuePresenter != null && ReferenceEquals(e.Source, this)) { _isFocusedState = false; UpdateVisualState(); }
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
    protected virtual object? ConvertTextToValue() => InnerText;

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
        => string.IsNullOrEmpty(Mask) || HDevMaskEngine.IsComplete(Mask!, InnerText);

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

    private HDevErrorTip _errorTip;

    private void RefreshError()
    {
        _errorTip.Show(this, ErrorText);
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
        if (e.Handled) return;
        FocusEditor();
        // Valeur non éditable : un clic sur la zone de valeur bascule le popup
        if (_valuePresenter != null && ActiveDropDown is { } dropDown && !_rightZone.IsPointerOver)
        {
            dropDown.Toggle();
            e.Handled = true;
        }
    }

    protected void UpdateVisualState()
        => ApplyVisualState(IsEnabled, HasError, _isFocusedState, _isHovered);

    /// <summary>
    /// Pose fond, filet et halo selon l'état ; les champs de saisie partagent celui de
    /// <see cref="HDevInputChrome"/>, une liste déroulante non éditable garde le sien
    /// </summary>
    private protected virtual void ApplyVisualState(bool isEnabled, bool hasError, bool isFocused, bool isHovered)
        => HDevInputChrome.ApplyState(this, InnerTextBox, isEnabled, hasError, isFocused, isHovered);

    /// <summary>Fige survol et focus pour la planche visuelle headless (tests/Gallery)</summary>
    internal void ForceVisualState(bool hovered = false, bool focused = false)
    {
        _isHovered = hovered;
        _isFocusedState = focused;
        UpdateVisualState();
    }

    public void FocusEditor()
    {
        if (_valuePresenter != null) Focus();
        else InnerTextBox.Focus();
    }
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
/// compilateur XAML lit la propriété sur la classe qui déclare <c>Value</c>. La propriété
/// publique de valeur porte le nom D3 (<c>Value</c>, ou <c>Text</c> pour un éditeur de texte) :
/// le socle n'en déclare pas, pour qu'il n'existe qu'un nom par valeur.
/// <code>
/// public static readonly StyledProperty&lt;DateTime?&gt; ValueProperty =
///     AvaloniaProperty.Register&lt;HDevDateEdit, DateTime?&gt;(nameof(Value),
///         defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);
/// protected override StyledProperty&lt;DateTime?&gt; ValuePropertyCore => ValueProperty;
/// public DateTime? Value { get => GetValue(ValueProperty); set => SetValueFromCode(value); }
/// </code>
/// </summary>
public abstract class HDevEditorBase<T> : HDevEditorBase, IHDevFieldEditor
{
    private bool _userCommit;
    private bool _userCommitFromText;
    private T _requested = default!;
    private bool _raisingChanging;

    /// <summary>La StyledProperty <c>Value</c> de l'éditeur concret</summary>
    protected abstract StyledProperty<T> ValuePropertyCore { get; }

    /// <summary>Texte affiché pour une valeur (culture courante)</summary>
    protected abstract string FormatValue(T value);

    public object? EditValue
    {
        get => GetValue(ValuePropertyCore);
        set => SetValueFromCode(value is null ? default! : (T)value);
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
    /// Coercition de la propriété de valeur, que chaque éditeur déclare à son enregistrement
    /// (<c>coerce: CoerceThroughChanging</c>) : la coercition propre à l'éditeur
    /// (<see cref="CoerceOwnValue"/>), puis ValueChanging, levé pour tout changement (utilisateur,
    /// code, liaison), dont la valeur substituée repasse par la coercition propre (QX-09). La
    /// valeur rendue est celle que prend la propriété, et que reçoit une source liée.
    /// </summary>
    protected static T CoerceThroughChanging(AvaloniaObject sender, T value)
    {
        var editor = (HDevEditorBase<T>)sender;
        return HDevValueChanging.Coerce(editor, editor.GetValue(editor.ValuePropertyCore), value,
            editor.CoerceOwnValue, ref editor._raisingChanging);
    }

    /// <summary>Coercition propre à l'éditeur : bornes, plage (défaut : aucune)</summary>
    protected virtual T CoerceOwnValue(T value) => value;

    /// <summary>
    /// Pose une valeur venue de l'utilisateur par SetCurrentValue, qui garde les liaisons en place
    /// et met la source à jour ; ValueChanging est levé par la coercition. Faux si la valeur prise
    /// n'est pas celle demandée : annulée, le texte revient à la valeur courante ; substituée, il
    /// affiche la valeur prise.
    /// </summary>
    /// <param name="fromText">Vrai si la valeur vient du texte tapé, qui reste alors tel quel</param>
    protected bool CommitUserValue(T value, bool fromText = false)
    {
        var current = GetValue(ValuePropertyCore);
        if (EqualityComparer<T>.Default.Equals(value, current))
            return true;

        _userCommit = true;
        _userCommitFromText = fromText;
        _requested = value;
        try { SetCurrentValue(ValuePropertyCore, value); }
        finally { _userCommit = false; _userCommitFromText = false; _requested = default!; }

        var taken = GetValue(ValuePropertyCore);
        if (EqualityComparer<T>.Default.Equals(taken, value))
            return true;
        // Annulé : aucun changement n'a réécrit le texte
        if (EqualityComparer<T>.Default.Equals(taken, current))
            ResyncText();
        return false;
    }

    /// <summary>Réaffiche la valeur courante et efface l'erreur de saisie</summary>
    protected void ResyncText()
    {
        SetTextFromValue(FormatValue(GetValue(ValuePropertyCore)));
        SetError(null);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != ValuePropertyCore)
            return;

        // Le texte suit la valeur, sauf quand l'utilisateur vient de la taper telle quelle
        // (une valeur substituée par ValueChanging, ou bornée, est réaffichée)
        if (_userCommit && _userCommitFromText && Equals(change.NewValue, _requested))
            SetError(null);
        else
            ResyncText();

        RaiseEvent(new HDevValueChangedEventArgs(change.OldValue, change.NewValue,
            _userCommit ? HDevValueOrigin.User : HDevValueOrigin.Code));
    }

    private protected override void ClearByUser()
    {
        if (!HasValuePresenter)
            base.ClearByUser();
        else
            CommitUserValue(default!);
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
