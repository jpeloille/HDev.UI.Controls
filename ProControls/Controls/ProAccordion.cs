using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ProControls.Theme;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ProControls.Controls;

/// <summary>Politique d'expansion de l'accordéon</summary>
public enum AccordionExpandMode
{
    /// <summary>Une seule section ouverte à la fois (en ouvrir une ferme l'autre)</summary>
    Single,
    /// <summary>Chaque section s'ouvre/se ferme indépendamment</summary>
    Multiple
}

/// <summary>Section d'un ProAccordion : en-tête + contenu hébergé</summary>
public class ProAccordionSection
{
    private string _header = "";
    private bool _isExpanded;

    internal ProAccordion? Owner { get; set; }

    public string Header
    {
        get => _header;
        set { _header = value; Owner?.InvalidateVisual(); }
    }

    /// <summary>Icône emoji/unicode optionnelle avant le texte</summary>
    public string? Icon { get; set; }

    /// <summary>Contenu affiché quand la section est ouverte</summary>
    public Control? Content { get; set; }

    /// <summary>
    /// Fond d'en-tête propre à la section (états : vert OK, rouge alerte…).
    /// null = fond standard. Passer une teinte douce, ex.
    /// ProTheme.WithOpacity(ProTheme.Accent.Success, 36).
    /// </summary>
    public Avalonia.Media.Color? HeaderBackground
    {
        get => _headerBackground;
        set { _headerBackground = value; Owner?.InvalidateVisual(); }
    }
    private Avalonia.Media.Color? _headerBackground;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            if (Owner != null) Owner.SetExpanded(this, value);
            else _isExpanded = value;
        }
    }

    /// <summary>Pose l'état sans repasser par la politique de l'accordéon</summary>
    internal void SetExpandedSilent(bool value) => _isExpanded = value;

    public object? Tag { get; set; }

    public ProAccordionSection() { }

    public ProAccordionSection(string header, Control? content = null, string? icon = null, bool isExpanded = false)
    {
        _header = header;
        Content = content;
        Icon = icon;
        _isExpanded = isExpanded;
    }
}

/// <summary>Arguments d'ouverture/fermeture de section (annulable)</summary>
public class ProAccordionSectionEventArgs : System.ComponentModel.CancelEventArgs
{
    public ProAccordionSection Section { get; }
    public bool Expanding { get; }

    public ProAccordionSectionEventArgs(ProAccordionSection section, bool expanding)
    {
        Section = section;
        Expanding = expanding;
    }
}

/// <summary>
/// Accordéon (équivalent NavBar/AccordionControl vertical) : sections empilées
/// à en-tête cliquable (chevron), contenu hébergé, mode exclusif ou multiple.
/// Clavier : ↑↓ déplacent le focus d'en-tête, Enter/Espace bascule.
/// </summary>
public class ProAccordion : Control
{
    private const double HeaderHeight = 38;

    private ProAccordionSection? _hoveredSection;
    private int _focusIndex = -1;
    private readonly List<Control> _attachedContents = new();

    public ObservableCollection<ProAccordionSection> Sections { get; } = new();

    public AccordionExpandMode ExpandMode { get; set; } = AccordionExpandMode.Single;

    /// <summary>Avant ouverture/fermeture (annulable)</summary>
    public event EventHandler<ProAccordionSectionEventArgs>? SectionExpanding;

    /// <summary>Après ouverture/fermeture</summary>
    public event EventHandler<ProAccordionSection>? SectionExpandedChanged;

    static ProAccordion()
    {
        FocusableProperty.OverrideDefaultValue<ProAccordion>(true);
    }

    public ProAccordion()
    {
        ClipToBounds = true;
        Sections.CollectionChanged += (s, e) =>
        {
            foreach (var section in Sections)
                section.Owner = this;
            SyncAttachedContents();
            InvalidateMeasure();
            InvalidateVisual();
        };
    }

    /// <summary>Ajoute une section (raccourci fluent)</summary>
    public ProAccordionSection AddSection(string header, Control? content = null,
        string? icon = null, bool isExpanded = false)
    {
        var section = new ProAccordionSection(header, content, icon, isExpanded);
        Sections.Add(section);
        if (isExpanded && ExpandMode == AccordionExpandMode.Single)
            SetExpanded(section, true); // ferme les autres, politique respectée
        return section;
    }

