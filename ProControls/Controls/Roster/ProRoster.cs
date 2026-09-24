using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using ProControls.Theme;
using System.Globalization;

namespace ProControls.Controls;

/// <summary>
/// Frise de roster : N voies × un axe de temps. Une gouttière de libellés à gauche, et par
/// voie trois couches superposées — bande d'amplitude, barre fine, blocs étiquetés.
/// Virtualisée verticalement, défilement et zoom autonomes.
/// </summary>
/// <remarks>
/// <b>Ce que ce contrôle n'est pas.</b> <c>ProGantt</c> est un planificateur de projet — WBS,
/// dépendances, chemin critique — et non un planning de ressources. <c>ProScheduler</c> est un
/// agenda façon Outlook, à voie unique. Un roster est la troisième forme : des lignes de
/// ressources, ou de journées, alignées sur un même axe temporel.
///
/// <para><b>Le contrôle affiche, le métier décide.</b> Les teintes viennent du modèle
/// (<c>Color?</c>) et jamais d'une interprétation faite ici ; une couche absente n'est pas
/// dessinée plutôt que devinée. Le fond, la grille, le texte et les bordures viennent de
/// <c>ProTheme</c>, donc la bascule clair/sombre suit. Les pinceaux sont alloués <b>à chaque
/// frame</b> — comme ProGantt et ProScheduler, et contrairement à la frise d'origine, dont les
/// caches statiques figeaient les couleurs au changement de thème.</para>
/// </remarks>
public class ProRoster : Control
{
    private const double ScrollBarSize = 14;
    private const double GutterPadding = 5;

    /// <summary>Hauteur d'une voie. Uniforme : la virtualisation en dépend.</summary>
    private const double RowHeight = 40;

    private const double BandMarginTop = 2;
    private const double BandHeight = RowHeight - 14;   // 26
    private const double BarHeight = 6;
    private const double BarMarginBottom = 2;
    private const double BlockHeight = 12;

    private RosterModel? _model;
    private double _verticalOffset;
    private bool _syncingScrollbars;

    private readonly ScrollBar _vScroll;
    private readonly ScrollBar _hScroll;

    // ── Interaction (lot 2b) ───────────────────────────────────────
    private const double DragThreshold = 5;

    /// <summary>Pas d'aimantation du glisser. Repris de la frise d'origine.</summary>
    private static readonly TimeSpan SnapStep = TimeSpan.FromMinutes(5);

    // Alloués une fois. La frise d'origine construisait un Cursor à CHAQUE déplacement
    // de souris — une allocation par frame de survol.
    private static readonly Cursor HandCursor = new(StandardCursorType.Hand);
    private static readonly Cursor DragCursor = new(StandardCursorType.DragMove);
    private static readonly Cursor ArrowCursor = new(StandardCursorType.Arrow);

    private RosterBlock? _hoveredBlock;
    private RosterLane? _hoveredLane;
    private RosterBlock? _selectedBlock;

    private bool _dragArmed;
    private bool _dragging;
    private RosterBlock? _dragBlock;
    private RosterLane? _dragSourceLane;
    private RosterLane? _dragTargetLane;
    private Point _dragOrigin;
    private TimeSpan _dragGrab;
    private DateTime _dragStart;
    private DateTime _dragEnd;

    static ProRoster()
    {
        FocusableProperty.OverrideDefaultValue<ProRoster>(true);
    }

    public ProRoster()
    {
        ClipToBounds = true;

        _vScroll = new ScrollBar
        {
            Orientation = Avalonia.Layout.Orientation.Vertical,
            Width = ScrollBarSize,
            AllowAutoHide = false
        };
        _vScroll.Scroll += (_, _) =>
        {
            if (_syncingScrollbars) return;
            _verticalOffset = _vScroll.Value;
            InvalidateVisual();
        };

        _hScroll = new ScrollBar
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Height = ScrollBarSize,
            AllowAutoHide = false
        };
        _hScroll.Scroll += (_, _) =>
        {
            if (_syncingScrollbars) return;
            Axis.ViewOffset = _hScroll.Value;
            InvalidateVisual();
        };

