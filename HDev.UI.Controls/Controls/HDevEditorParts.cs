using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using HDev.UI.Controls.Theme;
using System.Globalization;
using System.ComponentModel;
using System.Windows.Input;

namespace HDev.UI.Controls;

/// <summary>
/// Habillage commun des champs de saisie (HDevTextBox, HDevEditorBase, combo
/// éditable) : un seul jeu d'états, et neutralisation du chrome Fluent du
/// TextBox interne, qui sinon repeint son propre fond (blanc au focus, gris
/// foncé désactivé) et un soulignement bleu à l'intérieur de notre bordure
/// </summary>
internal static class HDevInputChrome
{
    private static readonly string[] TransparentKeys =
    {
        "TextControlBackground", "TextControlBackgroundPointerOver",
        "TextControlBackgroundFocused", "TextControlBackgroundDisabled",
        "TextControlBorderBrush", "TextControlBorderBrushPointerOver",
        "TextControlBorderBrushFocused", "TextControlBorderBrushDisabled"
    };

    /// <summary>
    /// Prépare un TextBox interne : aucun fond/bordure propre, pas de hauteur
    /// minimale Fluent (32), retrait texte commun. À rappeler au changement de
    /// variante (couleurs de placeholder et de texte désactivé).
    /// </summary>
    public static void Neutralize(TextBox tb)
    {
        foreach (var key in TransparentKeys)
            tb.Resources[key] = Brushes.Transparent;
        tb.Resources["TextControlBorderThemeThickness"] = new Thickness(0);
        tb.Resources["TextControlBorderThemeThicknessFocused"] = new Thickness(0);

        var placeholder = new SolidColorBrush(HDevTheme.Text.Placeholder);
        tb.Resources["TextControlPlaceholderForeground"] = placeholder;
        tb.Resources["TextControlPlaceholderForegroundPointerOver"] = placeholder;
        tb.Resources["TextControlPlaceholderForegroundFocused"] = placeholder;
        tb.Resources["TextControlPlaceholderForegroundDisabled"] =
            new SolidColorBrush(HDevTheme.Text.Disabled);
        tb.Resources["TextControlForegroundDisabled"] = new SolidColorBrush(HDevTheme.Text.Disabled);

        tb.BorderThickness = new Thickness(0);
        tb.Background = Brushes.Transparent;
        tb.MinHeight = 0;
    }

    /// <summary>
    /// Applique l'état visuel au Border hôte. Bordure toujours à 1 px : l'accent
    /// du focus/de l'erreur passe par la couleur et un halo extérieur, jamais par
    /// l'épaisseur (1 → 1,5 décalait le texte d'un demi-pixel et le floutait)
    /// </summary>
    public static void ApplyState(Avalonia.Controls.Border host, TextBox inner,
        bool isEnabled, bool hasError, bool isFocused, bool isHovered)
    {
        Color bg, border;
        Color? halo = null;

        if (!isEnabled)
        {
            bg = HDevTheme.Background.ControlDisabled;
            border = HDevTheme.Border.Disabled;
        }
        else if (hasError)
        {
            bg = HDevTheme.Background.Error;
            border = HDevTheme.Accent.Error;
            halo = ErrorHalo;
        }
        else if (isFocused)
        {
            bg = HDevTheme.Background.Input;
            border = HDevTheme.Border.Focused;
            halo = FocusHalo;
        }
        else
        {
            bg = HDevTheme.Background.Input;
            border = isHovered ? HDevTheme.Border.Hover : HDevTheme.Border.Default;
        }

        host.Background = new SolidColorBrush(bg);
        host.BorderBrush = new SolidColorBrush(border);
        host.BorderThickness = new Thickness(HDevTheme.Size.BorderThin);
        host.BoxShadow = halo is { } h
            ? new BoxShadows(new BoxShadow { Blur = 0, Spread = 2, Color = h })
            : new BoxShadows();

        inner.Foreground = new SolidColorBrush(isEnabled ? HDevTheme.Text.Primary : HDevTheme.Text.Disabled);
    }

    /// <summary>Halo de 2 px des champs au focus et en erreur (BoxShadow ou dessin)</summary>
    public static Color FocusHalo => HDevTheme.WithOpacity(HDevTheme.Border.Focused, 110);
    public static Color ErrorHalo => HDevTheme.WithOpacity(HDevTheme.Accent.Error, 90);

    /// <summary>
    /// Même halo que la BoxShadow (spread 2, sans flou) pour un champ dessiné à la main
    /// (HDevComboBox, HDevLookUpEdit) : 2 px tout autour, à l'extérieur du filet
    /// </summary>
    public static void DrawHalo(DrawingContext context, Rect bounds, double cornerRadius, Color halo)
        => context.DrawRectangle(null, new Pen(new SolidColorBrush(halo), 2),
            bounds.Inflate(1), cornerRadius + 1, cornerRadius + 1);
}