    /// <summary>
    /// Applique la politique d'expansion : événement annulable, exclusivité en
    /// mode Single, contenu (dé)monté, notification.
    /// </summary>
    internal void SetExpanded(ProAccordionSection section, bool expanded)
    {
        if (section.IsExpanded == expanded) return;

        var args = new ProAccordionSectionEventArgs(section, expanded);
        SectionExpanding?.Invoke(this, args);
        if (args.Cancel) return;

        if (expanded && ExpandMode == AccordionExpandMode.Single)
        {
            foreach (var other in Sections)
            {
                if (ReferenceEquals(other, section) || !other.IsExpanded) continue;
                var closeArgs = new ProAccordionSectionEventArgs(other, false);
                SectionExpanding?.Invoke(this, closeArgs);
                if (closeArgs.Cancel) return; // l'autre refuse de fermer : rien ne bouge
                other.SetExpandedSilent(false);
                SectionExpandedChanged?.Invoke(this, other);
            }
        }

        section.SetExpandedSilent(expanded);
        SyncAttachedContents();
        InvalidateMeasure();
        InvalidateVisual();
        SectionExpandedChanged?.Invoke(this, section);
    }

    // ═══════════════════════════════════════════════════════════════
    // CONTENU HÉBERGÉ (idiome ProTabControl : VisualChildren gérés à la main)
    // ═══════════════════════════════════════════════════════════════

