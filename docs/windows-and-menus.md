# Windows and menus

## Starting the application

```csharp
ReactorApp.Run<App>("My app", 1024, 768);
```

All the options of the window, when you need them:

```csharp
ReactorApp.Run<App>(new WindowOptions
{
    Title = "My app", Width = 1024, Height = 768, MinWidth = 400,
    Resizable = true, Borderless = false, Maximized = false, Fullscreen = false,
    Themes = new() { { "Standard", Theme.Light, Theme.Dark }, { "Sable", sable, sableNight } },
    Zoom = 1.25,              // interface zoom (0.5 to 3), on top of the display scale
    ZoomShortcuts = true,     // Ctrl + / Ctrl − / Ctrl 0 (Cmd on macOS), in 10 % steps
});
```

- `ReactorApp.Run(new MyApp(arguments), options)` shows a component you built yourself.
- `ReactorApp.Quit()` closes the application, for example from a **Quit** menu item.
- `Theme.WithFont("Inter")` names a font installed on the system.
- When idle, the window does no layout work: it only works when some state changes.

## Menu bar

The menu bar is declared in the `Body`, like any view: check marks, disabled items and lists follow the state.

```csharp
MenuBar(
    Menu("&File",
        MenuItem("&New", New).Shortcut("Ctrl+N"),
        MenuItem("&Save", Save).Shortcut("Ctrl+S").Enabled(IsDirty),
        Divider(),
        Menu("Open &recent", ForEach(recent, f => MenuItem(f.Name, () => Open(f)))),
        MenuItem("&Quit", ReactorApp.Quit).Shortcut("Ctrl+Q")),
    Menu("&View",
        MenuItem("&Dark mode", Bind.DarkMode),                          // check mark bound to a state
        MenuItem("&Light", () => Scheme = Light).Checked(Scheme == Light)))
```

- `&` marks the access key (Alt+F opens **File**); `&&` displays an ampersand.
- Shortcuts (`"Ctrl+Shift+S"`, `"F5"`, `"Alt+F4"`) work with the menus closed. On macOS, `Ctrl` means Cmd.
  While typing in a field, a shortcut without Ctrl, Alt, Cmd or a function key (`"P"`) leaves the letter to the field.
- **Nothing to choose**: on macOS, the menus go to the system menu bar at the top of the screen and take no space in
  the window; elsewhere, the bar is drawn at the top of the window with the colors of the theme
  (`Theme.WithMenu(m => m with { BarBackground = …, BarPadding = … })`).
- Text fields have a cut / copy / paste context menu whose labels come from the theme (`TextFieldTheme.EditMenu`).

> [!NOTE]
> Open menus still use Avalonia's look (the Fluent theme), not yet the colors of your theme.
