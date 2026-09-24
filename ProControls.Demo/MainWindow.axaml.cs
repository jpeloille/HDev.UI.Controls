using Avalonia.Controls;
using ProControls.Controls;
using ProControls.Demo.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ProControls.Demo;

public class Country
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public partial class MainWindow : ProWindow
{
    public ObservableCollection<Employee> Employees { get; set; }

    public MainWindow()
    {
        // Initialize data first
        Employees = SampleDataGenerator.GenerateEmployees(100);

        InitializeComponent();

        InitializeMenuBar();
        InitializeRibbon();
        InitializeToolbar();
        InitializeStatusBar();
        InitializeComboBoxDemo();
        InitializeEditorsDemo();
        InitializeTabsDemo();
        InitializeMessageBoxDemo();
        InitializeDataGrid();
    }

    private void InitializeTabsDemo()
    {
        var tabs = this.FindControl<ProTabControl>("DemoTabs");
        if (tabs == null) return;

        // Page 1 : arborescence services -> employés (données de la démo)
        var tree = new ProTreeView();
        foreach (var dept in Employees.GroupBy(emp => emp.Department).OrderBy(g => g.Key))
        {
            var deptNode = tree.Add($"{dept.Key} ({dept.Count()})", "👥");
            foreach (var emp in dept.OrderBy(emp => emp.LastName).Take(8))
                deptNode.Add(emp.FullName, emp.IsActive ? "🟢" : "⚪");
        }
        tree.SelectedNodeChanged += (s, e) =>
        {
            var statusBar = this.FindControl<ProStatusBar>("MainStatusBar");
            if (statusBar != null && statusBar.Items.Count > 0 && tree.SelectedNode != null)
                statusBar.Items[0].Text = $"Sélection : {tree.SelectedNode.Text}";
        };

        tabs.AddPage("Organisation", new Avalonia.Controls.ScrollViewer
        {
            Content = tree,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        }, "🌳");

        // Page 2 : contenu quelconque
        var aboutPanel = new Avalonia.Controls.StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 8 };
        aboutPanel.Children.Add(new Avalonia.Controls.TextBlock
        {
            Text = "Chaque page héberge un contenu arbitraire.\nNavigation : clic, flèches ←/→, clic milieu ferme les pages fermables."
        });
        var addButton = new ProButton { Text = "Ajouter une page fermable", Variant = ButtonVariant.Secondary };
        aboutPanel.Children.Add(addButton);
        tabs.AddPage("Infos", aboutPanel, "ℹ️");

        // Page 3 : ProGantt (lot 4b)
        tabs.AddPage("Projet", BuildGanttDemo(), "📊");

        // Page 4 : ProHtmlView (monstre n° 1, tête de lecture)
        tabs.AddPage("Mail", BuildHtmlViewDemo(), "📧");

        // Page 5 : ProRichEdit (monstre n° 2, tête d'édition)
        tabs.AddPage("Composer", BuildRichEditDemo(), "✍️");

        // Page 6 : mini-Outlook (ProListView + ProHtmlView)
        tabs.AddPage("Boîte", BuildInboxDemo(), "📥");

        // Page 7 : ProScheduler (4e chantier structurant Outlook)
        tabs.AddPage("Agenda", BuildSchedulerDemo(), "📅");

        // Page 8 : ProRoster (frise roster — voies x temps)
        tabs.AddPage("Roster", BuildRosterDemo(), "🗓");

        // Page 9 : ProMindMap (brainstorming)
        tabs.AddPage("Carte", BuildMindMapDemo(), "🧠");

        // Page 9 : ProDock (docking, phase 1) — poste dispatcher
        tabs.AddPage("Dispatcher", BuildDockDemo(), "🗂️");

        // Page 10 : Accordion / Avatar / Chip / ToggleButtonGroup
        tabs.AddPage("Composants", BuildWidgetsDemo(), "🧩");

        // Page 4 : fermable
        var closable = tabs.AddPage("Rapport", new Avalonia.Controls.TextBlock
        {
            Margin = new Avalonia.Thickness(16),
            Text = "Page fermable : croix sur l'onglet, ou clic milieu."
        }, "📄");
        closable.CanClose = true;

        var counter = 1;
        addButton.Click += (s, e) =>
        {
            var page = tabs.AddPage($"Rapport {counter++}", new Avalonia.Controls.TextBlock
            {
                Margin = new Avalonia.Thickness(16),
                Text = "Page ajoutée dynamiquement."
            }, "📄");
            page.CanClose = true;
            tabs.SelectedIndex = tabs.Items.Count - 1;
        };
    }

    private Avalonia.Controls.Control BuildWidgetsDemo()
    {
        void SetStatus(string message)
        {
            var statusBar = this.FindControl<ProStatusBar>("MainStatusBar");
            if (statusBar != null && statusBar.Items.Count > 0)
                statusBar.Items[0].Text = message;
        }

        // ── ProAccordion (volet gauche) : équipage + filtres + à propos ──
        var accordion = new ProAccordion { Width = 280 };

        var crewPanel = new Avalonia.Controls.StackPanel { Spacing = 6, Margin = new Avalonia.Thickness(12, 8) };
        foreach (var (name, status) in new[]
        {
            ("Julien Peloille", AvatarStatus.Online),
            ("Awa Wamytan", AvatarStatus.Busy),
            ("Teiki Brothers", AvatarStatus.Away),
            ("Lucie Martin", AvatarStatus.Offline),
        })
        {
            var row = new Avalonia.Controls.StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 10
            };
            row.Children.Add(new ProAvatar { FullName = name, Status = status });
            row.Children.Add(new Avalonia.Controls.TextBlock
            {
                Text = name,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            });
            crewPanel.Children.Add(row);
        }
        accordion.AddSection("Équipage", crewPanel, "👥", isExpanded: true);

        // Section filtres : chips cochables
        var filterPanel = new Avalonia.Controls.WrapPanel { Margin = new Avalonia.Thickness(10, 8) };
        foreach (var (label, icon) in new[] { ("A320", "✈️"), ("ATR 72", "✈️"), ("Cargo", "📦"), ("Charter", "🎫") })
        {
            var chip = new ProChip { Text = label, Icon = icon, IsCheckable = true, Margin = new Avalonia.Thickness(2) };
            chip.CheckedChanged += (s, e) =>
                SetStatus($"Filtre « {label} » : {(chip.IsChecked ? "actif" : "inactif")}");
            filterPanel.Children.Add(chip);
        }
        accordion.AddSection("Filtres flotte", filterPanel, "🔎");

        // Section à fond d'état (HeaderBackground) + chip à badge compteur
        var alertPanel = new Avalonia.Controls.StackPanel { Spacing = 6, Margin = new Avalonia.Thickness(12, 8) };
        var alertChip = new ProChip { Text = "NOTAM", Icon = "⚠️", Badge = "3", BadgeColor = ProControls.Theme.ProTheme.Accent.Error };
        alertChip.Click += (s, e) => SetStatus("3 NOTAM actifs sur NWWW");
        alertPanel.Children.Add(alertChip);
        var okChip = new ProChip { Text = "Messages", Icon = "✉️", Badge = "12" };
        alertPanel.Children.Add(okChip);
        var alertSection = accordion.AddSection("Alertes", alertPanel, "🔔");
        alertSection.HeaderBackground = ProControls.Theme.ProTheme.WithOpacity(
            ProControls.Theme.ProTheme.Accent.Error, 36);

        accordion.AddSection("À propos", new Avalonia.Controls.TextBlock
        {
            Margin = new Avalonia.Thickness(12, 8),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Text = "Mode Single : ouvrir une section ferme l'autre.\nClavier : ↑↓ puis Entrée/Espace."
        }, "ℹ️");

        accordion.SectionExpandedChanged += (s, section) =>
            SetStatus($"Section « {section.Header} » : {(section.IsExpanded ? "ouverte" : "fermée")}");

        // ── Volet droit : avatars, chips fermables, groupes segmentés ──
        var right = new Avalonia.Controls.StackPanel { Spacing = 18, Margin = new Avalonia.Thickness(24, 12) };

        // Tailles + statuts d'avatar
        var avatarRow = new Avalonia.Controls.StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 12
        };
        avatarRow.Children.Add(new ProAvatar { FullName = "Julien Peloille", Size = AvatarSize.Small });
        avatarRow.Children.Add(new ProAvatar { FullName = "Awa Wamytan", Size = AvatarSize.Medium, Status = AvatarStatus.Online });
        avatarRow.Children.Add(new ProAvatar { FullName = "Teiki Brothers", Size = AvatarSize.Large, Status = AvatarStatus.Busy });
        avatarRow.Children.Add(new ProAvatar { Initials = "NC", Size = AvatarSize.Large, Background = Avalonia.Media.Color.Parse("#00847E") });
        right.Children.Add(new ProCard
        {
            Title = "ProAvatar — tailles, statuts, couleur stable par nom",
            Content = avatarRow
        });

        // Chips fermables (destinataires)
        var chipsPanel = new Avalonia.Controls.WrapPanel();
        foreach (var dest in new[] { "ops@aircal.nc", "crew@aircal.nc", "dispatch@aircal.nc" })
        {
            var chip = new ProChip { Text = dest, Icon = "✉️", CanClose = true, Margin = new Avalonia.Thickness(2) };
            chip.Closed += (s, e) =>
            {
                chipsPanel.Children.Remove(chip);
                SetStatus($"Destinataire retiré : {dest}");
            };
            chipsPanel.Children.Add(chip);
        }
        right.Children.Add(new ProCard
        {
            Title = "ProChip — fermables (la croix retire le chip)",
            Content = chipsPanel
        });

        // Groupes segmentés
        var togglePanel = new Avalonia.Controls.StackPanel { Spacing = 10 };
        var viewGroup = new ProToggleButtonGroup();
        viewGroup.Add("Jour");
        viewGroup.Add("Semaine", isSelected: true);
        viewGroup.Add("Mois");
        viewGroup.SelectionChanged += (s, e) =>
            SetStatus($"Vue : {viewGroup.SelectedItems.FirstOrDefault()?.Text}");
        togglePanel.Children.Add(viewGroup);

        var styleGroup = new ProToggleButtonGroup { SelectionMode = ToggleGroupSelectionMode.Multiple };
        styleGroup.Add("G", isSelected: true).Tag = "bold";
        styleGroup.Add("I").Tag = "italic";
        styleGroup.Add("S").Tag = "underline";
        styleGroup.SelectionChanged += (s, e) =>
            SetStatus($"Styles actifs : {string.Join(", ", styleGroup.SelectedItems.Select(i => i.Text))}");
        togglePanel.Children.Add(styleGroup);

        right.Children.Add(new ProCard
        {
            Title = "ProToggleButtonGroup — Single (vue agenda) et Multiple (styles)",
            Content = togglePanel
        });

        var root = new Avalonia.Controls.DockPanel();
        var accordionHost = new Avalonia.Controls.Border
        {
            Width = 280,
            BorderBrush = new Avalonia.Media.SolidColorBrush(ProControls.Theme.ProTheme.Border.Subtle),
            BorderThickness = new Avalonia.Thickness(0, 0, 1, 0),
            Child = new Avalonia.Controls.ScrollViewer { Content = accordion }
        };
        Avalonia.Controls.DockPanel.SetDock(accordionHost, Avalonia.Controls.Dock.Left);
        root.Children.Add(accordionHost);
        root.Children.Add(new Avalonia.Controls.ScrollViewer { Content = right });
        return root;
    }

    private Avalonia.Controls.Control BuildDockDemo()
    {
        var dock = new ProDockManager();

        static Avalonia.Controls.Control Pad(string text) => new Avalonia.Controls.Border
        {
            Padding = new Avalonia.Thickness(12),
            Child = new Avalonia.Controls.TextBlock
            {
                Text = text,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            }
        };

        // Zone document (centre) : deux plans de vol en onglets.
        dock.AddPanel(new ProDockPanel("vol-ty201", "TY201",
            Pad("Plan de vol TY201 — Nouméa → Lifou\n\nZone document centrale : les volets ajoutés au centre s'empilent en onglets."),
            "🛫"), DockRegion.Center);
        dock.AddPanel(new ProDockPanel("vol-ty340", "TY340",
            Pad("Plan de vol TY340 — Nouméa → Wallis"), "🛫"), DockRegion.Center);

        // Volet gauche : la flotte (ProTreeView, dogfooding).
        var fleet = new ProTreeView();
        var a320 = fleet.Add("A320", "✈️");
        a320.Add("F-OJSB", "🟢");
        a320.Add("F-OJSC", "🟢");
        var atr = fleet.Add("ATR 72", "✈️");
        atr.Add("F-OIQA", "🟢");
        atr.Add("F-OIQB", "⚪");
        dock.AddPanel(new ProDockPanel("flotte", "Flotte",
            new Avalonia.Controls.ScrollViewer { Content = fleet }, "🛩️"), DockRegion.Left);

        // Volet droit : détails de sélection.
        dock.AddPanel(new ProDockPanel("details", "Détails",
            Pad("Détails de l'appareil / du vol sélectionné.\n\nRedimensionne les volets par les splitters entre eux."),
            "🔍"), DockRegion.Right);

        // Volet bas : journal.
        dock.AddPanel(new ProDockPanel("journal", "Journal",
            Pad("[08:15] TY201 embarquement\n[08:52] TY201 pushback\n[09:03] TY201 airborne"),
            "📜"), DockRegion.Bottom);

        // Barre d'outils : sérialisation du layout.
        var save = new ProButton { Text = "Sauver layout", Variant = ButtonVariant.Secondary };
        var load = new ProButton { Text = "Charger layout", Variant = ButtonVariant.Secondary, IsEnabled = false };
        var status = new Avalonia.Controls.TextBlock
        {
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Foreground = new Avalonia.Media.SolidColorBrush(ProControls.Theme.ProTheme.Text.Secondary),
            Text = "Réorganise (splitters, onglets, ×) puis sauve/recharge le layout."
        };

        string? savedLayout = null;
        save.Click += (_, _) =>
        {
            savedLayout = dock.SaveLayout();
            load.IsEnabled = true;
            status.Text = $"Layout sauvé ({savedLayout.Length} caractères JSON).";
        };
        load.Click += (_, _) =>
        {
            if (savedLayout == null) return;
            dock.LoadLayout(savedLayout);
            status.Text = "Layout rechargé depuis le JSON sauvé.";
        };

        dock.PanelClosed += (_, panel) => status.Text = $"Volet fermé : {panel.Title}.";

        var toolbar = new Avalonia.Controls.StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(8),
            Children = { save, load, status }
        };

        var root = new Avalonia.Controls.DockPanel();
        Avalonia.Controls.DockPanel.SetDock(toolbar, Avalonia.Controls.Dock.Top);
        root.Children.Add(toolbar);
        root.Children.Add(dock);
        return root;
    }

    private Avalonia.Controls.Control BuildHtmlViewDemo()
    {
        // Pixel rouge 2x2 en data: (image embarquée, rendue nativement)
        const string redPixel = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAEklEQVR4nGP8z8Dwn4GBgYEBAA0GAgHc9K/rAAAAAElFTkSuQmCC";

        var html = $"""
            <h1 style="color:#E95420">Confirmation de réservation</h1>
            <p>Bonjour <b>Julien</b>,</p>
            <p>Votre vol <i>Nouméa → Lifou</i> est <span style="color:green;font-weight:bold">confirmé</span>.
            Détails ci-dessous&nbsp;:</p>
            <table>
              <tr><th>Vol</th><th>Départ</th><th>Arrivée</th><th>Prix</th></tr>
              <tr><td>TY201</td><td>08:15 NOU</td><td>09:00 LIF</td><td>15&nbsp;900 XPF</td></tr>
              <tr><td colspan="3">Total (taxes incluses)</td><td><b>15&nbsp;900 XPF</b></td></tr>
            </table>
            <p>Consignes&nbsp;:</p>
            <ul>
              <li>Enregistrement <u>45 minutes</u> avant le départ</li>
              <li>Bagage cabine : <b>5 kg</b> maximum</li>
            </ul>
            <p>Logo embarqué : <img src="data:image/png;base64,{redPixel}" width="24" height="24"></p>
            <p>Image distante (non chargée sans résolveur — confidentialité) :
               <img src="https://tracker.example.com/pixel.png" alt="bannière distante" width="300" height="50"></p>
            <blockquote><p><i>Message précédent :</i><br>Merci de me confirmer le vol de mardi.</p></blockquote>
            <hr>
            <p style="text-align:center"><a href="https://aircalin.example/gerer">Gérer ma réservation</a>
               · <small>Ne pas répondre à ce message automatique.</small></p>
            <script>alert("ceci ne doit JAMAIS apparaître");</script>
            """;

        var view = new ProHtmlView { Html = html };
        view.LinkClicked += async (s, href) =>
            await ProMessageBox.ShowInfoAsync(this, $"Lien cliqué (l'app décide quoi en faire) :\n{href}", "LinkClicked");

        return new Avalonia.Controls.ScrollViewer
        {
            Content = view,
            Padding = new Avalonia.Thickness(8),
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
    }

    private Avalonia.Controls.Control BuildMindMapDemo()
    {
        var map = new ProMindMap();
        var root = new MindMapNode("Suite ProControls");

        var synaxis = root.Add("Synaxis");
        synaxis.Add("Planning équipages").Add("FTL");
        synaxis.Add("Pairings");
        synaxis.Add("Suivi J0");

        var ovidie = root.Add("Ovidie");
        ovidie.Add("Dashboards").Add("LiveCharts2");
        ovidie.Add("Rapports");
        ovidie.Add("Budget");

        var outlook = root.Add("Cible Outlook");
        var faits = outlook.Add("Faits ✅");
        faits.Add("HtmlView");
        faits.Add("RichEdit");
        faits.Add("ListView");
        faits.Add("Scheduler");
        outlook.Add("Backend (IMAP, stockage)");

        var qualite = root.Add("Qualité");
        qualite.Add("176 tests");
        qualite.Add("Dark mode");
        qualite.Add("Raccourcis clavier");

        root.Add("Idées en vrac").Add("… Tab pour ajouter !");

        map.Root = root;

        void SetStatus(string message)
        {
            var statusBar = this.FindControl<ProStatusBar>("MainStatusBar");
            if (statusBar != null && statusBar.Items.Count > 0)
                statusBar.Items[0].Text = message;
        }

        map.SelectedNodeChanged += (s, e) =>
        {
            if (map.SelectedNode != null)
                SetStatus($"Nœud : {map.SelectedNode.Text} ({map.SelectedNode.DescendantCount} descendants)");
        };
        map.NodeReparenting += (s, e) =>
            SetStatus($"« {e.Node.Text} » → sous « {e.NewParent.Text} »");

        var toolbar = new ProToolbar();
        toolbar.AddButton(null, "Radial", () => map.LayoutMode = MindMapLayoutMode.Radial);
        toolbar.AddButton(null, "Arbre →", () => map.LayoutMode = MindMapLayoutMode.TreeRight);
        toolbar.AddButton(null, "Arbre ↓", () => map.LayoutMode = MindMapLayoutMode.TreeDown);
        toolbar.AddSeparator();
        toolbar.AddButton("🎯", "Centrer", () => map.CenterOnRoot());
        toolbar.AddButton("🔍", "Tout voir", () => map.ZoomToFit());
        toolbar.AddSeparator();
        toolbar.AddButton("🖼", "Export PNG", async () =>
        {
            var path = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                "mindmap-export.png");
            map.ExportPng(path);
            await ProMessageBox.ShowInfoAsync(this, $"Carte exportée :\n{path}", "Export PNG");
        });
        toolbar.AddSeparator();
        toolbar.AddButton(null, "Test de charge (2 000 nœuds)", () =>
        {
            var big = new MindMapNode("Charge");
            for (int i = 0; i < 10; i++)
            {
                var b = big.Add($"Branche {i + 1}");
                for (int j = 0; j < 20; j++)
                {
                    var c = b.Add($"Sujet {i + 1}.{j + 1}");
                    for (int k = 0; k < 9; k++)
                        c.Add($"Idée {k + 1}");
                }
            }
            map.Root = big;
            map.ZoomToFit();
            SetStatus("2 000 nœuds — pan/zoom/repli doivent rester fluides");
        });

        var hint = new Avalonia.Controls.TextBlock
        {
            Text = "Tab = enfant · Enter = frère · F2/double-clic = éditer · Suppr = retirer · glisser un nœud = re-parenter · glisser le fond = déplacer · Ctrl+molette = zoom · Espace = replier",
            FontSize = 11,
            Margin = new Avalonia.Thickness(8, 2),
            Foreground = new Avalonia.Media.SolidColorBrush(
                ProControls.Theme.ProTheme.Text.Secondary)
        };

        var layout = new Avalonia.Controls.DockPanel();
        Avalonia.Controls.DockPanel.SetDock(toolbar, Avalonia.Controls.Dock.Top);
        Avalonia.Controls.DockPanel.SetDock(hint, Avalonia.Controls.Dock.Top);
        layout.Children.Add(toolbar);
        layout.Children.Add(hint);
        layout.Children.Add(map);
        return layout;
    }


    /// <summary>
    /// Vitrine de ProRoster : un mois de roster equipage, une voie par journee.
    /// </summary>
    /// <remarks>
    /// Les teintes de statut vivent ICI, pas dans la bibliotheque : le controle ne connait
    /// que des Color? poses par l'application. C'est ce qui lui permet de servir aussi bien
    /// un roster aerien qu'un planning d'atelier.
    /// </remarks>
    private Avalonia.Controls.Control BuildRosterDemo()
    {
        var roster = new ProRoster { LaneSpan = RosterEngine.Day };

        // Palette d'exemple, cote application (reprise de la frise d'origine).
        Avalonia.Media.Color Duty(byte r, byte g, byte b) => Avalonia.Media.Color.FromArgb(128, r, g, b);
        var amPm = Duty(65, 105, 225);
        var jour = Duty(139, 90, 43);
        var astreinte = Duty(255, 165, 0);
        var repos = Duty(128, 128, 128);
        var conge = Duty(169, 169, 169);
        var bureau = Duty(0, 128, 128);
        var fdp = Avalonia.Media.Color.FromArgb(204, 0, 128, 0);
        var vol = Avalonia.Media.Color.FromArgb(255, 30, 144, 255);
        var sol = Avalonia.Media.Color.FromArgb(255, 139, 90, 43);
        var reunion = Avalonia.Media.Color.FromArgb(255, 0, 128, 128);

        var first = new System.DateTime(System.DateTime.Today.Year, System.DateTime.Today.Month, 1);

        var model = new RosterModel { Origin = first };
        foreach (var lane in RosterEngine.BuildDailyLanes(
            first, 31,
            d => d.ToString("ddd dd/MM"),
            d => d.Day == 1 ? d.ToString("MMMM") : null))
        {
            model.Lanes.Add(lane);
        }

        void Duty0(int day, string code, Avalonia.Media.Color color, double from, double to)
        {
            if (day >= model.Lanes.Count) return;
            var lane = model.Lanes[day];
            lane.Bands.Add(new RosterBand(lane.WindowStart.AddHours(from), lane.WindowStart.AddHours(to), code)
            {
                Color = color,
                StartLabel = lane.WindowStart.AddHours(from).ToString("HH:mm"),
                EndLabel = lane.WindowStart.AddHours(to).ToString("HH:mm"),
            });
        }

        void Fdp(int day, double from, double to)
        {
            if (day >= model.Lanes.Count) return;
            var lane = model.Lanes[day];
            lane.Bars.Add(new RosterBar(lane.WindowStart.AddHours(from), lane.WindowStart.AddHours(to)) { Color = fdp });
        }

        void Leg(int day, string label, double from, double to, Avalonia.Media.Color color, int track = 1)
        {
            if (day >= model.Lanes.Count) return;
            var lane = model.Lanes[day];
            lane.Blocks.Add(new RosterBlock(lane.WindowStart.AddHours(from), lane.WindowStart.AddHours(to), label, track)
            {
                Color = color,
            });
        }

        // Un mois plausible : rotations, astreintes, bureau, repos.
        for (int d = 0; d < 31; d++)
        {
            switch (d % 7)
            {
                case 0:
                    Duty0(d, "AM", amPm, 5, 15);
                    Fdp(d, 5.75, 14.5);
                    Leg(d, "TY201", 6.5, 8, vol);
                    Leg(d, "TY204", 9, 10.5, vol);
                    Leg(d, "TY211", 12, 13.75, vol);
                    break;
                case 1:
                    Duty0(d, "PM", amPm, 12, 22);
                    Fdp(d, 12.75, 21.5);
                    Leg(d, "TY320", 13.5, 15.25, vol);
                    Leg(d, "TY327", 17, 19, vol);
                    break;
                case 2:
                    Duty0(d, "JOUR", jour, 8, 17.5);
                    Fdp(d, 8.75, 17);
                    Leg(d, "TY105", 9.5, 11, vol);
                    Leg(d, "Briefing", 8.25, 8.75, sol, 0);
                    break;
                case 3:
                    Duty0(d, "AST", astreinte, 5, 19);
                    break;
                case 4:
                    Duty0(d, "BUREAU", bureau, 9, 17);
                    Leg(d, "Prod PN", 10, 11.5, reunion, 0);
                    Leg(d, "BEO", 14, 15, reunion, 0);
                    break;
                case 5:
                    Duty0(d, "REP", repos, 0, 24);
                    break;
                default:
                    Duty0(d, "OFF", conge, 0, 24);
                    break;
            }
        }

        // Cas 1 : un jour de repos qui porte tout de meme une activite. La bande couvre
        // 00:00-24:00, les crochets marquent l'amplitude reelle. Sans eux, l'info se perd.
        if (model.Lanes.Count > 12)
        {
            var lane = model.Lanes[12];
            lane.Bands.Clear();
            lane.Bands.Add(new RosterBand(lane.WindowStart, lane.WindowStart.AddHours(24), "REP")
            {
                Color = repos,
                MarkerStart = lane.WindowStart.AddHours(14),
                MarkerEnd = lane.WindowStart.AddHours(17),
            });
            lane.HasWarning = true;
            Leg(12, "Simu", 14.5, 16.5, sol, 0);
        }

        // Cas 2 : un service de nuit. Decision du 17/09/2026 : on le COUPE sur deux voies
        // au lieu de le tronquer a minuit comme le faisait la frise d'origine.
        if (model.Lanes.Count > 20)
        {
            var start = model.Lanes[19].WindowStart.AddHours(21);
            var end = model.Lanes[19].WindowStart.AddHours(29);

            foreach (var segment in RosterEngine.Split(start, end, model.Lanes[19].WindowStart, RosterEngine.Day))
            {
                var index = 19 + segment.LaneOffset;
                if (index < 0 || index >= model.Lanes.Count) continue;

                var lane = model.Lanes[index];
                lane.Bands.Clear();
                lane.Bands.Add(new RosterBand(segment.Start, segment.End, "NUIT")
                {
                    Color = Avalonia.Media.Color.FromArgb(128, 90, 60, 140),
                    StartLabel = segment.Start.ToString("HH:mm"),
                    EndLabel = segment.End.ToString("HH:mm"),
                });
                lane.Blocks.Add(new RosterBlock(segment.Start.AddMinutes(45), segment.End.AddMinutes(-45), "TY900", 1)
                {
                    Color = vol,
                });
            }
        }

        roster.Model = model;
        roster.ScrollToToday();

        void SetStatus(string message)
        {
            var statusBar = this.FindControl<ProStatusBar>("MainStatusBar");
            if (statusBar != null && statusBar.Items.Count > 0)
                statusBar.Items[0].Text = message;
        }

        // Le controle affiche, le metier decide : chaque mutation passe par un *ing annulable.
        roster.BlockClicked += (s, e) =>
            SetStatus($"{e.Block.Label} — {e.Block.Start:ddd dd/MM HH:mm} → {e.Block.End:HH:mm} ({e.Lane.Label})");

        roster.BlockDoubleClicked += async (s, e) =>
            await ProMessageBox.ShowInfoAsync(this,
                $"{e.Block.Label}\n{e.Block.Start:dddd dd MMMM, HH:mm} → {e.Block.End:HH:mm}\nVoie : {e.Lane.Label}",
                "Activite");

        roster.LaneSummaryRequested += (s, e) =>
        {
            var blocs = e.Lane.Blocks.Count;
            var total = System.TimeSpan.Zero;
            foreach (var b in e.Lane.Blocks) total += b.End - b.Start;
            SetStatus($"Bilan {e.Lane.Label} : {blocs} activite(s), {total.TotalHours:0.0} h cumulees");
        };

        roster.EmptySlotDoubleClicked += (s, e) =>
            SetStatus($"Creneau libre vise : {e.Lane.Label} a {e.Time:HH:mm}");

        // Un refus metier : on n'accepte pas de poser une activite avant 04:00.
        roster.BlockMoving += (s, e) =>
        {
            if (e.NewStart.TimeOfDay < System.TimeSpan.FromHours(4))
            {
                e.Cancel = true;
                SetStatus($"Refuse : {e.Block.Label} ne peut pas commencer avant 04:00");
            }
        };

        roster.BlockMoved += (s, e) =>
            SetStatus($"{e.Block.Label} deplace sur {e.TargetLane.Label} a {e.NewStart:HH:mm}");

        roster.BlockContextMenuRequested += (s, e) =>
        {
            var menu = new ProContextMenu();
            menu.Add($"« {e.Block.Label} »", null, null, null);
            menu.AddSeparator();
            menu.Add("Dupliquer", "\u29C9", null, () =>
            {
                e.Lane.Blocks.Add(new RosterBlock(e.Block.Start, e.Block.End, e.Block.Label, e.Block.Track)
                {
                    Color = e.Block.Color,
                });
                roster.Model?.Touch();
                SetStatus($"{e.Block.Label} duplique");
            });
            menu.Add("Supprimer", "\u2716", null, () =>
            {
                e.Lane.Blocks.Remove(e.Block);
                roster.Model?.Touch();
                SetStatus($"{e.Block.Label} supprime");
            });
            menu.Show(roster, e.Position);
        };

        var toolbar = new ProToolbar();
        toolbar.AddButton(null, "Aujourd'hui", () => { roster.ScrollToToday(); SetStatus("Roster recadre sur aujourd'hui"); });
        toolbar.AddToggle(null, "Lecture seule", false).Click += (s, e) => roster.IsReadOnly = !roster.IsReadOnly;
        toolbar.AddSeparator();
        toolbar.AddButton("−", null, () => { roster.Axis.PixelsPerHour /= 1.25; roster.InvalidateVisual(); });
        toolbar.AddButton("+", null, () => { roster.Axis.PixelsPerHour *= 1.25; roster.InvalidateVisual(); });
        toolbar.AddButton(null, "Ajuster", () => roster.ZoomToFit());
        toolbar.AddSeparator();
        // Les deux formes que le meme controle sait rendre. Elles ne se melangent pas :
        // des voies journalieres avec LaneSpan > 1 jour se chevaucheraient.
        var parJour = model;
        var parNavigant = BuildCrewRosterModel(first, vol, sol, reunion, fdp);

        toolbar.AddButton(null, "Par journee", () =>
        {
            roster.LaneSpan = RosterEngine.Day;
            roster.Model = parJour;
            roster.ZoomToFit();
            SetStatus("Une voie = une journee : l'axe redemarre a 00:00 sur chaque ligne");
        });
        toolbar.AddButton(null, "Par navigant", () =>
        {
            roster.LaneSpan = System.TimeSpan.FromDays(7);
            roster.Model = parNavigant;
            roster.ZoomToFit();
            SetStatus("Une voie = un navigant sur sept jours : axe continu, deux niveaux de graduations");
        });

        var layout = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
        };
        Grid.SetRow(toolbar, 0);
        layout.Children.Add(toolbar);
        Grid.SetRow(roster, 1);
        layout.Children.Add(roster);

        return layout;
    }


    /// <summary>
    /// Seconde forme : une voie par navigant, toutes sur la MEME origine, sur sept jours.
    /// </summary>
    /// <remarks>
    /// C'est la forme dont myFlightOpsBoard a besoin, et la preuve de reemploi que la charte
    /// exige : meme controle, meme modele, une geometrie differente portee par WindowStart.
    /// </remarks>
    private static RosterModel BuildCrewRosterModel(
        System.DateTime origin,
        Avalonia.Media.Color vol,
        Avalonia.Media.Color sol,
        Avalonia.Media.Color reunion,
        Avalonia.Media.Color fdp)
    {
        var model = new RosterModel { Origin = origin };
        var equipages = new[] { "PLL", "BRT", "KAO", "MNU", "TVA", "WLS", "LIF", "MAR" };
        var amplitude = Avalonia.Media.Color.FromArgb(110, 65, 105, 225);
        var random = new System.Random(17);

        foreach (var code in equipages)
        {
            var lane = new RosterLane(code, code, origin) { SubLabel = "CDB" };

            for (int day = 0; day < 7; day++)
            {
                if (random.Next(4) == 0) continue;   // jour de repos

                var start = origin.AddDays(day).AddHours(5 + random.Next(9));
                var end = start.AddHours(8 + random.Next(3));

                lane.Bands.Add(new RosterBand(start, end, null) { Color = amplitude });
                lane.Bars.Add(new RosterBar(start.AddMinutes(45), end.AddMinutes(-30)) { Color = fdp });

                var legs = 1 + random.Next(3);
                for (int leg = 0; leg < legs; leg++)
                {
                    var legStart = start.AddHours(1 + leg * 2.2);
                    if (legStart.AddHours(1.4) >= end) break;

                    lane.Blocks.Add(new RosterBlock(legStart, legStart.AddHours(1.4), $"TY{200 + random.Next(99)}", 1)
                    {
                        Color = leg % 3 == 2 ? sol : vol,
                    });
                }
            }

            if (lane.Blocks.Count == 0)
            {
                lane.HasWarning = true;
                lane.Bands.Add(new RosterBand(origin, origin.AddDays(7), "Sans activite")
                {
                    Color = Avalonia.Media.Color.FromArgb(60, 128, 128, 128),
                });
            }

            model.Lanes.Add(lane);
        }

        return model;
    }

    private Avalonia.Controls.Control BuildSchedulerDemo()
    {
        var scheduler = new ProScheduler();
        var monday = System.DateTime.Today.AddDays(-(((int)System.DateTime.Today.DayOfWeek + 6) % 7));

        // Réunions de la semaine (avec chevauchements volontaires)
        scheduler.Events.Add(new ScheduleEvent("Comité de pilotage",
            monday.AddHours(9), monday.AddHours(11))
        { Location = "Salle A", Color = Avalonia.Media.Color.Parse("#0B67B2") });
        scheduler.Events.Add(new ScheduleEvent("Point RH",
            monday.AddHours(10), monday.AddHours(11.5))
        { Color = Avalonia.Media.Color.Parse("#77216F") });
        scheduler.Events.Add(new ScheduleEvent("Revue budget Ovidie",
            monday.AddDays(1).AddHours(14), monday.AddDays(1).AddHours(16))
        { Color = Avalonia.Media.Color.Parse("#C64614") });
        scheduler.Events.Add(new ScheduleEvent("Démo Synaxis",
            monday.AddDays(3).AddHours(15), monday.AddDays(3).AddHours(16.5))
        { Location = "Visio" });
        scheduler.Events.Add(new ScheduleEvent("Astreinte",
            monday.AddDays(4), monday.AddDays(5)) { AllDay = true, Color = Avalonia.Media.Color.Parse("#107C10") });

        // Stand-up récurrent (lun/mer/ven 08:30)
        var standup = new ScheduleEvent("Stand-up équipe",
            monday.AddHours(8.5), monday.AddHours(8.75))
        { Color = Avalonia.Media.Color.Parse("#5E2750") };
        standup.Recurrence.Kind = ScheduleRecurrenceKind.Weekly;
        standup.Recurrence.DaysOfWeek.Add(System.DayOfWeek.Monday);
        standup.Recurrence.DaysOfWeek.Add(System.DayOfWeek.Wednesday);
        standup.Recurrence.DaysOfWeek.Add(System.DayOfWeek.Friday);
        scheduler.Events.Add(standup);

        void SetStatus(string message)
        {
            var statusBar = this.FindControl<ProStatusBar>("MainStatusBar");
            if (statusBar != null && statusBar.Items.Count > 0)
                statusBar.Items[0].Text = message;
        }

        scheduler.TimeSlotDoubleClicked += (s, time) =>
        {
            scheduler.Events.Add(new ScheduleEvent("Nouveau rendez-vous",
                time, time.AddMinutes(60)));
            SetStatus($"Rendez-vous créé : {time:g}");
        };
        scheduler.EventChanged += (s, evt) =>
            SetStatus($"« {evt.Subject} » déplacé : {evt.Start:g} → {evt.End:t}");
        scheduler.EventDoubleClicked += async (s, evt) =>
            await ProMessageBox.ShowInfoAsync(this,
                $"{evt.Subject}\n{evt.Start:g} → {evt.End:t}\n{evt.Location}", "Événement");

        // Barre de navigation
        var toolbar = new ProToolbar();
        toolbar.AddButton("◀", null, () => scheduler.Navigate(-1));
        toolbar.AddButton(null, "Aujourd'hui", () => scheduler.GoToToday());
        toolbar.AddButton("▶", null, () => scheduler.Navigate(1));
        toolbar.AddSeparator();
        toolbar.AddButton(null, "Jour", () => scheduler.ViewMode = SchedulerViewMode.Day);
        toolbar.AddButton(null, "Sem. ouvrée", () => scheduler.ViewMode = SchedulerViewMode.WorkWeek);
        toolbar.AddButton(null, "Semaine", () => scheduler.ViewMode = SchedulerViewMode.Week);
        toolbar.AddButton(null, "Mois", () => scheduler.ViewMode = SchedulerViewMode.Month);

        // Navigateur de dates natif : clic = aller à la date, gras = jours à événements
        var monthCalendar = new ProMonthCalendar
        {
            Margin = new Avalonia.Thickness(8),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        foreach (var occ in ScheduleEngine.ExpandAll(scheduler.Events,
            System.DateTime.Today.AddMonths(-2), System.DateTime.Today.AddMonths(3)))
        {
            monthCalendar.BoldDates.Add(occ.Start.Date);
        }
        monthCalendar.SelectedDateChanged += (s, e) =>
        {
            if (monthCalendar.SelectedDate is { } date)
                scheduler.DisplayDate = date;
        };
        scheduler.Events.CollectionChanged += (s, e) =>
        {
            monthCalendar.BoldDates.Clear();
            foreach (var occ in ScheduleEngine.ExpandAll(scheduler.Events,
                System.DateTime.Today.AddMonths(-2), System.DateTime.Today.AddMonths(3)))
                monthCalendar.BoldDates.Add(occ.Start.Date);
            monthCalendar.InvalidateVisual();
        };

        var layout = new Avalonia.Controls.DockPanel();
        Avalonia.Controls.DockPanel.SetDock(toolbar, Avalonia.Controls.Dock.Top);
        Avalonia.Controls.DockPanel.SetDock(monthCalendar, Avalonia.Controls.Dock.Right);
        layout.Children.Add(toolbar);
        layout.Children.Add(monthCalendar);
        layout.Children.Add(scheduler);
        return layout;
    }

    private sealed class DemoMail
    {
        public string From = "";
        public string Subject = "";
        public string Preview = "";
        public System.DateTime Date;
        public bool IsRead;
        public bool IsFlagged;
        public string Body = "";
    }

    private Avalonia.Controls.Control BuildInboxDemo()
    {
        void SetStatus(string message)
        {
            var statusBar = this.FindControl<ProStatusBar>("MainStatusBar");
            if (statusBar != null && statusBar.Items.Count > 0)
                statusBar.Items[0].Text = message;
        }

        // Boîte de réception générée depuis les employés de la démo
        var rng = new System.Random(7);
        var subjects = new[]
        {
            ("Point hebdo équipe", "Voici l'ordre du jour de la réunion de"),
            ("Validation du planning", "Peux-tu confirmer les dates du sprint"),
            ("Rapport mensuel", "Le rapport est disponible, montant total"),
            ("Demande de congés", "Je souhaiterais poser des congés du"),
            ("Incident résolu", "L'incident de production de ce matin est"),
            ("Nouvelle procédure", "La procédure d'embarquement change à partir de")
        };

        var mails = new ObservableCollection<DemoMail>(
            Employees.Take(40).Select((emp, i) =>
            {
                var (subject, preview) = subjects[rng.Next(subjects.Length)];
                var date = System.DateTime.Now.AddHours(-rng.Next(0, 24 * 20));
                return new DemoMail
                {
                    From = emp.FullName,
                    Subject = subject,
                    Preview = preview + "…",
                    Date = date,
                    IsRead = rng.Next(3) > 0,
                    IsFlagged = rng.Next(8) == 0,
                    Body = $"<h2>{subject}</h2><p>Bonjour,</p><p>{preview} <b>détails à suivre</b>.</p>" +
                           $"<blockquote><p>Message précédent…</p></blockquote>" +
                           $"<p>Cordialement,<br><i>{emp.FullName}</i> — {emp.Department}</p>"
                };
            }).OrderByDescending(m => m.Date));

        static string GroupOf(DemoMail mail)
        {
            var today = System.DateTime.Today;
            if (mail.Date.Date == today) return "Aujourd'hui";
            if (mail.Date.Date == today.AddDays(-1)) return "Hier";
            if (mail.Date.Date >= today.AddDays(-7)) return "Cette semaine";
            return "Plus ancien";
        }

        var list = new ProListView
        {
            ItemsSource = mails,
            SelectionMode = Avalonia.Controls.SelectionMode.Multiple,
            GroupSelector = item => GroupOf((DemoMail)item),
            ItemAdapter = item =>
            {
                var mail = (DemoMail)item;
                var initials = string.Concat(mail.From.Split(' ').Take(2).Select(p => p[0]));
                return new ProListItemContent
                {
                    Title = mail.From,
                    Subtitle = $"{mail.Subject} — {mail.Preview}",
                    Trailing = mail.Date.Date == System.DateTime.Today
                        ? mail.Date.ToString("HH:mm") : mail.Date.ToString("dd/MM"),
                    Icon = initials,
                    Emphasized = !mail.IsRead,
                    Badge = mail.IsFlagged ? "⚑" : null
                };
            }
        };

        // Volet de lecture
        var reader = new ProHtmlView();
        var readerHost = new Avalonia.Controls.ScrollViewer
        {
            Content = reader,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        list.SelectionChanged += (s, e) =>
        {
            if (list.SelectedItem is DemoMail mail)
            {
                reader.Html = mail.Body;
                if (!mail.IsRead)
                {
                    mail.IsRead = true; // ouvrir = lu, comme Outlook
                    list.Refresh();
                }
            }
        };

        // Actions au survol : lu/non-lu, drapeau, suppression
        list.HoverActions.Add(new ProListHoverAction("✉", "Marquer non lu", item =>
        {
            ((DemoMail)item).IsRead = false;
            list.Refresh();
        }));
        list.HoverActions.Add(new ProListHoverAction("⚑", "Drapeau", item =>
        {
            var mail = (DemoMail)item;
            mail.IsFlagged = !mail.IsFlagged;
            list.Refresh();
        }));
        list.HoverActions.Add(new ProListHoverAction("🗑", "Supprimer", item =>
        {
            mails.Remove((DemoMail)item); // ObservableCollection : la liste suit
        }));

        // ── Shell Outlook : rail + dossiers + recherche + toasts ──

        // Rail de modules
        var navBar = new ProNavBar();
        var mailModule = navBar.Add("📧", "Courrier");
        navBar.Add("📅", "Agenda");
        navBar.Add("👥", "Contacts");
        navBar.Add("✓", "Tâches");

        void UpdateUnreadBadge()
        {
            var unread = mails.Count(m => !m.IsRead);
            mailModule.Badge = unread > 0 ? unread.ToString() : null;
        }
        UpdateUnreadBadge();
        mails.CollectionChanged += (s, e) => UpdateUnreadBadge();
        list.SelectionChanged += (s, e) => UpdateUnreadBadge();
        navBar.SelectedIndexChanged += (s, e) =>
            SetStatus($"Module : {navBar.SelectedItem?.Label}");

        // Arbre de dossiers, cible de drop
        var folders = new ProTreeView { AllowDropItems = true };
        var inbox = folders.Add("Boîte de réception", "📥");
        folders.Add("Archives", "🗄");
        folders.Add("Traité", "✅");
        folders.Add("Corbeille", "🗑");
        folders.SelectedNode = inbox;

        list.EnableDragItems = true;
        folders.ItemDropped += (s, drop) =>
        {
            if (drop.Item is DemoMail mail && !ReferenceEquals(drop.Node, inbox))
            {
                mails.Remove(mail);
                ProToast.Show(this, "Message déplacé",
                    $"« {mail.Subject} » → {drop.Node.Text}", "📁");
            }
        };

        // Recherche à suggestions (filtre la liste)
        var search = new ProSearchControl
        {
            Margin = new Avalonia.Thickness(4),
            SuggestionsProvider = query => mails
                .Where(m => m.From.Contains(query, System.StringComparison.OrdinalIgnoreCase)
                    || m.Subject.Contains(query, System.StringComparison.OrdinalIgnoreCase))
                .Select(m => (object)$"{m.From} — {m.Subject}")
                .Distinct()
        };
        search.SearchRequested += (s, query) =>
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                list.ItemsSource = mails;
                return;
            }
            list.ItemsSource = new ObservableCollection<DemoMail>(mails.Where(m =>
                m.From.Contains(query, System.StringComparison.OrdinalIgnoreCase)
                || m.Subject.Contains(query, System.StringComparison.OrdinalIgnoreCase)));
            SetStatus($"Recherche : « {query} »");
        };
        search.TextChanged += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(search.Text))
                list.ItemsSource = mails;
        };

        // Simulation d'arrivée de mail → toast cliquable
        var simulate = new ProButton
        {
            Text = "✉ Simuler un mail",
            Variant = ButtonVariant.Ghost,
            Size = ButtonSize.Small,
            Margin = new Avalonia.Thickness(4, 0, 4, 4)
        };
        simulate.Click += (s, e) =>
        {
            var mail = new DemoMail
            {
                From = "Tour de contrôle",
                Subject = "Créneau modifié",
                Preview = "Le créneau du vol TY201 est avancé de 15 minutes…",
                Date = System.DateTime.Now,
                Body = "<h2>Créneau modifié</h2><p>Le créneau du vol <b>TY201</b> est avancé de 15 minutes.</p>"
            };
            mails.Insert(0, mail);
            ProToast.Show(this, "Nouveau message", $"{mail.From} — {mail.Subject}", "📧",
                onClick: () => list.SelectItem(mail));
        };

        // Colonne liste : recherche + bouton + liste
        var listColumn = new Avalonia.Controls.DockPanel();
        Avalonia.Controls.DockPanel.SetDock(search, Avalonia.Controls.Dock.Top);
        Avalonia.Controls.DockPanel.SetDock(simulate, Avalonia.Controls.Dock.Top);
        listColumn.Children.Add(search);
        listColumn.Children.Add(simulate);
        listColumn.Children.Add(list);

        var grid = new Avalonia.Controls.Grid
        {
            ColumnDefinitions = new Avalonia.Controls.ColumnDefinitions("Auto,150,340,4,*")
        };
        Avalonia.Controls.Grid.SetColumn(navBar, 0);
        Avalonia.Controls.Grid.SetColumn(folders, 1);
        Avalonia.Controls.Grid.SetColumn(listColumn, 2);
        var splitter = new Avalonia.Controls.GridSplitter
        {
            Background = Avalonia.Media.Brushes.Transparent,
            ResizeDirection = Avalonia.Controls.GridResizeDirection.Columns
        };
        Avalonia.Controls.Grid.SetColumn(splitter, 3);
        Avalonia.Controls.Grid.SetColumn(readerHost, 4);
        grid.Children.Add(navBar);
        grid.Children.Add(folders);
        grid.Children.Add(listColumn);
        grid.Children.Add(splitter);
        grid.Children.Add(readerHost);
        return grid;
    }

    private Avalonia.Controls.Control BuildRichEditDemo()
    {
        var edit = new ProRichEdit
        {
            Html = "<h2>Brouillon</h2><p>Bonjour,</p><p>Voici un texte <b>gras</b>, " +
                   "<i>italique</i> et <u>souligné</u> à retravailler.</p><p>Cordialement.</p>"
        };

        var toolbar = new ProToolbar();
        toolbar.AddButton("↩", null, () => edit.Undo());
        toolbar.AddButton("↪", null, () => edit.Redo());
        toolbar.AddSeparator();
        toolbar.AddButton("𝐁", null, () => edit.ToggleBold());
        toolbar.AddButton("𝘐", null, () => edit.ToggleItalic());
        toolbar.AddButton("U̲", null, () => edit.ToggleUnderline());
        toolbar.AddButton("S̶", null, () => edit.ToggleStrikethrough());
        toolbar.AddSeparator();
        toolbar.AddButton("H2", null, () => edit.SetHeading(2));
        toolbar.AddButton("¶", null, () => edit.SetHeading(0));
        toolbar.AddSeparator();
        // v2 : listes, alignement, tailles, couleurs, lien, image
        toolbar.AddButton("•≡", "Liste à puces (Ctrl+Maj+L)", () => edit.ToggleBulletList());
        toolbar.AddButton("1≡", "Liste numérotée", () => edit.ToggleNumberedList());
        toolbar.AddSeparator();
        toolbar.AddButton("⯇", "Aligner à gauche", () => edit.SetAlignment(Avalonia.Media.TextAlignment.Left));
        toolbar.AddButton("≡", "Centrer (Ctrl+E)", () => edit.SetAlignment(Avalonia.Media.TextAlignment.Center));
        toolbar.AddButton("⯈", "Aligner à droite", () => edit.SetAlignment(Avalonia.Media.TextAlignment.Right));
        toolbar.AddSeparator();
        toolbar.AddButton("A⁺", "Agrandir (18 px)", () => edit.SetFontSize(18));
        toolbar.AddButton("A⁻", "Taille normale", () => edit.SetFontSize(null));
        toolbar.AddButton("🔴", "Texte rouge", () => edit.SetTextColor(ProControls.Theme.ProTheme.Accent.Error));
        toolbar.AddButton("🖍", "Surligner jaune", () => edit.SetHighlight(Avalonia.Media.Color.Parse("#FFF3B0")));
        toolbar.AddButton("⌫🎨", "Effacer couleurs", () =>
        {
            edit.SetTextColor(null);
            edit.SetHighlight(null);
        });
        toolbar.AddSeparator();
        toolbar.AddButton("🔗", "Lien (Ctrl+K)", () => _ = edit.InsertLinkInteractiveAsync());
        toolbar.AddButton("🖼", "Insérer une image", () => _ = edit.InsertImageInteractiveAsync());
        toolbar.AddSeparator();
        toolbar.AddButton("👁", "Aperçu HTML", async () =>
        {
            // La boucle est bouclée : le HTML produit par l'éditeur est rendu par le viewer
            var preview = new ProHtmlView { Html = edit.Html, MinWidth = 500 };
            var window = new ProWindow
            {
                Title = "Aperçu (ProHtmlView sur le HTML sérialisé)",
                Width = 560,
                Height = 420,
                Content = new Avalonia.Controls.ScrollViewer { Content = preview },
                WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner
            };
            await window.ShowDialog(this);
        });

        // Champ « À : » à jetons (suggestions = employés, validation = @)
        var recipients = new ProTokenEdit
        {
            Placeholder = "Destinataires…",
            Margin = new Avalonia.Thickness(4, 4, 4, 0),
            SuggestionsProvider = query => Employees
                .Where(emp => emp.FullName.Contains(query, System.StringComparison.OrdinalIgnoreCase)
                    || emp.Email.Contains(query, System.StringComparison.OrdinalIgnoreCase))
                .Select(emp => (object)emp),
            TokenText = token => token is Employee emp ? emp.FullName : token.ToString() ?? "",
            TokenValidator = text => text.Contains('@') ? text : null // texte libre = e-mail valide
        };

        var toRow = new Avalonia.Controls.DockPanel { Margin = new Avalonia.Thickness(4, 2) };
        var toLabel = new Avalonia.Controls.TextBlock
        {
            Text = "À :",
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(6, 0)
        };
        Avalonia.Controls.DockPanel.SetDock(toLabel, Avalonia.Controls.Dock.Left);
        toRow.Children.Add(toLabel);
        toRow.Children.Add(recipients);

        var layout = new Avalonia.Controls.DockPanel();
        Avalonia.Controls.DockPanel.SetDock(toolbar, Avalonia.Controls.Dock.Top);
        Avalonia.Controls.DockPanel.SetDock(toRow, Avalonia.Controls.Dock.Top);
        layout.Children.Add(toolbar);
        layout.Children.Add(toRow);
        layout.Children.Add(new Avalonia.Controls.ScrollViewer
        {
            Content = edit,
            Padding = new Avalonia.Thickness(4),
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        });
        return layout;
    }

    private Avalonia.Controls.Control BuildGanttDemo()
    {
        var gantt = new ProGantt();
        var today = System.DateTime.Today;

        var project = new GanttProject();
        project.Calendar.AddHoliday(new System.DateTime(today.Year, 7, 14)); // fête nationale

        project.BeginUpdate();

        var etude = project.Add("Étude", today, today);
        var cahier = etude.Add("Cahier des charges", today.AddDays(-15), today.AddDays(-8), 100);
        var maquettes = etude.Add("Maquettes", today.AddDays(-7), today.AddDays(-1), 100);
        maquettes.DependsOn(cahier);
        var jalonEtude = etude.AddMilestone("Validation étude", today);
        jalonEtude.DependsOn(maquettes);

        var dev = project.Add("Développement", today, today);
        var socle = dev.Add("Socle technique", today.AddDays(1), today.AddDays(9), 60);
        socle.DependsOn(jalonEtude);
        var moduleA = dev.Add("Module A", today.AddDays(10), today.AddDays(22), 20);
        moduleA.DependsOn(socle);
        var moduleB = dev.Add("Module B", today.AddDays(10), today.AddDays(28));
        moduleB.DependsOn(socle);
        var integration = dev.Add("Intégration", today.AddDays(29), today.AddDays(35));
        integration.DependsOn(moduleA);
        integration.DependsOn(moduleB);

        var recette = project.Add("Recette", today, today);
        var tests = recette.Add("Campagne de tests", today.AddDays(36), today.AddDays(45));
        tests.DependsOn(integration);
        var jalonGo = recette.AddMilestone("Go production", today.AddDays(46));
        jalonGo.DependsOn(tests);

        project.EndUpdate();
        gantt.Project = project;
        gantt.ScrollToToday();

        void SetStatus(string message)
        {
            var statusBar = this.FindControl<ProStatusBar>("MainStatusBar");
            if (statusBar != null && statusBar.Items.Count > 0)
                statusBar.Items[0].Text = message;
        }

        gantt.SelectedTaskChanged += (s, e) =>
        {
            if (gantt.SelectedTask != null)
                SetStatus($"Tâche : {gantt.SelectedTask.Name} ({gantt.SelectedTask.EffectiveProgress:F0} %)");
        };

        // Validation métier annulable : une tâche terminée ne se déplace pas
        gantt.TaskDatesChanging += (s, e) =>
        {
            if (e.Task.Progress >= 100)
            {
                e.Cancel = true;
                SetStatus($"« {e.Task.Name} » est terminée : déplacement refusé (TaskDatesChanging.Cancel)");
            }
        };
        gantt.TaskDatesChanged += (s, task) =>
            SetStatus($"« {task.Name} » : {task.Start:dd/MM} → {task.End:dd/MM}");
        gantt.ProgressChanged += (s, task) =>
            SetStatus($"« {task.Name} » : avancement {task.Progress:F0} %");
        gantt.LinkCreated += (s, e) =>
            SetStatus($"Lien créé : « {e.Predecessor.Name} » → « {e.Successor.Name} »");
        gantt.LinkRemoved += (s, e) =>
            SetStatus($"Lien supprimé : « {e.Predecessor.Name} » → « {e.Successor.Name} »");

        // Édition structurelle (L1) : Ins / Suppr / Alt+Maj+←→ / Alt+↑↓, ou clic droit
        gantt.TaskInserted += (s, task) =>
            SetStatus($"Tâche insérée : saisis son nom (Entrée valide, Échap annule)");
        gantt.TaskDeleted += (s, task) =>
            SetStatus($"« {task.Name} » supprimée (sous-arbre et liens entrants compris) — Ctrl+Z annule");
        gantt.TaskStructureChanged += (s, task) =>
            SetStatus($"« {task.Name} » remaniée — Ctrl+Z annule");

        // Validation métier annulable : on ne supprime pas une tâche terminée
        gantt.TaskDeleting += (s, e) =>
        {
            if (e.Task is { Progress: >= 100 })
            {
                e.Cancel = true;
                SetStatus($"« {e.Task.Name} » est terminée : suppression refusée (TaskDeleting.Cancel)");
            }
        };

        // Barre d'outils : test de charge 2 500 tâches + retour aujourd'hui
        var toolbar = new Avalonia.Controls.StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(8, 6)
        };
        var todayButton = new ProButton { Text = "Aujourd'hui", Variant = ButtonVariant.Secondary, Size = ButtonSize.Small };
        todayButton.Click += (s, e) => gantt.ScrollToToday();

        var criticalToggle = new ProToggleSwitch { OnLabel = "Chemin critique", OffLabel = "Chemin critique", ShowLabels = true };
        criticalToggle.Toggled += (s, e) => gantt.HighlightCriticalPath = criticalToggle.IsOn;

        var autoToggle = new ProToggleSwitch { OnLabel = "Ordonnancement auto", OffLabel = "Ordonnancement auto", ShowLabels = true };
        autoToggle.Toggled += (s, e) =>
        {
            if (gantt.Project != null)
                gantt.Project.AutoSchedule = autoToggle.IsOn;
        };
        var stressButton = new ProButton { Text = "Test de charge (2 500 tâches)", Variant = ButtonVariant.Ghost, Size = ButtonSize.Small };
        stressButton.Click += (s, e) =>
        {
            var big = new GanttProject();
            big.BeginUpdate();
            var rng = new System.Random(42);
            for (int p = 0; p < 50; p++)
            {
                var phase = big.Add($"Phase {p + 1:00}", today, today);
                for (int t = 0; t < 49; t++)
                {
                    var start = today.AddDays(rng.Next(-60, 240));
                    var task = phase.Add($"Tâche {p + 1:00}.{t + 1:00}", start,
                        start.AddDays(rng.Next(2, 20)), rng.Next(0, 101));
                    if (t > 0 && rng.Next(3) == 0)
                        task.DependsOn(phase.Children[t - 1]);
                }
            }
            big.EndUpdate();
            gantt.Project = big;
            gantt.ScrollToToday();
        };
        var baselineButton = new ProButton { Text = "Figer la baseline", Variant = ButtonVariant.Secondary, Size = ButtonSize.Small };
        var baselineToggle = new ProToggleSwitch { OnLabel = "Baseline", OffLabel = "Baseline", ShowLabels = true };
        baselineButton.Click += (s, e) =>
        {
            gantt.Project?.SetBaseline();
            baselineToggle.IsOn = true;
            gantt.ShowBaseline = true;
            SetStatus("Baseline figée : déplace des tâches puis compare (barres grises = prévu)");
        };
        baselineToggle.Toggled += (s, e) => gantt.ShowBaseline = baselineToggle.IsOn;

        var exportButton = new ProButton { Text = "Export PNG", Variant = ButtonVariant.Secondary, Size = ButtonSize.Small };
        exportButton.Click += async (s, e) =>
        {
            var path = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                "gantt-export.png");
            gantt.ExportPng(path, entireProject: true);
            await ProMessageBox.ShowInfoAsync(this, $"Diagramme complet exporté :\n{path}", "Export PNG");
        };

        // Undo / redo (Ctrl+Z / Ctrl+Y aussi, contrôle focalisé)
        var undoButton = new ProButton { Text = "↶ Annuler", Variant = ButtonVariant.Secondary, Size = ButtonSize.Small, IsEnabled = false };
        var redoButton = new ProButton { Text = "↷ Rétablir", Variant = ButtonVariant.Secondary, Size = ButtonSize.Small, IsEnabled = false };
        undoButton.Click += (s, e) => gantt.Undo();
        redoButton.Click += (s, e) => gantt.Redo();
        gantt.HistoryChanged += (s, e) =>
        {
            undoButton.IsEnabled = gantt.CanUndo;
            redoButton.IsEnabled = gantt.CanRedo;
        };

        // Persistance : sauve/relit le projet en JSON
        string? savedProject = null;
        var saveButton = new ProButton { Text = "💾 Sauver", Variant = ButtonVariant.Secondary, Size = ButtonSize.Small };
        var loadButton = new ProButton { Text = "📂 Charger", Variant = ButtonVariant.Secondary, Size = ButtonSize.Small, IsEnabled = false };
        saveButton.Click += (s, e) =>
        {
            savedProject = gantt.Project?.ToJson();
            loadButton.IsEnabled = savedProject != null;
            SetStatus($"Projet sauvé ({savedProject?.Length ?? 0} caractères JSON).");
        };
        loadButton.Click += (s, e) =>
        {
            if (savedProject == null) return;
            gantt.Project = GanttProject.FromJson(savedProject); // remplace + purge l'historique
            gantt.ScrollToToday();
            SetStatus("Projet rechargé depuis le JSON sauvé.");
        };

        toolbar.Children.Add(undoButton);
        toolbar.Children.Add(redoButton);
        toolbar.Children.Add(saveButton);
        toolbar.Children.Add(loadButton);
        toolbar.Children.Add(todayButton);
        toolbar.Children.Add(criticalToggle);
        toolbar.Children.Add(autoToggle);
        toolbar.Children.Add(baselineButton);
        toolbar.Children.Add(baselineToggle);
        toolbar.Children.Add(exportButton);
        toolbar.Children.Add(stressButton);

        var layout = new Avalonia.Controls.DockPanel();
        Avalonia.Controls.DockPanel.SetDock(toolbar, Avalonia.Controls.Dock.Top);
        layout.Children.Add(toolbar);
        layout.Children.Add(gantt);
        return layout;
    }

    private void InitializeEditorsDemo()
    {
        var result = this.FindControl<TextBlock>("EditorsResult");
        void Show(string message)
        {
            if (result != null) result.Text = message;
        }

        var dateEdit = this.FindControl<ProDateEdit>("DateEditDemo");
        if (dateEdit != null)
        {
            dateEdit.MinDate = new System.DateTime(2020, 1, 1);
            dateEdit.MaxDate = new System.DateTime(2030, 12, 31);
            dateEdit.Value = System.DateTime.Today;
            dateEdit.ValueChanged += (s, e) =>
                Show($"Date : {dateEdit.Value:dd/MM/yyyy}");
        }

        var timeEdit = this.FindControl<ProTimeEdit>("TimeEditDemo");
        if (timeEdit != null)
        {
            timeEdit.Value = new System.TimeSpan(14, 30, 0);
            timeEdit.ValueChanged += (s, e) =>
                Show($"Heure : {timeEdit.Value:hh\\:mm}");
        }

        var spinEdit = this.FindControl<ProSpinEdit>("SpinEditDemo");
        if (spinEdit != null)
        {
            spinEdit.MinValue = 0;
            spinEdit.MaxValue = 10_000;
            spinEdit.Decimals = 2;
            spinEdit.Increment = 0.5m;
            spinEdit.Value = 1250.50m;
            spinEdit.ValueChanged += (s, e) =>
                Show($"Montant : {spinEdit.Value:N2}");
        }

        var comboEditable = this.FindControl<ProComboBox>("ComboEditable");
        if (comboEditable != null)
        {
            comboEditable.Properties.TextEditStyle = TextEditStyles.Standard;
            comboEditable.Properties.NullValuePrompt = "Taper un pays...";
            comboEditable.Properties.Items.AddRange(new object[]
            {
                "France", "Nouvelle-Calédonie", "Nouvelle-Zélande", "Australie",
                "Japon", "Singapour", "Allemagne", "Espagne"
            });
            comboEditable.EditValueChanged += (s, e) =>
                Show($"Combo éditable : {comboEditable.EditValue}");
        }

        var lookUp = this.FindControl<ProLookUpEdit>("LookUpDemo");
        if (lookUp != null)
        {
            lookUp.DataSource = Employees;
            lookUp.DisplayMember = "FullName";
            lookUp.ValueMember = "Id";
            lookUp.NullText = "Choisir un employé...";
            lookUp.DropDownWidth = 420;
            lookUp.Columns.Add(new Julien.Avalonia.DataGrid.Models.GridColumn
            {
                FieldName = "Id", Header = "ID", Width = new Avalonia.Controls.GridLength(60)
            });
            lookUp.Columns.Add(new Julien.Avalonia.DataGrid.Models.GridColumn
            {
                FieldName = "FullName", Header = "Nom",
                Width = new Avalonia.Controls.GridLength(1, Avalonia.Controls.GridUnitType.Star)
            });
            lookUp.Columns.Add(new Julien.Avalonia.DataGrid.Models.GridColumn
            {
                FieldName = "Department", Header = "Service", Width = new Avalonia.Controls.GridLength(120)
            });
            lookUp.EditValueChanged += (s, e) =>
                Show($"LookUp : Id={lookUp.EditValue} ({(lookUp.SelectedRow as Employee)?.FullName})");
        }
    }

    private void InitializeToolbar()
    {
        var toolbar = this.FindControl<ProToolbar>("MainToolbar");
        if (toolbar == null) return;

        toolbar.AddButton("📄", "Nouveau");
        toolbar.AddButton("📂", "Ouvrir");
        toolbar.AddButton("💾", "Enregistrer");
        toolbar.AddSeparator();
        toolbar.AddButton("✂️");
        toolbar.AddButton("📋");
        toolbar.AddButton("📌");
        toolbar.AddSeparator();
        toolbar.AddToggle("🔍", "Aperçu", isChecked: true);
        var boldToggle = toolbar.AddToggle("𝐁");
        boldToggle.Tooltip = "Gras";
        toolbar.AddSeparator();
        toolbar.AddButton("🖨️", "Imprimer", () =>
        {
            var statusBar = this.FindControl<ProStatusBar>("MainStatusBar");
            if (statusBar != null && statusBar.Items.Count > 0)
                statusBar.Items[0].Text = "Impression demandée...";
        });
        toolbar.AddSeparator();

        // Bascule clair/sombre à chaud
        var darkToggle = toolbar.AddToggle("🌙", "Sombre",
            isChecked: ProControls.Theme.ProTheme.IsDark);
        darkToggle.Click += (s, e) =>
        {
            ProControls.Theme.ProTheme.Variant = darkToggle.IsChecked
                ? ProControls.Theme.ProThemeVariant.Dark
                : ProControls.Theme.ProThemeVariant.Light;

            // Fond racine de la démo (hors ProWindow)
            var root = this.FindControl<Avalonia.Controls.Grid>("RootGrid");
            if (root != null)
                root.Background = new Avalonia.Media.SolidColorBrush(
                    ProControls.Theme.ProTheme.Background.Window);
        };
    }

    private void InitializeStatusBar()
    {
        var statusBar = this.FindControl<ProStatusBar>("MainStatusBar");
        if (statusBar == null) return;

        statusBar.AddPanel("Prêt");
        statusBar.AddSeparator();
        statusBar.AddPanel("17 contrôles", icon: "🧩");
        statusBar.AddPanel("UTF-8", ProStatusBarPanelAlignment.Right);
        statusBar.AddSeparator(ProStatusBarPanelAlignment.Right);
        var zoomPanel = statusBar.AddPanel("100 %", ProStatusBarPanelAlignment.Right, icon: "🔍");
        zoomPanel.Click += (s, e) => zoomPanel.Text = zoomPanel.Text == "100 %" ? "125 %" : "100 %";
    }

    private void InitializeMessageBoxDemo()
    {
        var result = this.FindControl<TextBlock>("MsgBoxResult");
        void ShowResult(ProDialogResult r)
        {
            if (result != null) result.Text = $"Résultat : {r}";
        }

        var btnInfo = this.FindControl<ProButton>("BtnMsgInfo");
        if (btnInfo != null)
            btnInfo.Click += async (s, e) =>
                ShowResult(await ProMessageBox.ShowInfoAsync(this, "Les contrôles du lot 1 sont installés."));

        var btnWarning = this.FindControl<ProButton>("BtnMsgWarning");
        if (btnWarning != null)
            btnWarning.Click += async (s, e) =>
                ShowResult(await ProMessageBox.ShowWarningAsync(this, "Cette action modifiera la configuration."));

        var btnError = this.FindControl<ProButton>("BtnMsgError");
        if (btnError != null)
            btnError.Click += async (s, e) =>
                ShowResult(await ProMessageBox.ShowErrorAsync(this, "Impossible de contacter le serveur.\nVérifiez la connexion réseau."));

        var btnQuestion = this.FindControl<ProButton>("BtnMsgQuestion");
        if (btnQuestion != null)
            btnQuestion.Click += async (s, e) =>
                ShowResult(await ProMessageBox.ShowQuestionAsync(this, "Voulez-vous enregistrer les modifications avant de quitter ?"));
    }

    private void InitializeDataGrid()
    {
        var dataGrid = this.FindControl<Julien.Avalonia.DataGrid.Controls.JDataGrid>("EmployeeDataGrid");
        if (dataGrid != null)
        {
            dataGrid.ItemsSource = Employees;
        }

        var exportButton = this.FindControl<ProButton>("BtnExportCsv");
        if (exportButton != null && dataGrid != null)
        {
            exportButton.Click += async (s, e) =>
            {
                var path = System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                    "employes-export.csv");
                await dataGrid.ExportCsvAsync(path);
                await ProMessageBox.ShowInfoAsync(this,
                    $"Vue courante exportée (tri + filtres appliqués) :\n{path}", "Export CSV");
            };
        }
    }

    private void InitializeComboBoxDemo()
    {
        var priorities = new[] { "Basse", "Normale", "Haute", "Urgente" };

        // ComboBox avec pays (utilise CustomDisplayText pour afficher le nom)
        var comboCountries = this.FindControl<ProComboBox>("ComboCountries");
        if (comboCountries != null)
        {
            comboCountries.Properties.NullValuePrompt = "Sélectionner un pays...";
            comboCountries.Properties.Items.Add(new Country { Code = "FR", Name = "France" });
            comboCountries.Properties.Items.Add(new Country { Code = "DE", Name = "Allemagne" });
            comboCountries.Properties.Items.Add(new Country { Code = "ES", Name = "Espagne" });
            comboCountries.Properties.Items.Add(new Country { Code = "IT", Name = "Italie" });
            comboCountries.Properties.Items.Add(new Country { Code = "GB", Name = "Royaume-Uni" });
            comboCountries.Properties.Items.Add(new Country { Code = "US", Name = "États-Unis" });
            comboCountries.Properties.Items.Add(new Country { Code = "JP", Name = "Japon" });
            comboCountries.Properties.Items.Add(new Country { Code = "CN", Name = "Chine" });

            // CustomDisplayText pour afficher le nom du pays
            comboCountries.CustomDisplayText += (s, e) =>
            {
                if (e.Value is Country country)
                {
                    e.DisplayText = country.Name;
                }
            };
        }

        // ComboBox avec sélection pré-remplie
        var comboWithSelection = this.FindControl<ProComboBox>("ComboWithSelection");
        if (comboWithSelection != null)
        {
            comboWithSelection.Properties.NullValuePrompt = "Choisir...";
            comboWithSelection.Properties.Items.AddRange(priorities);
            comboWithSelection.SelectedIndex = 1; // "Normale"
        }

        // ComboBox disabled
        var comboDisabled = this.FindControl<ProComboBox>("ComboDisabled");
        if (comboDisabled != null)
        {
            comboDisabled.Properties.NullValuePrompt = "Non disponible";
            comboDisabled.Properties.Items.AddRange(priorities);
        }

        // ComboBox readonly avec valeur
        var comboReadOnly = this.FindControl<ProComboBox>("ComboReadOnly");
        if (comboReadOnly != null)
        {
            comboReadOnly.Properties.Items.AddRange(priorities);
            comboReadOnly.SelectedIndex = 2; // "Haute"
            comboReadOnly.ReadOnly = true;
        }

        // ComboBox trié + type-to-select (taper "ma" sélectionne Marseille)
        var comboSorted = this.FindControl<ProComboBox>("ComboSorted");
        if (comboSorted != null)
        {
            comboSorted.Properties.NullValuePrompt = "Taper pour rechercher...";
            comboSorted.Properties.Items.AddRange(new object[]
            {
                "Nouméa", "Paris", "Marseille", "Lyon", "Bordeaux", "Toulouse", "Auckland", "Sydney"
            });
            comboSorted.Properties.Sorted = true;
        }
    }
    
    private void InitializeMenuBar()
    {
        var menuBar = this.FindControl<ProMenuBar>("MainMenuBar");
        if (menuBar == null) return;
        
        void Notify(string action) =>
            ProToast.Show(this, action, "Déclenché par le menu ou son raccourci clavier", "⌨️",
                System.TimeSpan.FromSeconds(2.5));

        // File menu — actions réelles : les raccourcis sont des accélérateurs vivants
        var fileMenu = new ProMenuBarItem("File");
        fileMenu.Items.Add(new ProMenuItem("New", "📄", "Ctrl+N", () => Notify("Nouveau document")));
        fileMenu.Items.Add(new ProMenuItem("Open", "📂", "Ctrl+O", () => Notify("Ouvrir")));
        fileMenu.Items.Add(new ProMenuItem("Save", "💾", "Ctrl+S", () => Notify("Enregistré")));
        fileMenu.Items.Add(new ProMenuItem("Save As...", null, "Ctrl+Shift+S", () => Notify("Enregistrer sous")));
        fileMenu.Items.Add(ProMenuItem.Separator());
        fileMenu.Items.Add(new ProMenuItem("Export", "📤"));
        fileMenu.Items.Add(new ProMenuItem("Print", "🖨️", "Ctrl+P", () => Notify("Impression")));
        fileMenu.Items.Add(ProMenuItem.Separator());
        fileMenu.Items.Add(new ProMenuItem("Exit", null, "Alt+F4", Close));
        menuBar.Items.Add(fileMenu);

        // Edit menu
        var editMenu = new ProMenuBarItem("Edit");
        editMenu.Items.Add(new ProMenuItem("Undo", "↩️", "Ctrl+Z", () => Notify("Annuler")));
        editMenu.Items.Add(new ProMenuItem("Redo", "↪️", "Ctrl+Y", () => Notify("Rétablir")));
        editMenu.Items.Add(ProMenuItem.Separator());
        editMenu.Items.Add(new ProMenuItem("Cut", "✂️", "Ctrl+X"));
        editMenu.Items.Add(new ProMenuItem("Copy", "📋", "Ctrl+C"));
        editMenu.Items.Add(new ProMenuItem("Paste", "📌", "Ctrl+V"));
        editMenu.Items.Add(ProMenuItem.Separator());
        editMenu.Items.Add(new ProMenuItem("Select All", null, "Ctrl+A"));
        menuBar.Items.Add(editMenu);
        
        // View menu
        var viewMenu = new ProMenuBarItem("View");
        viewMenu.Items.Add(new ProMenuItem("Zoom In", "🔍", "Ctrl++"));
        viewMenu.Items.Add(new ProMenuItem("Zoom Out", "🔎", "Ctrl+-"));
        viewMenu.Items.Add(new ProMenuItem("Reset Zoom", null, "Ctrl+0"));
        viewMenu.Items.Add(ProMenuItem.Separator());
        viewMenu.Items.Add(new ProMenuItem { Header = "Show Grid", IsCheckable = true, IsChecked = true });
        viewMenu.Items.Add(new ProMenuItem { Header = "Show Rulers", IsCheckable = true });
        menuBar.Items.Add(viewMenu);
        
        // Help menu
        var helpMenu = new ProMenuBarItem("Help");
        helpMenu.Items.Add(new ProMenuItem("Documentation", "📖", "F1"));
        helpMenu.Items.Add(new ProMenuItem("Check for Updates", "🔄"));
        helpMenu.Items.Add(ProMenuItem.Separator());
        helpMenu.Items.Add(new ProMenuItem("About ProControls", "ℹ️"));
        menuBar.Items.Add(helpMenu);
    }
    
    private void InitializeRibbon()
    {
        var ribbon = this.FindControl<ProRibbon>("MainRibbon");
        if (ribbon == null) return;
        
        // Home Tab
        var homeTab = new ProRibbonTab("Home");
        
        // Clipboard group
        homeTab.AddGroup("Clipboard", g =>
        {
            g.AddLargeButton("Paste", "📋");
            g.AddSmallButton("Cut", "✂️");
            g.AddSmallButton("Copy", "📄");
            g.AddSmallButton("Format", "🎨");
        });
        
        // Font group (avec dialog launcher, cf. coin bas-droit style Office)
        homeTab.AddGroup("Font", g =>
        {
            g.AddSmallButton("Bold", "B");
            g.AddSmallButton("Italic", "I");
            g.AddSmallButton("Underline", "U");
            g.AddSeparator();
            g.AddSmallButton("Font Color", "A");
            g.AddSmallButton("Highlight", "🖍️");
            g.DialogLauncher = async () =>
                await ProMessageBox.ShowInfoAsync(this, "Dialog launcher du groupe Font : ouvrirait la boîte de dialogue complète des polices.", "Font");
        });
        
        // Paragraph group
        homeTab.AddGroup("Paragraph", g =>
        {
            g.AddSmallButton("Align Left", "⬅️");
            g.AddSmallButton("Center", "↔️");
            g.AddSmallButton("Align Right", "➡️");
            g.AddSeparator();
            g.AddSmallButton("Bullets", "•");
            g.AddSmallButton("Numbering", "1.");
        });
        
        ribbon.Tabs.Add(homeTab);
        
        // Insert Tab
        var insertTab = new ProRibbonTab("Insert");
        
        insertTab.AddGroup("Pages", g =>
        {
            g.AddLargeButton("Cover\nPage", "📰");
            g.AddLargeButton("Blank\nPage", "📄");
            g.AddSmallButton("Page Break", "⏎");
        });
        
        insertTab.AddGroup("Tables", g =>
        {
            g.AddLargeButton("Table", "📊");
        });
        
        insertTab.AddGroup("Illustrations", g =>
        {
            g.AddLargeButton("Pictures", "🖼️");
            g.AddLargeButton("Shapes", "⬡");
            g.AddSmallButton("Chart", "📈");
            g.AddSmallButton("SmartArt", "🔷");
            g.AddSmallButton("Screenshot", "📸");
        });
        
        insertTab.AddGroup("Links", g =>
        {
            g.AddLargeButton("Link", "🔗");
            g.AddSmallButton("Bookmark", "🔖");
            g.AddSmallButton("Cross-ref", "↗️");
        });
        
        ribbon.Tabs.Add(insertTab);
        
        // View Tab
        var viewTab = new ProRibbonTab("View");
        
        viewTab.AddGroup("Views", g =>
        {
            g.AddLargeButton("Print\nLayout", "📋");
            g.AddLargeButton("Web\nLayout", "🌐");
            g.AddSmallButton("Outline", "📝");
            g.AddSmallButton("Draft", "📄");
        });
        
        viewTab.AddGroup("Show", g =>
        {
            g.AddSmallButton("Ruler", "📏");
            g.AddSmallButton("Gridlines", "⊞");
            g.AddSmallButton("Navigation", "🧭");
        });
        
        viewTab.AddGroup("Zoom", g =>
        {
            g.AddLargeButton("Zoom", "🔍");
            g.AddSmallButton("100%", null);
            g.AddSmallButton("One Page", "1️⃣");
            g.AddSmallButton("Two Pages", "2️⃣");
        });
        
        ribbon.Tabs.Add(viewTab);
    }
}
