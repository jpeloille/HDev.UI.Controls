using System.Collections.ObjectModel;

namespace ProControls.Controls;

/// <summary>
/// Type de dépendance entre tâches (sémantique MS Project)
/// </summary>
public enum GanttDependencyType
{
    /// <summary>Fin → Début (le successeur commence après la fin du prédécesseur)</summary>
    FinishToStart,
    /// <summary>Début → Début</summary>
    StartToStart,
    /// <summary>Fin → Fin</summary>
    FinishToFinish,
    /// <summary>Début → Fin</summary>
    StartToFinish
}

/// <summary>
/// Dépendance : cette tâche ne peut démarrer/finir qu'en fonction du prédécesseur
/// </summary>
public class GanttDependency
{
    public GanttTask Predecessor { get; }
    public GanttDependencyType Type { get; }

    /// <summary>Décalage en jours ouvrés (peut être négatif = avance)</summary>
    public int LagDays { get; }

    public GanttDependency(GanttTask predecessor,
        GanttDependencyType type = GanttDependencyType.FinishToStart, int lagDays = 0)
    {
        Predecessor = predecessor;
        Type = type;
        LagDays = lagDays;
    }
}

/// <summary>
/// Tâche d'un diagramme de Gantt projet : feuille, récapitulative (WBS) ou jalon.
/// Phase 1 : les dates sont posées par l'application ; les récapitulatives
/// sont calculées par roll-up (GanttProject.Recalculate).
/// </summary>
public class GanttTask
{
    private string _name = "";
    private DateTime _start = DateTime.Today;
    private DateTime _end = DateTime.Today;
    private double _progress;
    private bool _isExpanded = true;

    internal GanttProject? Project { get; set; }
    public GanttTask? Parent { get; internal set; }

    public ObservableCollection<GanttTask> Children { get; } = new();
    public List<GanttDependency> Predecessors { get; } = new();

    public string Name
    {
        get => _name;
        set { _name = value; Project?.NotifyVisualChange(); }
    }

    /// <summary>Début (date). Ignoré sur une récapitulative (roll-up)</summary>
    public DateTime Start
    {
        get => _start;
        set { _start = value.Date; Project?.NotifyDataChange(); }
    }

    /// <summary>Fin incluse (date). Ignorée sur une récapitulative (roll-up)</summary>
    public DateTime End
    {
        get => _end;
        set { _end = value.Date; Project?.NotifyDataChange(); }
    }

    /// <summary>Avancement 0-100 %. Sur une récapitulative : moyenne pondérée (roll-up)</summary>
    public double Progress
    {
        get => _progress;
        set { _progress = Math.Clamp(value, 0, 100); Project?.NotifyDataChange(); }
    }

    /// <summary>Jalon : affiché en losange (durée nulle)</summary>
    public bool IsMilestone { get; set; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            Project?.NotifyVisualChange();
        }
    }

    public object? Tag { get; set; }

    /// <summary>Récapitulative = a des enfants (dates et avancement par roll-up)</summary>
    public bool IsSummary => Children.Count > 0;

    /// <summary>
    /// Exclut cette tâche de l'ordonnancement automatique (ses dates posées
    /// font foi même quand Project.AutoSchedule est actif)
    /// </summary>
    public bool IsManuallyScheduled { get; set; }

    // Valeurs effectives après roll-up (récapitulatives) — cachées, recalculées
    // par GanttProject.Recalculate, jamais pendant le rendu (volume > 2000)
    public DateTime EffectiveStart { get; internal set; }
    public DateTime EffectiveEnd { get; internal set; }
    public double EffectiveProgress { get; internal set; }

    // Baseline (instantané prévu, posé par GanttProject.SetBaseline)
    /// <summary>Début prévu figé (null = pas de baseline)</summary>
    public DateTime? BaselineStart { get; internal set; }

    /// <summary>Fin prévue figée (null = pas de baseline)</summary>
    public DateTime? BaselineEnd { get; internal set; }

    public bool HasBaseline => BaselineStart.HasValue;

    /// <summary>
    /// Retire une dépendance vers ce prédécesseur (tous types confondus).
    /// Retourne le nombre de liens retirés.
    /// </summary>
    public int RemoveDependency(GanttTask predecessor)
    {
        var removed = Predecessors.RemoveAll(d => ReferenceEquals(d.Predecessor, predecessor));
        if (removed > 0)
            Project?.NotifyDataChange();
        return removed;
    }

    // Résultats CPM (posés par GanttScheduler.ComputeCriticalPath)
    /// <summary>Marge totale en jours ouvrés (null = non calculée / récapitulative)</summary>
    public int? TotalFloatDays { get; internal set; }

    /// <summary>Sur le chemin critique (récapitulative : un descendant l'est)</summary>
    public bool IsCritical { get; internal set; }

    /// <summary>Pose les dates sans déclencher de recalcul (usage interne du scheduler)</summary>
    internal void SetDatesSilent(DateTime start, DateTime end)
    {
        _start = start.Date;
        _end = end.Date;
    }

    public GanttTask()
    {
        Children.CollectionChanged += (s, e) =>
        {
            foreach (var child in Children)
            {
                child.Parent = this;
                child.SetProject(Project);
            }
            Project?.NotifyDataChange();
        };
    }

    public GanttTask(string name, DateTime start, DateTime end, double progress = 0) : this()
    {
        _name = name;
        _start = start.Date;
        _end = end.Date;
        _progress = Math.Clamp(progress, 0, 100);
    }

    /// <summary>Ajoute une sous-tâche (raccourci fluent)</summary>
    public GanttTask Add(string name, DateTime start, DateTime end, double progress = 0)
    {
        var task = new GanttTask(name, start, end, progress);
        Children.Add(task);
        return task;
    }

    /// <summary>Ajoute un jalon (raccourci fluent)</summary>
    public GanttTask AddMilestone(string name, DateTime date)
    {
        var task = new GanttTask(name, date, date) { IsMilestone = true };
        Children.Add(task);
        return task;
    }

    /// <summary>
    /// Déclare un prédécesseur (raccourci fluent).
    /// Refuse la création d'un cycle de dépendances.
    /// </summary>
    public GanttTask DependsOn(GanttTask predecessor,
        GanttDependencyType type = GanttDependencyType.FinishToStart, int lagDays = 0)
    {
        if (ReferenceEquals(predecessor, this) || GanttScheduler.WouldCreateCycle(predecessor, this))
            throw new InvalidOperationException(
                $"La dépendance « {predecessor.Name} » → « {Name} » créerait un cycle.");

        Predecessors.Add(new GanttDependency(predecessor, type, lagDays));
        Project?.NotifyDataChange();
        return this;
    }

    internal void SetProject(GanttProject? project)
    {
        Project = project;
        foreach (var child in Children)
            child.SetProject(project);
    }
}

