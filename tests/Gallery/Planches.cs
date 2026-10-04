using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls.Gallery;

/// <summary>
/// Les planches de la suite, des fondations aux composites. Chaque constructeur est
/// rappelé une fois par variante de thème : il relit HDevTheme à chaque appel.
/// </summary>
static class Planches
{
    public static readonly Planche[] All =
    {
        new("fondations", "Fondations — couleurs, typographie, géométrie, glyphes", 1280, Fondations),
        new("boutons", "Boutons — variantes × états, tailles, boutons-icônes", 1280, Boutons),
        new("champs", "Champs de saisie — états, sélection de texte, rangée de formulaire", 1400, Champs, ChampsAfterShow),
        new("choix", "Choix — cases, radios, interrupteurs, cartes-radio, segments, curseurs, chips, badges", 1280, Choix),
        new("collections", "Collections — onglets, rail, arbre, liste, accordéon, calendrier", 1400, Collections),
        new("barres", "Barres et popovers — menu, ruban, barre d'outils, barre d'état, cartes, toast", 1280, Barres, BarresAfterShow),
        new("donnees", "Données — Gantt, Scheduler, Roster, MindMap", 1280, Donnees, DonneesAfterShow),
        new("documents", "Documents — HtmlView, RichEdit", 1280, Documents),
        new("bascule", "Bascule de thème à chaud dans une HDevWindow", 900, Bascule, BasculeAfterShow,
            () => new HDevWindow { Title = "Bascule" }),
    };

    // ═══════════════════════════════════════════════════════════════
    // FONDATIONS
    // ═══════════════════════════════════════════════════════════════

    private static Control Fondations()
    {
        // Couleurs : toutes les propriétés Color de la palette active, par famille
        var palette = HDevTheme.Current;
        var families = typeof(HDevPalette).GetProperties()
            .Select(p => (p.Name, Value: p.GetValue(palette) as Color?))
            .Where(x => x.Value.HasValue)
            .GroupBy(x => Family(x.Name));

        var colors = new StackPanel { Spacing = 14 };
        foreach (var family in families)
        {
            var wrap = new WrapPanel();
            foreach (var (name, value) in family)
                wrap.Children.Add(new Border { Margin = new Thickness(0, 0, 12, 12), Child = Kit.Swatch(name, value!.Value) });
            colors.Children.Add(new StackPanel { Spacing = 6, Children = { Kit.Caption(family.Key), wrap } });
        }

        // Typographie : l'échelle des tokens, puis les tailles relevées hors échelle
        var typo = new StackPanel { Spacing = 8 };
        foreach (var field in typeof(HDevTheme.Typography).GetFields().Where(f => f.Name.StartsWith("FontSize")))
        {
            var size = Convert.ToDouble(field.GetValue(null));
            typo.Children.Add(new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 16,
                Children =
                {
                    new Border { Width = 190, Child = Kit.Caption($"{field.Name} · {size} px") },
                    Kit.Text("Équipage TY201 — départ 14:35 · 1 234,50 €", size, HDevTheme.Text.Primary)
                }
            });
        }
        var offScale = new WrapPanel();
        foreach (var size in new[] { 9, 10, 10.5, 11, 12.5, 13, 15, 20, 22 })
        {
            offScale.Children.Add(new Border
            {
                Margin = new Thickness(0, 0, 18, 4),
                Child = Kit.Text($"{size} px", size, HDevTheme.Text.Tertiary)
            });
        }

        // Géométrie : rayons et hauteurs déclarés dans HDevTheme.Size
        var radii = new List<(string, Control)>();
        foreach (var field in typeof(HDevTheme.Size).GetFields().Where(f => f.Name.StartsWith("CornerRadius")))
        {
            var radius = Convert.ToDouble(field.GetValue(null));
            radii.Add(($"{field.Name} · {radius}", new Border
            {
                Width = 96,
                Height = 48,
                CornerRadius = new CornerRadius(radius),
                Background = Kit.Brush(HDevTheme.Background.Control),
                BorderBrush = Kit.Brush(HDevTheme.Border.Strong),
                BorderThickness = new Thickness(1)
            }));
        }

        var heights = new List<(string, Control)>();
        foreach (var field in typeof(HDevTheme.Size).GetFields().Where(f => f.Name.Contains("Height")))
        {
            var height = Convert.ToDouble(field.GetValue(null));
            heights.Add(($"{field.Name} · {height}", new Border
            {
                Width = 96,
                Height = height,
                CornerRadius = new CornerRadius(4),
                Background = Kit.Brush(HDevTheme.Accent.Primary)
            }));
        }

        var glyphs = Enum.GetValues<HDevEditorIcon>()
            .Where(icon => icon != HDevEditorIcon.None)
            .Select(icon => (icon.ToString(), (Control)new HDevIconButton { Icon = icon }))
            .ToArray();

