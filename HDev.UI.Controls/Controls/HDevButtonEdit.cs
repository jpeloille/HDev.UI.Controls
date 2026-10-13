using Avalonia;
using Avalonia.Data;

namespace HDev.UI.Controls;

/// <summary>
/// Champ texte à boutons : la configuration nommée du
/// socle « texte + boutons ». Un bouton <see cref="HDevEditorButtonKind.Ellipsis"/> par défaut,
/// que l'application garde, remplace ou complète par <see cref="HDevEditorBase.Buttons"/>.
/// La valeur est le texte (D3) : <see cref="Text"/>, liable dans les deux sens.
/// </summary>
public class HDevButtonEdit : HDevEditorBase<string?>
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<HDevButtonEdit, string?>(nameof(Text), "",
            defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true, coerce: CoerceThroughChanging);

    protected override StyledProperty<string?> ValuePropertyCore => TextProperty;

    /// <summary>Le texte, valeur de l'éditeur (StyledProperty liable, la seule propriété Text)</summary>
    public override string Text
    {
        get => GetValue(TextProperty) ?? "";
        set => SetValueFromCode(value);
    }

    public HDevButtonEdit()
    {
        MinWidth = 120;
        Buttons.Add(new HDevEditorButton { Kind = HDevEditorButtonKind.Ellipsis });
    }

    protected override string FormatValue(string? value) => value ?? "";

    protected override void OnInnerTextChanged()
    {
        if (!IsUpdatingText)
            CommitUserValue(InnerTextBox.Text ?? "", fromText: true);
    }
}
