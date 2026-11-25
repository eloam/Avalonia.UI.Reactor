# Theming

No color or dimension is hard-coded: everything comes from a `Theme`
([Theming/](../src/MartletUI.Core/Theming)). Light and Dark are built in; a custom theme starts from one of
them and changes only what differs.

```csharp
var sable = Theme.Light.Named("Sable")          // starts from Light: a light theme
    .WithColor(ColorRole.Page, "#FBF7F2")
    .WithAccent("#C2410C")
    .WithFont("Georgia", bodySize: 15)
    .WithRadius(12);

var sableNight = Theme.Dark.Named("Sable Night") // starts from Dark: a dark theme
    .WithAccent("#F97316");
```

## The catalog

The application's themes are grouped into families: a light and/or a dark theme under one name.

```csharp
MartletApp.Run<App>(new WindowOptions
{
    Themes = new()
    {
        { "Standard", Theme.Light, Theme.Dark },   // the first family dresses the application by default
        { "Sable", sable, sableNight },            // light or dark depending on the current mode
        { "Contrast", contrast },                  // a single theme: always this one
    },
});
```

## Applying a theme

The view declares its theme, like any other style:

```csharp
Button("OK").Theme("Sable")              // a single button in another theme
counter.Theme("Sable")                   // a whole component
Card(…).ColorScheme(ColorScheme.Dark)    // the dark variant of the current family
Text("…").Theme(sableNight)              // an exact theme, as is
```

Letting the user choose is plain state; the view follows:

```csharp
[State] partial string ThemeName { get; set; } = "Standard";
[State] partial ColorScheme? Scheme { get; set; }        // null: follow the system

public override View Body => Page()
    .Theme(ThemeName)          // the whole screen in the chosen family…
    .ColorScheme(Scheme);      // …light, dark, or following the system
```

- A family with two variants follows the system mode, including when it changes while the application runs.
- A view without a theme inherits its parent's (family **and** mode). An unknown name is reported on the standard
  error output and the view keeps the inherited theme.
- `Themes.Families` lists the catalog, for example to build a theme picker.

## Named tokens

Add your own colors, text styles and button styles, then use them by name:

```csharp
theme.WithColor("success", "#16A34A")                                // .Background("success"), .Foreground("success")
     .WithTextStyle("caption", size: 12)                             // .Font("caption")
     .WithButtonStyle("success", new("success", ColorRole.OnAccent)) // .Style("success")
```

A color name missing from the theme is displayed in magenta, so the mistake shows immediately.

## What a theme contains

| Part | Content |
|---|---|
| `Palette` | Roles (`Page`, `Surface`, `Border`, `Text`, `TextMuted`, `Accent`, `Danger`…) and named colors |
| `Typography` | Font family (the system font by default), size and weight of each style, named styles |
| `ButtonTheme` | Padding, radius, font, `Primary` / `Secondary` / `Ghost` / `Danger` variants, named styles, template |
| `TextFieldTheme` | Colors per state, minimum width, caret color and blink, selection, context menu labels, template |
| `CheckboxTheme` | Colors per state, size, check mark thickness, template |
| `MenuTheme` | Drawn menu bar: background, bottom line, padding |
| `Theme` | Default stack spacing, separator color and thickness, hover and pressed overlays |

- **Views use roles**: `.Foreground(ColorRole.TextMuted)` adapts to every theme. A fixed color is still possible:
  `.Background("#FF0000")`.
- **Modifiers take precedence** over the theme.
- Buttons, checkboxes and text fields carry **our** look (a structure-only `ControlTheme`, not Fluent): their colors
  for each state (rest, hover, pressed, focus) come from the theme of their branch.

## Control templates

Changing colors is not always enough: a **template** replaces the whole look of a control with a view computed from
its state. The behavior (click, check, typing, focus) does not change.

```csharp
// For every button of the theme…
theme.WithButton(b => b with
{
    Template = s => HStack(Text("★"), Text(s.Label))
        .Padding(14, 8)
        .Background(s.Pressed ? ColorRole.Accent : s.Variant.Background)
        .Radius(999),
});

// …or for a single control
Checkbox(done, v => done = v).Template(s => Text(s.IsChecked ? "☑" : "☐"));

// A text field: the template draws the frame and places s.Editor where the text and the caret appear
theme.WithTextField(f => f with
{
    Template = s => VStack(s.Editor, Divider().Height(2).Background(s.Focused ? ColorRole.Accent : ColorRole.Border))
        .Spacing(4),
});
```

| Control | The template receives |
|---|---|
| `Button` | `Label`, `Variant` (colors of the chosen style), `Hovered`, `Pressed` |
| `Checkbox` | `IsChecked`, `Label`, `Hovered`, `Pressed` |
| `TextField` | `Value`, `Placeholder`, `Focused`, `Hovered`, `Editor` |

Without a template, the standard look applies. The template also gives the control its size. The Sable theme of the
sample app ([DemoThemes.cs](../samples/TodoDemo/DemoThemes.cs)) uses two: underlined text fields and round checkboxes.