/// <summary>Icône vectorielle d'un <see cref="HDevEditorGlyphButton"/></summary>
public enum HDevEditorIcon
{
    /// <summary>Pas d'icône : le texte <see cref="HDevEditorGlyphButton.Glyph"/> est affiché</summary>
    None,
    Calendar,
    ChevronDown,
    ChevronUp,
    ChevronLeft,
    ChevronRight,

    // Glyphes des boutons d'éditeur (lot E2)

    /// <summary>Croix d'effacement</summary>
    Clear,
    /// <summary>Points de suspension (« … » : ouvrir un dialogue)</summary>
    Ellipsis,
    /// <summary>Loupe</summary>
    Search,
    Clock,
    Calculator,
    Link,
    Folder,
    /// <summary>Pastille pleine (couleur, état)</summary>
    Swatch,
    Plus,
    Minus
}

/// <summary>
/// Petit bouton à glyphe pour la zone droite des éditeurs (calendrier, effacer...)
/// </summary>
public class HDevEditorGlyphButton : Control
{
    private bool _isHovered;
    private bool _isPressed;

    public string Glyph { get; set; } = "";
    public double GlyphSize { get; set; } = 14;

    /// <summary>Icône dessinée en traits fins (prioritaire sur <see cref="Glyph"/>)</summary>
    public HDevEditorIcon Icon { get; set; }

    public event EventHandler? Click;

    static HDevEditorGlyphButton()
    {
        // Grisé quand l'éditeur hôte est désactivé
        AffectsRender<HDevEditorGlyphButton>(IsEffectivelyEnabledProperty);
    }