        return Kit.Page("Fondations",
            $"Palette active : {HDevTheme.Variant} — contraste de chaque couleur mesuré sur Background.Panel",
            Kit.Section("Couleurs", colors),
            Kit.Section("Typographie", new StackPanel
            {
                Spacing = 12,
                Children = { typo, Kit.Caption("Tailles relevées dans le code hors de l'échelle (audit du 04/10/2026)"), offScale }
            }),
            Kit.Section("Rayons", Kit.Tiles(radii.ToArray())),
            Kit.Section("Hauteurs", Kit.Tiles(heights.ToArray())),
            Kit.Section("Glyphes vectoriels", Kit.Tiles(glyphs)));
    }

    /// <summary>Famille d'un token : le premier mot du nom (BackgroundPanel → Background)</summary>
    private static string Family(string name)
    {
        for (var i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]))
                return name[..i];
        }
        return name;
    }

    // ═══════════════════════════════════════════════════════════════
    // BOUTONS
    // ═══════════════════════════════════════════════════════════════

    private static readonly string[] ButtonStates = { "Repos", "Survol", "Pressé", "Focus clavier", "Désactivé" };

    private static Control Boutons()
    {
        var variants = Kit.Matrix(ButtonStates, Enum.GetValues<ButtonVariant>().Select(variant =>
            (variant.ToString(), ButtonStates.Select((_, state) =>
                (Control?)Stateful(new HDevButton { Text = variant.ToString(), Variant = variant }, state)).ToArray())));

        var icons = Kit.Matrix(ButtonStates, new[]
        {
            ButtonVariant.Outline, ButtonVariant.Secondary, ButtonVariant.Ghost, ButtonVariant.Primary
        }.Select(variant =>
            (variant.ToString(), ButtonStates.Select((_, state) =>
                (Control?)Stateful(new HDevIconButton { Icon = HDevEditorIcon.ChevronRight, Variant = variant }, state)).ToArray())));

        var sizes = Kit.Row(
            new HDevButton { Text = "Small", Variant = ButtonVariant.Primary, Size = ButtonSize.Small },
            new HDevButton { Text = "Medium", Variant = ButtonVariant.Primary, Size = ButtonSize.Medium },
            new HDevButton { Text = "Large", Variant = ButtonVariant.Primary, Size = ButtonSize.Large });

        var mixed = Kit.Row(
            new HDevIconButton { Icon = HDevEditorIcon.ChevronLeft, Size = ButtonSize.Small },
            new HDevIconButton { Icon = HDevEditorIcon.ChevronLeft },
            new HDevIconButton { Icon = HDevEditorIcon.ChevronRight, Size = ButtonSize.Large },
            new HDevButton { Text = "Texte Medium" });

        var dialog = Kit.Row(
            new HDevButton { Text = "Annuler", Variant = ButtonVariant.Outline, Size = ButtonSize.Large },
            new HDevButton
            {
                Text = "Aperçu", Variant = ButtonVariant.OutlineAccent, Size = ButtonSize.Large,
                TextWeight = FontWeight.SemiBold
            },
            new HDevButton
            {
                Text = "Imprimer", Variant = ButtonVariant.Primary, Size = ButtonSize.Large,
                TextWeight = FontWeight.SemiBold
            });

        return Kit.Page("Boutons", "HDevButton et HDevIconButton, états figés par ForceVisualState",
            Kit.Section("Variantes × états", variants),
            Kit.Section("Boutons-icônes × états", icons),
            Kit.Section("Tailles côte à côte (StackPanel horizontal)", sizes,
                "Chaque taille doit garder sa propre hauteur dans une rangée commune"),
            Kit.Section("Boutons-icônes et bouton texte dans une même rangée", mixed,
                "Un bouton-icône doit rester carré"),
            Kit.Section("Pied de dialogue (modèle PrintMeetingDialog de Synaxis)", dialog));
    }

    /// <summary>État de colonne : 0 repos, 1 survol, 2 pressé, 3 focus, 4 désactivé</summary>
    private static T Stateful<T>(T control, int state) where T : HDevControlBase
    {
        if (state == 4)
            control.IsEnabled = false;
        else
            control.ForceVisualState(hovered: state == 1, pressed: state == 2, focused: state == 3);
        return control;
    }

    // ═══════════════════════════════════════════════════════════════
    // CHAMPS
    // ═══════════════════════════════════════════════════════════════

    private static readonly string[] FieldStates =
        { "Vide", "Rempli", "Survol", "Focus", "Erreur", "Désactivé", "Lecture seule" };

    private static HDevTextBox? _selectionDemo;

    private sealed record CrewMember(int Id, string Name);

    private static readonly CrewMember[] Crew =
    {
        new(1, "Awa Wamytan"), new(2, "Teiki Brothers"), new(3, "Lucie Martin")
    };

    private static Control Champs()
    {
        var rows = new List<(string, Control?[])>
        {
            ("HDevTextBox", Cells(TextBoxIn)),
            ("HDevDateEdit", Cells(s => EditorIn(new HDevDateEdit(), s,
                e => ((HDevDateEdit)e).Value = new DateTime(2026, 10, 5)))),
            ("HDevTimeEdit", Cells(s => EditorIn(new HDevTimeEdit(), s,
                e => ((HDevTimeEdit)e).Value = new TimeSpan(14, 35, 0)))),
            ("HDevSpinEdit", Cells(s => EditorIn(new HDevSpinEdit(), s,
                e => ((HDevSpinEdit)e).Value = 1234.5m))),
            ("HDevSearchControl", Cells(s => EditorIn(new HDevSearchControl { Placeholder = "Rechercher…" }, s,
                e => e.Text = "Planning"))),
            ("HDevComboBox", Cells(s => ComboIn(s, editable: false))),
            ("Combo éditable", Cells(s => ComboIn(s, editable: true))),
            ("HDevLookUpEdit", Cells(LookUpIn)),
            ("HDevTokenEdit", Cells(TokenEditIn)),
        };

        _selectionDemo = new HDevTextBox
        {
            Width = 320, Text = "Texte sélectionné dans le champ", HorizontalAlignment = HorizontalAlignment.Left
        };

        var form = Kit.Row(
            Kit.Text("Vol", HDevTheme.Typography.FontSizeBody, HDevTheme.Text.Primary),
            new HDevTextBox { Width = 110, Text = "TY201" },
            new HDevDateEdit { Width = 130, Value = new DateTime(2026, 10, 5) },
            ComboIn(1, editable: false)!,
            new HDevIconButton { Icon = HDevEditorIcon.Calendar, Size = ButtonSize.Small },
            new HDevButton { Text = "Rechercher", Size = ButtonSize.Small },
            new HDevButton { Text = "Valider", Variant = ButtonVariant.Primary });

        return Kit.Page("Champs de saisie",
            "Cellule vide : état non forçable sur ce contrôle (pas de hook) ou sans objet",
            Kit.Section("Éditeurs × états", Kit.Matrix(FieldStates, rows)),
            Kit.Section("Sélection de texte (focus réel + SelectAll)", _selectionDemo),
            Kit.Section("Rangée de formulaire (StackPanel horizontal)", form,
                "Champs, combo, bouton-icône et boutons doivent partager la même ligne médiane et, en Small, la même hauteur"),
            Kit.Section("Multiligne", new HDevTextBox
            {
                IsMultiline = true, Width = 320, Text = "Ligne 1 du mémo\nLigne 2 du mémo",
                HorizontalAlignment = HorizontalAlignment.Left
            }));
    }

    private static void ChampsAfterShow(Window window)
    {
        _selectionDemo?.FocusTextBox();
        _selectionDemo?.SelectAll();
    }

    private static Control?[] Cells(Func<int, Control?> build)
        => FieldStates.Select((_, state) => build(state)).ToArray();

    private static Control TextBoxIn(int state)
    {
        var box = new HDevTextBox { Width = 150, Placeholder = "Numéro de vol" };
        if (state != 0)
            box.Text = "TY201";
        switch (state)
        {
            case 2: box.ForceVisualState(hovered: true); break;
            case 3: box.ForceVisualState(focused: true); break;
            case 4: box.HasError = true; break;
            case 5: box.IsEnabled = false; break;
            case 6: box.IsReadOnly = true; break;
        }
        return box;
    }

    private static Control EditorIn(HDevEditorBase editor, int state, Action<HDevEditorBase> fill)
    {
        editor.Width = 150;
        if (state != 0)
            fill(editor);
        switch (state)
        {
            case 2: editor.ForceVisualState(hovered: true); break;
            case 3: editor.ForceVisualState(focused: true); break;
            case 4: editor.SetError("Valeur invalide"); break;
            case 5: editor.IsEnabled = false; break;
            case 6: editor.IsReadOnly = true; break;
        }
        return editor;
    }

    private static Control? ComboIn(int state, bool editable)
    {
        if (state is 3 or 4)
            return null;

        var combo = new HDevComboBox { Width = 150 };
        combo.Properties.NullValuePrompt = "Choisir…";
        combo.Properties.Items.AddRange(new object[] { "Basse", "Normale", "Haute" });
        if (editable)
            combo.Properties.TextEditStyle = TextEditStyles.Standard;
        if (state != 0)
            combo.SelectedIndex = 1;
        switch (state)
        {
            case 2: combo.ForceVisualState(hovered: true); break;
            case 5: combo.IsEnabled = false; break;
            case 6: combo.ReadOnly = true; break;
        }
        return combo;
    }

    private static Control? LookUpIn(int state)
    {
        if (state is 3 or 4 or 6)
            return null;

        var lookUp = new HDevLookUpEdit
        {
            Width = 150,
            NullText = "Choisir…",
            DataSource = Crew,
            DisplayMember = nameof(CrewMember.Name),
            ValueMember = nameof(CrewMember.Id)
        };
        if (state != 0)
            lookUp.EditValue = 2;
        switch (state)
        {
            case 2: lookUp.ForceVisualState(hovered: true); break;
            case 5: lookUp.IsEnabled = false; break;
        }
        return lookUp;
    }

    private static Control? TokenEditIn(int state)
    {
        if (state is 2 or 3 or 4 or 6)
            return null;

        var tokens = new HDevTokenEdit { Width = 150, Placeholder = "Destinataires…" };
        if (state != 0)
            tokens.Tokens.Add("ops@aircal.nc");
        if (state == 5)
            tokens.IsEnabled = false;
        return tokens;
    }

    // ═══════════════════════════════════════════════════════════════
    // CHOIX
    // ═══════════════════════════════════════════════════════════════

    private static readonly string[] ChoiceStates = { "Repos", "Survol", "Focus clavier", "Désactivé" };

    /// <summary>État de colonne : 0 repos, 1 survol, 2 focus, 3 désactivé</summary>
    private static T Choice<T>(T control, int state) where T : HDevControlBase
    {
        if (state == 3)
            control.IsEnabled = false;
        else
            control.ForceVisualState(hovered: state == 1, focused: state == 2);
        return control;
    }

    private static Control?[] ChoiceCells(Func<HDevControlBase> build)
        => ChoiceStates.Select((_, state) => (Control?)Choice(build(), state)).ToArray();

    private static Control Choix()
    {
        var checks = Kit.Matrix(ChoiceStates, new (string, Control?[])[]
        {
            ("Décochée", ChoiceCells(() => new HDevCheckBox { Label = "Option", IsChecked = false })),
            ("Cochée", ChoiceCells(() => new HDevCheckBox { Label = "Option", IsChecked = true })),
            ("Indéterminée", ChoiceCells(() => new HDevCheckBox { Label = "Option", IsChecked = null })),
            ("Radio éteint", ChoiceCells(() => new HDevRadioButton { Label = "Choix" })),
            ("Radio choisi", ChoiceCells(() => new HDevRadioButton { Label = "Choix", IsChecked = true })),
            ("Interrupteur off", ChoiceCells(() => new HDevToggleSwitch { IsOn = false })),
            ("Interrupteur on", ChoiceCells(() => new HDevToggleSwitch { IsOn = true })),
            ("Curseur", ChoiceCells(() => new HDevSlider { Minimum = 0, Maximum = 100, Value = 70, Width = 180 })),
        });

        HDevRadioCard Card(bool isChecked) => new()
        {
            Width = 260,
            Title = "Support de séance",
            Subtitle = "Pendant · feuille à annoter · 5 pages",
            IsChecked = isChecked
        };
        var disabledChecked = Card(true);
        disabledChecked.IsEnabled = false;
        var cards = Kit.Tiles(
            ("Repos", Card(false)),
            ("Survol", Choice(Card(false), 1)),
            ("Choisie", Card(true)),
            ("Choisie + focus clavier", Choice(Card(true), 2)),
            ("Désactivée", Choice(Card(false), 3)),
            ("Désactivée et choisie", disabledChecked));

        var single = new HDevToggleButtonGroup();
        single.Add("Jour");
        single.Add("Semaine", isSelected: true);
        single.Add("Mois");
        var multiple = new HDevToggleButtonGroup { SelectionMode = ToggleGroupSelectionMode.Multiple };
        multiple.Add("G", isSelected: true);
        multiple.Add("I");
        multiple.Add("S", isSelected: true);
        var disabledGroup = new HDevToggleButtonGroup { IsEnabled = false };
        disabledGroup.Add("Liste");
        disabledGroup.Add("Grille", isSelected: true);
        var segments = Kit.Tiles(("Single", single), ("Multiple", multiple), ("Désactivé", disabledGroup));

        var chips = Kit.Tiles(
            ("Repos", new HDevChip { Text = "A320", Icon = "✈️" }),
            ("Survol", Choice(new HDevChip { Text = "A320", Icon = "✈️" }, 1)),
            ("Cochée", new HDevChip { Text = "ATR 72", Icon = "✈️", IsCheckable = true, IsChecked = true }),
            ("Focus clavier", Choice(new HDevChip { Text = "Cargo", Icon = "📦" }, 2)),
            ("Fermable", new HDevChip { Text = "ops@aircal.nc", Icon = "✉️", CanClose = true }),
            ("Badge", new HDevChip { Text = "NOTAM", Icon = "⚠️", Badge = "3", BadgeColor = HDevTheme.Accent.Error }),
            ("Désactivée", Choice(new HDevChip { Text = "Charter", Icon = "🎫" }, 3)));

        var badgeSizes = Enum.GetValues<BadgeSize>();
        var badges = Kit.Matrix(badgeSizes.Select(s => s.ToString()).ToArray(), Enum.GetValues<BadgeVariant>()
            .Select(variant => (variant.ToString(), badgeSizes
                .Select(size => (Control?)new HDevBadge { Text = variant == BadgeVariant.Primary ? "99+" : variant.ToString(), Variant = variant, Size = size })
                .ToArray())));

        var avatars = Kit.Row(
            new HDevAvatar { FullName = "Awa Wamytan", Size = AvatarSize.Small },
            new HDevAvatar { FullName = "Teiki Brothers", Size = AvatarSize.Medium, Status = AvatarStatus.Online },
            new HDevAvatar { FullName = "Lucie Martin", Size = AvatarSize.Large, Status = AvatarStatus.Busy },
            new HDevAvatar { FullName = "Marc Dupont", Size = AvatarSize.Large, Status = AvatarStatus.Away },
            new HDevAvatar { Initials = "NC", Size = AvatarSize.Large, Background = Color.Parse("#00847E") });

        var progress = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new HDevProgressBar { Value = 65, Width = 320, HorizontalAlignment = HorizontalAlignment.Left },
                new HDevProgressBar { Value = 100, Width = 320, HorizontalAlignment = HorizontalAlignment.Left },
                new HDevProgressBar { Value = 40, Width = 320, IsEnabled = false, HorizontalAlignment = HorizontalAlignment.Left }
            }
        };

        return Kit.Page("Choix", "Contrôles à état coché / choisi, et indicateurs",
            Kit.Section("Cases, radios, interrupteurs, curseur × états", checks),
            Kit.Section("Cartes-radio", cards),
            Kit.Section("Groupes segmentés", segments),
            Kit.Section("Chips", chips),
            Kit.Section("Badges (variantes × tailles)", badges),
            Kit.Section("Avatars (StackPanel horizontal)", avatars),
            Kit.Section("Barres de progression (65 %, 100 %, désactivée)", progress));
    }

    // ═══════════════════════════════════════════════════════════════
    // COLLECTIONS
    // ═══════════════════════════════════════════════════════════════

    private sealed record GalleryMail(string From, string Subject, string Preview, string Time,
        string Group, bool IsRead, bool IsFlagged);

    private static Control Collections()
    {
        var tabs = new HDevTabControl { Width = 640, Height = 150 };
        tabs.AddPage("Organisation", Kit.Caption("Contenu de la page Organisation"), "🌳");
        tabs.AddPage("Infos", Kit.Caption("Contenu de la page Infos (sélectionnée)"), "ℹ️");
        tabs.AddPage("Rapport", Kit.Caption("Page fermable"), "📄").CanClose = true;
        tabs.SelectedIndex = 1;

        var calendar = new HDevMonthCalendar
        {
            DisplayMonth = new DateTime(2026, 10, 1),
            SelectedDate = new DateTime(2026, 10, 14),
            VerticalAlignment = VerticalAlignment.Top
        };
        calendar.BoldDates.Add(new DateTime(2026, 10, 7));
        calendar.BoldDates.Add(new DateTime(2026, 10, 21));

        var nav = new HDevNavBar { Height = 300 };
        nav.Add("📧", "Courrier").Badge = "3";
        nav.Add("📅", "Agenda");
        nav.Add("👥", "Contacts");
        nav.Add("✓", "Tâches");
        nav.SelectedIndex = 0;

        var tree = new HDevTreeView { Width = 240, Height = 300 };
        var ops = tree.Add("Opérations (3)", "👥");
        ops.Add("Awa Wamytan", "🟢");
        var selected = ops.Add("Marc Dupont", "🟢");
        ops.Add("Lucie Martin", "⚪");
        var tech = tree.Add("Technique (1)", "👥");
        tech.Add("Teiki Brothers", "🟢");
        tree.ExpandAll();
        tree.SelectedNode = selected;

        var mails = new[]
        {
            new GalleryMail("Direction des opérations", "Planning S41", "Le planning de la semaine 41 est publié", "09:12", "Aujourd'hui", false, false),
            new GalleryMail("Awa Wamytan", "Rotation TY201", "Peux-tu confirmer l'équipage de demain ?", "08:40", "Aujourd'hui", true, true),
            new GalleryMail("Service qualité", "Revue CPA", "Ordre du jour de la revue mensuelle", "Hier", "Hier", false, false),
            new GalleryMail("Teiki Brothers", "Simulateur", "Créneau de jeudi déplacé à 14 h", "Hier", "Hier", true, false),
        };
        var list = new HDevListView
        {
            Width = 420,
            Height = 300,
            ItemsSource = mails,
            GroupSelector = item => ((GalleryMail)item).Group,
            ItemAdapter = item =>
            {
                var mail = (GalleryMail)item;
                return new HDevListItemContent
                {
                    Title = mail.From,
                    Subtitle = $"{mail.Subject} — {mail.Preview}",
                    Trailing = mail.Time,
                    Icon = string.Concat(mail.From.Split(' ').Take(2).Select(word => word[0])),
                    Emphasized = !mail.IsRead,
                    Badge = mail.IsFlagged ? "⚑" : null
                };
            }
        };
        list.SelectItem(mails[1]);

        var accordion = new HDevAccordion { Width = 260 };
        accordion.AddSection("Équipage", new StackPanel
        {
            Margin = new Thickness(12, 8),
            Spacing = 6,
            Children = { Kit.Caption("Awa Wamytan"), Kit.Caption("Teiki Brothers") }
        }, "👥", isExpanded: true);
        accordion.AddSection("Filtres flotte", Kit.Caption("A320 · ATR 72"), "🔎");
        accordion.AddSection("Alertes", Kit.Caption("3 NOTAM actifs"), "🔔").HeaderBackground =
            HDevTheme.WithOpacity(HDevTheme.Accent.Error, 36);
        accordion.AddSection("À propos", Kit.Caption("Mode Single"), "ℹ️");

        var top = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 24, Children = { tabs, calendar } };
        var bottom = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, Children = { nav, tree, list, accordion } };

        return Kit.Page("Collections", "Sélections, éléments courants, états vides et badges des contrôles de collection",
            Kit.Section("Onglets (page 2 sélectionnée) et calendrier (14 sélectionné, 7 et 21 en gras)", top),
            Kit.Section("Rail, arbre, liste, accordéon", bottom,
                "Rail : Courrier sélectionné · Arbre : « Marc Dupont » sélectionné · Liste : 2e élément sélectionné, non-lus en gras"));
    }

    // ═══════════════════════════════════════════════════════════════
    // BARRES ET POPOVERS
    // ═══════════════════════════════════════════════════════════════

    private static HDevMenuBar? _menuBar;

    private static Control Barres()
    {
        _menuBar = new HDevMenuBar();
        var file = new HDevMenuBarItem("Fichier");
        file.Items.Add(new HDevMenuItem("Nouveau", "📄", "Ctrl+N"));
        file.Items.Add(new HDevMenuItem("Ouvrir…", "📂", "Ctrl+O"));
        file.Items.Add(new HDevMenuItem("Enregistrer", "💾", "Ctrl+S"));
        file.Items.Add(HDevMenuItem.Separator());
        file.Items.Add(new HDevMenuItem { Header = "Afficher la grille", IsCheckable = true, IsChecked = true });
        file.Items.Add(new HDevMenuItem("Exporter", "📤") { IsEnabled = false });
        file.Items.Add(HDevMenuItem.Separator());
        file.Items.Add(new HDevMenuItem("Quitter", null, "Alt+F4"));
        _menuBar.Items.Add(file);
        _menuBar.Items.Add(new HDevMenuBarItem("Édition"));
        _menuBar.Items.Add(new HDevMenuBarItem("Affichage"));

        var menuHost = new StackPanel
        {
            Children =
            {
                _menuBar,
                new Border { Height = 250 } // place du menu ouvert
            }
        };

        var ribbon = new HDevRibbon();
        var home = new HDevRibbonTab("Accueil");
        home.AddGroup("Presse-papiers", g =>
        {
            g.AddLargeButton("Coller", "📋");
            g.AddSmallButton("Couper", "✂️");
            g.AddSmallButton("Copier", "📄");
        });
        home.AddGroup("Vol", g =>
        {
            g.AddLargeButton("Nouveau", "✈️");
            g.AddDropdownButton("Plus", "⋯", isLarge: true, menu => menu.Add("Action"));
        });
        ribbon.Tabs.Add(home);
        ribbon.Tabs.Add(new HDevRibbonTab("Insertion"));

        var toolbar = new HDevToolbar();
        toolbar.AddButton("◀");
        toolbar.AddButton(null, "Aujourd'hui");
        toolbar.AddButton("▶");
        toolbar.AddSeparator();
        toolbar.AddToggle("🌙", "Sombre", isChecked: true);
        toolbar.AddToggle("G");

        var status = new HDevStatusBar();
        status.AddPanel("Prêt");
        status.AddSeparator();
        status.AddPanel("3 éléments sélectionnés");
        status.AddPanel("100 %", HDevStatusBarPanelAlignment.Right);

        var cards = Kit.Row(Enum.GetValues<CardElevation>().Select(elevation => (Control)new HDevCard
        {
            Title = elevation.ToString(),
            Elevation = elevation,
            Width = 200,
            Content = Kit.Caption($"Elevation = {elevation}")
        }).ToArray());

        return Kit.Page("Barres et popovers", "Menu « Fichier » ouvert par un clic simulé, toast affiché en bas à droite",
            Kit.Section("Barre de menu", menuHost),
            Kit.Section("Ruban", ribbon),
            Kit.Section("Barre d'outils (bascule « Sombre » cochée) et barre d'état",
                new StackPanel { Spacing = 12, Children = { toolbar, status } }),
            Kit.Section("Cartes (élévations)", cards));
    }

    private static void BarresAfterShow(Window window)
    {
        if (_menuBar?.Items.FirstOrDefault() is { } file
            && file.TranslatePoint(new Point(file.Bounds.Width / 2, file.Bounds.Height / 2), window) is { } point)
        {
            window.MouseMove(point, RawInputModifiers.None);
            window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
            window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        }
        HDevToast.Show(window, "Message déplacé", "« Planning S41 » → Archives", "📁");
    }

    // ═══════════════════════════════════════════════════════════════
    // DONNÉES
    // ═══════════════════════════════════════════════════════════════

    private static HDevGantt? _gantt;
    private static HDevMindMap? _mindMap;

    private static Control Donnees()
    {
        var today = DateTime.Today;
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));

        // Gantt : le projet de la démo, chemin critique affiché
        var project = new GanttProject();
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
        moduleB.Color = Color.Parse("#1C71D8");
        var integration = dev.Add("Intégration", today.AddDays(29), today.AddDays(35));
        integration.DependsOn(moduleA);
        integration.DependsOn(moduleB);
        project.EndUpdate();
        _gantt = new HDevGantt { Height = 300, HighlightCriticalPath = true, Project = project };

        // Scheduler : semaine ouvrée, un événement sans couleur (repli sur l'accent)
        var scheduler = new HDevScheduler { Height = 420, ViewMode = SchedulerViewMode.WorkWeek, DisplayDate = today };
        scheduler.Events.Add(new ScheduleEvent("Comité de pilotage", monday.AddHours(9), monday.AddHours(11))
            { Location = "Salle A", Color = Color.Parse("#0B67B2") });
        scheduler.Events.Add(new ScheduleEvent("Point RH", monday.AddHours(10), monday.AddHours(11.5))
            { Color = Color.Parse("#77216F") });
        scheduler.Events.Add(new ScheduleEvent("Revue budget", monday.AddDays(1).AddHours(14), monday.AddDays(1).AddHours(16))
            { Color = Color.Parse("#C64614") });
        scheduler.Events.Add(new ScheduleEvent("Démo Synaxis (couleur par défaut)", monday.AddDays(3).AddHours(9), monday.AddDays(3).AddHours(10.5))
            { Location = "Visio" });
        scheduler.Events.Add(new ScheduleEvent("Astreinte", monday.AddDays(4), monday.AddDays(5))
            { AllDay = true, Color = Color.Parse("#107C10") });

        // Roster : cinq jours, bandes de service, barres FDP, blocs de vol
        var roster = new HDevRoster { Height = 240, LaneSpan = RosterEngine.Day };
        var model = new RosterModel { Origin = monday };
        foreach (var lane in RosterEngine.BuildDailyLanes(monday, 5, d => d.ToString("ddd dd/MM"), _ => null))
            model.Lanes.Add(lane);
        var duty = Color.FromArgb(128, 65, 105, 225);
        var fdp = Color.FromArgb(204, 0, 128, 0);
        var vol = Color.FromArgb(255, 30, 144, 255);
        for (var d = 0; d < model.Lanes.Count; d++)
        {
            var lane = model.Lanes[d];
            if (d == 2)
            {
                lane.Bands.Add(new RosterBand(lane.WindowStart, lane.WindowStart.AddHours(24), "REP")
                    { Color = Color.FromArgb(128, 128, 128, 128) });
                continue;
            }
            lane.Bands.Add(new RosterBand(lane.WindowStart.AddHours(5), lane.WindowStart.AddHours(14), "AM")
            {
                Color = duty,
                StartLabel = "05:00",
                EndLabel = "14:00"
            });
            lane.Bars.Add(new RosterBar(lane.WindowStart.AddHours(5.5), lane.WindowStart.AddHours(13.5)) { Color = fdp });
            lane.Blocks.Add(new RosterBlock(lane.WindowStart.AddHours(6), lane.WindowStart.AddHours(7.5), $"TY20{d}", 1)
                { Color = vol });
            lane.Blocks.Add(new RosterBlock(lane.WindowStart.AddHours(8.5), lane.WindowStart.AddHours(10), $"TY21{d}", 1)
                { Color = vol });
        }
        roster.Model = model;

        // MindMap : l'arbre de la démo
        var root = new MindMapNode("Suite HDev.UI.Controls");
        var synaxis = root.Add("Synaxis");
        synaxis.Add("Planning équipages").Add("FTL");
        synaxis.Add("Pairings");
        var ovidie = root.Add("Ovidie");
        ovidie.Add("Dashboards");
        ovidie.Add("Budget");
        var outlook = root.Add("Cible Outlook");
        outlook.Add("HtmlView");
        outlook.Add("Scheduler");
        var quality = root.Add("Qualité");
        quality.Add("Tests");
        quality.Add("Dark mode");
        root.Add("Idées en vrac");
        _mindMap = new HDevMindMap { Height = 360, Root = root };

        return Kit.Page("Données", "Composites de visualisation : couleurs catégorielles, texte sur couleur, lignes « maintenant »",
            Kit.Section("Gantt (chemin critique, « Module B » en couleur de tâche)", _gantt),
            Kit.Section("Scheduler (semaine ouvrée)", scheduler),
            Kit.Section("Roster", roster),
            Kit.Section("MindMap", _mindMap));
    }

    private static void DonneesAfterShow(Window window)
    {
        _gantt?.ZoomToFit();
        _mindMap?.ZoomToFit();
    }

    // ═══════════════════════════════════════════════════════════════
    // DOCUMENTS
    // ═══════════════════════════════════════════════════════════════

    private const string SampleHtml =
        "<h2 style=\"color:#1E1E1E\">Confirmation de réservation</h2>"
        + "<p style=\"color:#333333\">Votre vol <strong>TY201</strong> Nouméa → Lifou est confirmé. "
        + "<mark>Embarquement 30 minutes avant le départ.</mark> Détails sur "
        + "<a href=\"https://example.org\">votre espace client</a>.</p>"
        + "<p>Paragraphe sans couleur imposée : il doit suivre le thème.</p>"
        + "<blockquote>Citation : « Le contrôle affiche, le métier décide. »</blockquote>"
        + "<ul><li>Bagage cabine : 7 kg</li><li>Bagage soute : 20 kg</li></ul>";

    // ═══════════════════════════════════════════════════════════════
    // BASCULE À CHAUD
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Contenu construit dans la variante de la passe, puis rendu après bascule vers
    /// l'autre variante : seuls les contrôles HDev qui suivent VariantChanged changent
    /// (l'habillage de la planche, lui, reste figé à la construction)
    /// </summary>
    private static Control Bascule()
    {
        var menuBar = new HDevMenuBar();
        menuBar.Items.Add(new HDevMenuBarItem("Fichier"));
        menuBar.Items.Add(new HDevMenuBarItem("Édition"));

        var toolbar = new HDevToolbar();
        toolbar.AddButton(null, "Aujourd'hui");
        toolbar.AddToggle("🌙", "Sombre", isChecked: true);

        var status = new HDevStatusBar();
        status.AddPanel("Prêt");

        var body = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new HDevCard
                {
                    Title = "Carte",
                    Content = new StackPanel
                    {
                        Spacing = 8,
                        Children =
                        {
                            new HDevTextBox { Width = 220, Text = "TY201", HorizontalAlignment = HorizontalAlignment.Left },
                            new HDevButton { Text = "Valider", Variant = ButtonVariant.Primary },
                            new HDevCheckBox { Label = "Option", IsChecked = true }
                        }
                    }
                }
            }
        };

        var root = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(menuBar, Dock.Top);
        DockPanel.SetDock(toolbar, Dock.Top);
        DockPanel.SetDock(status, Dock.Bottom);
        root.Children.Add(menuBar);
        root.Children.Add(toolbar);
        root.Children.Add(status);
        root.Children.Add(body);
        return root;
    }

    private static void BasculeAfterShow(Window window)
        => HDevTheme.Variant = HDevTheme.IsDark ? HDevThemeVariant.Light : HDevThemeVariant.Dark;

    private static Control Documents()
    {
        var view = new HDevHtmlView { Html = SampleHtml, Width = 560, Height = 320 };
        var edit = new HDevRichEdit { Html = SampleHtml, Width = 560, Height = 320 };

        return Kit.Page("Documents", "Le même HTML (couleurs inline, lien, surlignage) en lecture et en édition",
            Kit.Section("HtmlView et RichEdit", new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 24,
                Children = { view, edit }
            }));
    }
}
