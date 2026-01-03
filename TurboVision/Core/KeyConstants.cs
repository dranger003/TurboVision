namespace TurboVision.Core;

/// <summary>
/// Key code constants for keyboard events.
/// </summary>
public static class KeyConstants
{
    // Special keys
    public const ushort kbNoKey = 0x0000;
    public const ushort kbEnter = 0x1C0D;
    public const ushort kbEsc = 0x011B;
    public const ushort kbTab = 0x0F09;
    public const ushort kbShiftTab = 0x0F00;
    public const ushort kbBackSpace = 0x0E08;

    // Arrow keys
    public const ushort kbLeft = 0x4B00;
    public const ushort kbRight = 0x4D00;
    public const ushort kbUp = 0x4800;
    public const ushort kbDown = 0x5000;

    // Navigation keys
    public const ushort kbHome = 0x4700;
    public const ushort kbEnd = 0x4F00;
    public const ushort kbPgUp = 0x4900;
    public const ushort kbPgDn = 0x5100;
    public const ushort kbIns = 0x5200;
    public const ushort kbDel = 0x5300;

    // Control key combinations
    public const ushort kbCtrlLeft = 0x7300;
    public const ushort kbCtrlRight = 0x7400;
    public const ushort kbCtrlHome = 0x7700;
    public const ushort kbCtrlEnd = 0x7500;
    public const ushort kbCtrlPgUp = 0x8400;
    public const ushort kbCtrlPgDn = 0x7600;

    // Function keys
    public const ushort kbF1 = 0x3B00;
    public const ushort kbF2 = 0x3C00;
    public const ushort kbF3 = 0x3D00;
    public const ushort kbF4 = 0x3E00;
    public const ushort kbF5 = 0x3F00;
    public const ushort kbF6 = 0x4000;
    public const ushort kbF7 = 0x4100;
    public const ushort kbF8 = 0x4200;
    public const ushort kbF9 = 0x4300;
    public const ushort kbF10 = 0x4400;
    public const ushort kbF11 = 0x8500;
    public const ushort kbF12 = 0x8600;

    // Alt key combinations
    public const ushort kbAltA = 0x1E00;
    public const ushort kbAltB = 0x3000;
    public const ushort kbAltC = 0x2E00;
    public const ushort kbAltD = 0x2000;
    public const ushort kbAltE = 0x1200;
    public const ushort kbAltF = 0x2100;
    public const ushort kbAltG = 0x2200;
    public const ushort kbAltH = 0x2300;
    public const ushort kbAltI = 0x1700;
    public const ushort kbAltJ = 0x2400;
    public const ushort kbAltK = 0x2500;
    public const ushort kbAltL = 0x2600;
    public const ushort kbAltM = 0x3200;
    public const ushort kbAltN = 0x3100;
    public const ushort kbAltO = 0x1800;
    public const ushort kbAltP = 0x1900;
    public const ushort kbAltQ = 0x1000;
    public const ushort kbAltR = 0x1300;
    public const ushort kbAltS = 0x1F00;
    public const ushort kbAltT = 0x1400;
    public const ushort kbAltU = 0x1600;
    public const ushort kbAltV = 0x2F00;
    public const ushort kbAltW = 0x1100;
    public const ushort kbAltX = 0x2D00;
    public const ushort kbAltY = 0x1500;
    public const ushort kbAltZ = 0x2C00;

    // Alt + function keys
    public const ushort kbAltF1 = 0x6800;
    public const ushort kbAltF2 = 0x6900;
    public const ushort kbAltF3 = 0x6A00;
    public const ushort kbAltF4 = 0x6B00;
    public const ushort kbAltF5 = 0x6C00;
    public const ushort kbAltF6 = 0x6D00;
    public const ushort kbAltF7 = 0x6E00;
    public const ushort kbAltF8 = 0x6F00;
    public const ushort kbAltF9 = 0x7000;
    public const ushort kbAltF10 = 0x7100;

    // Control key state masks
    public const ushort kbRightShift = 0x0001;
    public const ushort kbLeftShift = 0x0002;
    public const ushort kbCtrlShift = 0x0004;
    public const ushort kbAltShift = 0x0008;
    public const ushort kbScrollState = 0x0010;
    public const ushort kbNumState = 0x0020;
    public const ushort kbCapsState = 0x0040;
    public const ushort kbInsState = 0x0080;
}

/// <summary>
/// Represents a key code with modifier state.
/// </summary>
public readonly record struct TKey(ushort KeyCode, ushort ControlKeyState = 0)
{
    public byte ScanCode
    {
        get { return (byte)(KeyCode >> 8); }
    }

    public byte CharCode
    {
        get { return (byte)(KeyCode & 0xFF); }
    }

    public static implicit operator TKey(ushort keyCode)
    {
        return new TKey(keyCode);
    }
}
