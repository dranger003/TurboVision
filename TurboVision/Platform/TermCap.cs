using TurboVision.Core;

namespace TurboVision.Platform;

/// <summary>
/// Terminal color capability levels.
/// Matches upstream TermCapColors in termcap.h
/// </summary>
internal enum TermCapColors : byte
{
    /// <summary>No color support</summary>
    NoColor,
    /// <summary>8 colors (standard ANSI)</summary>
    Indexed8,
    /// <summary>16 colors (ANSI + bright colors)</summary>
    Indexed16,
    /// <summary>256 colors (xterm-256color)</summary>
    Indexed256,
    /// <summary>16M colors (24-bit true color)</summary>
    Direct
}

/// <summary>
/// Terminal color type classification.
/// Matches upstream TermColorType in termcap.h
/// </summary>
internal enum TermColorType : byte
{
    /// <summary>Default terminal color</summary>
    Default,
    /// <summary>Indexed color (0-255)</summary>
    Indexed,
    /// <summary>RGB color (24-bit)</summary>
    RGB,
    /// <summary>No color specified</summary>
    NoColor
}

/// <summary>
/// Represents a terminal color.
/// Matches upstream TermColor in termcap.h
/// </summary>
internal struct TermColor
{
    /// <summary>Color index (for Indexed type)</summary>
    public byte Idx;

    /// <summary>Red component (for RGB type)</summary>
    public byte R;

    /// <summary>Green component (for RGB type)</summary>
    public byte G;

    /// <summary>Blue component (for RGB type)</summary>
    public byte B;

    /// <summary>Color type classification</summary>
    public TermColorType Type;

    /// <summary>
    /// Creates an indexed color.
    /// </summary>
    public TermColor(byte idx, TermColorType type)
    {
        Idx = idx;
        R = G = B = 0;
        Type = type;
    }

    /// <summary>
    /// Creates an RGB color.
    /// </summary>
    public TermColor(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
        Idx = 0;
        Type = TermColorType.RGB;
    }

    /// <summary>
    /// Gets the default color.
    /// </summary>
    public static TermColor Default => new TermColor { Type = TermColorType.Default };

    /// <summary>
    /// Gets the no-color value.
    /// </summary>
    public static TermColor NoColor => new TermColor { Type = TermColorType.NoColor };
}

/// <summary>
/// Represents terminal text attributes (colors and style).
/// Matches upstream TermAttr in termcap.h
/// </summary>
internal struct TermAttr
{
    /// <summary>Foreground color</summary>
    public TermColor Fg;

    /// <summary>Background color</summary>
    public TermColor Bg;

    /// <summary>Text style flags (bold, underline, etc.)</summary>
    public ushort Style;
}

/// <summary>
/// Terminal quirks and special handling flags.
/// Matches upstream TermQuirks in termcap.h
/// </summary>
[Flags]
internal enum TermQuirks : ushort
{
    /// <summary>No quirks</summary>
    None = 0,

    /// <summary>Use bright colors (8-15) instead of bold attribute for bold text</summary>
    BoldIsBright = 0x0001,

    /// <summary>Use bright background colors instead of blink attribute</summary>
    BlinkIsBright = 0x0002,

    /// <summary>Terminal doesn't support italic text</summary>
    NoItalic = 0x0004,

    /// <summary>Terminal doesn't support underline</summary>
    NoUnderline = 0x0008
}

/// <summary>
/// Terminal capabilities structure.
/// Matches upstream TermCap in termcap.h
/// </summary>
internal struct TermCap
{
    /// <summary>Color capability level</summary>
    public TermCapColors Colors;

    /// <summary>Terminal quirks and special handling</summary>
    public TermQuirks Quirks;

    /// <summary>
    /// Detects display capabilities based on environment and display adapter.
    /// Matches upstream TermCap::getDisplayCapabilities()
    /// </summary>
    /// <param name="con">Console control instance</param>
    /// <param name="display">Display adapter</param>
    /// <returns>Terminal capabilities</returns>
    public static TermCap GetDisplayCapabilities(ConsoleCtl con, DisplayAdapter display)
    {
        var termcap = new TermCap();

        // Check COLORTERM environment variable for true color support
        string? colorterm = Environment.GetEnvironmentVariable("COLORTERM");
        if (colorterm == "truecolor" || colorterm == "24bit")
        {
            termcap.Colors = TermCapColors.Direct;
            return termcap;
        }

        // Determine color support from display adapter
        int colors = display.GetColorCount();

        if (colors >= 256 * 256 * 256)
        {
            // 24-bit true color (16M colors)
            termcap.Colors = TermCapColors.Direct;
        }
        else if (colors >= 256)
        {
            // 256 color support
            termcap.Colors = TermCapColors.Indexed256;
        }
        else if (colors >= 16)
        {
            // 16 color support (standard + bright colors)
            termcap.Colors = TermCapColors.Indexed16;
        }
        else if (colors >= 8)
        {
            // 8 color support (standard ANSI only)
            termcap.Colors = TermCapColors.Indexed8;
            // For 8-color terminals, use bold for bright colors
            termcap.Quirks |= TermQuirks.BoldIsBright;
        }
        else
        {
            // No color support
            termcap.Colors = TermCapColors.NoColor;
        }

        return termcap;
    }
}
