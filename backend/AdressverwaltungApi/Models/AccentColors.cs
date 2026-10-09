namespace AdressverwaltungApi.Models;

/// <summary>
/// Akzentfarben der Oberfläche. Gespeichert wird nur der Name; welche Farbtöne
/// dazugehören, legt das Frontend fest (app/globals.css).
/// </summary>
public static class AccentColors
{
    public const string Default = "blue";

    public static readonly IReadOnlyList<string> All =
        [Default, "green", "teal", "violet", "red", "orange", "slate"];

    public static bool IsValid(string? color) => color is not null && All.Contains(color);
}
