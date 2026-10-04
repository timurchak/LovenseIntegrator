namespace LovenseIntegrator.Core;

public static class MouseInputs
{
    public static readonly string[] Buttons = ["Left", "Right", "Middle", "X1", "X2"];
    public static readonly string[] Wheel = ["Up", "Down"];
    public static readonly string[] All = [.. Buttons, .. Wheel];
}
