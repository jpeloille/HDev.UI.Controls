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
        InitializeComboBoxDemo();
        InitializeDataGrid();
    }

    private void InitializeDataGrid()
    {
        var dataGrid = this.FindControl<Julien.Avalonia.DataGrid.Controls.JDataGrid>("EmployeeDataGrid");
        if (dataGrid != null)
        {
            dataGrid.ItemsSource = Employees;
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
        
        // Font group
        homeTab.AddGroup("Font", g =>
        {
            g.AddSmallButton("Bold", "B");
            g.AddSmallButton("Italic", "I");
            g.AddSmallButton("Underline", "U");
            g.AddSeparator();
            g.AddSmallButton("Font Color", "A");
            g.AddSmallButton("Highlight", "🖍️");
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
