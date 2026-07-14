using Avalonia.Media;

namespace ProControls.Controls;

/// <summary>Fréquence de récurrence (v1 : motifs simples)</summary>
public enum ScheduleRecurrenceKind
{
    None,
    Daily,
    /// <summary>Hebdomadaire sur les jours de DaysOfWeek</summary>
    Weekly,
    /// <summary>Mensuelle au jour du mois de la date de départ</summary>
    Monthly
}

/// <summary>Règle de récurrence d'un événement</summary>
public class ScheduleRecurrence
{
    public ScheduleRecurrenceKind Kind { get; set; } = ScheduleRecurrenceKind.None;

    /// <summary>Intervalle (toutes les N unités), ≥ 1</summary>
    public int Interval { get; set; } = 1;

    /// <summary>Jours actifs (Weekly) ; vide = le jour de la date de départ</summary>
    public HashSet<DayOfWeek> DaysOfWeek { get; } = new();

    /// <summary>Fin de la récurrence (incluse) ; null = sans fin</summary>
    public DateTime? Until { get; set; }
}

/// <summary>
/// Événement d'agenda (équivalent Appointment) : ponctuel ou récurrent.
/// Les occurrences d'un récurrent sont générées à l'affichage — l'événement
/// maître reste unique dans la collection.
/// </summary>
public class ScheduleEvent
{
    public string Subject { get; set; } = "";
    public string? Location { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool AllDay { get; set; }
    public Color? Color { get; set; }
    public ScheduleRecurrence Recurrence { get; set; } = new();
    public object? Tag { get; set; }

    public bool IsRecurring => Recurrence.Kind != ScheduleRecurrenceKind.None;

    public TimeSpan Duration => End - Start;

    public ScheduleEvent() { }

    public ScheduleEvent(string subject, DateTime start, DateTime end)
    {
        Subject = subject;
        Start = start;
        End = end;
    }
}

/// <summary>
/// Occurrence concrète affichée (un récurrent en produit plusieurs) —
/// pointe vers son événement maître
/// </summary>
public readonly record struct ScheduleOccurrence(ScheduleEvent Master, DateTime Start, DateTime End)
{
    public bool AllDay => Master.AllDay;
}

/// <summary>
/// Moteur d'agenda : expansion des récurrences sur une plage, affectation de
/// couloirs pour les chevauchements, plages des vues. Logique pure, testée.
/// </summary>
public static class ScheduleEngine
{
    // ═══════════════════════════════════════════════════════════════
    // EXPANSION DES RÉCURRENCES
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Occurrences de l'événement croisant [rangeStart, rangeEnd) — un
    /// ponctuel en produit 0 ou 1, un récurrent autant que nécessaire
    /// </summary>
    public static IEnumerable<ScheduleOccurrence> Expand(ScheduleEvent evt,
        DateTime rangeStart, DateTime rangeEnd)
    {
        if (!evt.IsRecurring)
        {
            if (evt.Start < rangeEnd && evt.End > rangeStart)
                yield return new ScheduleOccurrence(evt, evt.Start, evt.End);
            yield break;
        }

        var rule = evt.Recurrence;
        var interval = Math.Max(1, rule.Interval);
        var duration = evt.Duration;
        var until = rule.Until?.Date.AddDays(1) ?? DateTime.MaxValue;
        var hardStop = rangeEnd < until ? rangeEnd : until;

        switch (rule.Kind)
        {
            case ScheduleRecurrenceKind.Daily:
            {
                var date = evt.Start;
                while (date < hardStop)
                {
                    if (date + duration > rangeStart)
                        yield return new ScheduleOccurrence(evt, date, date + duration);
                    date = date.AddDays(interval);
                }
                break;
            }

            case ScheduleRecurrenceKind.Weekly:
            {
                var days = rule.DaysOfWeek.Count > 0
                    ? rule.DaysOfWeek
                    : new HashSet<DayOfWeek> { evt.Start.DayOfWeek };

                // Balayer semaine par semaine depuis la semaine de départ
                var weekStart = StartOfWeek(evt.Start.Date);
                var week = 0;
                while (true)
                {
                    var currentWeek = weekStart.AddDays(week * 7 * interval);
                    if (currentWeek >= hardStop) break;

                    foreach (var day in Enumerable.Range(0, 7))
                    {
                        var date = currentWeek.AddDays(day);
                        if (!days.Contains(date.DayOfWeek)) continue;

                        var start = date + evt.Start.TimeOfDay;
                        if (start < evt.Start) continue; // avant la première
                        if (start >= hardStop) continue;
                        if (start + duration > rangeStart)
                            yield return new ScheduleOccurrence(evt, start, start + duration);
                    }
                    week++;
                }
                break;
            }

            case ScheduleRecurrenceKind.Monthly:
            {
                var month = new DateTime(evt.Start.Year, evt.Start.Month, 1);
                var dayOfMonth = evt.Start.Day;
                while (month < hardStop)
                {
                    if (dayOfMonth <= DateTime.DaysInMonth(month.Year, month.Month))
                    {
                        var start = new DateTime(month.Year, month.Month, dayOfMonth)
                            + evt.Start.TimeOfDay;
                        if (start >= evt.Start && start < hardStop && start + duration > rangeStart)
                            yield return new ScheduleOccurrence(evt, start, start + duration);
                    }
                    month = month.AddMonths(interval);
                }
                break;
            }
        }
    }

