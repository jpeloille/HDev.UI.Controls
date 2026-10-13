using Avalonia;
using Avalonia.Data;

namespace HDev.UI.Controls;

/// <summary>
/// Champ de saisie de texte, monoligne, multiligne (<see cref="HDevEditorBase.IsMultiline"/>) ou
/// mot de passe, sur le socle des éditeurs. La valeur est le texte (D3) : <see cref="Text"/>,
/// liable dans les deux sens.
/// </summary>
public class HDevTextBox : HDevEditorBase<string?>
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<HDevTextBox, string?>(nameof(Text), "",
            defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);

    public static readonly StyledProperty<bool> IsPasswordProperty =
        AvaloniaProperty.Register<HDevTextBox, bool>(nameof(IsPassword));

    public static readonly StyledProperty<int> MaxLengthProperty =
        AvaloniaProperty.Register<HDevTextBox, int>(nameof(MaxLength));

    protected override StyledProperty<string?> ValuePropertyCore => TextProperty;

    /// <summary>Le texte, valeur de l'éditeur (StyledProperty liable, la seule propriété Text)</summary>
    public override string Text
    {
        get => GetValue(TextProperty) ?? "";
        set => SetValueFromCode(value);
    }

    /// <summary>Mot de passe : les caractères sont masqués</summary>
    public bool IsPassword
    {
        get => GetValue(IsPasswordProperty);
        set => SetValue(IsPasswordProperty, value);
    }

    /// <summary>Longueur maximale du texte saisi (0 = sans limite)</summary>
    public int MaxLength
    {
        get => GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    static HDevTextBox()
    {
        IsPasswordProperty.Changed.AddClassHandler<HDevTextBox>((x, _) =>
            x.InnerTextBox.PasswordChar = x.IsPassword ? '●' : '\0');
        MaxLengthProperty.Changed.AddClassHandler<HDevTextBox>((x, _) => x.InnerTextBox.MaxLength = x.MaxLength);
    }

    public HDevTextBox()
    {
        // Pas de largeur minimale propre : le champ texte prend celle que lui donne la mise en page
        MinWidth = 0;
    }

    protected override string FormatValue(string? value) => value ?? "";

    protected override void OnInnerTextChanged()
    {
        if (!IsUpdatingText)
            CommitUserValue(InnerText, fromText: true);
    }

    /// <summary>Vide le texte (changement du code)</summary>
    public void Clear() => Text = "";
}
