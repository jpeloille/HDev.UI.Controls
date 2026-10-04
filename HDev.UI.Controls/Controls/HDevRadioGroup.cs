using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace HDev.UI.Controls;

/// <summary>
/// Groupe de choix exclusifs (<see cref="HDevRadioButton"/>, <see cref="HDevRadioCard"/>) :
/// les membres sont les enfants du même parent visuel, de même type et de même
/// GroupName (vide = groupe par défaut du conteneur).
/// </summary>
internal static class HDevRadioGroup
{
    /// <summary>
    /// Les autres membres du groupe, désactivés compris : l'exclusivité vaut pour
    /// tous (un membre désactivé resté coché fausserait la valeur du groupe)
    /// </summary>
    public static IEnumerable<T> Siblings<T>(T self, Func<T, string> groupName) where T : Control
    {
        if (self.Parent is not Visual parent) yield break;

        foreach (var child in parent.GetVisualChildren())
        {
            if (child is T other && !ReferenceEquals(other, self) && groupName(other) == groupName(self))
                yield return other;
        }
    }

    /// <summary>
    /// Flèches : passe au membre actif suivant ou précédent (en boucle) et lui donne un
    /// focus clavier, donc visible ; l'appelant le coche. Null si la touche ne s'applique pas.
    /// </summary>
    public static T? Navigate<T>(T self, Key key, Func<T, string> groupName) where T : Control
    {
        if (key is not (Key.Down or Key.Right or Key.Up or Key.Left)) return null;

        var group = Siblings(self, groupName).Where(member => member.IsEnabled)
            .Append(self).OrderBy(IndexInParent).ToList();
        if (group.Count < 2) return null;

        var forward = key is Key.Down or Key.Right;
        var index = group.IndexOf(self);
        var next = group[(index + (forward ? 1 : -1) + group.Count) % group.Count];
        next.Focus(NavigationMethod.Directional);
        return next;
    }

    private static int IndexInParent(Control control)
        => control.Parent is Visual parent ? parent.GetVisualChildren().ToList().IndexOf(control) : -1;
}
