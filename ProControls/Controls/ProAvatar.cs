using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ProControls.Theme;
using System.Globalization;
using System.Linq;

namespace ProControls.Controls;

/// <summary>Taille d'un ProAvatar</summary>
public enum AvatarSize
{
    /// <summary>24 px (listes denses)</summary>
    Small,
    /// <summary>32 px (défaut — aligné sur la pastille de ProListView)</summary>
    Medium,
    /// <summary>40 px (en-têtes, fiches)</summary>
    Large
}

/// <summary>Pastille de présence de l'avatar</summary>
public enum AvatarStatus { None, Online, Away, Busy, Offline }

/// <summary>
/// Avatar rond : initiales dérivées du nom (ou posées), image optionnelle,
/// pastille de présence. La couleur de fond est stable par nom (hash sur la
/// palette) — la même personne a toujours la même couleur.
/// </summary>
public class ProAvatar : Control
{
    // Palette de fonds (teintes soutenues, texte blanc lisible dessus)
    private static readonly Color[] Palette =
    {
        Color.Parse("#E95420"), // orange Ubuntu
        Color.Parse("#0073E5"),
        Color.Parse("#2C9E4B"),
        Color.Parse("#9141AC"),
        Color.Parse("#C64600"),
        Color.Parse("#00847E"),
        Color.Parse("#B5104B"),
        Color.Parse("#5B5EA6"),
    };

    private string _name = "";
    private string? _initials;
    private IImage? _image;
    private AvatarSize _size = AvatarSize.Medium;
    private AvatarStatus _status = AvatarStatus.None;
    private Color? _background;

    /// <summary>
    /// Nom complet : source des initiales et de la couleur stable.
    /// (Pas « Name » : ce serait masquer StyledElement.Name, le nom d'élément
    /// Avalonia utilisé par FindControl.)
    /// </summary>
    public string FullName
    {
        get => _name;
        set { _name = value; InvalidateVisual(); }
    }

    /// <summary>Initiales explicites (sinon dérivées du nom : « Julien Peloille » → JP)</summary>
    public string? Initials
    {
        get => _initials;
        set { _initials = value; InvalidateVisual(); }
    }

    /// <summary>Photo : prime sur les initiales quand posée</summary>
    public IImage? Image
    {
        get => _image;
        set { _image = value; InvalidateVisual(); }
    }

    public AvatarSize Size
    {
        get => _size;
        set { _size = value; InvalidateMeasure(); InvalidateVisual(); }
    }

    public AvatarStatus Status
    {
        get => _status;
        set { _status = value; InvalidateVisual(); }
    }

    /// <summary>Fond explicite (sinon couleur stable par hash du nom)</summary>
    public Color? Background
    {
        get => _background;
        set { _background = value; InvalidateVisual(); }
    }

    public double Diameter => _size switch
    {
        AvatarSize.Small => 24,
        AvatarSize.Large => 40,
        _ => 32,
    };

    private string EffectiveInitials
    {
        get
        {
            if (!string.IsNullOrEmpty(_initials)) return _initials!;
            var words = _name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Length switch
            {
                0 => "?",
                1 => char.ToUpper(words[0][0], CultureInfo.CurrentCulture).ToString(),
                _ => string.Concat(
                    char.ToUpper(words[0][0], CultureInfo.CurrentCulture),
                    char.ToUpper(words[^1][0], CultureInfo.CurrentCulture)),
            };
        }
    }

    private Color EffectiveBackground
    {
        get
        {
            if (_background.HasValue) return _background.Value;
            if (string.IsNullOrEmpty(_name)) return Palette[0];
            // Hash déterministe (pas string.GetHashCode : randomisé par process)
            uint h = 2166136261;
            foreach (var c in _name)
                h = (h ^ c) * 16777619;
            return Palette[(int)(h % (uint)Palette.Length)];
        }
    }

    protected override Size MeasureOverride(Size availableSize)
        => new(Diameter, Diameter);

    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);
        var d = Diameter;
        var center = new Point(d / 2, d / 2);
        var radius = d / 2;

        if (_image != null)
        {
            // Photo rognée au cercle
            using (context.PushGeometryClip(new EllipseGeometry(new Rect(0, 0, d, d))))
                context.DrawImage(_image, new Rect(0, 0, d, d));
            context.DrawEllipse(null, new Pen(new SolidColorBrush(ProTheme.Border.Subtle), 1),
                center, radius - 0.5, radius - 0.5);
        }
        else
        {
            context.DrawEllipse(new SolidColorBrush(EffectiveBackground), null, center, radius, radius);
            var initials = new FormattedText(EffectiveInitials,
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(ProTheme.Typography.FontFamily, FontStyle.Normal, FontWeight.SemiBold),
                d * 0.4, new SolidColorBrush(Colors.White));
            context.DrawText(initials, Crisp.Snap(new Point(
                center.X - initials.Width / 2, center.Y - initials.Height / 2)));
        }

        // Pastille de présence (bas-droite, cerclée du fond du panneau)
        if (_status != AvatarStatus.None)
        {
            var dotR = Math.Max(3.5, d * 0.14);
            var dotCenter = new Point(d - dotR - 0.5, d - dotR - 0.5);
            var dotColor = _status switch
            {
                AvatarStatus.Online => ProTheme.Accent.Success,
                AvatarStatus.Away => ProTheme.Accent.Warning,
                AvatarStatus.Busy => ProTheme.Accent.Error,
                _ => ProTheme.Text.Disabled,
            };
            context.DrawEllipse(new SolidColorBrush(dotColor),
                new Pen(new SolidColorBrush(ProTheme.Background.Panel), 2),
                dotCenter, dotR, dotR);
        }
    }
}