/// <summary>
/// Calendrier ouvré : week-ends + fériés exclus des durées
/// </summary>
public class GanttCalendar
{
    private readonly HashSet<DateTime> _holidays = new();

    /// <summary>Jours chômés hebdomadaires (défaut : samedi + dimanche)</summary>
    public HashSet<DayOfWeek> WeekendDays { get; } = new() { DayOfWeek.Saturday, DayOfWeek.Sunday };

    public void AddHoliday(DateTime date) => _holidays.Add(date.Date);
    public void RemoveHoliday(DateTime date) => _holidays.Remove(date.Date);

    public bool IsWorkingDay(DateTime date)
        => !WeekendDays.Contains(date.DayOfWeek) && !_holidays.Contains(date.Date);

    /// <summary>Nombre de jours ouvrés entre start et end inclus (0 si end &lt; start)</summary>
    public int CountWorkingDays(DateTime start, DateTime end)
    {
        start = start.Date;
        end = end.Date;
        if (end < start) return 0;

        int count = 0;
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            if (IsWorkingDay(d)) count++;
        }
        return count;
    }

    /// <summary>
    /// Ajoute n jours ouvrés à partir de date (n peut être négatif = recul).
    /// n = 0 : le jour ouvré même, sinon le prochain (n ≥ 0) ou le précédent (n &lt; 0)
    /// </summary>
    public DateTime AddWorkingDays(DateTime date, int n)
    {
        var d = date.Date;
        while (!IsWorkingDay(d))
            d = d.AddDays(n >= 0 ? 1 : -1);

        while (n > 0)
        {
            d = d.AddDays(1);
            if (IsWorkingDay(d)) n--;
        }
        while (n < 0)
        {
            d = d.AddDays(-1);
            if (IsWorkingDay(d)) n++;
        }
        return d;
    }
}

/// <summary>
/// Projet Gantt : racines WBS + calendrier + roll-up caché.
/// Les événements séparent mutation de données (recalcul + layout) et
/// changement purement visuel (rafraîchissement).
/// </summary>
public class GanttProject
{
    private bool _suspendNotifications;

    public ObservableCollection<GanttTask> Tasks { get; } = new();
    public GanttCalendar Calendar { get; } = new();

    private bool _autoSchedule;

    /// <summary>
    /// Ordonnancement automatique : les tâches à prédécesseurs (non
    /// récapitulatives, non IsManuallyScheduled) sont calées selon leurs
    /// dépendances, durée préservée. Défaut false = dates posées font foi.
    /// </summary>
    public bool AutoSchedule
    {
        get => _autoSchedule;
        set
        {
            if (_autoSchedule == value) return;
            _autoSchedule = value;
            NotifyDataChange();
        }
    }

