namespace HDev.UI.Controls;

/// <summary>
/// Moteur d'ordonnancement du Gantt : tri topologique, propagation des
/// dépendances (calage des tâches auto), chemin critique (CPM par dates,
/// marges en jours ouvrés). Logique pure, testée unitairement.
/// </summary>
public static class GanttScheduler
{
    // ═══════════════════════════════════════════════════════════════
    // CYCLES + TRI TOPOLOGIQUE
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Vrai si ajouter la dépendance predecessor → successor créerait un cycle
    /// (c.-à-d. si successor est déjà — directement ou non — un prédécesseur
    /// de predecessor)
    /// </summary>
    public static bool WouldCreateCycle(GanttTask predecessor, GanttTask successor)
    {
        var visited = new HashSet<GanttTask>();
        var stack = new Stack<GanttTask>();
        stack.Push(predecessor);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (ReferenceEquals(current, successor)) return true;
            if (!visited.Add(current)) continue;

            foreach (var dep in current.Predecessors)
                stack.Push(dep.Predecessor);
        }
        return false;
    }

    /// <summary>
    /// Tri topologique (Kahn) des tâches selon les dépendances.
    /// Retourne false si un cycle existe ; cyclic contient les tâches impliquées.
    /// </summary>
    public static bool TryTopologicalSort(IReadOnlyList<GanttTask> tasks,
        out List<GanttTask> order, out List<GanttTask> cyclic)
    {
        var taskSet = new HashSet<GanttTask>(tasks);
        var inDegree = tasks.ToDictionary(t => t, _ => 0);
        var successors = tasks.ToDictionary(t => t, _ => new List<GanttTask>());

        foreach (var task in tasks)
        {
            foreach (var dep in task.Predecessors)
            {
                if (!taskSet.Contains(dep.Predecessor)) continue;
                inDegree[task]++;
                successors[dep.Predecessor].Add(task);
            }
        }

        var queue = new Queue<GanttTask>(tasks.Where(t => inDegree[t] == 0));
        order = new List<GanttTask>(tasks.Count);

        while (queue.Count > 0)
        {
            var task = queue.Dequeue();
            order.Add(task);
            foreach (var successor in successors[task])
            {
                if (--inDegree[successor] == 0)
                    queue.Enqueue(successor);
            }
        }

        cyclic = tasks.Where(t => inDegree[t] > 0).ToList();
        return cyclic.Count == 0;
    }

    // ═══════════════════════════════════════════════════════════════
    // PROPAGATION (ordonnancement automatique)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Cale les tâches automatiques (non récapitulatives, à prédécesseurs,
    /// non IsManuallyScheduled) au plus tôt selon leurs dépendances,
    /// durée ouvrée préservée
    /// </summary>
    public static void Propagate(GanttProject project)
    {
        var all = project.AllTasks().ToList();
        TryTopologicalSort(all, out var order, out var cyclic);
        project.CyclicTasks = cyclic;

        var calendar = project.Calendar;

        foreach (var task in order)
        {
            if (task.IsSummary || task.IsManuallyScheduled || task.Predecessors.Count == 0)
                continue;

            var duration = task.IsMilestone
                ? 0
                : Math.Max(1, calendar.CountWorkingDays(task.Start, task.End));

            DateTime? earliestStart = null;

            foreach (var dep in task.Predecessors)
            {
                var (predStart, predEnd) = GetSpan(dep.Predecessor);

                var constraint = dep.Type switch
                {
                    // Fin → Début : commence le jour ouvré suivant la fin (+ lag)
                    GanttDependencyType.FinishToStart
                        => calendar.AddWorkingDays(predEnd, 1 + dep.LagDays),

                    // Début → Début : commence avec le prédécesseur (+ lag)
                    GanttDependencyType.StartToStart
                        => calendar.AddWorkingDays(predStart, dep.LagDays),

                    // Fin → Fin : finit avec le prédécesseur (+ lag) -> début recalé
                    GanttDependencyType.FinishToFinish
                        => calendar.AddWorkingDays(
                            calendar.AddWorkingDays(predEnd, dep.LagDays),
                            -Math.Max(0, duration - 1)),

                    // Début → Fin : finit quand le prédécesseur commence (+ lag)
                    GanttDependencyType.StartToFinish
                        => calendar.AddWorkingDays(
                            calendar.AddWorkingDays(predStart, dep.LagDays),
                            -Math.Max(0, duration - 1)),

                    _ => task.Start
                };

                if (earliestStart == null || constraint > earliestStart)
                    earliestStart = constraint;
            }

            if (earliestStart == null) continue;

            var newStart = earliestStart.Value;
            var newEnd = task.IsMilestone
                ? newStart
                : calendar.AddWorkingDays(newStart, duration - 1);

            task.SetDatesSilent(newStart, newEnd);
        }
    }

    /// <summary>
    /// Étendue temporelle courante d'une tâche : ses dates pour une feuille,
    /// le min/max des descendants pour une récapitulative (frais, indépendant
    /// du cache de roll-up qui peut être périmé au milieu d'une propagation)
    /// </summary>
    private static (DateTime Start, DateTime End) GetSpan(GanttTask task)
    {
        if (!task.IsSummary)
            return (task.Start, task.End < task.Start ? task.Start : task.End);

        var start = DateTime.MaxValue;
        var end = DateTime.MinValue;
        foreach (var child in task.Children)
        {
            var (cs, ce) = GetSpan(child);
            if (cs < start) start = cs;
            if (ce > end) end = ce;
        }
        return (start, end);
    }

    // ═══════════════════════════════════════════════════════════════
    // CHEMIN CRITIQUE (CPM par dates, marges en jours ouvrés)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Calcule la marge totale (jours ouvrés) et le drapeau critique de chaque
    /// tâche : passe arrière sur les dates planifiées — la fin au plus tard
    /// d'une tâche est bornée par les contraintes de ses successeurs, la marge
    /// est l'écart avec sa fin planifiée. Marge ≤ 0 = critique.
    /// Respecte les dates manuelles (CPM « par dates », style outils projet).
    /// </summary>
    public static void ComputeCriticalPath(GanttProject project)
    {
        var all = project.AllTasks().ToList();
        var leaves = all.Where(t => !t.IsSummary).ToList();

        TryTopologicalSort(leaves, out var order, out _);

        var calendar = project.Calendar;
        var projectEnd = leaves.Count > 0
            ? leaves.Max(t => t.IsMilestone ? t.Start : (t.End < t.Start ? t.Start : t.End))
            : DateTime.Today;

        // Successeurs par tâche (le modèle ne stocke que les prédécesseurs)
        var successors = leaves.ToDictionary(t => t, _ => new List<(GanttTask Succ, GanttDependency Dep)>());
        foreach (var task in leaves)
        {
            foreach (var dep in task.Predecessors)
            {
                if (successors.TryGetValue(dep.Predecessor, out var list))
                    list.Add((task, dep));
            }
        }

        // Passe arrière : fin au plus tard (LF) en remontant l'ordre topologique
        var latestFinish = new Dictionary<GanttTask, DateTime>();

        for (int i = order.Count - 1; i >= 0; i--)
        {
            var task = order[i];
            var duration = task.IsMilestone
                ? 0
                : Math.Max(1, calendar.CountWorkingDays(task.Start, task.End));

            DateTime lf = projectEnd;

            foreach (var (succ, dep) in successors[task])
            {
                var succLf = dep.Type switch
                {
                    // FS : je dois finir au plus tard la veille ouvrée du début du successeur (- lag)
                    GanttDependencyType.FinishToStart
                        => calendar.AddWorkingDays(succ.Start, -(1 + dep.LagDays)),

                    // SS : mon début borne le début du successeur -> LS = succ.Start - lag
                    GanttDependencyType.StartToStart
                        => calendar.AddWorkingDays(
                            calendar.AddWorkingDays(succ.Start, -dep.LagDays),
                            Math.Max(0, duration - 1)),

                    // FF : ma fin borne la fin du successeur
                    GanttDependencyType.FinishToFinish
                        => calendar.AddWorkingDays(succ.End, -dep.LagDays),

                    // SF : mon début borne la fin du successeur
                    GanttDependencyType.StartToFinish
                        => calendar.AddWorkingDays(
                            calendar.AddWorkingDays(succ.End, -dep.LagDays),
                            Math.Max(0, duration - 1)),

                    _ => projectEnd
                };

                if (succLf < lf) lf = succLf;
            }

            latestFinish[task] = lf;

            var actualEnd = task.IsMilestone ? task.Start : task.End;
            var slack = lf >= actualEnd
                ? calendar.CountWorkingDays(actualEnd, lf) - 1
                : -(calendar.CountWorkingDays(lf, actualEnd) - 1);

            task.TotalFloatDays = slack;
            task.IsCritical = slack <= 0;
        }

        // Récapitulatives : critiques si un descendant l'est, marge = min des enfants
        foreach (var task in all.Where(t => t.IsSummary))
        {
            task.IsCritical = false;
            task.TotalFloatDays = null;
        }
        MarkSummaries(project.Tasks);
    }

    private static (bool AnyCritical, int? MinFloat) MarkSummaries(IEnumerable<GanttTask> tasks)
    {
        bool anyCritical = false;
        int? minFloat = null;

        foreach (var task in tasks)
        {
            if (task.IsSummary)
            {
                var (childCritical, childFloat) = MarkSummaries(task.Children);
                task.IsCritical = childCritical;
                task.TotalFloatDays = childFloat;
            }

            anyCritical |= task.IsCritical;
            if (task.TotalFloatDays is { } f && (minFloat == null || f < minFloat))
                minFloat = f;
        }

        return (anyCritical, minFloat);
    }
}