        VisualChildren.Add(_vScroll);
        VisualChildren.Add(_hScroll);
        LogicalChildren.Add(_vScroll);
        LogicalChildren.Add(_hScroll);
    }

    // ═══════════════════════════════════════════════════════════════
    // API PUBLIQUE
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Le graphe affiché. Le contrôle se repeint sur <c>RosterModel.Changed</c>.</summary>
    public RosterModel? Model
    {
        get => _model;
        set
        {
            if (ReferenceEquals(_model, value)) return;

            if (_model is not null)
            {
                _model.Changed -= OnModelChanged;
            }

            _model = value;

            if (_model is not null)
            {
                _model.Changed += OnModelChanged;
            }

            _verticalOffset = 0;
            _selectedBlock = null;
            InvalidateVisual();
        }
    }

    /// <summary>Axe temporel, en résolution horaire.</summary>
    public RosterTimeAxis Axis { get; } = new();

    /// <summary>
    /// Étendue temporelle d'une voie, depuis son <c>WindowStart</c>.
    /// </summary>
    /// <remarks>
    /// Une journée pour un consommateur « une voie = un jour » — l'axe redémarre alors à 00:00
    /// sur chaque ligne. Plusieurs jours pour un consommateur « une voie = une ressource ».
    /// </remarks>
    public TimeSpan LaneSpan { get; set; } = RosterEngine.Day;

    /// <summary>Largeur de la gouttière des libellés.</summary>
    public double GutterWidth { get; set; } = 120;

    public bool ShowNowLine { get; set; } = true;

    /// <summary>
    /// Instant « maintenant », pour la ligne de temps et pour <see cref="ScrollToToday"/>.
    /// </summary>
    /// <remarks>
    /// <b>À poser dès que les voies ne sont pas dans le fuseau du poste.</b> Par défaut le contrôle
    /// prend <c>DateTime.Now</c>, l'heure machine — juste seulement si les instants du modèle sont eux
    /// aussi en heure machine. Un consommateur qui projette ses voies dans un autre fuseau y met
    /// l'instant courant exprimé dans ce même fuseau, sans quoi la ligne rouge se pose à côté et le
    /// cadrage initial rate la journée.
    /// </remarks>
    public DateTime? Now { get; set; }

    /// <summary>L'instant courant retenu : celui qu'on a posé, à défaut l'heure machine.</summary>
    private DateTime NowInstant => Now ?? DateTime.Now;

    /// <summary>Ombrage des samedis et dimanches.</summary>
    public bool ShowWeekendShading { get; set; } = true;

    /// <summary>Bloc sélectionné. Le poser depuis le code repeint.</summary>
    public RosterBlock? SelectedBlock
    {
        get => _selectedBlock;
        set
        {
            if (ReferenceEquals(_selectedBlock, value)) return;

            _selectedBlock = value;
            InvalidateVisual();
        }
    }

    /// <summary>En lecture seule, le contrôle se survole et se sélectionne, mais ne se déplace pas.</summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Peinture supplémentaire par voie, laissée à l'application.
    /// </summary>
    /// <remarks>
    /// C'est le point d'extension qui évite à un consommateur de modifier la bibliothèque pour
    /// poser ses propres indicateurs — un avertissement, un compteur, une pastille.
    /// </remarks>
    public Action<DrawingContext, RosterLane, Rect>? LaneAdorner { get; set; }

    /// <summary>Hauteur de l'en-tête : deux niveaux de graduations au-delà de la journée.</summary>
    public double HeaderHeight => LaneSpan > RosterEngine.Day ? 44 : 26;

    /// <summary>Cadre la vue sur l'instant courant.</summary>
    public void ScrollToToday()
    {
        var now = NowInstant;
        var lane = _model?.Lanes.FirstOrDefault(l => now >= l.WindowStart && now < l.WindowStart + LaneSpan);

        if (lane is not null)
        {
            Axis.ViewOffset = Axis.ToX(now - lane.WindowStart) + Axis.ViewOffset - TimelineWidth / 2;

            var index = _model!.Lanes.IndexOf(lane);
            _verticalOffset = Math.Max(0, index * RowHeight - RowsViewport.Height / 2);
        }

        UpdateScrollBars();
        InvalidateVisual();
    }

    /// <summary>Ajuste le zoom pour qu'une voie entière tienne dans la vue.</summary>
    public void ZoomToFit()
    {
        if (TimelineWidth <= 0) return;

        Axis.PixelsPerHour = TimelineWidth / LaneSpan.TotalHours;
        Axis.ViewOffset = 0;

        UpdateScrollBars();
        InvalidateVisual();
    }

    // ── Géométrie exposée ──────────────────────────────────────────
    // La frise d'origine ne publiait rien : son hôte recopiait les constantes pour placer ses
    // popups, s'est trompé sur trois d'entre elles et ignorait le zoom. On publie donc.

    /// <summary>Rectangle peint d'un bloc, ou null s'il n'appartient à aucune voie.</summary>
    public Rect? GetBlockBounds(RosterBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);

        if (_model is null) return null;

        for (var i = 0; i < _model.Lanes.Count; i++)
        {
            if (_model.Lanes[i].Blocks.Contains(block))
            {
                return BlockRect(_model.Lanes[i], block, LaneY(i));
            }
        }

        return null;
    }

    /// <summary>Voie sous le point, ou null.</summary>
    public RosterLane? LaneAt(Point point)
    {
        if (_model is null || point.Y < HeaderHeight) return null;

        var index = (int)((point.Y - HeaderHeight + _verticalOffset) / RowHeight);

        return index >= 0 && index < _model.Lanes.Count ? _model.Lanes[index] : null;
    }

    /// <summary>Instant sous le point, résolu dans la voie qui s'y trouve.</summary>
    public DateTime? TimeAt(Point point)
    {
        var lane = LaneAt(point);

        return lane is null ? null : lane.WindowStart + Axis.ToOffset(point.X - GutterWidth);
    }

    // ═══════════════════════════════════════════════════════════════
    // DISPOSITION
    // ═══════════════════════════════════════════════════════════════

    private double TimelineX => GutterWidth;

    private double TimelineWidth => Math.Max(0, Bounds.Width - GutterWidth - ScrollBarSize);

    private Rect RowsViewport => new(
        0,
        HeaderHeight,
        Math.Max(0, Bounds.Width - ScrollBarSize),
        Math.Max(0, Bounds.Height - HeaderHeight - ScrollBarSize));

    private double LaneY(int index) => HeaderHeight + index * RowHeight - _verticalOffset;

    /// <summary>
    /// Première et dernière voie visibles.
    /// </summary>
    /// <remarks>
    /// En O(1) parce que la hauteur de voie est uniforme. Une hauteur variable imposerait un
    /// tableau d'offsets cumulés et une recherche dichotomique.
    /// </remarks>
    private (int First, int Last) VisibleLaneRange()
    {
        if (_model is null || _model.Lanes.Count == 0) return (0, -1);

        var viewport = RowsViewport;
        var first = Math.Max(0, (int)(_verticalOffset / RowHeight));
        var last = Math.Min(
            _model.Lanes.Count - 1,
            (int)((_verticalOffset + viewport.Height) / RowHeight) + 1);

        return (first, last);
    }

    protected override Size MeasureOverride(Size availableSize)
        => new(
            double.IsInfinity(availableSize.Width) ? 800 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 400 : availableSize.Height);

    protected override Size ArrangeOverride(Size finalSize)
    {
        _vScroll.Measure(finalSize);
        _hScroll.Measure(finalSize);

        _vScroll.Arrange(new Rect(
            finalSize.Width - ScrollBarSize, HeaderHeight,
            ScrollBarSize, Math.Max(0, finalSize.Height - HeaderHeight - ScrollBarSize)));

        _hScroll.Arrange(new Rect(
            TimelineX, finalSize.Height - ScrollBarSize,
            Math.Max(0, finalSize.Width - ScrollBarSize - TimelineX), ScrollBarSize));

        UpdateScrollBars();

        return finalSize;
    }

    private void UpdateScrollBars()
    {
        _syncingScrollbars = true;

        var viewport = RowsViewport;
        var totalHeight = (_model?.Lanes.Count ?? 0) * RowHeight;

        _vScroll.Minimum = 0;
        _vScroll.Maximum = Math.Max(0, totalHeight - viewport.Height);
        _vScroll.ViewportSize = viewport.Height;
        _verticalOffset = Math.Clamp(_verticalOffset, 0, Math.Max(0, _vScroll.Maximum));
        _vScroll.Value = _verticalOffset;

        var totalWidth = LaneSpan.TotalHours * Axis.PixelsPerHour;

        _hScroll.Minimum = 0;
        _hScroll.Maximum = Math.Max(0, totalWidth - TimelineWidth);
        _hScroll.ViewportSize = TimelineWidth;
        Axis.ViewOffset = Math.Clamp(Axis.ViewOffset, 0, Math.Max(0, _hScroll.Maximum));
        _hScroll.Value = Axis.ViewOffset;

        _syncingScrollbars = false;
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        UpdateScrollBars();
        InvalidateVisual();
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════

    public override void Render(DrawingContext context)
    {
        Crisp.BeginFrame(this);

        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(new SolidColorBrush(ProTheme.Background.Panel), bounds);

        if (_model is null || _model.Lanes.Count == 0)
        {
            var empty = CreateText("Aucune voie à afficher", ProTheme.Text.Secondary, 13);
            context.DrawText(empty, Crisp.Snap(new Point(
                (bounds.Width - empty.Width) / 2, (bounds.Height - empty.Height) / 2)));
            return;
        }

        var (first, last) = VisibleLaneRange();

        // Le contenu est peint AVANT la gouttière et l'en-tête : il glisse dessous quand on
        // défile. La frise d'origine faisait l'inverse parce qu'elle ne défilait pas elle-même.
        var timeline = new Rect(TimelineX, HeaderHeight, TimelineWidth, RowsViewport.Height);

        using (context.PushClip(timeline))
        {
            RenderWeekendShading(context, first, last);
            RenderHoveredLane(context, first, last);
            RenderLaneSeparators(context, first, last);
            RenderBands(context, first, last);
            RenderBars(context, first, last);
            RenderBlocks(context, first, last);
            RenderAdorners(context, first, last);
            RenderNowLine(context, first, last);
            RenderDragGhost(context);
        }

        RenderGutter(context, first, last);
        RenderHeader(context);
    }

    private void RenderWeekendShading(DrawingContext context, int first, int last)
    {
        if (!ShowWeekendShading) return;

        var brush = new SolidColorBrush(ProTheme.Background.PanelAlt);

        // Une voie = une journée : c'est la ligne entière qui est un week-end.
        if (LaneSpan <= RosterEngine.Day)
        {
            for (var i = first; i <= last; i++)
            {
                if (!IsWeekend(_model!.Lanes[i].WindowStart)) continue;

                context.FillRectangle(brush, new Rect(TimelineX, LaneY(i), TimelineWidth, RowHeight));
            }

            return;
        }

        // Une voie couvre plusieurs jours : ce sont les colonnes qui sont des week-ends.
        // Suppose que les voies partagent la même origine — c'est le cas d'usage
        // « une voie = une ressource ». Des voies journalières avec LaneSpan > 1 jour se
        // chevaucheraient, et l'ombrage n'aurait alors aucun sens à calculer voie par voie.
        var lane = _model!.Lanes[first];
        var top = LaneY(first);
        var height = (last - first + 1) * RowHeight;

        for (var day = 0; day < (int)Math.Ceiling(LaneSpan.TotalDays); day++)
        {
            if (!IsWeekend(lane.WindowStart.AddDays(day))) continue;

            var x = TimelineX + Axis.ToX(TimeSpan.FromDays(day));
            context.FillRectangle(brush, new Rect(x, top, Axis.PixelsPerDay, height));
        }
    }

    private void RenderHoveredLane(DrawingContext context, int first, int last)
    {
        if (_hoveredLane is null || _model is null) return;

        var index = _model.Lanes.IndexOf(_hoveredLane);
        if (index < first || index > last) return;

        context.FillRectangle(
            new SolidColorBrush(ProTheme.Background.Selection, 0.28),
            new Rect(TimelineX, LaneY(index), TimelineWidth, RowHeight));
    }

    private void RenderLaneSeparators(DrawingContext context, int first, int last)
    {
        var pen = new Pen(new SolidColorBrush(ProTheme.Border.Subtle), 1);

        for (var i = first; i <= last + 1 && i <= _model!.Lanes.Count; i++)
        {
            var y = SnapY(LaneY(i));
            context.DrawLine(pen, new Point(TimelineX, y), new Point(TimelineX + TimelineWidth, y));
        }
    }

    private void RenderBands(DrawingContext context, int first, int last)
    {
        var neutral = ProTheme.Background.ControlHover;
        var borderPen = new Pen(new SolidColorBrush(ProTheme.Border.Default), 1);
        var markerPen = new Pen(new SolidColorBrush(ProTheme.Text.Primary), 2);

        for (var i = first; i <= last; i++)
        {
            var lane = _model!.Lanes[i];
            var top = LaneY(i) + BandMarginTop;

            foreach (var band in lane.Bands)
            {
                var clipped = RosterEngine.Clip(band.Start, band.End, lane.WindowStart, LaneSpan);
                if (clipped is null) continue;

                var rect = HorizontalRect(lane, clipped.Value.Start, clipped.Value.End, top, BandHeight);
                context.FillRectangle(new SolidColorBrush(band.Color ?? neutral), rect);
                context.DrawRectangle(borderPen, rect);

                RenderBandMarkers(context, lane, band, top, markerPen);
                RenderBandLabels(context, band, rect);
            }
        }
    }

    /// <summary>
    /// Crochets marquant l'étendue réelle, quand la bande peinte est plus large.
    /// </summary>
    private void RenderBandMarkers(DrawingContext context, RosterLane lane, RosterBand band, double top, Pen pen)
    {
        if (band.MarkerStart is null || band.MarkerEnd is null) return;

        var clipped = RosterEngine.Clip(band.MarkerStart.Value, band.MarkerEnd.Value, lane.WindowStart, LaneSpan);
        if (clipped is null) return;

        var rect = HorizontalRect(lane, clipped.Value.Start, clipped.Value.End, top, BandHeight);
        const double arm = 6;

        // « [ » puis « ] » : trois traits chacun, comme la frise d'origine.
        foreach (var (x, direction) in new[] { (rect.Left, 1.0), (rect.Right, -1.0) })
        {
            var snapped = SnapX(x);
            context.DrawLine(pen, new Point(snapped, rect.Top), new Point(snapped, rect.Bottom));
            context.DrawLine(pen, new Point(snapped, rect.Top), new Point(snapped + arm * direction, rect.Top));
            context.DrawLine(pen, new Point(snapped, rect.Bottom), new Point(snapped + arm * direction, rect.Bottom));
        }
    }

    /// <summary>
    /// Trois libellés à trois ancrages : le type dans la bande, les heures dessous.
    /// </summary>
    private void RenderBandLabels(DrawingContext context, RosterBand band, Rect rect)
    {
        if (band.Label is { Length: > 0 })
        {
            var label = CreateText(band.Label, ProTheme.Text.Primary, 10);

            if (label.Width + 6 <= rect.Width)
            {
                context.DrawText(label, Crisp.Snap(new Point(rect.Left + 3, rect.Top + 2)));
            }
        }

        var start = band.StartLabel is { Length: > 0 }
            ? CreateText(band.StartLabel, ProTheme.Text.Secondary, 9)
            : null;

        var end = band.EndLabel is { Length: > 0 }
            ? CreateText(band.EndLabel, ProTheme.Text.Secondary, 9)
            : null;

        // Les deux heures se chevauchaient dès que la bande devenait étroite (« 09:0017:00 »).
        // Deux libellés qui se touchent ne valent pas mieux qu'aucun : on ne garde que le début.
        if (start is not null && end is not null && start.Width + end.Width + 12 > rect.Width)
        {
            end = null;
        }

        if (start is not null && start.Width + 6 <= rect.Width)
        {
            context.DrawText(start, Crisp.Snap(new Point(rect.Left + 3, rect.Bottom + 1)));
        }

        if (end is not null)
        {
            context.DrawText(end, Crisp.Snap(new Point(rect.Right - end.Width - 3, rect.Bottom + 1)));
        }
    }

    private void RenderBars(DrawingContext context, int first, int last)
    {
        var neutral = ProTheme.Accent.Success;

        for (var i = first; i <= last; i++)
        {
            var lane = _model!.Lanes[i];
            var top = LaneY(i) + BandMarginTop + BandHeight - BarHeight - BarMarginBottom;

            foreach (var bar in lane.Bars)
            {
                var clipped = RosterEngine.Clip(bar.Start, bar.End, lane.WindowStart, LaneSpan);
                if (clipped is null) continue;

                var rect = HorizontalRect(lane, clipped.Value.Start, clipped.Value.End, top, BarHeight);
                context.FillRectangle(new SolidColorBrush(bar.Color ?? neutral), rect);
            }
        }
    }

    private void RenderBlocks(DrawingContext context, int first, int last)
    {
        // Un bloc sans couleur est un bloc que le métier n'a pas voulu classer : il se peint
        // en surface neutre cernée, pas en couleur d'accent. Une frise entière en accent
        // crierait une importance que personne n'a déclarée.
        var neutral = ProTheme.Background.Control;
        var neutralPen = new Pen(new SolidColorBrush(ProTheme.Border.Strong), 1);
        var selectionPen = new Pen(new SolidColorBrush(ProTheme.Border.Focused), 2);
        var hoverPen = new Pen(new SolidColorBrush(ProTheme.Accent.Primary), 1.5);

        for (var i = first; i <= last; i++)
        {
            var lane = _model!.Lanes[i];
            var laneTop = LaneY(i);

            foreach (var block in lane.Blocks)
            {
                var rect = BlockRect(lane, block, laneTop);
                if (rect is null) continue;

                context.FillRectangle(new SolidColorBrush(block.Color ?? neutral), rect.Value);

                if (block.Color is null)
                {
                    context.DrawRectangle(neutralPen, rect.Value);
                }

                // Survol : un voile clair par-dessus la teinte métier, comme la frise
                // d'origine — plutôt qu'une seconde couleur qui trahirait le statut.
                if (ReferenceEquals(block, _hoveredBlock))
                {
                    context.FillRectangle(new SolidColorBrush(Colors.White, 0.22), rect.Value);
                    context.DrawRectangle(hoverPen, rect.Value);
                }

                if (ReferenceEquals(block, _selectedBlock))
                {
                    context.DrawRectangle(selectionPen, rect.Value);
                }

                RenderBlockLabel(context, block, rect.Value);
            }
        }
    }

    /// <summary>
    /// Étiquette d'un bloc, <b>omise si elle ne tient pas</b> : mieux vaut un bloc nu qu'un
    /// texte qui déborde sur le voisin.
    /// </summary>
    private static void RenderBlockLabel(DrawingContext context, RosterBlock block, Rect rect)
    {
        if (block.Label is not { Length: > 0 }) return;

        var label = CreateText(
            block.Label,
            block.Color is null ? ProTheme.Text.Primary : ProTheme.Text.OnAccent,
            9);
        if (label.Width + 4 > rect.Width) return;

        context.DrawText(label, Crisp.Snap(new Point(
            rect.Left + (rect.Width - label.Width) / 2,
            rect.Top + (rect.Height - label.Height) / 2)));
    }

    private void RenderAdorners(DrawingContext context, int first, int last)
    {
        if (LaneAdorner is null) return;

        for (var i = first; i <= last; i++)
        {
            LaneAdorner(context, _model!.Lanes[i], new Rect(TimelineX, LaneY(i), TimelineWidth, RowHeight));
        }
    }

    private void RenderNowLine(DrawingContext context, int first, int last)
    {
        if (!ShowNowLine) return;

        var now = NowInstant;
        var pen = new Pen(new SolidColorBrush(ProTheme.Accent.Error), 2);

        for (var i = first; i <= last; i++)
        {
            var lane = _model!.Lanes[i];

            if (now < lane.WindowStart || now >= lane.WindowStart + LaneSpan) continue;

            var x = SnapX(TimelineX + Axis.ToX(now - lane.WindowStart));
            context.DrawLine(pen, new Point(x, LaneY(i)), new Point(x, LaneY(i) + RowHeight));
        }
    }

    private void RenderGutter(DrawingContext context, int first, int last)
    {
        context.FillRectangle(
            new SolidColorBrush(ProTheme.Background.Panel),
            new Rect(0, HeaderHeight, GutterWidth, RowsViewport.Height));

        var separator = new Pen(new SolidColorBrush(ProTheme.Border.Default), 1);
        var x = SnapX(GutterWidth - 0.5);
        context.DrawLine(separator, new Point(x, HeaderHeight), new Point(x, RowsViewport.Bottom));

        for (var i = first; i <= last; i++)
        {
            var lane = _model!.Lanes[i];
            var y = LaneY(i);

            var label = CreateText(
                lane.Label,
                lane.HasWarning ? ProTheme.Text.Warning : ProTheme.Text.Primary,
                11);

            context.DrawText(label, Crisp.Snap(new Point(GutterPadding, y + 4)));

            if (lane.SubLabel is { Length: > 0 })
            {
                var sub = CreateText(lane.SubLabel, ProTheme.Text.Secondary, 9);
                context.DrawText(sub, Crisp.Snap(new Point(GutterPadding, y + 4 + label.Height)));
            }
        }
    }

    private void RenderHeader(DrawingContext context)
    {
        var header = new Rect(0, 0, Bounds.Width, HeaderHeight);
        context.FillRectangle(new SolidColorBrush(ProTheme.Background.Toolbar), header);

        var gridPen = new Pen(new SolidColorBrush(ProTheme.Border.Subtle), 1);
        var borderPen = new Pen(new SolidColorBrush(ProTheme.Border.Default), 1);

        using (context.PushClip(new Rect(TimelineX, 0, TimelineWidth, HeaderHeight)))
        {
            var twoLevels = LaneSpan > RosterEngine.Day;
            var minorTop = twoLevels ? HeaderHeight / 2 : 0;

            if (twoLevels)
            {
                foreach (var tick in Axis.MajorTicks(TimelineWidth, LaneSpan))
                {
                    var x = SnapX(TimelineX + tick.X);
                    context.DrawLine(borderPen, new Point(x, 0), new Point(x, HeaderHeight));
                    context.DrawText(
                        CreateText(tick.Label, ProTheme.Text.Primary, 11),
                        Crisp.Snap(new Point(x + 4, 2)));
                }
            }

            foreach (var tick in Axis.MinorTicks(TimelineWidth, LaneSpan))
            {
                var x = SnapX(TimelineX + tick.X);
                context.DrawLine(gridPen, new Point(x, minorTop), new Point(x, HeaderHeight));

                var label = CreateText(tick.Label, ProTheme.Text.Secondary, 12);

                // Centré sur la graduation. S'il déborde sur la gouttière, on ne le dessine
                // pas : le recaler à droite le ferait chevaucher la graduation suivante, et
                // deux libellés collés se lisent plus mal qu'un libellé manquant.
                var labelX = x - label.Width / 2;

                if (labelX >= TimelineX)
                {
                    context.DrawText(label, Crisp.Snap(new Point(labelX, minorTop + 3)));
                }
            }
        }

        var bottom = SnapY(HeaderHeight - 0.5);
        context.DrawLine(borderPen, new Point(0, bottom), new Point(Bounds.Width, bottom));
    }

    // ═══════════════════════════════════════════════════════════════
    // ÉVÉNEMENTS — « le contrôle affiche, le métier décide »
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Clic simple sur un bloc.</summary>
    public event EventHandler<RosterBlockEventArgs>? BlockClicked;

    /// <summary>Double-clic sur un bloc.</summary>
    public event EventHandler<RosterBlockEventArgs>? BlockDoubleClicked;

    /// <summary>Clic droit sur un bloc : à l'application d'ouvrir son menu.</summary>
    public event EventHandler<RosterBlockEventArgs>? BlockContextMenuRequested;

    /// <summary>Clic simple sur une voie, hors de tout bloc.</summary>
    public event EventHandler<RosterLaneEventArgs>? LaneClicked;

    /// <summary>Maj + clic sur une voie : la demande de bilan de la frise d'origine.</summary>
    public event EventHandler<RosterLaneEventArgs>? LaneSummaryRequested;

    /// <summary>Double-clic sur une zone vide : l'instant visé est dans les arguments.</summary>
    public event EventHandler<RosterSlotEventArgs>? EmptySlotDoubleClicked;

    /// <summary>
    /// Déplacement sur le point d'être appliqué. <b>Annulable</b>, et les dates comme la voie
    /// cible sont <b>modifiables</b> : le métier corrige au lieu de seulement refuser.
    /// </summary>
    public event EventHandler<RosterBlockMoveEventArgs>? BlockMoving;

    /// <summary>Déplacement appliqué.</summary>
    public event EventHandler<RosterBlockMoveEventArgs>? BlockMoved;

    // ═══════════════════════════════════════════════════════════════
    // INTERACTION
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Bloc sous le point, calculé <b>géométriquement</b>.
    /// </summary>
    /// <remarks>
    /// La frise d'origine remplissait ses listes de régions <i>pendant</i> le <c>Render</c> :
    /// avant la première peinture, elle ne répondait à aucun clic. On recalcule ici, ce qui
    /// coûte le parcours des seules voies visibles.
    /// </remarks>
    private (RosterLane Lane, RosterBlock Block)? BlockAt(Point point)
    {
        if (_model is null || point.X < GutterWidth || point.Y < HeaderHeight) return null;

        var (first, last) = VisibleLaneRange();

        for (var i = last; i >= first; i--)
        {
            var lane = _model.Lanes[i];
            var top = LaneY(i);

            // À rebours : le dernier peint l'emporte, comme dans la frise d'origine.
            for (var b = lane.Blocks.Count - 1; b >= 0; b--)
            {
                var rect = BlockRect(lane, lane.Blocks[b], top);

                if (rect?.Contains(point) == true)
                {
                    return (lane, lane.Blocks[b]);
                }
            }
        }

        return null;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var point = e.GetPosition(this);

        if (_dragArmed && !_dragging && Distance(point, _dragOrigin) > DragThreshold)
        {
            _dragging = true;
            Cursor = DragCursor;
        }

        if (_dragging)
        {
            UpdateDrag(point);
            InvalidateVisual();
            base.OnPointerMoved(e);
            return;
        }

        var hit = BlockAt(point);
        var lane = LaneAt(point);

        if (!ReferenceEquals(hit?.Block, _hoveredBlock) || !ReferenceEquals(lane, _hoveredLane))
        {
            _hoveredBlock = hit?.Block;
            _hoveredLane = lane;

            // Sur des blocs de 12 px, l'infobulle porte les horaires qui ne tiennent pas dedans.
            ToolTip.SetTip(this, BuildTooltip(hit, lane));
            Cursor = _hoveredBlock is not null ? HandCursor : ArrowCursor;

            InvalidateVisual();
        }

        base.OnPointerMoved(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _hoveredBlock = null;
        _hoveredLane = null;
        ToolTip.SetTip(this, null);
        Cursor = ArrowCursor;
        InvalidateVisual();

        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);
        var properties = e.GetCurrentPoint(this).Properties;
        var hit = BlockAt(point);
        var lane = LaneAt(point);

        if (properties.IsRightButtonPressed)
        {
            if (hit is not null)
            {
                SelectedBlock = hit.Value.Block;
                BlockContextMenuRequested?.Invoke(this, new RosterBlockEventArgs(hit.Value.Block, hit.Value.Lane, point));
            }

            base.OnPointerPressed(e);
            return;
        }

        if (!properties.IsLeftButtonPressed || lane is null)
        {
            base.OnPointerPressed(e);
            return;
        }

        Focus();

        // Maj + clic : le bilan de journée de la frise d'origine.
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            LaneSummaryRequested?.Invoke(this, new RosterLaneEventArgs(lane));
            base.OnPointerPressed(e);
            return;
        }

        if (e.ClickCount == 2)
        {
            if (hit is not null)
            {
                BlockDoubleClicked?.Invoke(this, new RosterBlockEventArgs(hit.Value.Block, hit.Value.Lane, point));
            }
            else
            {
                EmptySlotDoubleClicked?.Invoke(this, new RosterSlotEventArgs(lane, SnapTime(TimeAtIn(lane, point))));
            }

            base.OnPointerPressed(e);
            return;
        }

        if (hit is not null)
        {
            SelectedBlock = hit.Value.Block;

            // Le clic n'est PAS émis ici : on ne sait pas encore si c'est un clic ou le début
            // d'un glisser. La frise d'origine tranchait avec un System.Threading.Timer de
            // 250 ms, jamais annulé ni libéré. Le relâchement suffit.
            if (!IsReadOnly)
            {
                ArmDrag(hit.Value.Lane, hit.Value.Block, point);
                e.Pointer.Capture(this);
            }
        }
        else
        {
            LaneClicked?.Invoke(this, new RosterLaneEventArgs(lane));
        }

        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_dragging && _dragBlock is not null && _dragSourceLane is not null && _dragTargetLane is not null)
        {
            CommitDrag();
        }
        else if (_dragArmed && _dragBlock is not null && _dragSourceLane is not null)
        {
            BlockClicked?.Invoke(this, new RosterBlockEventArgs(_dragBlock, _dragSourceLane));
        }

        ResetDrag();
        e.Pointer.Capture(null);
        Cursor = _hoveredBlock is not null ? HandCursor : ArrowCursor;
        InvalidateVisual();

        base.OnPointerReleased(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            Axis.ZoomAt(e.GetPosition(this).X - TimelineX, e.Delta.Y > 0 ? 1.25 : 0.8);
        }
        else
        {
            _verticalOffset = Math.Clamp(_verticalOffset - e.Delta.Y * RowHeight * 2, 0, Math.Max(0, _vScroll.Maximum));
        }

        UpdateScrollBars();
        InvalidateVisual();
        e.Handled = true;

        base.OnPointerWheelChanged(e);
    }

    // ── Glisser ────────────────────────────────────────────────────

    private void ArmDrag(RosterLane lane, RosterBlock block, Point point)
    {
        _dragArmed = true;
        _dragging = false;
        _dragBlock = block;
        _dragSourceLane = lane;
        _dragTargetLane = lane;
        _dragOrigin = point;
        _dragStart = block.Start;
        _dragEnd = block.End;

        // Où l'utilisateur a saisi le bloc : sans cela, le bloc sauterait sous le curseur.
        _dragGrab = TimeAtIn(lane, point) - block.Start;
    }

    private void UpdateDrag(Point point)
    {
        if (_dragBlock is null) return;

        var target = LaneAt(point) ?? _dragTargetLane;
        if (target is null) return;

        _dragTargetLane = target;

        var duration = _dragBlock.End - _dragBlock.Start;
        _dragStart = SnapTime(TimeAtIn(target, point) - _dragGrab);
        _dragEnd = _dragStart + duration;
    }

    /// <summary>
    /// Applique le déplacement, après avoir laissé le métier le corriger ou le refuser.
    /// </summary>
    /// <remarks>
    /// Rien n'est muté pendant le glisser : le modèle ne bouge qu'au relâchement, et une
    /// seule fois. C'est ce qui rend l'annulation par <c>BlockMoving</c> réellement propre.
    /// </remarks>
    private void CommitDrag()
    {
        var block = _dragBlock!;
        var source = _dragSourceLane!;

        var args = new RosterBlockMoveEventArgs(block, source, _dragTargetLane!, _dragStart, _dragEnd);
        BlockMoving?.Invoke(this, args);

        if (args.Cancel) return;

        block.Start = args.NewStart;
        block.End = args.NewEnd;

        if (!ReferenceEquals(args.TargetLane, source))
        {
            source.Blocks.Remove(block);
            args.TargetLane.Blocks.Add(block);
        }

        BlockMoved?.Invoke(this, args);
        _model?.Touch();
    }

    private void ResetDrag()
    {
        _dragArmed = false;
        _dragging = false;
        _dragBlock = null;
        _dragSourceLane = null;
        _dragTargetLane = null;
    }

    private void RenderDragGhost(DrawingContext context)
    {
        if (!_dragging || _dragBlock is null || _dragTargetLane is null || _model is null) return;

        var index = _model.Lanes.IndexOf(_dragTargetLane);
        if (index < 0) return;

        var clipped = RosterEngine.Clip(_dragStart, _dragEnd, _dragTargetLane.WindowStart, LaneSpan);
        if (clipped is null) return;

        var available = _dragBlock.Track > 0 ? BandHeight - BarHeight - BarMarginBottom : BandHeight;
        var top = LaneY(index) + BandMarginTop + (available - BlockHeight) / 2;
        var rect = HorizontalRect(_dragTargetLane, clipped.Value.Start, clipped.Value.End, top, BlockHeight);

        var accent = ProTheme.Accent.Primary;
        context.FillRectangle(new SolidColorBrush(accent, 0.45), rect);
        context.DrawRectangle(
            new Pen(new SolidColorBrush(accent), 2, new DashStyle(DashStyle.Dash.Dashes, 0)),
            rect);

        var label = CreateText(_dragStart.ToString("dd/MM HH:mm"), ProTheme.Text.Primary, 10);
        context.DrawText(label, Crisp.Snap(new Point(rect.Left, rect.Bottom + 2)));
    }

    // ── Aides ──────────────────────────────────────────────────────

    /// <summary>Instant sous le point, résolu dans une voie donnée.</summary>
    private DateTime TimeAtIn(RosterLane lane, Point point)
        => lane.WindowStart + Axis.ToOffset(point.X - GutterWidth);

    private static DateTime SnapTime(DateTime value)
        => new(value.Ticks / SnapStep.Ticks * SnapStep.Ticks, value.Kind);

    private static double Distance(Point a, Point b)
        => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    private static string? BuildTooltip((RosterLane Lane, RosterBlock Block)? hit, RosterLane? lane)
    {
        if (hit is not null)
        {
            var block = hit.Value.Block;
            var header = block.Label is { Length: > 0 } ? block.Label + "\n" : "";

            return $"{header}{block.Start:dd/MM HH:mm} → {block.End:HH:mm}";
        }

        if (lane?.SubLabel is { Length: > 0 })
        {
            return $"{lane.Label} — {lane.SubLabel}";
        }

        return null;
    }

    // ── Géométrie interne ──────────────────────────────────────────

    private Rect? BlockRect(RosterLane lane, RosterBlock block, double laneTop)
    {
        var clipped = RosterEngine.Clip(block.Start, block.End, lane.WindowStart, LaneSpan);
        if (clipped is null) return null;

        // Piste 1 : au-dessus de la barre, quand une barre occupe le bas de la bande.
        var available = block.Track > 0 ? BandHeight - BarHeight - BarMarginBottom : BandHeight;
        var top = laneTop + BandMarginTop + (available - BlockHeight) / 2;

        return HorizontalRect(lane, clipped.Value.Start, clipped.Value.End, top, BlockHeight);
    }

    private Rect HorizontalRect(RosterLane lane, DateTime start, DateTime end, double top, double height)
    {
        var x1 = TimelineX + Axis.ToX(start - lane.WindowStart);
        var x2 = TimelineX + Axis.ToX(end - lane.WindowStart);

        return new Rect(x1, top, Math.Max(1, x2 - x1), height);
    }

    /// <summary>
    /// Accrochage au pixel d'une abscisse. <c>Crisp.Snap</c> ne prend qu'un <c>Point</c> ;
    /// à 1 440 px/jour, les lignes verticales de la grille horaire dérivent autant que le
    /// texte si on ne les cale pas.
    /// </summary>
    private static double SnapX(double x) => Crisp.Snap(new Point(x, 0)).X;

    /// <inheritdoc cref="SnapX"/>
    private static double SnapY(double y) => Crisp.Snap(new Point(0, y)).Y;

    private static bool IsWeekend(DateTime date)
        => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    private static FormattedText CreateText(string text, Color color, double size)
        => new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(ProTheme.Typography.FontFamily), size, new SolidColorBrush(color));
}