    /// <summary>Tâches impliquées dans un cycle de dépendances (exclues de l'ordonnancement)</summary>
    public IReadOnlyList<GanttTask> CyclicTasks { get; internal set; } = Array.Empty<GanttTask>();

    public bool HasCycle => CyclicTasks.Count > 0;

    /// <summary>Données modifiées (dates, structure) : roll-up refait, layout à refaire</summary>
    public event EventHandler? DataChanged;

    /// <summary>Changement visuel seul (nom, expansion)</summary>
    public event EventHandler? VisualChanged;

    public GanttProject()
    {
        Tasks.CollectionChanged += (s, e) =>
        {
            foreach (var task in Tasks)
            {
                task.Parent = null;
                task.SetProject(this);
            }
            NotifyDataChange();
        };
    }

    /// <summary>Ajoute une tâche racine (raccourci fluent)</summary>
    public GanttTask Add(string name, DateTime start, DateTime end, double progress = 0)
    {
        var task = new GanttTask(name, start, end, progress);
        Tasks.Add(task);
        return task;
    }

    /// <summary>Suspend les notifications pendant un chargement en masse</summary>
    public void BeginUpdate() => _suspendNotifications = true;

    public void EndUpdate()
    {
        _suspendNotifications = false;
        NotifyDataChange();
    }

    internal void NotifyDataChange()
    {
        if (_suspendNotifications) return;
        Recalculate();
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    internal void NotifyVisualChange()
    {
        if (_suspendNotifications) return;
        VisualChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Recalcul complet sur mutation (jamais pendant le rendu) :
    /// roll-up → propagation des dépendances (si AutoSchedule) → roll-up → CPM
    /// </summary>
    public void Recalculate()
    {
        RollUpAll();

        if (_autoSchedule)
        {
            GanttScheduler.Propagate(this);
            RollUpAll();
        }

        GanttScheduler.ComputeCriticalPath(this);
    }

    private void RollUpAll()
    {
        foreach (var task in Tasks)
            RollUp(task);
    }

    private void RollUp(GanttTask task)
    {
        if (!task.IsSummary)
        {
            task.EffectiveStart = task.Start;
            task.EffectiveEnd = task.End < task.Start ? task.Start : task.End;
            task.EffectiveProgress = task.Progress;
            return;
        }

        var start = DateTime.MaxValue;
        var end = DateTime.MinValue;
        double weightedProgress = 0;
        double totalWeight = 0;

        foreach (var child in task.Children)
        {
            RollUp(child);

            if (child.EffectiveStart < start) start = child.EffectiveStart;
            if (child.EffectiveEnd > end) end = child.EffectiveEnd;

            // Poids = durée ouvrée (un jalon pèse 0 mais compte s'il est seul)
            var weight = Math.Max(child.IsMilestone ? 0 : 1,
                Calendar.CountWorkingDays(child.EffectiveStart, child.EffectiveEnd));
            weightedProgress += child.EffectiveProgress * weight;
            totalWeight += weight;
        }

        task.EffectiveStart = start;
        task.EffectiveEnd = end;
        task.EffectiveProgress = totalWeight > 0 ? weightedProgress / totalWeight : 0;
    }

    /// <summary>
    /// Fige la baseline : instantané des dates effectives courantes de toutes
    /// les tâches (comparaison prévu/réel ensuite)
    /// </summary>
    public void SetBaseline()
    {
        foreach (var task in AllTasks())
        {
            task.BaselineStart = task.EffectiveStart;
            task.BaselineEnd = task.EffectiveEnd;
        }
        NotifyVisualChange();
    }

    /// <summary>Efface la baseline</summary>
    public void ClearBaseline()
    {
        foreach (var task in AllTasks())
        {
            task.BaselineStart = null;
            task.BaselineEnd = null;
        }
        NotifyVisualChange();
    }

    /// <summary>Une baseline a été figée (au moins une tâche en porte une)</summary>
    public bool HasBaseline => AllTasks().Any(t => t.HasBaseline);

    /// <summary>Bornes du projet (après Recalculate)</summary>
    public (DateTime Start, DateTime End) GetBounds()
    {
        if (Tasks.Count == 0)
            return (DateTime.Today, DateTime.Today.AddDays(30));

        var start = Tasks.Min(t => t.EffectiveStart);
        var end = Tasks.Max(t => t.EffectiveEnd);
        return (start, end);
    }

    /// <summary>Toutes les tâches (préfixe, profondeur d'abord)</summary>
    public IEnumerable<GanttTask> AllTasks()
    {
        static IEnumerable<GanttTask> Walk(IEnumerable<GanttTask> tasks)
        {
            foreach (var task in tasks)
            {
                yield return task;
                foreach (var child in Walk(task.Children))
                    yield return child;
            }
        }
        return Walk(Tasks);
    }
}