    public HDevEditorGlyphButton()
    {
        Width = 26;
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        _isHovered = true;
        InvalidateVisual();
        base.OnPointerEntered(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _isHovered = false;
        _isPressed = false;
        InvalidateVisual();
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _isPressed = true;
        InvalidateVisual();
        e.Handled = true;
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_isPressed && _isHovered)
            Click?.Invoke(this, EventArgs.Empty);
        _isPressed = false;
        InvalidateVisual();
        e.Handled = true;
        base.OnPointerReleased(e);
    }

    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);
        var bounds = new Rect(Bounds.Size);

        if (Icon != HDevEditorIcon.None)
        {
            RenderButton(context, bounds, _isHovered, _isPressed, IsEffectivelyEnabled, Icon);
            return;
        }

        RenderButton(context, bounds, _isHovered, _isPressed, IsEffectivelyEnabled, HDevEditorIcon.None);
        var color = IsEffectivelyEnabled ? HDevTheme.Text.Secondary : HDevTheme.Text.Disabled;

        var text = new FormattedText(Glyph, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(HDevTheme.Typography.FontFamily), GlyphSize,
            new SolidColorBrush(color));
        context.DrawText(text, Crisp.Snap(new Point(
            (bounds.Width - text.Width) / 2,
            (bounds.Height - text.Height) / 2)));
    }

    /// <summary>Fond de survol et de pression, puis glyphe : rendu commun des boutons d'éditeur</summary>
    internal static void RenderButton(DrawingContext context, Rect bounds, bool hovered, bool pressed,
        bool enabled, HDevEditorIcon icon)
    {
        // Toute la surface du bouton est cliquable, pas seulement les traits du glyphe :
        // le test de clic d'Avalonia suit ce qui est dessiné
        context.FillRectangle(Brushes.Transparent, bounds);
        if (hovered && enabled)
        {
            var bg = pressed ? HDevTheme.Background.ControlPressed : HDevTheme.Background.ControlHover;
            context.FillRectangle(new SolidColorBrush(bg), bounds.Deflate(new Thickness(2, 3)), 4);
        }
        if (icon != HDevEditorIcon.None)
            DrawIcon(context, icon, bounds.Center,
                enabled ? HDevTheme.Text.Secondary : HDevTheme.Text.Disabled);
    }

    /// <summary>
    /// Icônes au trait 1,2 px, même graisse que les chevrons de
    /// <see cref="HDevSpinButtons"/> ; partagé avec le rendu de HDevComboBox.
    /// <paramref name="scale"/> agrandit glyphe et trait ensemble (HDevIconButton Medium/Large).
    /// </summary>
    internal static void DrawIcon(DrawingContext context, HDevEditorIcon icon, Point center, Color color,
        double scale = 1.0)
    {
        var pen = new Pen(new SolidColorBrush(color), 1.2);
        // Centre calé sur le demi-pixel : traits de 1 px nets
        var c = Crisp.Snap(center);
        double cx = c.X + 0.5, cy = c.Y + 0.5;
        using var zoom = context.PushTransform(Matrix.CreateTranslation(-cx, -cy)
            * Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(cx, cy));

        switch (icon)
        {
            case HDevEditorIcon.ChevronDown:
                context.DrawLine(pen, new Point(cx - 4, cy - 2), new Point(cx, cy + 2));
                context.DrawLine(pen, new Point(cx, cy + 2), new Point(cx + 4, cy - 2));
                break;

            case HDevEditorIcon.ChevronUp:
                context.DrawLine(pen, new Point(cx - 4, cy + 2), new Point(cx, cy - 2));
                context.DrawLine(pen, new Point(cx, cy - 2), new Point(cx + 4, cy + 2));
                break;

            case HDevEditorIcon.ChevronLeft:
                context.DrawLine(pen, new Point(cx + 2, cy - 4), new Point(cx - 2, cy));
                context.DrawLine(pen, new Point(cx - 2, cy), new Point(cx + 2, cy + 4));
                break;

            case HDevEditorIcon.ChevronRight:
                context.DrawLine(pen, new Point(cx - 2, cy - 4), new Point(cx + 2, cy));
                context.DrawLine(pen, new Point(cx + 2, cy), new Point(cx - 2, cy + 4));
                break;

            case HDevEditorIcon.Clear:
                context.DrawLine(pen, new Point(cx - 3.5, cy - 3.5), new Point(cx + 3.5, cy + 3.5));
                context.DrawLine(pen, new Point(cx - 3.5, cy + 3.5), new Point(cx + 3.5, cy - 3.5));
                break;

            case HDevEditorIcon.Ellipsis:
                var dot = new SolidColorBrush(color);
                for (var i = -1; i <= 1; i++)
                    context.DrawEllipse(dot, null, new Point(cx + 4 * i, cy + 1), 1.2, 1.2);
                break;

            case HDevEditorIcon.Search:
                context.DrawEllipse(null, pen, new Point(cx - 1, cy - 1), 4, 4);
                context.DrawLine(pen, new Point(cx + 2, cy + 2), new Point(cx + 5.5, cy + 5.5));
                break;

            case HDevEditorIcon.Clock:
                context.DrawEllipse(null, pen, new Point(cx, cy), 5.5, 5.5);
                context.DrawLine(pen, new Point(cx, cy - 3.5), new Point(cx, cy));
                context.DrawLine(pen, new Point(cx, cy), new Point(cx + 2.5, cy + 1.5));
                break;

            case HDevEditorIcon.Calculator:
                context.DrawRectangle(null, pen, new Rect(cx - 5, cy - 6, 10, 12), 1.5, 1.5);
                context.DrawLine(pen, new Point(cx - 2.5, cy - 3), new Point(cx + 2.5, cy - 3));
                var key = new SolidColorBrush(color);
                foreach (var (kx, ky) in new[] { (-2.5, 0.5), (0.0, 0.5), (2.5, 0.5), (-2.5, 3.0), (0.0, 3.0), (2.5, 3.0) })
                    context.DrawEllipse(key, null, new Point(cx + kx, cy + ky), 0.8, 0.8);
                break;

            case HDevEditorIcon.Link:
                // Deux maillons en biais, qui se chevauchent
                using (context.PushTransform(Matrix.CreateTranslation(-cx, -cy)
                    * Matrix.CreateRotation(-Math.PI / 4) * Matrix.CreateTranslation(cx, cy)))
                {
                    context.DrawRectangle(null, pen, new Rect(cx - 6.5, cy - 2.25, 8, 4.5), 2.25, 2.25);
                    context.DrawRectangle(null, pen, new Rect(cx - 1.5, cy - 2.25, 8, 4.5), 2.25, 2.25);
                }
                break;

            case HDevEditorIcon.Folder:
                var folder = new StreamGeometry();
                using (var g = folder.Open())
                {
                    g.BeginFigure(new Point(cx - 6, cy + 4.5), true);
                    g.LineTo(new Point(cx - 6, cy - 4.5));
                    g.LineTo(new Point(cx - 2, cy - 4.5));
                    g.LineTo(new Point(cx - 0.5, cy - 3));
                    g.LineTo(new Point(cx + 6, cy - 3));
                    g.LineTo(new Point(cx + 6, cy + 4.5));
                    g.EndFigure(true);
                }
                context.DrawGeometry(null, pen, folder);
                break;

            case HDevEditorIcon.Swatch:
                context.DrawEllipse(new SolidColorBrush(color), null, new Point(cx, cy), 4.5, 4.5);
                break;

            case HDevEditorIcon.Plus:
                context.DrawLine(pen, new Point(cx - 4, cy), new Point(cx + 4, cy));
                context.DrawLine(pen, new Point(cx, cy - 4), new Point(cx, cy + 4));
                break;

            case HDevEditorIcon.Minus:
                context.DrawLine(pen, new Point(cx - 4, cy), new Point(cx + 4, cy));
                break;

            case HDevEditorIcon.Calendar:
                // Cadre 12×11, bandeau d'en-tête, deux anneaux
                var frame = new Rect(cx - 6, cy - 5, 12, 11);
                context.DrawRectangle(null, pen, frame, 1.5, 1.5);
                context.DrawLine(pen, new Point(frame.Left, cy - 2), new Point(frame.Right, cy - 2));
                context.DrawLine(pen, new Point(cx - 3, cy - 7), new Point(cx - 3, cy - 4));
                context.DrawLine(pen, new Point(cx + 3, cy - 7), new Point(cx + 3, cy - 4));
                break;
        }
    }
}

