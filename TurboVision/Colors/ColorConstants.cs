namespace TurboVision.Colors;

/// <summary>
/// Command constants for the color selector system.
/// </summary>
public static class ColorCommands
{
    public const ushort cmColorForegroundChanged = 71;
    public const ushort cmColorBackgroundChanged = 72;
    public const ushort cmColorSet = 73;
    public const ushort cmNewColorItem = 74;
    public const ushort cmNewColorIndex = 75;
    public const ushort cmSaveColorIndex = 76;
}

/// <summary>
/// Monochrome color values.
/// </summary>
public static class MonoColors
{
    public static readonly byte[] Values = [0x07, 0x0F, 0x01, 0x70, 0x09];

    public const string Normal = "Normal";
    public const string Highlight = "Highlight";
    public const string Underline = "Underline";
    public const string Inverse = "Inverse";
}

/// <summary>
/// String constants for the color dialog.
/// </summary>
public static class ColorStrings
{
    public const string Colors = "Colors";
    public const string GroupText = "~G~roup";
    public const string ItemText = "~I~tem";
    public const string ForText = "~F~oreground";
    public const string BakText = "~B~ackground";
    public const string TextText = "Text ";
    public const string ColorText = "Color";
    public const string OkText = "O~K~";
    public const string CancelText = "Cancel";
}