    private void SyncAttachedContents()
    {
        foreach (var control in _attachedContents)
        {
            VisualChildren.Remove(control);
            LogicalChildren.Remove(control);
        }
        _attachedContents.Clear();

        foreach (var section in Sections)
        {
            if (!section.IsExpanded || section.Content == null) continue;
            _attachedContents.Add(section.Content);
            VisualChildren.Add(section.Content);
            LogicalChildren.Add(section.Content);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double height = 0;
        foreach (var section in Sections)
        {
            height += HeaderHeight;
            if (section.IsExpanded && section.Content != null)
            {
                section.Content.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                height += section.Content.DesiredSize.Height;
            }
        }
        return new Size(
            double.IsInfinity(availableSize.Width) ? 260 : availableSize.Width,
            height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double y = 0;
        foreach (var section in Sections)
        {
            y += HeaderHeight;
            if (section.IsExpanded && section.Content != null)
            {
                var h = section.Content.DesiredSize.Height;
                section.Content.Arrange(new Rect(0, y, finalSize.Width, h));
                y += h;
            }
        }
        return finalSize;
    }

    // ═══════════════════════════════════════════════════════════════
    // LAYOUT DES EN-TÊTES (partagé rendu / hit-test)
    // ═══════════════════════════════════════════════════════════════

    private IEnumerable<(ProAccordionSection Section, Rect HeaderRect)> HeadersLayout()
    {
        double y = 0;
        var width = Bounds.Width;
        foreach (var section in Sections)
        {
            yield return (section, new Rect(0, y, width, HeaderHeight));
            y += HeaderHeight;
            if (section.IsExpanded && section.Content != null)
                y += section.Content.DesiredSize.Height;
        }
    }

    private ProAccordionSection? HitTestHeader(Point pos)
    {
        foreach (var (section, rect) in HeadersLayout())
            if (rect.Contains(pos))
                return section;
        return null;
    }

    // ═══════════════════════════════════════════════════════════════
    // SOURIS / CLAVIER
    // ═══════════════════════════════════════════════════════════════

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var section = HitTestHeader(e.GetPosition(this));
        if (section != _hoveredSection)
        {
            _hoveredSection = section;
            Cursor = section != null ? new Cursor(StandardCursorType.Hand) : Cursor.Default;
            InvalidateVisual();
        }
        base.OnPointerMoved(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _hoveredSection = null;
        Cursor = Cursor.Default;
        InvalidateVisual();
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var section = HitTestHeader(e.GetPosition(this));
        if (section != null)
        {
            Focus();
            _focusIndex = Sections.IndexOf(section);
            SetExpanded(section, !section.IsExpanded);
            e.Handled = true;
            return;
        }
        base.OnPointerPressed(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (Sections.Count == 0)
        {
            base.OnKeyDown(e);
            return;
        }

        switch (e.Key)
        {
            case Key.Up:
                _focusIndex = _focusIndex <= 0 ? 0 : _focusIndex - 1;
                InvalidateVisual();
                e.Handled = true;
                break;
            case Key.Down:
                _focusIndex = Math.Min(Sections.Count - 1, _focusIndex + 1);
                InvalidateVisual();
                e.Handled = true;
                break;
            case Key.Home:
                _focusIndex = 0;
                InvalidateVisual();
                e.Handled = true;
                break;
            case Key.End:
                _focusIndex = Sections.Count - 1;
                InvalidateVisual();
                e.Handled = true;
                break;
            case Key.Enter:
            case Key.Space:
                if (_focusIndex >= 0 && _focusIndex < Sections.Count)
                {
                    var section = Sections[_focusIndex];
                    SetExpanded(section, !section.IsExpanded);
                    e.Handled = true;
                }
                break;
        }
        base.OnKeyDown(e);
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        if (_focusIndex < 0 && Sections.Count > 0) _focusIndex = 0;
        InvalidateVisual();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        InvalidateVisual();
        base.OnLostFocus(e);
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════

    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(new SolidColorBrush(ProTheme.Background.Panel), bounds);

        var index = 0;
        foreach (var (section, rect) in HeadersLayout())
        {
            var isHovered = ReferenceEquals(section, _hoveredSection);
            var isFocused = IsFocused && index == _focusIndex;

            // Fond d'en-tête : couleur de section si posée, sinon standard.
            // Le hover se fait en SURCOUCHE translucide pour rester visible
            // au-dessus d'un fond custom (vert/rouge) comme du fond standard.
            var headerBg = section.HeaderBackground ?? ProTheme.Background.Toolbar;
            context.FillRectangle(new SolidColorBrush(headerBg), rect);
            if (isHovered)
                context.FillRectangle(new SolidColorBrush(
                    ProTheme.WithOpacity(ProTheme.Text.Primary, 16)), rect);

            // Séparateur bas d'en-tête
            context.DrawLine(new Pen(new SolidColorBrush(ProTheme.Border.Subtle), 1),
                new Point(rect.X, rect.Bottom - 0.5), new Point(rect.Right, rect.Bottom - 0.5));

            // Chevron (▸ fermé, ▾ ouvert), dessiné au trait
            var chevronPen = new Pen(new SolidColorBrush(
                section.IsExpanded ? ProTheme.Accent.Primary : ProTheme.Text.Secondary), 1.6)
            {
                LineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };
            var cx = rect.X + 16;
            var cy = rect.Center.Y;
            if (section.IsExpanded)
            {
                context.DrawLine(chevronPen, new Point(cx - 4, cy - 2), new Point(cx, cy + 2.5));
                context.DrawLine(chevronPen, new Point(cx, cy + 2.5), new Point(cx + 4, cy - 2));
            }
            else
            {
                context.DrawLine(chevronPen, new Point(cx - 2, cy - 4), new Point(cx + 2.5, cy));
                context.DrawLine(chevronPen, new Point(cx + 2.5, cy), new Point(cx - 2, cy + 4));
            }

            // Icône + titre
            double x = rect.X + 32;
            if (!string.IsNullOrEmpty(section.Icon))
            {
                var icon = CreateText(section.Icon, ProTheme.Text.Primary, weight: FontWeight.Regular);
                context.DrawText(icon, Crisp.Snap(new Point(x, cy - icon.Height / 2)));
                x += icon.Width + 8;
            }
            var title = CreateText(section.Header,
                section.IsExpanded ? ProTheme.Text.Primary : ProTheme.Text.Secondary,
                weight: section.IsExpanded ? FontWeight.SemiBold : FontWeight.Regular);
            context.DrawText(title, Crisp.Snap(new Point(x, cy - title.Height / 2)));

            // Anneau de focus clavier
            if (isFocused)
            {
                context.DrawRectangle(null,
                    new Pen(new SolidColorBrush(ProTheme.WithOpacity(ProTheme.Border.FocusOuter, 120)), 1.5),
                    rect.Deflate(1.5), 4, 4);
            }

            index++;
        }
    }

    private static FormattedText CreateText(string text, Color color, FontWeight weight)
        => new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(ProTheme.Typography.FontFamily, FontStyle.Normal, weight),
            ProTheme.Typography.FontSizeBody, new SolidColorBrush(color));
}
