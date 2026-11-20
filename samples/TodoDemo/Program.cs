using TodoDemo;

ReactorApp.Run<TodoApp>(new WindowOptions
{
    Title = "POC — Todo",
    Width = 560,
    Height = 720,
    Themes = DemoThemes.Create(),
});
