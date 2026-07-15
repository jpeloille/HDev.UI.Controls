using System.Collections.Generic;
using System.Linq;

namespace ProControls.Controls;

// ═══════════════════════════════════════════════════════════════════════════
// L1 — Édition structurelle du plan (moteur pur, aucune dépendance Avalonia).
//
// Le constat qui a motivé ce lot : ProGantt savait tout AFFICHER et tout
// MODIFIER, sauf FABRIQUER un plan — ni créer, ni supprimer, ni indenter une
// tâche. Durée, contraintes, EVM sont des raffinements sur un plan qu'on ne
// pouvait pas encore bâtir.
//
// INVARIANT central : supprimer une tâche PURGE les dépendances qui la
// visent (elle et tout son sous-arbre). Sans ça l'arbre garde des
// GanttDependency pointant sur des tâches hors projet — le bug classique,
// invisible jusqu'au premier Recalculate ou à la première sérialisation.
//
// Chaque opération est atomique : notifications suspendues pendant la
// mutation, UN SEUL Recalculate au bout (via un scope RÉENTRANT — un
// BeginUpdate applicatif englobant n'est pas cassé par l'opération).
// ═══════════════════════════════════════════════════════════════════════════

public partial class GanttProject
{
    /// <summary>Collection fratrie d'une tâche (enfants du parent, ou racines).</summary>
    private IList<GanttTask> SiblingsOf(GanttTask task)
        => task.Parent?.Children ?? (IList<GanttTask>)Tasks;

    /// <summary>Le sous-arbre d'une tâche, elle comprise (préordre).</summary>
    private static IEnumerable<GanttTask> Subtree(GanttTask task)
    {
        yield return task;
        foreach (var child in task.Children)
            foreach (var d in Subtree(child))
                yield return d;
    }

    // ── Scope de suspension réentrant ────────────────────────────────────────
    // BeginUpdate/EndUpdate publics posent/lèvent le drapeau sans compteur : un
    // EndUpdate interne lèverait prématurément un BeginUpdate applicatif. Ces
    // deux helpers sauvent/restaurent l'état au lieu de le forcer.

    private bool BeginSilent()
    {
        var previous = _suspendNotifications;
        _suspendNotifications = true;
        return previous;
    }

    private void EndSilent(bool previous)
    {
        _suspendNotifications = previous;
        if (!previous) NotifyDataChange();
    }

    // ── Insertion ────────────────────────────────────────────────────────────

    /// <summary>
    /// Insère une tâche juste après <paramref name="reference"/>, au même niveau.
    /// Durée d'un jour calée sur le début de la référence.
    /// </summary>
    public GanttTask InsertAfter(GanttTask reference, string name = "Nouvelle tâche")
    {
        var siblings = SiblingsOf(reference);
        var index = siblings.IndexOf(reference);
        var start = reference.EffectiveStart == default ? DateTime.Today : reference.EffectiveStart;
        var task = new GanttTask(name, start, start);

        var previous = BeginSilent();
        siblings.Insert(index + 1, task);
        EndSilent(previous);
        return task;
    }

    /// <summary>Ajoute une tâche racine en fin de plan (durée d'un jour).</summary>
    public GanttTask InsertRoot(string name = "Nouvelle tâche")
    {
        var start = Tasks.Count > 0 ? GetBounds().Start : DateTime.Today;
        var task = new GanttTask(name, start, start);

        var previous = BeginSilent();
        Tasks.Add(task);
        EndSilent(previous);
        return task;
    }

    // ── Suppression ──────────────────────────────────────────────────────────

    /// <summary>
    /// Supprime une tâche ET son sous-arbre, puis purge toutes les dépendances
    /// qui visaient l'une des tâches retirées. Retourne false si absente.
    /// </summary>
    public bool Remove(GanttTask task)
    {
        var siblings = SiblingsOf(task);
        if (!siblings.Contains(task)) return false;

        var removed = new HashSet<GanttTask>(Subtree(task));

        var previous = BeginSilent();
        siblings.Remove(task);
        task.Parent = null;
        task.SetProject(null);

        // INVARIANT : aucune dépendance ne doit survivre vers une tâche retirée.
        foreach (var remaining in AllTasks())
            remaining.Predecessors.RemoveAll(d => removed.Contains(d.Predecessor));

        EndSilent(previous);
        return true;
    }

    // ── Indentation ──────────────────────────────────────────────────────────

    /// <summary>Indentable = a une fratrie précédente (qui devient sa récapitulative).</summary>
    public bool CanIndent(GanttTask task) => SiblingsOf(task).IndexOf(task) > 0;

    /// <summary>
    /// Indente : la tâche devient le dernier enfant de sa fratrie précédente
    /// (laquelle devient récapitulative — dates par roll-up). Son sous-arbre suit.
    /// </summary>
    public bool Indent(GanttTask task)
    {
        var siblings = SiblingsOf(task);
        var index = siblings.IndexOf(task);
        if (index <= 0) return false;

        var newParent = siblings[index - 1];

        var previous = BeginSilent();
        siblings.RemoveAt(index);
        newParent.Children.Add(task); // le handler recâble Parent + Project
        newParent.IsExpanded = true;  // sinon la tâche indentée disparaîtrait
        EndSilent(previous);
        return true;
    }

    /// <summary>Désindentable = a un parent (une racine ne se désindente pas).</summary>
    public bool CanOutdent(GanttTask task) => task.Parent != null;

    /// <summary>
    /// Désindente : la tâche devient la fratrie suivante de son ancien parent.
    /// Son sous-arbre suit.
    /// </summary>
    public bool Outdent(GanttTask task)
    {
        var parent = task.Parent;
        if (parent == null) return false;

        var target = parent.Parent?.Children ?? (IList<GanttTask>)Tasks;
        var parentIndex = target.IndexOf(parent);

        var previous = BeginSilent();
        parent.Children.Remove(task);
        target.Insert(parentIndex + 1, task); // le handler recâble Parent + Project
        EndSilent(previous);
        return true;
    }

    // ── Réordonnancement dans la fratrie ─────────────────────────────────────

    public bool CanMoveUp(GanttTask task) => SiblingsOf(task).IndexOf(task) > 0;

    public bool CanMoveDown(GanttTask task)
    {
        var siblings = SiblingsOf(task);
        var index = siblings.IndexOf(task);
        return index >= 0 && index < siblings.Count - 1;
    }

    /// <summary>Monte la tâche d'un cran dans sa fratrie (sous-arbre compris).</summary>
    public bool MoveUp(GanttTask task) => MoveBy(task, -1);

    /// <summary>Descend la tâche d'un cran dans sa fratrie (sous-arbre compris).</summary>
    public bool MoveDown(GanttTask task) => MoveBy(task, +1);

    private bool MoveBy(GanttTask task, int delta)
    {
        var siblings = SiblingsOf(task);
        var index = siblings.IndexOf(task);
        var target = index + delta;
        if (index < 0 || target < 0 || target >= siblings.Count) return false;

        var previous = BeginSilent();
        siblings.RemoveAt(index);
        siblings.Insert(target, task);
        EndSilent(previous);
        return true;
    }
}
