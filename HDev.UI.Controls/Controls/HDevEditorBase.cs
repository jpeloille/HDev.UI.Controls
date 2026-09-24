using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
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

    /// <summary>Erreur de validation courante (null = valide) ; bordure rouge + tooltip</summary>
    public string? ErrorText { get; private set; }

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
        MinHeight = HDevTheme.Size.ControlHeightSmall;
        MinWidth = 100;
        ClipToBounds = true;

        InnerTextBox = new TextBox
        {
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Padding = new Thickness(8, 4),
            Margin = new Thickness(0),
            MinHeight = 0,
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily(HDevTheme.Typography.FontFamily),
            FontSize = HDevTheme.Typography.FontSizeBody,
            CaretBrush = new SolidColorBrush(HDevTheme.Text.Primary),
            SelectionBrush = new SolidColorBrush(HDevTheme.WithOpacity(HDevTheme.Accent.Primary, 80)),
            SelectionForegroundBrush = new SolidColorBrush(Colors.White)
        };

        // Masque : filtrer la saisie AVANT le TextBox (tunnel)
        InnerTextBox.AddHandler(TextInputEvent, OnInnerTextInput, RoutingStrategies.Tunnel);
        InnerTextBox.AddHandler(KeyDownEvent, OnInnerKeyDownForMask, RoutingStrategies.Tunnel);

        InnerTextBox.TextChanged += (s, e) =>
        {
            OnInnerTextChanged();
            TextChanged?.Invoke(this, EventArgs.Empty);
        };

        InnerTextBox.GotFocus += (s, e) => { _isFocusedState = true; UpdateVisualState(); };
        InnerTextBox.LostFocus += (s, e) =>
        {
            _isFocusedState = false;
            DoValidate();
            UpdateVisualState();
        };

        _layoutGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
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
        InnerTextBox.SelectionBrush = new SolidColorBrush(HDevTheme.WithOpacity(HDevTheme.Accent.Primary, 80));
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
            InnerTextBox.CaretIndex = newCaret;
            _suppressMask = false;
        }
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
        InnerTextBox.CaretIndex = InnerTextBox.Text?.Length ?? 0;
        _suppressMask = false;
    }

    /// <summary>Vrai si le texte remplit tous les emplacements requis du masque</summary>
    public bool IsMaskComplete
        => string.IsNullOrEmpty(Mask) || HDevMaskEngine.IsComplete(Mask!, Text);

    // ═══════════════════════════════════════════════════════════════
    // VALIDATION
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Force la validation ; retourne false si annulée (erreur affichée)</summary>
    public bool DoValidate()
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

    /// <summary>Pose ou efface l'erreur affichée (bordure rouge + tooltip)</summary>
    public void SetError(string? errorText)
    {
        ErrorText = errorText;
        ToolTip.SetTip(this, errorText);
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
    {
        Color bgColor, borderColor;
        double borderThickness = 1;

        if (!IsEnabled)
        {
            bgColor = HDevTheme.Background.ControlDisabled;
            borderColor = HDevTheme.Border.Disabled;
            InnerTextBox.Foreground = new SolidColorBrush(HDevTheme.Text.Disabled);
            InnerTextBox.IsEnabled = false;
        }
        else if (HasError)
        {
            bgColor = HDevTheme.Background.Error;
            borderColor = HDevTheme.Accent.Error;
            borderThickness = 1.5;
            InnerTextBox.Foreground = new SolidColorBrush(HDevTheme.Text.Primary);
            InnerTextBox.IsEnabled = true;
        }
        else if (_isFocusedState)
        {
            bgColor = HDevTheme.Background.Control;
            borderColor = HDevTheme.Border.Focused;
            borderThickness = 1.5;
            InnerTextBox.Foreground = new SolidColorBrush(HDevTheme.Text.Primary);
            InnerTextBox.IsEnabled = true;
        }
        else if (_isHovered)
        {
            bgColor = HDevTheme.Background.Control;
            borderColor = HDevTheme.Border.Hover;
            InnerTextBox.Foreground = new SolidColorBrush(HDevTheme.Text.Primary);
            InnerTextBox.IsEnabled = true;
        }
        else
        {
            bgColor = HDevTheme.Background.Control;
            borderColor = HDevTheme.Border.Default;
            InnerTextBox.Foreground = new SolidColorBrush(HDevTheme.Text.Primary);
            InnerTextBox.IsEnabled = true;
        }

        Background = new SolidColorBrush(bgColor);
        BorderBrush = new SolidColorBrush(borderColor);
        BorderThickness = new Thickness(borderThickness);

        if (_isFocusedState && IsEnabled && !HasError)
        {
            BoxShadow = new BoxShadows(new BoxShadow
            {
                Blur = 0, Spread = 2,
                Color = HDevTheme.WithOpacity(HDevTheme.Border.Focused, 50)
            });
        }
        else if (HasError)
        {
            BoxShadow = new BoxShadows(new BoxShadow
            {
                Blur = 0, Spread = 2,
                Color = HDevTheme.WithOpacity(HDevTheme.Accent.Error, 40)
            });
        }
        else
        {
            BoxShadow = new BoxShadows();
        }
    }

    public void FocusEditor() => InnerTextBox.Focus();
    public void SelectAll() => InnerTextBox.SelectAll();
}
