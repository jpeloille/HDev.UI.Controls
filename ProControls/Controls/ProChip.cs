using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using ProControls.Theme;

namespace ProControls.Controls;

/// <summary>
/// Chip (pilule) : étiquette cliquable, cochable (filtre) et/ou fermable (×).
/// Visuel coché aligné sur les jetons de ProTokenEdit (accent 30/90). Hérite de
/// ProControlBase : hover/pressed/focus et activation Espace/Entrée gratuits.
/// </summary>
public class ProChip : ProControlBase
{
    private const double ChipHeight = 26;
    private const double CloseSize = 16;

    private string _text = "";
    private string? _icon;
    private bool _isCheckable;
    private bool _isChecked;
    private bool _canClose;
    private bool _closeHovered;

    public string Text
    {
        get => _text;
        set { _text = value; InvalidateMeasure(); InvalidateVisual(); }
    }

    /// <summary>Icône emoji/unicode optionnelle avant le texte</summary>
    public string? Icon
    {
        get => _icon;
        set { _icon = value; InvalidateMeasure(); InvalidateVisual(); }
    }

    /// <summary>Chip-filtre : le clic bascule <see cref="IsChecked"/></summary>
    public bool IsCheckable
    {
        get => _isCheckable;
        set { _isCheckable = value; InvalidateVisual(); }
    }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value) return;
            _isChecked = value;
            InvalidateVisual();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Affiche la croix de fermeture</summary>
    public bool CanClose
    {
        get => _canClose;
        set { _canClose = value; InvalidateMeasure(); InvalidateVisual(); }
    }

    /// <summary>Clic sur le corps du chip (hors croix)</summary>
    public event EventHandler? Click;

    /// <summary>Bascule de <see cref="IsChecked"/></summary>
    public event EventHandler? CheckedChanged;

    /// <summary>Avant fermeture (annulable)</summary>
    public event EventHandler<System.ComponentModel.CancelEventArgs>? CloseRequested;

    /// <summary>Après fermeture : à l'app de retirer le chip de son conteneur</summary>
    public event EventHandler? Closed;

    private string DisplayText => string.IsNullOrEmpty(_icon) ? _text : $"{_icon} {_text}";

    private Rect CloseRect => new(
        Bounds.Width - CloseSize - 5, (Bounds.Height - CloseSize) / 2, CloseSize, CloseSize);

    protected override Size MeasureOverride(Size availableSize)
    {
        var text = CreateText(DisplayText, ProTheme.Text.Primary, 12.5);
        var width = 12 + text.Width + 12 + (_canClose ? CloseSize + 2 : 0);
        return new Size(width, ChipHeight);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var over = _canClose && CloseRect.Contains(e.GetPosition(this));
        if (over != _closeHovered)
        {
            _closeHovered = over;
            InvalidateVisual();
        }
        base.OnPointerMoved(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _closeHovered = false;
        base.OnPointerExited(e); // ProControlBase gère hover/pressed
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        // La croix court-circuite le clic du corps
        if (_canClose && IsEnabled && CloseRect.Contains(e.GetPosition(this)))
        {
            RequestClose();
            e.Handled = true;
            return;
        }
        base.OnPointerReleased(e); // déclenche OnClick si pressé sur le corps
    }

    /// <summary>Demande la fermeture (même chemin que la croix : annulable)</summary>
    public void RequestClose()
    {
        var args = new System.ComponentModel.CancelEventArgs();
        CloseRequested?.Invoke(this, args);
        if (args.Cancel) return;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnClick()
    {
        if (_isCheckable)
            IsChecked = !_isChecked;
        Click?.Invoke(this, EventArgs.Empty);
    }

    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);
        var rect = new Rect(Bounds.Size);
        var radius = rect.Height / 2;

        // Fond/bord : coché = accent 30/90 (idiome ProTokenEdit), sinon neutre
        Color background, border, textColor;
        if (!IsEnabled)
        {
            background = ProTheme.Background.ControlDisabled;
            border = ProTheme.Border.Disabled;
            textColor = ProTheme.Text.Disabled;
        }
        else if (_isCheckable && _isChecked)
        {
            background = ProTheme.WithOpacity(ProTheme.Accent.Primary,
                (byte)(IsPressed ? 55 : IsHovered ? 42 : 30));
            border = ProTheme.WithOpacity(ProTheme.Accent.Primary, 90);
            textColor = ProTheme.Text.Primary;
        }
        else
        {
            background = IsPressed ? ProTheme.Background.ControlPressed
                : IsHovered ? ProTheme.Background.ControlHover
                : ProTheme.Background.Toolbar;
            border = IsHovered ? ProTheme.Border.Hover : ProTheme.Border.Subtle;
            textColor = ProTheme.Text.Primary;
        }

        context.DrawRectangle(new SolidColorBrush(background),
            new Pen(new SolidColorBrush(border), 1), rect.Deflate(0.5), radius, radius);

        if (IsFocused)
            DrawFocusRing(context, rect, radius);

        var text = CreateText(DisplayText, textColor, 12.5);
        context.DrawText(text, Crisp.Snap(new Point(12, (rect.Height - text.Height) / 2)));

        if (_canClose)
        {
            var closeRect = CloseRect;
            if (_closeHovered)
                context.DrawRectangle(new SolidColorBrush(ProTheme.Background.ControlPressed),
                    null, closeRect, CloseSize / 2, CloseSize / 2);
            var pen = new Pen(new SolidColorBrush(textColor), 1.2);
            var c = closeRect.Center;
            context.DrawLine(pen, new Point(c.X - 3, c.Y - 3), new Point(c.X + 3, c.Y + 3));
            context.DrawLine(pen, new Point(c.X - 3, c.Y + 3), new Point(c.X + 3, c.Y - 3));
        }
    }
}
