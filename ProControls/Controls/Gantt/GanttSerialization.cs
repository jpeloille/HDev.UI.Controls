using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ProControls.Controls;

// ═══════════════════════════════════════════════════════════════════════════
// Persistance GanttProject — sérialise l'ÉTAT (topologie WBS + dépendances par
// Id + calendrier + AutoSchedule + baseline). Le DÉRIVÉ (roll-up, CPM, effectifs)
// est recalculé au chargement, jamais stocké.
//
// Dates : TOUJOURS InvariantCulture + "yyyy-MM-dd" (écriture ET lecture). Le
// rendu texte du contrôle utilise CurrentCulture — surtout pas ici : un JSON
// écrit en fr-FR doit se relire à l'identique sur n'importe quelle machine.
//
// Dépendances par Id (pas par index) : robuste à la reconstruction des objets
// lors d'un undo, et l'app peut relier sa clé métier via GanttTask.Id.
// ═══════════════════════════════════════════════════════════════════════════

public partial class GanttProject
{
    private const string DateFormat = "yyyy-MM-dd";

    /// <summary>
    /// Version du schéma JSON. À incrémenter à CHAQUE changement cassant du modèle
    /// sérialisé (ex. l'arrivée de Duration/Work/types de tâches), en branchant la
    /// migration dans <see cref="LoadJson"/>. Un JSON sans champ « schema » est
    /// supposé en version 1 (fichiers d'avant l'introduction du versionnage).
    /// </summary>
    public const int SchemaVersion = 1;

    /// <summary>Sérialise l'état du projet en JSON (indenté).</summary>
    public string ToJson()
    {
        var root = new JsonObject
        {
            ["schema"] = SchemaVersion,
            ["autoSchedule"] = AutoSchedule,
            ["calendar"] = new JsonObject
            {
                ["weekend"] = new JsonArray(
                    Calendar.WeekendDays.Select(d => (JsonNode)(int)d).ToArray()),
                ["holidays"] = new JsonArray(
                    Calendar.Holidays.OrderBy(d => d).Select(d => (JsonNode)Fmt(d)).ToArray()),
            },
            ["tasks"] = new JsonArray(Tasks.Select(SerializeTask).ToArray()),
        };
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>Crée un projet depuis un JSON produit par <see cref="ToJson"/>.</summary>
    public static GanttProject FromJson(string json)
    {
        var project = new GanttProject();
        project.LoadJson(json);
        return project;
    }

    /// <summary>
    /// Recharge cet objet-projet depuis un JSON (remplace tâches/calendrier/état).
    /// La référence du projet et les abonnements DataChanged/VisualChanged restent
    /// valides — c'est le chemin utilisé par l'undo/redo.
    /// </summary>
    public void LoadJson(string json)
    {
        var obj = JsonNode.Parse(json)!.AsObject();

        // Absent = 1 (fichiers d'avant le versionnage). Un schéma plus récent que ce
        // binaire n'est pas lisible : mieux vaut refuser que charger un plan tronqué.
        var schema = (int?)obj["schema"] ?? 1;
        if (schema > SchemaVersion)
            throw new NotSupportedException(
                $"Projet en schéma v{schema}, cette version n'en lit que jusqu'à v{SchemaVersion}.");

        BeginUpdate();
        try
        {
            Tasks.Clear();

            // Calendrier
            Calendar.WeekendDays.Clear();
            if (obj["calendar"]?["weekend"] is JsonArray weekend)
                foreach (var d in weekend)
                    Calendar.WeekendDays.Add((DayOfWeek)(int)d!);
            Calendar.ClearHolidays();
            if (obj["calendar"]?["holidays"] is JsonArray holidays)
                foreach (var h in holidays)
                    Calendar.AddHoliday(Parse((string)h!));

            // Tâches (préordre) — on collecte à plat pour recâbler les dépendances par Id.
            var flat = new List<(GanttTask Task, JsonObject Json)>();
            if (obj["tasks"] is JsonArray roots)
                foreach (var node in roots)
                    Tasks.Add(BuildTask(node!.AsObject(), flat));

            // Dépendances : câblage direct dans Predecessors (contourne DependsOn pour
            // préserver un éventuel cycle stocké — Recalculate le reclassera en CyclicTasks).
            var byId = new Dictionary<string, GanttTask>();
            foreach (var (task, _) in flat)
                byId[task.Id] = task; // dernier gagnant si Id dupliqué (robustesse)
            foreach (var (task, node) in flat)
            {
                if (node["deps"] is not JsonArray deps) continue;
                foreach (var dep in deps)
                {
                    var d = dep!.AsObject();
                    var predId = (string)d["pred"]!;
                    if (!byId.TryGetValue(predId, out var pred)) continue; // prédécesseur absent : ignoré
                    var type = System.Enum.Parse<GanttDependencyType>((string)d["type"]!);
                    var lag = (int?)d["lag"] ?? 0;
                    task.Predecessors.Add(new GanttDependency(pred, type, lag));
                }
            }

            AutoSchedule = (bool?)obj["autoSchedule"] ?? false;
        }
        finally
        {
            EndUpdate(); // déclenche Recalculate (roll-up + CPM) + DataChanged
        }
    }

    private static JsonNode SerializeTask(GanttTask task)
    {
        var node = new JsonObject
        {
            ["id"] = task.Id,
            ["name"] = task.Name,
            ["start"] = Fmt(task.Start),
            ["end"] = Fmt(task.End),
            ["progress"] = task.Progress,
            ["milestone"] = task.IsMilestone,
            ["expanded"] = task.IsExpanded,
            ["manual"] = task.IsManuallyScheduled,
            ["baselineStart"] = task.BaselineStart.HasValue ? Fmt(task.BaselineStart.Value) : null,
            ["baselineEnd"] = task.BaselineEnd.HasValue ? Fmt(task.BaselineEnd.Value) : null,
            ["deps"] = new JsonArray(task.Predecessors.Select(p => (JsonNode)new JsonObject
            {
                ["pred"] = p.Predecessor.Id,
                ["type"] = p.Type.ToString(),
                ["lag"] = p.LagDays,
            }).ToArray()),
            ["children"] = new JsonArray(task.Children.Select(SerializeTask).ToArray()),
        };
        return node;
    }

    private static GanttTask BuildTask(JsonObject node, List<(GanttTask, JsonObject)> flat)
    {
        var task = new GanttTask
        {
            Id = (string?)node["id"] ?? Guid.NewGuid().ToString("N"),
            Name = (string?)node["name"] ?? "",
            IsMilestone = (bool?)node["milestone"] ?? false,
            IsExpanded = (bool?)node["expanded"] ?? true,
            IsManuallyScheduled = (bool?)node["manual"] ?? false,
        };
        // Dates via SetDatesSilent : évite de déclencher NotifyDataChange par propriété.
        task.SetDatesSilent(Parse((string)node["start"]!), Parse((string)node["end"]!));
        task.Progress = (double?)node["progress"] ?? 0;
        if (node["baselineStart"] is JsonValue bs) task.BaselineStart = Parse((string)bs!);
        if (node["baselineEnd"] is JsonValue be) task.BaselineEnd = Parse((string)be!);

        flat.Add((task, node)); // préordre : parent avant enfants

        if (node["children"] is JsonArray children)
            foreach (var child in children)
                task.Children.Add(BuildTask(child!.AsObject(), flat));

        return task;
    }

    private static string Fmt(DateTime date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);

    private static DateTime Parse(string s) =>
        DateTime.ParseExact(s, DateFormat, CultureInfo.InvariantCulture);
}
