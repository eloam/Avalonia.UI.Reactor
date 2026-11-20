using Avalonia.UI.Reactor.Desktop;
using TodoDemo;

Desktop.Run<TodoApp>(new WindowOptions
{
    Title = "POC — Todo",
    Width = 560,
    Height = 720,
    Themes = DemoThemes.Create(),
    MenuBar = MenuBarMode.Auto
});
