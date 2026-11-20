using Mvu.Primitives;
using Mvu.Rendering;
using Mvu.Skia;

namespace Mvu.Tests;

/// <summary>Polices fournies par l'application, prioritaires sur celles du système.</summary>
public class FontsTests
{
    [Fact]
    public void Registered_font_file_is_used_under_its_family_name()
    {
        // Un fichier de police réel, enregistré sous un nom qui n'existe pas dans le système.
        const string file = @"C:\Windows\Fonts\georgia.ttf";
        if (!File.Exists(file)) return; // poste sans cette police : rien à vérifier

        using var ctx = new SkiaDrawingContext();
        var before = ctx.MeasureText("Bonjour le monde", new Font("PoliceDeTest", 20, 400)).Width;

        Fonts.Register("PoliceDeTest", file);
        var after = ctx.MeasureText("Bonjour le monde", new Font("PoliceDeTest", 20, 400)).Width;
        var georgia = ctx.MeasureText("Bonjour le monde", new Font("Georgia", 20, 400)).Width;

        Assert.NotEqual(before, after);           // la police enregistrée remplace celle de repli
        Assert.Equal(georgia, after, precision: 1); // et c'est bien le fichier fourni
    }

    [Fact]
    public void Nearest_registered_weight_is_chosen()
    {
        Fonts.Register("PoidsDeTest", [1], weight: 400);
        Fonts.Register("PoidsDeTest", [2], weight: 700);
        Assert.Equal([2], Fonts.Find("PoidsDeTest", 600));
        Assert.Equal([1], Fonts.Find("poidsdetest", 300));
        Assert.Null(Fonts.Find("Absente", 400));
    }
}
