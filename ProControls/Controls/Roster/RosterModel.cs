using Avalonia.Media;

namespace ProControls.Controls;

/// <summary>
/// Bande de fond d'une voie : l'amplitude « large » sur laquelle se posent barres et blocs.
/// </summary>
/// <remarks>
/// <see cref="Start"/>/<see cref="End"/> donnent l'étendue <b>peinte</b>. Quand l'étendue
/// réelle en diffère — un jour de repos qui porte tout de même des activités, peint sur
/// 00:00→24:00 alors que le service effectif est plus court — <see cref="MarkerStart"/> et
/// <see cref="MarkerEnd"/> la marquent par des crochets. Sans ce second couple, l'information
/// serait perdue.
/// </remarks>
public class RosterBand
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    /// <summary>Étendue réelle, marquée par des crochets ; null = identique à Start/End.</summary>
    public DateTime? MarkerStart { get; set; }

    /// <inheritdoc cref="MarkerStart"/>
    public DateTime? MarkerEnd { get; set; }

    /// <summary>Libellé posé dans la bande, en haut à gauche.</summary>
    public string? Label { get; set; }

    /// <summary>Libellé posé sous la bande, calé à gauche.</summary>
    public string? StartLabel { get; set; }

    /// <summary>Libellé posé sous la bande, calé à droite.</summary>
    public string? EndLabel { get; set; }

    /// <summary>Teinte du métier ; null = teinte neutre du thème.</summary>
    public Color? Color { get; set; }

    public object? Tag { get; set; }

    public RosterBand() { }

    public RosterBand(DateTime start, DateTime end, string? label = null)
    {
        Start = start;
        End = end;
        Label = label;
    }
}

/// <summary>
/// Barre fine ancrée en bas de la bande : une seconde durée, incluse dans la première.
/// </summary>
public class RosterBar
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public Color? Color { get; set; }
    public object? Tag { get; set; }

    public RosterBar() { }

    public RosterBar(DateTime start, DateTime end)
    {
        Start = start;
        End = end;
    }
}

/// <summary>
/// Bloc étiqueté posé sur la voie : l'unité que l'utilisateur clique et déplace.
/// </summary>
public class RosterBlock
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string? Label { get; set; }
    public Color? Color { get; set; }

    /// <summary>
    /// Piste verticale dans la voie. 0 = pleine hauteur de la bande ; 1 = au-dessus de la
    /// barre, quand une barre occupe le bas.
    /// </summary>
    public int Track { get; set; }

    public object? Tag { get; set; }

    public RosterBlock() { }

    public RosterBlock(DateTime start, DateTime end, string? label = null, int track = 0)
    {
        Start = start;
        End = end;
        Label = label;
        Track = track;
    }
}

/// <summary>
/// Une ligne de la frise : sa gouttière, sa fenêtre temporelle, et ses trois couches.
/// </summary>
/// <remarks>
/// <b><see cref="WindowStart"/> est ce qui rend le contrôle réutilisable.</b> L'abscisse d'un
/// instant vaut <c>Axis.ToX(t - WindowStart)</c>. Un consommateur « une voie = un jour » y met
/// le jour à minuit, et son axe redémarre donc à 00:00 sur chaque ligne ; un consommateur
/// « une voie = une ressource » y met une origine commune à toutes les voies, et son axe est
/// continu sur plusieurs jours. Même code de rendu, deux visuels.
/// </remarks>
public class RosterLane
{
    /// <summary>Clé stable. Sert à retrouver la sélection après un rechargement.</summary>
    /// <remarks>
    /// <c>Tag</c> ne se sérialise pas : c'est <see cref="Id"/> qui porte le lien métier.
    /// </remarks>
    public string Id { get; set; } = "";

    /// <summary>Libellé de gouttière.</summary>
    public string Label { get; set; } = "";

    /// <summary>Second libellé de gouttière, plus discret.</summary>
    public string? SubLabel { get; set; }

    /// <summary>Origine temporelle de la voie. Voir les remarques du type.</summary>
    public DateTime WindowStart { get; set; }

    /// <summary>Signale une voie à regarder, sans dire pourquoi : c'est au métier de peindre.</summary>
    public bool HasWarning { get; set; }

    public object? Tag { get; set; }

    public IList<RosterBand> Bands { get; } = new List<RosterBand>();
    public IList<RosterBar> Bars { get; } = new List<RosterBar>();
    public IList<RosterBlock> Blocks { get; } = new List<RosterBlock>();

    public RosterLane() { }

    public RosterLane(string id, string label, DateTime windowStart)
    {
        Id = id;
        Label = label;
        WindowStart = windowStart;
    }
}

/// <summary>
/// Le graphe complet affiché par <c>ProRoster</c>.
/// </summary>
/// <remarks>
/// <b>Les voies sont ordonnées par position dans la liste, jamais déduites d'une date.</b>
/// C'est ce qui permet à une voie d'être une ressource. Un consommateur « une voie = un jour »
/// doit donc matérialiser les journées vides — sinon le 15 et le 17 mars se retrouvent
/// collés. <see cref="RosterEngine.BuildDailyLanes"/> est là pour ça.
///
/// <para><see cref="Changed"/> n'est pas décoratif : une <c>IList</c> ne notifie rien, donc
/// sans lui, muter les voies ne repeindrait pas. Appeler <see cref="Touch"/> après une
/// modification.</para>
/// </remarks>
public class RosterModel
{
    /// <summary>Levé après <see cref="Touch"/>. Le contrôle s'y abonne pour se repeindre.</summary>
    public event EventHandler? Changed;

    public IList<RosterLane> Lanes { get; } = new List<RosterLane>();

    /// <summary>Instant de référence du modèle, pour la ligne « maintenant » et le cadrage.</summary>
    public DateTime Origin { get; set; } = DateTime.Today;

    /// <summary>Signale que le graphe a changé.</summary>
    public void Touch() => Changed?.Invoke(this, EventArgs.Empty);
}