/// <summary>Type d'un bouton d'éditeur : son glyphe et son action par défaut</summary>
public enum HDevEditorButtonKind
{
    /// <summary>Chevron : bascule le popup de l'éditeur</summary>
    DropDown,
    /// <summary>Croix : vide la valeur, sauf si Click est marqué traité</summary>
    Clear,
    /// <summary>« … » : ouvrir un dialogue (Click)</summary>
    Ellipsis,
    /// <summary>Loupe (Click)</summary>
    Search,
    Plus,
    Minus,
    /// <summary>Glyphe donné par <see cref="HDevEditorButton.Icon"/> (Click)</summary>
    Custom
}

/// <summary>
/// Bouton d'éditeur (pièce du socle, D1) : se déclare dans <see cref="HDevEditorBase.Buttons"/>
/// de n'importe quel éditeur. Infobulle, visibilité et activation sont celles d'Avalonia
/// (<c>ToolTip.Tip</c>, <c>IsVisible</c>, <c>IsEnabled</c>), comme la commande.
/// <code>
/// &lt;h:HDevButtonEdit Text="{Binding Chemin}"&gt;
///   &lt;h:HDevButtonEdit.Buttons&gt;
///     &lt;h:HDevEditorButton Kind="Clear" ToolTip.Tip="Effacer" /&gt;
///   &lt;/h:HDevButtonEdit.Buttons&gt;
/// &lt;/h:HDevButtonEdit&gt;
/// </code>
/// Il ne prend pas le focus : le clavier reste au champ (F4, Alt+↓ pour le popup).
/// </summary>
public class HDevEditorButton : Control
{
    private bool _isHovered;
    private bool _isPressed;
    private bool _canExecute = true;

    public static readonly StyledProperty<HDevEditorButtonKind> KindProperty =
        AvaloniaProperty.Register<HDevEditorButton, HDevEditorButtonKind>(nameof(Kind), HDevEditorButtonKind.Ellipsis);

    public static readonly StyledProperty<HDevEditorIcon> IconProperty =
        AvaloniaProperty.Register<HDevEditorButton, HDevEditorIcon>(nameof(Icon));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<HDevEditorButton, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<HDevEditorButton, object?>(nameof(CommandParameter));

    public HDevEditorButtonKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    /// <summary>Glyphe d'un bouton <see cref="HDevEditorButtonKind.Custom"/></summary>
    public HDevEditorIcon Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Exécutée au clic, après Click ; son CanExecute grise le bouton</summary>
    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>L'éditeur qui porte le bouton</summary>
    internal HDevEditorBase? Owner { get; set; }

    static HDevEditorButton()
    {
        AffectsRender<HDevEditorButton>(KindProperty, IconProperty, IsEffectivelyEnabledProperty);
    }