    /// <summary>Toutes les occurrences d'une collection sur une plage, triées</summary>
    public static List<ScheduleOccurrence> ExpandAll(IEnumerable<ScheduleEvent> events,
        DateTime rangeStart, DateTime rangeEnd)
        => events.SelectMany(e => Expand(e, rangeStart, rangeEnd))
            .OrderBy(o => o.Start).ThenByDescending(o => o.End)
            .ToList();

    // ═══════════════════════════════════════════════════════════════
    // COULOIRS DE CHEVAUCHEMENT (rendu côte à côte)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Affecte un couloir à chaque occurrence d'un même jour : les événements
    /// qui se chevauchent sont posés côte à côte (greedy, premier couloir
    /// libre). Retourne (occurrence, couloir, nombre de couloirs du groupe).
    /// </summary>
    public static List<(ScheduleOccurrence Occurrence, int Lane, int LaneCount)>
        AssignLanes(IReadOnlyList<ScheduleOccurrence> occurrences)
    {
        var result = new List<(ScheduleOccurrence, int, int)>();
        if (occurrences.Count == 0) return result;

        var sorted = occurrences.OrderBy(o => o.Start).ThenByDescending(o => o.End).ToList();

        // Groupes de chevauchement transitifs (cluster) : le nombre de
        // couloirs est partagé au sein d'un cluster
        var laneEnds = new List<DateTime>();     // fin du dernier événement par couloir
        var cluster = new List<(ScheduleOccurrence Occ, int Lane)>();
        var clusterEnd = DateTime.MinValue;

        void FlushCluster()
        {
            var count = laneEnds.Count;
            foreach (var (occ, lane) in cluster)
                result.Add((occ, lane, Math.Max(1, count)));
            cluster.Clear();
            laneEnds.Clear();
        }

        foreach (var occ in sorted)
        {
            if (cluster.Count > 0 && occ.Start >= clusterEnd)
                FlushCluster();

            // Premier couloir dont le dernier événement est terminé
            var lane = -1;
            for (int i = 0; i < laneEnds.Count; i++)
            {
                if (occ.Start >= laneEnds[i]) { lane = i; break; }
            }
            if (lane < 0)
            {
                lane = laneEnds.Count;
                laneEnds.Add(DateTime.MinValue);
            }

            laneEnds[lane] = occ.End;
            cluster.Add((occ, lane));
            if (occ.End > clusterEnd) clusterEnd = occ.End;
        }
        FlushCluster();

        return result;
    }

    // ═══════════════════════════════════════════════════════════════
    // PLAGES DES VUES
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Lundi de la semaine contenant la date</summary>
    public static DateTime StartOfWeek(DateTime date)
        => date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    /// <summary>Jours affichés par la vue (jour / semaine ouvrée / semaine)</summary>
    public static (DateTime Start, int Days) DayViewRange(DateTime displayDate, int dayCount)
        => dayCount switch
        {
            1 => (displayDate.Date, 1),
            5 => (StartOfWeek(displayDate), 5),
            _ => (StartOfWeek(displayDate), 7)
        };

    /// <summary>
    /// Grille de la vue mois : premier lundi affiché + nombre de semaines
    /// pour couvrir le mois entier
    /// </summary>
    public static (DateTime GridStart, int Weeks) MonthViewRange(DateTime displayDate)
    {
        var firstOfMonth = new DateTime(displayDate.Year, displayDate.Month, 1);
        var gridStart = StartOfWeek(firstOfMonth);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
        var weeks = (int)Math.Ceiling(((lastOfMonth - gridStart).Days + 1) / 7.0);
        return (gridStart, Math.Max(4, weeks));
    }
}
