using Mvu.Layout;
using Mvu.Primitives;
using Mvu.Runtime;
using Mvu.Skia;
using Mvu.State;
using Mvu.Theming;
using Mvu.Views;
using SkiaSharp;

namespace Mvu.Tests;

/// <summary>
/// Hôte sans fenêtre : le vrai runtime (UiHost) + le vrai moteur de texte Skia sur une surface en mémoire.
/// Les interactions passent par les mêmes entrées que l'hôte SDL (pointeur, clavier, texte).
/// </summary>
public sealed class TestHost : IDisposable
{
    readonly SkiaDrawingContext _ctx = new();
    readonly SKSurface _surface;
    readonly Size _size;
    readonly SynchronizationContext? _previousContext;

    public UiHost Ui { get; }

    public TestHost(Component app, float width = 560, float height = 720, Themes? themes = null,
        INativeMenuBar? nativeMenuBar = null)
    {
        _size = new Size(width, height);
        _surface = SKSurface.Create(new SKImageInfo((int)width, (int)height));
        Ui = new UiHost(app, _ctx, themes) { NativeMenuBar = nativeMenuBar };
        // Comme l'hôte SDL : les suites d'await reviennent sur le dispatcher de l'interface.
        _previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(Ui.Dispatcher);
        Paint();
    }

    public void Paint()
    {
        _ctx.Begin(_surface.Canvas, 1);
        Ui.Paint(_ctx, _size);
    }

    public IEnumerable<View> Views => Ui.Elements.Select(e => e.View);
    public UpdateStats Stats => Ui.LastUpdate;

    public Rect BoundsOf(Func<View, bool> match) => Ui.Elements.First(e => match(e.View)).Bounds;
    public Rect BoundsOfKey(string key) => BoundsOf(v => v.Key == key);

    /// <summary>Couleur d'un pixel du dernier rendu (coordonnées logiques, échelle 1).</summary>
    public Color PixelAt(float x, float y)
    {
        Paint();
        using var image = _surface.Snapshot();
        using var bitmap = SKBitmap.FromImage(image);
        var c = bitmap.GetPixel((int)x, (int)y);
        return new Color(c.Red, c.Green, c.Blue, c.Alpha);
    }

    public bool HasText(string text) => Views.OfType<TextView>().Any(t => t.Content == text);
    public bool HasButton(string label) => Views.OfType<ButtonView>().Any(b => b.Label == label);

    public void Click(Func<View, bool> match)
    {
        var bounds = Ui.Elements.First(e => match(e.View)).Bounds;
        var p = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        Ui.PointerMove(p);
        Ui.PointerDown(p);
        Ui.PointerUp(p);
    }

    public void ClickButton(string label) => Click(v => v is ButtonView b && b.Label == label);
    public void ClickText(string text) => Click(v => v is TextView t && t.Content == text);

    public void Type(string text)
    {
        foreach (var c in text) Ui.TextInput(c.ToString());
    }


    public bool Press(KeyCode key, KeyModifiers modifiers = KeyModifiers.None) => Ui.KeyDown(key, modifiers);
    public bool Release(KeyCode key, KeyModifiers modifiers = KeyModifiers.None) => Ui.KeyUp(key, modifiers);

    // Menus (libellés sans leurs « & » : « Fichier » pour « &Fichier »).
    static string Shown(string label) => Mnemonics.Parse(label).Text;
    public void ClickMenu(string title) => Click(v => v is MenuTitleView t && Shown(t.Menu.Title) == title);
    public void ClickMenuItem(string label) => Click(v => v is MenuRowView r && Shown(r.Label) == label);
    public void HoverMenuItem(string label) => Ui.PointerMove(Center(BoundsOf(v => v is MenuRowView r && Shown(r.Label) == label)));
    public Rect MenuTitleBounds(string title) => BoundsOf(v => v is MenuTitleView t && Shown(t.Menu.Title) == title);
    public Rect MenuItemBounds(string label) => BoundsOf(v => v is MenuRowView r && Shown(r.Label) == label);
    public MenuItemView? MenuEntry(string label) =>
        Views.OfType<MenuRowView>().FirstOrDefault(r => Shown(r.Label) == label)?.Item as MenuItemView;
    public bool MenuIsOpen => Views.OfType<MenuRowView>().Any();

    static Point Center(Rect r) => new(r.X + r.Width / 2, r.Y + r.Height / 2);

    /// <summary>Fait tourner la boucle de l'interface (comme l'hôte SDL) jusqu'à ce que la condition soit vraie.</summary>
    public void WaitUntil(Func<bool> condition, int timeoutMs = 5000)
    {
        var end = Environment.TickCount64 + timeoutMs;
        while (true)
        {
            Ui.Dispatcher.Drain();
            if (condition()) return;
            if (Environment.TickCount64 > end) throw new TimeoutException($"The condition was not met within {timeoutMs} ms.");
            Thread.Sleep(10);
        }
    }

    public void Dispose()
    {
        Ui.Dispose();
        SynchronizationContext.SetSynchronizationContext(_previousContext);
        _surface.Dispose();
        _ctx.Dispose();
    }
}