    public HDevEditorButton()
    {
        Width = 26;
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    /// <summary>
    /// Glyphe dessiné : celui du type, ou <see cref="Icon"/> pour un bouton personnalisé.
    /// Interne : il sert au rendu ; à rendre public si une application en a besoin.
    /// </summary>
    internal HDevEditorIcon EffectiveIcon => Kind switch
    {
        HDevEditorButtonKind.DropDown => HDevEditorIcon.ChevronDown,
        HDevEditorButtonKind.Clear => HDevEditorIcon.Clear,
        HDevEditorButtonKind.Ellipsis => HDevEditorIcon.Ellipsis,
        HDevEditorButtonKind.Search => HDevEditorIcon.Search,
        HDevEditorButtonKind.Plus => HDevEditorIcon.Plus,
        HDevEditorButtonKind.Minus => HDevEditorIcon.Minus,
        _ => Icon
    };

    protected override bool IsEnabledCore => base.IsEnabledCore && _canExecute;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CommandProperty)
        {
            if (change.OldValue is ICommand old) old.CanExecuteChanged -= OnCanExecuteChanged;
            if (change.NewValue is ICommand now) now.CanExecuteChanged += OnCanExecuteChanged;
            OnCanExecuteChanged(this, EventArgs.Empty);
        }
        else if (change.Property == CommandParameterProperty)
            OnCanExecuteChanged(this, EventArgs.Empty);
    }

    private void OnCanExecuteChanged(object? sender, EventArgs e)
    {
        var can = Command?.CanExecute(CommandParameter) ?? true;
        if (can == _canExecute) return;
        _canExecute = can;
        UpdateIsEffectivelyEnabled();
    }

    /// <summary>
    /// Clic du bouton, routé et remontant (comme <c>Button.Click</c>) : l'éditeur ou le formulaire
    /// l'écoute par <c>AddHandler</c>, le bouton se lit sur <c>Source</c>. <c>Handled = true</c>
    /// empêche l'action par défaut (popup, effacement) ; la commande s'exécute dans tous les cas.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<HDevEditorButton, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    /// <summary>Simule un clic (tests, accessibilité) : Click, commande, action par défaut</summary>
    internal void PerformClick()
    {
        if (!IsEffectivelyEnabled) return;
        var args = new RoutedEventArgs(ClickEvent, this);
        RaiseEvent(args);
        Owner?.OnEditorButtonClick(this, args.Handled);
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        _isHovered = true;
        InvalidateVisual();
        base.OnPointerEntered(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _isHovered = false;
        _isPressed = false;
        InvalidateVisual();
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _isPressed = true;
        InvalidateVisual();
        e.Handled = true;
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        var click = _isPressed && _isHovered;
        _isPressed = false;
        InvalidateVisual();
        e.Handled = true;
        base.OnPointerReleased(e);
        if (click) PerformClick();
    }

    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);
        HDevEditorGlyphButton.RenderButton(context, new Rect(Bounds.Size), _isHovered, _isPressed,
            IsEffectivelyEnabled, EffectiveIcon);
    }
}

/// <summary>
/// Boutons spin (▲/▼) empilés pour les éditeurs numériques
/// </summary>
public class HDevSpinButtons : Control
{
    private bool _upHovered;
    private bool _downHovered;
    private bool _pressed;

    public event EventHandler? UpClicked;
    public event EventHandler? DownClicked;

    static HDevSpinButtons()
    {
        // Grisé quand l'éditeur hôte est désactivé
        AffectsRender<HDevSpinButtons>(IsEffectivelyEnabledProperty);
    }

    public HDevSpinButtons()
    {
        Width = 20;
    }

    private bool IsUpZone(Point p) => p.Y < Bounds.Height / 2;

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var p = e.GetPosition(this);
        var up = IsUpZone(p);
        if (up != _upHovered || up == _downHovered)
        {
            _upHovered = up;
            _downHovered = !up;
            InvalidateVisual();
        }
        base.OnPointerMoved(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _upHovered = _downHovered = _pressed = false;
        InvalidateVisual();
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _pressed = true;
        if (IsUpZone(e.GetPosition(this)))
            UpClicked?.Invoke(this, EventArgs.Empty);
        else
            DownClicked?.Invoke(this, EventArgs.Empty);
        InvalidateVisual();
        e.Handled = true;
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _pressed = false;
        InvalidateVisual();
        e.Handled = true;
        base.OnPointerReleased(e);
    }

    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);
        var bounds = new Rect(Bounds.Size);
        var half = bounds.Height / 2;
        var upRect = new Rect(2, 2, bounds.Width - 4, half - 3);
        var downRect = new Rect(2, half + 1, bounds.Width - 4, half - 3);

        if (_upHovered)
            context.FillRectangle(new SolidColorBrush(
                _pressed ? HDevTheme.Background.ControlPressed : HDevTheme.Background.ControlHover), upRect, 3);
        if (_downHovered)
            context.FillRectangle(new SolidColorBrush(
                _pressed ? HDevTheme.Background.ControlPressed : HDevTheme.Background.ControlHover), downRect, 3);

        var pen = new Pen(new SolidColorBrush(
            IsEffectivelyEnabled ? HDevTheme.Text.Secondary : HDevTheme.Text.Disabled), 1.2);
        var cx = bounds.Width / 2;

        // ▲
        var uy = upRect.Center.Y;
        context.DrawLine(pen, new Point(cx - 3.5, uy + 1.5), new Point(cx, uy - 1.5));
        context.DrawLine(pen, new Point(cx, uy - 1.5), new Point(cx + 3.5, uy + 1.5));

        // ▼
        var dy = downRect.Center.Y;
        context.DrawLine(pen, new Point(cx - 3.5, dy - 1.5), new Point(cx, dy + 1.5));
        context.DrawLine(pen, new Point(cx, dy + 1.5), new Point(cx + 3.5, dy - 1.5));
    }
}

/// <summary>Cause d'une fermeture du popup d'un éditeur</summary>
public enum HDevDropDownCloseReason
{
    /// <summary>Un choix fait dans le popup (Entrée, clic sur un jour…) : la valeur est acceptée</summary>
    Accepted,

