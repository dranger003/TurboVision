namespace TurboVision.Platform;

/// <summary>
/// Display capabilities and mode constants.
/// </summary>
public static class TDisplay
{
    // Video mode constants
    public const ushort smBW80 = 0x0002;
    public const ushort smCO80 = 0x0003;
    public const ushort smMono = 0x0007;
    public const ushort smFont8x8 = 0x0100;
    public const ushort smColor256 = 0x0200;
    public const ushort smColorHigh = 0x0400;
    public const ushort smUpdate = 0x8000;

    // Cursor type constants
    public const ushort cursorHidden = 0x2000;
    public const ushort cursorNormal = 0x0607;
    public const ushort cursorBlock = 0x0007;
}