/// <summary>Arguments portant un bloc, sa voie et la position du pointeur.</summary>
/// <remarks>
/// <see cref="Position"/> évite à l'hôte de recalculer la géométrie pour placer un menu ou
/// une fenêtre surgissante — c'est précisément ce que faisait la frise d'origine, avec trois
/// constantes recopiées sur quatre, et le zoom ignoré.
/// </remarks>
public class RosterBlockEventArgs : EventArgs
{
    public RosterBlock Block { get; }
    public RosterLane Lane { get; }

    /// <summary>Position du pointeur, en coordonnées du contrôle.</summary>
    public Point Position { get; }

    public RosterBlockEventArgs(RosterBlock block, RosterLane lane, Point position = default)
    {
        Block = block;
        Lane = lane;
        Position = position;
    }
}

/// <summary>Arguments portant une voie.</summary>
public class RosterLaneEventArgs : EventArgs
{
    public RosterLane Lane { get; }

    public RosterLaneEventArgs(RosterLane lane) => Lane = lane;
}

/// <summary>Arguments d'un créneau vide : la voie et l'instant visé, déjà aimanté.</summary>
public class RosterSlotEventArgs : EventArgs
{
    public RosterLane Lane { get; }
    public DateTime Time { get; }

    public RosterSlotEventArgs(RosterLane lane, DateTime time)
    {
        Lane = lane;
        Time = time;
    }
}