    /// <summary>Échap, Alt+↑, F4, le bouton ou le code : la valeur d'avant reste</summary>
    Canceled,

    /// <summary>Clic extérieur ou fenêtre désactivée (garde de fermeture) : le choix est gardé</summary>
    LightDismiss
}

/// <summary>Fermeture à venir du popup d'un éditeur (<see cref="HDevEditorBase.DropDownClosing"/>)</summary>
public class HDevDropDownClosingEventArgs : CancelEventArgs
{
    public HDevDropDownClosingEventArgs(HDevDropDownCloseReason reason)
    {
        CloseReason = reason;
        IsValueAccepted = reason != HDevDropDownCloseReason.Canceled;
    }

    public HDevDropDownCloseReason CloseReason { get; }

    /// <summary>
    /// Le choix fait dans le popup vaut ; vrai par défaut pour <see cref="HDevDropDownCloseReason.Accepted"/>
    /// et <see cref="HDevDropDownCloseReason.LightDismiss"/>, faux pour <see cref="HDevDropDownCloseReason.Canceled"/>
    /// </summary>
    public bool IsValueAccepted { get; set; }
}

/// <summary>Popup d'un éditeur fermé (<see cref="HDevEditorBase.DropDownClosed"/>)</summary>
public class HDevDropDownClosedEventArgs : EventArgs
{
    public HDevDropDownClosedEventArgs(HDevDropDownCloseReason reason, bool isValueAccepted)
    {
        CloseReason = reason;
        IsValueAccepted = isValueAccepted;
    }

    public HDevDropDownCloseReason CloseReason { get; }

    /// <summary>Le choix fait dans le popup a valu (tel que laissé par DropDownClosing)</summary>
    public bool IsValueAccepted { get; }
}

/// <summary>
/// Popup du socle des éditeurs (D1, forme B) : une pièce que tout éditeur active sans changer
/// de classe. Elle porte l'habillage (fond, filet, ombre, thème), le placement retourné avec son
/// écart (<see cref="HDevPopupPlacement"/>), la garde de fermeture commune et le clavier :
/// F4 et Alt+↓ basculent, Alt+↑ et Échap ferment, depuis le champ comme depuis le contenu.
/// </summary>
internal sealed class HDevEditorDropDown
{
    private readonly HDevEditorBase _owner;
    private readonly Border _chrome;
    private readonly HDevPopupDismissGuard _guard;

    public HDevEditorDropDown(HDevEditorBase owner)
    {
        _owner = owner;
        _chrome = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(HDevTheme.Size.CornerRadiusMedium),
            Padding = new Thickness(4)
        };
        RefreshTheme();

        Popup = new Avalonia.Controls.Primitives.Popup
        {
            Child = _chrome,
            PlacementTarget = owner,
            // Garde maison : le light dismiss ferme au moindre rebond d'activation (GNOME/XWayland)
            IsLightDismissEnabled = false
        };
        HDevPopupPlacement.Apply(Popup, HDevPopupPlacement.Gap);

        _guard = new HDevPopupDismissGuard(() => IsOpen, () => Close(HDevDropDownCloseReason.LightDismiss));
        Popup.Opened += (_, _) => _guard.Install(owner, Popup);
        Popup.Closed += (_, _) =>
        {
            _guard.Remove();
            // Fermé par Avalonia (éditeur détaché…) sans passer par Close
            if (IsOpen) { IsOpen = false; Closed?.Invoke(HDevDropDownCloseReason.Canceled); }
        };

        // Le contenu vit en overlay, hors de l'arbre visuel de l'éditeur : ses touches n'atteignent
        // pas le champ, d'où ce second point d'écoute, après le contenu (qui peut garder Échap)
        _chrome.AddHandler(InputElement.KeyDownEvent, (_, e) =>
        {
            // Entrée valide le choix, même quand le contenu l'a prise (une liste sélectionne
            // l'élément courant) ; seul un texte multiligne la garde pour ses sauts de ligne
            if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None && IsOpen
                && e.Source is not TextBox { AcceptsReturn: true })
            {
                e.Handled = Close(HDevDropDownCloseReason.Accepted) || e.Handled;
                return;
            }
            if (!e.Handled && HandleKey(e)) e.Handled = true;
        }, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    public Avalonia.Controls.Primitives.Popup Popup { get; }

    /// <summary>Habillage du popup (fond, filet, coins, ombre), qui porte le contenu</summary>
    public Border Chrome => _chrome;

    public Control? Content
    {
        get => _chrome.Child;
        set => _chrome.Child = value;
    }

    public bool IsOpen { get; private set; }

    /// <summary>Largeur minimale du popup = largeur de l'éditeur</summary>
    public bool MatchOwnerWidth { get; set; }

    /// <summary>Avant l'ouverture ; faux = l'ouverture est annulée</summary>
    public Func<bool>? Opening { get; set; }

