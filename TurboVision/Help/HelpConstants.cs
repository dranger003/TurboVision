namespace TurboVision.Help;

/// <summary>
/// Constants for the help system.
/// </summary>
public static class HelpConstants
{
    /// <summary>
    /// Magic header for help files: "FBHF" (0x46484246).
    /// </summary>
    public const int MagicHeader = 0x46484246;

    /// <summary>
    /// Palette for THelpViewer: normal, keyword, selected keyword.
    /// </summary>
    public static readonly byte[] HelpViewerPalette = [0x06, 0x07, 0x08];

    /// <summary>
    /// Palette for THelpWindow: frame colors.
    /// </summary>
    public static readonly byte[] HelpWindowPalette = [0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87];

    /// <summary>
    /// Default title for help windows.
    /// </summary>
    public const string HelpWindowTitle = "Help";

    /// <summary>
    /// Message shown when an invalid topic is requested.
    /// </summary>
    public const string InvalidContext = "No help available for this topic.";
}
