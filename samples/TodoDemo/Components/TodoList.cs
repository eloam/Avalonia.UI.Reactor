using Avalonia.UI.Reactor.Primitives;
using Avalonia.UI.Reactor.State;
using Avalonia.UI.Reactor.Theming;
using Avalonia.UI.Reactor.Views;

namespace TodoDemo;

public sealed partial class TodoList : Component
{
    [State] partial string Draft { get; set; } = "";
    [State] partial bool Thinking { get; set; }
    [State] partial Filter Filter { get; set; }

    readonly StateList<Todo> todos =
    [
        new("Écrire le moteur de layout", done: true),
        new("Brancher le rendu Skia GPU", done: true),
        new("Ajouter un backend mobile"),
    ];

    public override View Body
    {
        get
        {
            var visible = todos.Where(t => Filter switch
            {
                Filter.Active => !t.Done,
                Filter.Done => t.Done,
                _ => true,
            }).ToList();

            return Card(
                HStack(
                    Text("Tâches").Font(TextStyle.Headline),
                    Spacer(),
                    Observe(() => ProgressRing(todos.Count(t => t.Done), todos.Count))),
                // Bloc observé à part : taper du texte ne recalcule que cette ligne.
                Observe(() => HStack(
                    TextField(Bind.Draft, "Que faut-il faire ?", onSubmit: Add).Grow(),
                    Button("Ajouter", Add).Style("success"),   // style de bouton ajouté par le thème
                    Button(Thinking ? "…" : "Idée", Suggest).Style(ButtonStyle.Secondary))),
                visible.Count == 0
                    ? Text("Rien à afficher").Foreground(ColorRole.TextMuted).Padding(4, 8)
                    : VStack(ForEach(visible, Row, t => t.Id)).Spacing(0),
                Divider(),
                Observe(Footer));
        }
    }

    View Row(Todo t) =>
        HStack(
            Checkbox(t.Bind.Done),
            Text(t.Title).Foreground(t.Done ? ColorRole.TextMuted : ColorRole.Text).Grow(),
            Button("×", () => todos.Remove(t)).Style(ButtonStyle.Ghost).Padding(8, 0).FontSize(18))
        .Padding(6, 4)
        .Radius(6)
        .OnTap(() => t.Done = !t.Done);

    View Footer() =>
        HStack(
            Text($"{todos.Count(t => !t.Done)} restante(s)").Foreground(ColorRole.TextMuted).Font("caption"),   // style de texte ajouté par le thème
            Spacer(),
            Segmented(Bind.Filter, ("Toutes", Filter.All), ("Actives", Filter.Active), ("Faites", Filter.Done)),
            todos.Any(t => t.Done)
                ? Button("Purger", Purge)
                    .Style(ButtonStyle.Danger).Padding(10, 4).Font(TextStyle.Small)
                : null);

    /// <summary>Au moins une tâche faite (menu « Purger » grisé sinon).</summary>
    public bool HasDone => todos.Any(t => t.Done);

    public void Purge() => todos.RemoveAll(t => t.Done);

    void Add()
    {
        if (string.IsNullOrWhiteSpace(Draft)) return;
        todos.Add(new(Draft.Trim()));
        Draft = "";
    }

    /// <summary>Action asynchrone : l'interface se met à jour avant et après l'await, sans rien faire de plus.
    /// Une exception ici serait signalée par l'hôte, sans fermer l'application.</summary>
    public async Task Suggest()
    {
        Thinking = true;
        await Task.Delay(800); // simule un appel réseau
        todos.Add(new(Ideas[Random.Shared.Next(Ideas.Length)]));
        Thinking = false;
    }

    static readonly string[] Ideas = ["Écrire les tests", "Ajouter un ScrollView", "Préparer la démo", "Mesurer les perfs"];
}