    public Action? Opened { get; set; }

    /// <summary>Avant la fermeture ; faux = le popup reste ouvert (sauf fermeture forcée)</summary>
    public Func<HDevDropDownCloseReason, bool>? Closing { get; set; }

    public Action<HDevDropDownCloseReason>? Closed { get; set; }

    /// <summary>Vrai si la garde vient de fermer le popup sur ce même clic (anti-réouverture)</summary>
    public bool JustClosedByGuard => _guard.JustClosed;

    public bool Open()
    {
        if (IsOpen) return true;
        if (!_owner.IsEffectivelyEnabled || _owner.IsReadOnly) return false;
        if (Opening?.Invoke() == false) return false;

        _chrome.MinWidth = MatchOwnerWidth ? _owner.Bounds.Width : 0;
        IsOpen = true;
        Popup.IsOpen = true;
        Opened?.Invoke();
        return true;
    }

    /// <param name="force">Vrai : la fermeture ne s'annule pas (éditeur détaché)</param>
    public bool Close(HDevDropDownCloseReason reason, bool force = false)
    {
        if (!IsOpen) return true;
        if (Closing?.Invoke(reason) == false && !force) return false;

        // Le focus était dans le popup : il revient au champ
        var focusInside = TopLevel.GetTopLevel(_owner)?.FocusManager?.GetFocusedElement() is Visual f
            && (ReferenceEquals(f, _chrome) || _chrome.IsVisualAncestorOf(f));

        IsOpen = false;
        Popup.IsOpen = false;
        if (focusInside && reason != HDevDropDownCloseReason.LightDismiss)
            _owner.FocusEditor();
        Closed?.Invoke(reason);
        return true;
    }

    /// <summary>Bouton ou F4 : ouvre ou ferme ; ne rouvre pas sur le clic qui vient de fermer</summary>
    public void Toggle()
    {
        if (IsOpen) Close(HDevDropDownCloseReason.Canceled);
        else if (!_guard.JustClosed) Open();
    }

    /// <summary>Clavier du popup ; vrai si la touche est traitée</summary>
    public bool HandleKey(KeyEventArgs e)
    {
        var alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        switch (e.Key)
        {
            case Key.F4 when e.KeyModifiers == KeyModifiers.None:
            case Key.Down when alt:
                if (IsOpen) Close(HDevDropDownCloseReason.Canceled); else Open();
                return true;
            case Key.Up when alt && IsOpen:
            case Key.Escape when IsOpen:
                Close(HDevDropDownCloseReason.Canceled);
                return true;
            default:
                return false;
        }
    }

    public void RefreshTheme()
    {
        _chrome.Background = new SolidColorBrush(HDevTheme.Background.Panel);
        _chrome.BorderBrush = new SolidColorBrush(HDevTheme.Border.Default);
        _chrome.BoxShadow = new BoxShadows(new BoxShadow
        {
            OffsetX = 0, OffsetY = 2, Blur = 8,
            Color = HDevTheme.Shadow.Color
        });
    }
}

/// <summary>
/// Écart vertical d'un popup d'éditeur à son champ, reflété quand le positionneur
/// d'Avalonia le retourne au-dessus faute de place (constat E0 n° 4).
/// <list type="bullet">
/// <item>Le positionneur retourne l'ancrage et la gravité, pas <c>VerticalOffset</c> : un
/// jour de 2 px dessous devenait 2 px de chevauchement dessus.</item>
/// <item>Une marge du contenu ne sert pas : Avalonia 12 la compense (place d'une ombre).</item>
/// </list>
/// L'écart est donc porté par le rectangle d'ancrage, élargi (jour) ou réduit (bordure
/// fusionnée) en hauteur : le positionneur le retourne avec le popup, et l'écart est le
/// même dessous et dessus. Le glissement horizontal et le retournement restent les siens.
/// </summary>
internal static class HDevPopupPlacement
{
    /// <summary>Jour de 2 px entre le champ et le popup</summary>
    public const double Gap = 2;

    /// <summary>Bordure du popup fusionnée avec celle du champ (chevauchement de 1 px)</summary>
    public const double FusedBorder = -1;

    /// <summary>Popup sous le champ, aligné à gauche, retourné au-dessus faute de place</summary>
    public static void Apply(Avalonia.Controls.Primitives.Popup popup, double gap)
    {
        popup.VerticalOffset = 0;
        popup.Placement = PlacementMode.Custom;
        popup.CustomPopupPlacementCallback = placement =>
        {
            placement.AnchorRectangle = placement.AnchorRectangle.Inflate(new Thickness(0, gap));
            placement.Anchor = Avalonia.Controls.Primitives.PopupPositioning.PopupAnchor.BottomLeft;
            placement.Gravity = Avalonia.Controls.Primitives.PopupPositioning.PopupGravity.BottomRight;
        };
    }
}

