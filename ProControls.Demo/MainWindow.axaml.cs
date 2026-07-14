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
        
        // File menu
        var fileMenu = new ProMenuBarItem("File");
        fileMenu.Items.Add(new ProMenuItem("New", "📄", "Ctrl+N"));
        fileMenu.Items.Add(new ProMenuItem("Open", "📂", "Ctrl+O"));
        fileMenu.Items.Add(new ProMenuItem("Save", "💾", "Ctrl+S"));
        fileMenu.Items.Add(new ProMenuItem("Save As...", null, "Ctrl+Shift+S"));
        fileMenu.Items.Add(ProMenuItem.Separator());
        fileMenu.Items.Add(new ProMenuItem("Export", "📤"));
        fileMenu.Items.Add(new ProMenuItem("Print", "🖨️", "Ctrl+P"));
        fileMenu.Items.Add(ProMenuItem.Separator());
        fileMenu.Items.Add(new ProMenuItem("Exit", null, "Alt+F4"));
        menuBar.Items.Add(fileMenu);
        
        // Edit menu
        var editMenu = new ProMenuBarItem("Edit");
        editMenu.Items.Add(new ProMenuItem("Undo", "↩️", "Ctrl+Z"));
        editMenu.Items.Add(new ProMenuItem("Redo", "↪️", "Ctrl+Y"));
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
