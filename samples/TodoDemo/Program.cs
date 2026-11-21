// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using TodoDemo;

ReactorApp.Run<TodoApp>(new WindowOptions
{
    Title = "Reactor — Todo",
    Width = 560,
    Height = 720,
    Themes = DemoThemes.Create(),
});