/// <summary>
/// Arguments d'un déplacement de bloc (annulable).
/// </summary>
/// <remarks>
/// <see cref="NewStart"/>, <see cref="NewEnd"/> et <see cref="TargetLane"/> sont
/// <b>modifiables</b> : un gestionnaire qui connaît la règle métier corrige la proposition du
/// contrôle plutôt que de la rejeter en bloc. Poser <c>Cancel</c> annule le déplacement.
///
/// <para><see cref="TargetLane"/> est ce qui distingue une frise multi-voies d'un simple
/// agenda : déplacer un bloc d'une ressource à une autre est le geste central.</para>
/// </remarks>
public class RosterBlockMoveEventArgs : System.ComponentModel.CancelEventArgs
{
    public RosterBlock Block { get; }
    public RosterLane SourceLane { get; }
    public RosterLane TargetLane { get; set; }
    public DateTime NewStart { get; set; }
    public DateTime NewEnd { get; set; }

    public RosterBlockMoveEventArgs(
        RosterBlock block,
        RosterLane sourceLane,
        RosterLane targetLane,
        DateTime newStart,
        DateTime newEnd)
    {
        Block = block;
        SourceLane = sourceLane;
        TargetLane = targetLane;
        NewStart = newStart;
        NewEnd = newEnd;
    }
}
