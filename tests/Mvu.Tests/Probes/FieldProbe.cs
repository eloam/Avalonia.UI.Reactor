using Mvu.Primitives;
using Mvu.State;
using Mvu.Views;

namespace Mvu.Tests;

/// <summary>Champ avec modèle : fond bleu au focus, zone d'édition décalée de 12 par la marge du modèle.</summary>
public partial class FieldProbe : Component
{
    [State] public partial string Value { get; set; } = "";

    public override View Body => VStack(
        TextField(Value, v => Value = v).Width(200).Template(f =>
            HStack(f.Editor).Spacing(0)
                .Padding(12, 6)
                .Background(f.Focused ? Color.Hex("#0000FF") : Color.Hex("#EEEEEE"))));
}