/// <summary>
/// La garde de fermeture des popups, unique pour toute la suite (menus, combo, popups des
/// éditeurs) : clic dans la fenêtre hors du popup ; désactivation de la fenêtre seulement pour
/// un popup natif, avec délai de grâce de 250 ms (un popup overlay y survit).
/// Remplace le light dismiss Avalonia, instable sous GNOME/XWayland.
/// </summary>
internal sealed class HDevPopupDismissGuard
{
    private readonly Func<bool> _isOpen;
    private readonly Action _close;
    private TopLevel? _root;
    private Avalonia.Controls.Primitives.Popup? _popup;

    /// <summary>
    /// Le popup est-il dessiné dans la fenêtre (overlay) ? Point de substitution pour le banc
    /// headless, qui n'a que des popups overlay et doit aussi couvrir le chemin natif.
    /// </summary>
    internal static Func<Avalonia.Controls.Primitives.Popup, bool> IsDrawnInWindow { get; set; } = IsOverlayHosted;

    /// <summary>
    /// Vrai si le contenu du popup vit dans la fenêtre (hôte overlay : OverlayPopups, popup
    /// overlay) ; faux pour un popup natif, fenêtre séparée (sa racine est un PopupRoot)
    /// </summary>
    internal static bool IsOverlayHosted(Avalonia.Controls.Primitives.Popup popup)
        => popup.Child is Visual child
           && TopLevel.GetTopLevel(child) is not Avalonia.Controls.Primitives.PopupRoot
           && child.GetVisualAncestors().OfType<Avalonia.Controls.Primitives.OverlayPopupHost>().Any();

    /// <summary>Instant de la dernière fermeture par clic extérieur (anti-réouverture)</summary>
    public DateTime LastOutsideClose { get; private set; }

    public HDevPopupDismissGuard(Func<bool> isOpen, Action close)
    {
        _isOpen = isOpen;
        _close = close;
    }

    /// <param name="anchor">Le contrôle qui porte le popup (sa fenêtre est surveillée)</param>
    /// <param name="popup">Le popup gardé : selon qu'il est dessiné dans la fenêtre ou natif, la
    /// désactivation de la fenêtre le ferme ou non</param>
    public void Install(Visual anchor, Avalonia.Controls.Primitives.Popup popup)
    {
        _popup = popup;
        _root = TopLevel.GetTopLevel(anchor);
        if (_root == null) return;

        _root.AddHandler(InputElement.PointerPressedEvent, OnRootPointerPressed,
            RoutingStrategies.Tunnel, handledEventsToo: true);
        if (_root is Window w)
            w.Deactivated += OnRootDeactivated;
    }

    public void Remove()
    {
        if (_root == null) return;
        _root.RemoveHandler(InputElement.PointerPressedEvent, OnRootPointerPressed);
        if (_root is Window w)
            w.Deactivated -= OnRootDeactivated;
        _root = null;
        _popup = null;
    }

    /// <summary>Vrai si une fermeture par le garde date de moins de 250 ms (même clic)</summary>
    public bool JustClosed
        => (DateTime.UtcNow - LastOutsideClose).TotalMilliseconds <= 250;

    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Visual v && IsInsidePopupHost(v)) return;

        LastOutsideClose = DateTime.UtcNow;
        _close();
    }

    /// <summary>
    /// Vrai si le visuel est dans un popup : en overlay, le contenu des popups vit dans l'arbre
    /// de la fenêtre, et un clic dans un popup (même imbriqué) n'est pas un clic extérieur
    /// </summary>
    internal static bool IsInsidePopupHost(Visual v)
    {
        foreach (var a in v.GetVisualAncestors())
            if (a is Avalonia.Controls.Primitives.OverlayPopupHost) return true;
        return false;
    }

    private void OnRootDeactivated(object? sender, EventArgs e)
    {
        // Popup dessiné dans la fenêtre (overlay) : il ne flotte jamais au-dessus des autres
        // applications, et la désactivation ne le ferme pas, quelle que soit sa durée (Alt+Tab,
        // focus pris par une autre fenêtre…). Il se ferme par un clic dans la fenêtre hors de
        // lui, Échap, un choix ou le code (décision du 05/10/2026, QX-02).
        if (_popup != null && IsDrawnInWindow(_popup))
            return;

        // Popup natif (fenêtre séparée) : fermé si la fenêtre reste inactive 250 ms, pour qu'il
        // ne flotte pas au-dessus des autres applications. Certains bureaux (GNOME/XWayland)
        // font rebondir l'activation : le délai absorbe ce rebond.
        Avalonia.Threading.DispatcherTimer.RunOnce(() =>
        {
            if (_isOpen() && _root is Window { IsActive: false })
                _close();
        }, TimeSpan.FromMilliseconds(250));
    }
}
