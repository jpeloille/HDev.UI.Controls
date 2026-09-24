using System.Globalization;

namespace ProControls.Controls;

/// <summary>
/// Axe temporel de la frise : transformation temps↔pixels en résolution <b>horaire</b>, zoom
/// continu, graduations à deux niveaux. Logique pure (testée).
/// </summary>
/// <remarks>
/// <b>Pourquoi ce type existe, alors que <see cref="GanttTimeAxis"/> lui ressemble.</b>
/// L'axe du Gantt calcule <c>(date.Date - Origin).TotalDays * PixelsPerDay</c> : le
/// <c>.Date</c> écrase l'heure du jour, donc 08:00 et 14:30 tombent au même pixel. Un roster
/// est précisément intra-journée. S'y ajoutent une borne de zoom à 80 px/jour — une frise en
/// tient 1 440 — et des graduations qui ne descendent jamais sous la journée. Corriger le
/// Gantt sur place décalerait ses barres et casserait un test qui verrouille la borne : on le
/// laisse tranquille.
///
/// <para>L'axe travaille en <b>décalage depuis <see cref="RosterLane.WindowStart"/></b>, pas
/// en date absolue. C'est ce qui laisse une voie être une journée chez l'un et une ressource
/// chez l'autre, avec le même code de rendu.</para>
/// </remarks>
public class RosterTimeAxis
{
    private double _pixelsPerHour = 60;

    public const double MinPixelsPerHour = 2;
    public const double MaxPixelsPerHour = 240;

    /// <summary>Décalage de vue en pixels (défilement horizontal).</summary>
    public double ViewOffset { get; set; }

    /// <summary>Zoom : largeur d'une heure en pixels, borné [2 ; 240].</summary>
    public double PixelsPerHour
    {
        get => _pixelsPerHour;
        set => _pixelsPerHour = Math.Clamp(value, MinPixelsPerHour, MaxPixelsPerHour);
    }

    /// <summary>Largeur d'une journée entière, en pixels.</summary>
    public double PixelsPerDay => _pixelsPerHour * 24;

    /// <summary>
    /// Mise en forme des libellés de graduation. Null = heure du jour sur deux chiffres.
    /// </summary>
    /// <remarks>
    /// L'axe ne connaît que des décalages : il ne peut pas deviner la date absolue. Une
    /// application qui affiche plusieurs jours sur une même voie pose ici son propre format.
    /// </remarks>
    public Func<TimeSpan, string>? LabelFormatter { get; set; }

    /// <summary>Position X du décalage donné.</summary>
    public double ToX(TimeSpan offset) => offset.TotalHours * _pixelsPerHour - ViewOffset;

    /// <summary>Décalage au pixel X de la vue.</summary>
    public TimeSpan ToOffset(double x) => TimeSpan.FromHours((x + ViewOffset) / _pixelsPerHour);

    /// <summary>Zoom centré : ajuste l'échelle en gardant l'instant sous <paramref name="pivotX"/> immobile.</summary>
    public void ZoomAt(double pivotX, double factor)
    {
        var pivot = ToOffset(pivotX);
        PixelsPerHour = _pixelsPerHour * factor;
        ViewOffset = pivot.TotalHours * _pixelsPerHour - pivotX;
    }

    /// <summary>Fenêtre visible pour une largeur de vue donnée, débordée d'une heure de chaque côté.</summary>
    public (TimeSpan Start, TimeSpan End) VisibleRange(double viewWidth)
        => (ToOffset(0) - TimeSpan.FromHours(1), ToOffset(viewWidth) + TimeSpan.FromHours(1));

    // ═══════════════════════════════════════════════════════════════
    // GRADUATIONS
    // ═══════════════════════════════════════════════════════════════

    public enum TickTier
    {
        /// <summary>Zoom fort : jours en haut, quarts d'heure en bas</summary>
        DaysQuarters,
        /// <summary>Zoom moyen : jours en haut, heures en bas</summary>
        DaysHours,
        /// <summary>Zoom faible : jours en haut, tranches de 3 h en bas</summary>
        DaysThreeHours,
        /// <summary>Zoom très faible : jours en haut et en bas</summary>
        Days
    }

    public TickTier CurrentTier => _pixelsPerHour switch
    {
        >= 120 => TickTier.DaysQuarters,
        >= 24 => TickTier.DaysHours,
        >= 6 => TickTier.DaysThreeHours,
        _ => TickTier.Days
    };

    public readonly record struct Tick(TimeSpan Offset, double X, string Label);

    /// <summary>Pas du niveau bas, selon le zoom.</summary>
    public TimeSpan MinorStep => CurrentTier switch
    {
        TickTier.DaysQuarters => TimeSpan.FromMinutes(15),
        TickTier.DaysHours => TimeSpan.FromHours(1),
        TickTier.DaysThreeHours => TimeSpan.FromHours(3),
        _ => TimeSpan.FromDays(1)
    };

    /// <summary>
    /// Graduations du niveau bas, bornées à <paramref name="span"/> — l'étendue de la voie.
    /// </summary>
    public IEnumerable<Tick> MinorTicks(double viewWidth, TimeSpan span)
    {
        var step = MinorStep;
        var (from, to) = VisibleRange(viewWidth);

        if (from < TimeSpan.Zero)
        {
            from = TimeSpan.Zero;
        }

        if (to > span)
        {
            to = span;
        }

        var first = Math.Floor(from.Ticks / (double)step.Ticks);

        for (var offset = TimeSpan.FromTicks((long)first * step.Ticks); offset <= to; offset += step)
        {
            if (offset < TimeSpan.Zero)
            {
                continue;
            }

            yield return new Tick(offset, ToX(offset), Label(offset));
        }
    }

    /// <summary>Graduations du niveau haut : les journées.</summary>
    public IEnumerable<Tick> MajorTicks(double viewWidth, TimeSpan span)
    {
        var (from, to) = VisibleRange(viewWidth);

        if (from < TimeSpan.Zero)
        {
            from = TimeSpan.Zero;
        }

        if (to > span)
        {
            to = span;
        }

        var firstDay = (int)Math.Floor(from.TotalDays);

        for (var day = Math.Max(0, firstDay); ; day++)
        {
            var offset = TimeSpan.FromDays(day);

            if (offset > to)
            {
                yield break;
            }

            yield return new Tick(offset, ToX(offset), LabelFormatter?.Invoke(offset) ?? $"J+{day}");
        }
    }

    private string Label(TimeSpan offset)
    {
        if (LabelFormatter is not null)
        {
            return LabelFormatter(offset);
        }

        if (CurrentTier == TickTier.Days)
        {
            return $"J+{(int)offset.TotalDays}";
        }

        var hour = (int)Math.Floor(offset.TotalHours) % 24;

        return CurrentTier == TickTier.DaysQuarters
            ? $"{hour:00}:{offset.Minutes:00}"
            : hour.ToString("00", CultureInfo.InvariantCulture);
    }
}
