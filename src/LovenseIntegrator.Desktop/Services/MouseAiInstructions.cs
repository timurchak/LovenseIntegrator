using System.IO;
using System.Reflection;

namespace LovenseIntegrator.Desktop.Services;

public static class MouseAiInstructions
{
    public static string Read()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MouseAiInstructions.md") ?? throw new InvalidOperationException("Mouse AI instructions are missing from the build.");
        using var reader = new StreamReader(stream); return reader.ReadToEnd();
    }
}
