// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

MartletApp.Run<TodoApp>(new WindowOptions
{
    Title = "Martlet — Todo",
    Width = 560,
    Height = 720,
    Themes = DemoThemes.Create(),
});
