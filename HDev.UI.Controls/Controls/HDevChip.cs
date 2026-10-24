using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

/// <summary>
/// Chip (pilule) : étiquette cliquable, cochable (filtre) et/ou fermable (×).
/// Visuel coché aligné sur les jetons de HDevTokenEdit (accent 30/90). Hérite de
/// HDevControlBase : hover/pressed/focus et activation Espace/Entrée gratuits.
/// </summary>
public class HDevChip : HDevControlBase
{
    private const double ChipHeight = HDevTheme.Size.RowHeight;
    private const double CloseSize = 16;

    static HDevChip()
    {
        VerticalAlignmentProperty.OverrideDefaultValue<HDevChip>(Avalonia.Layout.VerticalAlignment.Center);
    }

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

    /// <summary>Compteur/badge après le texte (« 3 », « 12+ », « ! »). null = pas de badge</summary>
    public string? Badge
    {
        get => _badge;
        set { _badge = value; InvalidateMeasure(); InvalidateVisual(); }
    }
    private string? _badge;

    /// <summary>Couleur du badge (défaut : accent ; alerte = HDevTheme.Accent.Error)</summary>
    public Color? BadgeColor
    {
        get => _badgeColor;
        set { _badgeColor = value; InvalidateVisual(); }
    }
    private Color? _badgeColor;

    /// <summary>Clic sur le corps du chip (hors croix)</summary>
    public event EventHandler? Click;

    /// <summary>Bascule de <see cref="IsChecked"/></summary>
    public event EventHandler? CheckedChanged;

    /// <summary>Avant fermeture (annulable)</summary>
    public event EventHandler<System.ComponentModel.CancelEventArgs>? CloseRequested;

    /// <summary>Après fermeture : à l'app de retirer le chip de son conteneur</summary>
    public event EventHandler? Closed;

    private HDevIcon _vectorIcon;

    /// <summary>Icône vectorielle avant le texte (prioritaire sur <see cref="Icon"/>)</summary>
    public HDevIcon VectorIcon
    {
        get => _vectorIcon;
        set { _vectorIcon = value; InvalidateMeasure(); InvalidateVisual(); }
    }

    private double IconSlot => _vectorIcon != HDevIcon.None ? HDevTheme.Size.IconSmall + 4 : 0;

    private string DisplayText => string.IsNullOrEmpty(_icon) || _vectorIcon != HDevIcon.None ? _text : $"{_icon} {_text}";

    private Rect CloseRect => new(
        Bounds.Width - CloseSize - 5, (Bounds.Height - CloseSize) / 2, CloseSize, CloseSize);

    protected override Size MeasureOverride(Size availableSize)
    {
        var text = CreateText(DisplayText, HDevTheme.Text.Primary, HDevTheme.Typography.FontSizeCaption);
        var width = 12 + IconSlot + text.Width + 12 + (_canClose ? CloseSize + 2 : 0)
                    + (string.IsNullOrEmpty(_badge) ? 0 : BadgeWidth() + 2);
        return new Size(width, ChipHeight);
    }

    private double BadgeWidth()
    {
        var t = CreateText(_badge!, HDevTheme.Text.OnAccent, HDevTheme.Typography.FontSizeMicro, FontWeight.SemiBold);
        return Math.Max(16, t.Width + 9);
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
        base.OnPointerExited(e); // HDevControlBase gère hover/pressed
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

        // Fond/bord : coché = accent 30/90 (idiome HDevTokenEdit), sinon neutre
        Color background, border, textColor;
        if (!IsEnabled)
        {
            background = HDevTheme.Disabled.Background;
            border = HDevTheme.Disabled.Border;
            textColor = HDevTheme.Disabled.Text;
        }
        else if (_isCheckable && _isChecked)
        {
            background = IsPressed ? HDevTheme.Lerp(HDevTheme.Background.Selection, HDevTheme.Accent.Primary, 0.25)
                : IsHovered ? HDevTheme.Lerp(HDevTheme.Background.Selection, HDevTheme.Accent.Primary, 0.12)
                : HDevTheme.SelectionFill();
            border = HDevTheme.SelectionBorder;
            textColor = HDevTheme.Text.Primary;
        }
        else
        {
            background = IsPressed ? HDevTheme.Background.ControlPressed
                : IsHovered ? HDevTheme.Background.ControlHover
                : HDevTheme.Background.Toolbar;
            border = IsHovered ? HDevTheme.Border.Hover : HDevTheme.Border.Subtle;
            textColor = HDevTheme.Text.Primary;
        }

        context.DrawRectangle(new SolidColorBrush(background),
            new Pen(new SolidColorBrush(border), 1), rect.Deflate(0.5), radius, radius);

        if (IsFocusVisible && IsEnabled)
            DrawFocusRing(context, rect, radius);

        var text = CreateText(DisplayText, textColor, HDevTheme.Typography.FontSizeCaption);
        if (_vectorIcon != HDevIcon.None)
            HDevIcons.Draw(context, _vectorIcon,
                new Point(12 + HDevTheme.Size.IconSmall / 2.0, rect.Height / 2), HDevTheme.Size.IconSmall, textColor);
        context.DrawText(text, Crisp.Snap(new Point(12 + IconSlot, (rect.Height - text.Height) / 2)));

        // Badge compteur après le texte
        if (!string.IsNullOrEmpty(_badge))
        {
            var bw = BadgeWidth();
            const double bh = 16;
            var badgeRect = new Rect(12 + IconSlot + text.Width + 6, (rect.Height - bh) / 2, bw, bh);
            var badgeColor = IsEnabled ? (_badgeColor ?? HDevTheme.Accent.Fill) : HDevTheme.Disabled.Text;
            context.DrawRectangle(new SolidColorBrush(badgeColor), null, badgeRect, bh / 2, bh / 2);
            // Couleur de badge libre (BadgeColor) : le chiffre prend le texte le plus lisible
            var badgeText = CreateText(_badge!, HDevTheme.OnColor(badgeColor), HDevTheme.Typography.FontSizeMicro, FontWeight.SemiBold);
            context.DrawText(badgeText, Crisp.Snap(new Point(
                badgeRect.Center.X - badgeText.Width / 2,
                badgeRect.Center.Y - badgeText.Height / 2)));
        }

        if (_canClose)
        {
            var closeRect = CloseRect;
            if (_closeHovered)
                context.DrawRectangle(new SolidColorBrush(HDevTheme.Background.ControlPressed),
                    null, closeRect, CloseSize / 2, CloseSize / 2);
            var pen = new Pen(new SolidColorBrush(textColor), 1.2);
            var c = closeRect.Center;
            context.DrawLine(pen, new Point(c.X - 3, c.Y - 3), new Point(c.X + 3, c.Y + 3));
            context.DrawLine(pen, new Point(c.X - 3, c.Y + 3), new Point(c.X + 3, c.Y - 3));
        }
    }
}
