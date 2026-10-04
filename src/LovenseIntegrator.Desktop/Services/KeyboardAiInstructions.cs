using System.IO;
using System.Reflection;
using LovenseIntegrator.Desktop.ViewModels;

namespace LovenseIntegrator.Desktop.Services;

public static class KeyboardAiInstructions
{
    public static string Read()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("KeyboardAiInstructions.md") ?? throw new InvalidOperationException("AI instructions are missing from the build.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd() + "\n\nComplete key code list for this version (use these values):\n" + string.Join(", ", KeyboardLayout.Keys.Select(k => k.Id).Distinct().OrderBy(k => k)) + "\n";
    }
}
