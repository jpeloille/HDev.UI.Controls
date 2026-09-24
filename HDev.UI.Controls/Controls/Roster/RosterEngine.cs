namespace HDev.UI.Controls;

/// <summary>
/// Portion d'un élément retombant dans une seule voie.
/// </summary>
/// <param name="Start">Début de la portion.</param>
/// <param name="End">Fin de la portion.</param>
/// <param name="LaneOffset">
/// Décalage, en nombre de fenêtres, par rapport à la voie d'origine. 0 = la voie elle-même,
/// 1 = la suivante. Négatif si l'élément commence avant la fenêtre de départ.
/// </param>
public readonly record struct RosterSegment(DateTime Start, DateTime End, int LaneOffset);

/// <summary>
/// Moteur de la frise : découpage aux bornes de voie, bornage, construction des voies
/// journalières. Logique pure, testée.
/// </summary>
/// <remarks>
/// <b>Le découpage de minuit est une décision, pas un détail.</b> La frise d'origine
/// (Organizer) ne rend jamais un franchissement de minuit : elle le tronque à 24 h. HDevRoster
/// coupe — un service 22 h → 06 h se dessine en deux morceaux, fin de J et début de J+1.
/// C'est ce qu'exige un consommateur dont les voies sont des ressources et non des journées,
/// et c'est plus juste : un service de nuit tronqué se lit comme un service qui finit à
/// minuit.
/// </remarks>
public static class RosterEngine
{
    /// <summary>Fenêtre d'une voie journalière.</summary>
    public static readonly TimeSpan Day = TimeSpan.FromDays(1);

    /// <summary>
    /// Découpe <c>[start, end)</c> en portions, une par fenêtre de voie traversée.
    /// </summary>
    /// <remarks>
    /// Un intervalle vide ou inversé ne produit rien : mieux vaut ne rien peindre qu'une barre
    /// de largeur nulle, qui serait invisible mais cliquable.
    /// </remarks>
    public static IEnumerable<RosterSegment> Split(
        DateTime start,
        DateTime end,
        DateTime windowStart,
        TimeSpan window)
    {
        if (window <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(window), window, "La fenêtre doit être positive.");
        }

        if (end <= start)
        {
            yield break;
        }

        var offset = FloorDiv(start - windowStart, window);
        var cursor = start;

        while (cursor < end)
        {
            var boundary = windowStart + window * (offset + 1);
            var segmentEnd = boundary < end ? boundary : end;

            yield return new RosterSegment(cursor, segmentEnd, offset);

            cursor = segmentEnd;
            offset++;
        }
    }

    /// <summary>
    /// Borne <c>[start, end)</c> dans la fenêtre d'une voie. Null si l'intervalle n'y entre pas.
    /// </summary>
    public static RosterSegment? Clip(
        DateTime start,
        DateTime end,
        DateTime windowStart,
        TimeSpan window)
    {
        if (window <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(window), window, "La fenêtre doit être positive.");
        }

        var windowEnd = windowStart + window;
        var from = start > windowStart ? start : windowStart;
        var to = end < windowEnd ? end : windowEnd;

        return to > from ? new RosterSegment(from, to, 0) : null;
    }

    /// <summary>
    /// Construit une suite <b>continue</b> de voies journalières, journées vides comprises.
    /// </summary>
    /// <remarks>
    /// Les voies étant ordonnées par position et non par date, sauter une journée sans donnée
    /// collerait le 15 et le 17 mars l'un contre l'autre. On matérialise donc chaque jour.
    /// </remarks>
    public static List<RosterLane> BuildDailyLanes(
        DateTime firstDay,
        int dayCount,
        Func<DateTime, string> label,
        Func<DateTime, string?>? subLabel = null)
    {
        ArgumentNullException.ThrowIfNull(label);

        if (dayCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dayCount), dayCount, "Le nombre de jours ne peut pas être négatif.");
        }

        var lanes = new List<RosterLane>(dayCount);

        for (var i = 0; i < dayCount; i++)
        {
            var day = firstDay.Date.AddDays(i);

            lanes.Add(new RosterLane(day.ToString("yyyy-MM-dd"), label(day), day)
            {
                SubLabel = subLabel?.Invoke(day),
            });
        }

        return lanes;
    }

    /// <summary>
    /// Division entière vers le bas. <c>TimeSpan</c> tronque vers zéro, ce qui renverrait 0
    /// pour un élément commençant avant la fenêtre au lieu de -1.
    /// </summary>
    private static int FloorDiv(TimeSpan value, TimeSpan window)
        => (int)Math.Floor(value.Ticks / (double)window.Ticks);
}
