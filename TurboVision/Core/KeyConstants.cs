namespace TurboVision.Core;

/// <summary>
/// String utilities for TUI controls.
/// </summary>
public static class TStringUtils
{
    /// <summary>
    /// Extracts the hot key character from a string containing ~tilde~ markers.
    /// Returns the uppercase character immediately after the first ~, or '\0' if none.
    /// </summary>
    public static char HotKey(ReadOnlySpan<char> s)
    {
        int idx = s.IndexOf('~');
        if (idx >= 0 && idx + 1 < s.Length)
        {
            return char.ToUpperInvariant(s[idx + 1]);
        }
        return '\0';
    }

    /// <summary>
    /// Returns the display length of a string, excluding ~tilde~ markers.
    /// </summary>
    public static int CstrLen(ReadOnlySpan<char> s)
    {
        int len = 0;
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] != '~')
            {
                len++;
            }
        }
        return len;
    }

    /// <summary>
    /// Returns the Alt+key code for a character, or 0 if no mapping exists.
    /// </summary>
    public static ushort GetAltCode(char c)
    {
        c = char.ToUpperInvariant(c);
        return c switch
        {
            'A' => KeyConstants.kbAltA,
            'B' => KeyConstants.kbAltB,
            'C' => KeyConstants.kbAltC,
            'D' => KeyConstants.kbAltD,
            'E' => KeyConstants.kbAltE,
            'F' => KeyConstants.kbAltF,
            'G' => KeyConstants.kbAltG,
            'H' => KeyConstants.kbAltH,
            'I' => KeyConstants.kbAltI,
            'J' => KeyConstants.kbAltJ,
            'K' => KeyConstants.kbAltK,
            'L' => KeyConstants.kbAltL,
            'M' => KeyConstants.kbAltM,
            'N' => KeyConstants.kbAltN,
            'O' => KeyConstants.kbAltO,
            'P' => KeyConstants.kbAltP,
            'Q' => KeyConstants.kbAltQ,
            'R' => KeyConstants.kbAltR,
            'S' => KeyConstants.kbAltS,
            'T' => KeyConstants.kbAltT,
            'U' => KeyConstants.kbAltU,
            'V' => KeyConstants.kbAltV,
            'W' => KeyConstants.kbAltW,
            'X' => KeyConstants.kbAltX,
            'Y' => KeyConstants.kbAltY,
            'Z' => KeyConstants.kbAltZ,
            _ => 0
        };
    }

    /// <summary>
    /// Converts Ctrl+letter key codes to their arrow key equivalents (WordStar-style).
    /// Ctrl+E = Up, Ctrl+S = Left, Ctrl+D = Right, Ctrl+X = Down.
    /// </summary>
    public static ushort CtrlToArrow(ushort keyCode)
    {
        // Check for WordStar-style control key navigation
        byte charCode = (byte)(keyCode & 0xFF);
        if (charCode >= 1 && charCode <= 26)
        {
            return (char)(charCode + 'A' - 1) switch
            {
                'E' => KeyConstants.kbUp,     // Ctrl+E = Up
                'S' => KeyConstants.kbLeft,   // Ctrl+S = Left
                'D' => KeyConstants.kbRight,  // Ctrl+D = Right
                'X' => KeyConstants.kbDown,   // Ctrl+X = Down
                'A' => KeyConstants.kbHome,   // Ctrl+A = Home (word left in some contexts)
                'F' => KeyConstants.kbEnd,    // Ctrl+F = End (word right in some contexts)
                _ => keyCode
            };
        }
        return keyCode;
    }
}

/// <summary>
/// Key code constants for keyboard events.
/// </summary>
public static class KeyConstants
{
    // Additional key codes used by controls
    public const ushort kbBack = kbBackSpace;
    public const ushort kbCtrlBack = 0x0E7F;
    public const ushort kbAltBack = 0x0E00;
    public const ushort kbCtrlDel = 0x9300;

    // Shift state flag (combined left/right shift)
    public const ushort kbShift = 0x0010;
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
/// Normalizes key codes to a canonical form for consistent comparison.
/// </summary>
public readonly struct TKey : IEquatable<TKey>
{
    // Lookup table entry for key code normalization
    private readonly record struct KeyCodeLookupEntry(ushort NormalKeyCode, ushort ShiftState);

    // Control key lookup table (0x0000-0x001A)
    private static readonly KeyCodeLookupEntry[] CtrlKeyLookup =
    [
        new(0, 0),                                    // 0x0000
        new((ushort)'A', KeyConstants.kbCtrlShift),   // 0x0001 - Ctrl+A
        new((ushort)'B', KeyConstants.kbCtrlShift),   // 0x0002 - Ctrl+B
        new((ushort)'C', KeyConstants.kbCtrlShift),   // 0x0003 - Ctrl+C
        new((ushort)'D', KeyConstants.kbCtrlShift),   // 0x0004 - Ctrl+D
        new((ushort)'E', KeyConstants.kbCtrlShift),   // 0x0005 - Ctrl+E
        new((ushort)'F', KeyConstants.kbCtrlShift),   // 0x0006 - Ctrl+F
        new((ushort)'G', KeyConstants.kbCtrlShift),   // 0x0007 - Ctrl+G
        new((ushort)'H', KeyConstants.kbCtrlShift),   // 0x0008 - Ctrl+H
        new((ushort)'I', KeyConstants.kbCtrlShift),   // 0x0009 - Ctrl+I (Tab)
        new((ushort)'J', KeyConstants.kbCtrlShift),   // 0x000A - Ctrl+J
        new((ushort)'K', KeyConstants.kbCtrlShift),   // 0x000B - Ctrl+K
        new((ushort)'L', KeyConstants.kbCtrlShift),   // 0x000C - Ctrl+L
        new((ushort)'M', KeyConstants.kbCtrlShift),   // 0x000D - Ctrl+M (Enter)
        new((ushort)'N', KeyConstants.kbCtrlShift),   // 0x000E - Ctrl+N
        new((ushort)'O', KeyConstants.kbCtrlShift),   // 0x000F - Ctrl+O
        new((ushort)'P', KeyConstants.kbCtrlShift),   // 0x0010 - Ctrl+P
        new((ushort)'Q', KeyConstants.kbCtrlShift),   // 0x0011 - Ctrl+Q
        new((ushort)'R', KeyConstants.kbCtrlShift),   // 0x0012 - Ctrl+R
        new((ushort)'S', KeyConstants.kbCtrlShift),   // 0x0013 - Ctrl+S
        new((ushort)'T', KeyConstants.kbCtrlShift),   // 0x0014 - Ctrl+T
        new((ushort)'U', KeyConstants.kbCtrlShift),   // 0x0015 - Ctrl+U
        new((ushort)'V', KeyConstants.kbCtrlShift),   // 0x0016 - Ctrl+V
        new((ushort)'W', KeyConstants.kbCtrlShift),   // 0x0017 - Ctrl+W
        new((ushort)'X', KeyConstants.kbCtrlShift),   // 0x0018 - Ctrl+X
        new((ushort)'Y', KeyConstants.kbCtrlShift),   // 0x0019 - Ctrl+Y
        new((ushort)'Z', KeyConstants.kbCtrlShift),   // 0x001A - Ctrl+Z
    ];

    // Extended key lookup table (indexed by scan code when charCode == 0)
    private static readonly KeyCodeLookupEntry[] ExtKeyLookup =
    [
        new(0, 0),                                          // 0x00
        new(KeyConstants.kbEsc, KeyConstants.kbAltShift),   // 0x01 - Alt+Esc
        new((ushort)' ', KeyConstants.kbAltShift),          // 0x02 - Alt+Space
        new(0, 0),                                          // 0x03
        new(KeyConstants.kbIns, KeyConstants.kbCtrlShift),  // 0x04 - Ctrl+Ins
        new(KeyConstants.kbIns, kbShift),                   // 0x05 - Shift+Ins
        new(KeyConstants.kbDel, KeyConstants.kbCtrlShift),  // 0x06 - Ctrl+Del
        new(KeyConstants.kbDel, kbShift),                   // 0x07 - Shift+Del
        new(0, 0),                                          // 0x08
        new(0, 0),                                          // 0x09
        new(0, 0),                                          // 0x0A
        new(0, 0),                                          // 0x0B
        new(0, 0),                                          // 0x0C
        new(0, 0),                                          // 0x0D
        new(KeyConstants.kbBackSpace, KeyConstants.kbAltShift), // 0x0E - Alt+Backspace
        new(KeyConstants.kbTab, kbShift),                   // 0x0F - Shift+Tab
        new((ushort)'Q', KeyConstants.kbAltShift),          // 0x10 - Alt+Q
        new((ushort)'W', KeyConstants.kbAltShift),          // 0x11 - Alt+W
        new((ushort)'E', KeyConstants.kbAltShift),          // 0x12 - Alt+E
        new((ushort)'R', KeyConstants.kbAltShift),          // 0x13 - Alt+R
        new((ushort)'T', KeyConstants.kbAltShift),          // 0x14 - Alt+T
        new((ushort)'Y', KeyConstants.kbAltShift),          // 0x15 - Alt+Y
        new((ushort)'U', KeyConstants.kbAltShift),          // 0x16 - Alt+U
        new((ushort)'I', KeyConstants.kbAltShift),          // 0x17 - Alt+I
        new((ushort)'O', KeyConstants.kbAltShift),          // 0x18 - Alt+O
        new((ushort)'P', KeyConstants.kbAltShift),          // 0x19 - Alt+P
        new(0, 0),                                          // 0x1A
        new(0, 0),                                          // 0x1B
        new(0, 0),                                          // 0x1C
        new(0, 0),                                          // 0x1D
        new((ushort)'A', KeyConstants.kbAltShift),          // 0x1E - Alt+A
        new((ushort)'S', KeyConstants.kbAltShift),          // 0x1F - Alt+S
        new((ushort)'D', KeyConstants.kbAltShift),          // 0x20 - Alt+D
        new((ushort)'F', KeyConstants.kbAltShift),          // 0x21 - Alt+F
        new((ushort)'G', KeyConstants.kbAltShift),          // 0x22 - Alt+G
        new((ushort)'H', KeyConstants.kbAltShift),          // 0x23 - Alt+H
        new((ushort)'J', KeyConstants.kbAltShift),          // 0x24 - Alt+J
        new((ushort)'K', KeyConstants.kbAltShift),          // 0x25 - Alt+K
        new((ushort)'L', KeyConstants.kbAltShift),          // 0x26 - Alt+L
        new(0, 0),                                          // 0x27
        new(0, 0),                                          // 0x28
        new(0, 0),                                          // 0x29
        new(0, 0),                                          // 0x2A
        new(0, 0),                                          // 0x2B
        new((ushort)'Z', KeyConstants.kbAltShift),          // 0x2C - Alt+Z
        new((ushort)'X', KeyConstants.kbAltShift),          // 0x2D - Alt+X
        new((ushort)'C', KeyConstants.kbAltShift),          // 0x2E - Alt+C
        new((ushort)'V', KeyConstants.kbAltShift),          // 0x2F - Alt+V
        new((ushort)'B', KeyConstants.kbAltShift),          // 0x30 - Alt+B
        new((ushort)'N', KeyConstants.kbAltShift),          // 0x31 - Alt+N
        new((ushort)'M', KeyConstants.kbAltShift),          // 0x32 - Alt+M
        new(0, 0),                                          // 0x33
        new(0, 0),                                          // 0x34
        new((ushort)'/', KeyConstants.kbCtrlShift),         // 0x35 - Ctrl+Keypad /
        new(0, 0),                                          // 0x36
        new((ushort)'*', KeyConstants.kbCtrlShift),         // 0x37 - Ctrl+Keypad *
        new(0, 0),                                          // 0x38
        new(0, 0),                                          // 0x39
        new(0, 0),                                          // 0x3A
        new(KeyConstants.kbF1, 0),                          // 0x3B - F1
        new(KeyConstants.kbF2, 0),                          // 0x3C - F2
        new(KeyConstants.kbF3, 0),                          // 0x3D - F3
        new(KeyConstants.kbF4, 0),                          // 0x3E - F4
        new(KeyConstants.kbF5, 0),                          // 0x3F - F5
        new(KeyConstants.kbF6, 0),                          // 0x40 - F6
        new(KeyConstants.kbF7, 0),                          // 0x41 - F7
        new(KeyConstants.kbF8, 0),                          // 0x42 - F8
        new(KeyConstants.kbF9, 0),                          // 0x43 - F9
        new(KeyConstants.kbF10, 0),                         // 0x44 - F10
        new(0, 0),                                          // 0x45
        new(0, 0),                                          // 0x46
        new(KeyConstants.kbHome, 0),                        // 0x47 - Home
        new(KeyConstants.kbUp, 0),                          // 0x48 - Up
        new(KeyConstants.kbPgUp, 0),                        // 0x49 - PgUp
        new((ushort)'-', KeyConstants.kbCtrlShift),         // 0x4A - Ctrl+Keypad -
        new(KeyConstants.kbLeft, 0),                        // 0x4B - Left
        new(0, 0),                                          // 0x4C
        new(KeyConstants.kbRight, 0),                       // 0x4D - Right
        new((ushort)'+', KeyConstants.kbCtrlShift),         // 0x4E - Ctrl+Keypad +
        new(KeyConstants.kbEnd, 0),                         // 0x4F - End
        new(KeyConstants.kbDown, 0),                        // 0x50 - Down
        new(KeyConstants.kbPgDn, 0),                        // 0x51 - PgDn
        new(KeyConstants.kbIns, 0),                         // 0x52 - Ins
        new(KeyConstants.kbDel, 0),                         // 0x53 - Del
        new(KeyConstants.kbF1, kbShift),                    // 0x54 - Shift+F1
        new(KeyConstants.kbF2, kbShift),                    // 0x55 - Shift+F2
        new(KeyConstants.kbF3, kbShift),                    // 0x56 - Shift+F3
        new(KeyConstants.kbF4, kbShift),                    // 0x57 - Shift+F4
        new(KeyConstants.kbF5, kbShift),                    // 0x58 - Shift+F5
        new(KeyConstants.kbF6, kbShift),                    // 0x59 - Shift+F6
        new(KeyConstants.kbF7, kbShift),                    // 0x5A - Shift+F7
        new(KeyConstants.kbF8, kbShift),                    // 0x5B - Shift+F8
        new(KeyConstants.kbF9, kbShift),                    // 0x5C - Shift+F9
        new(KeyConstants.kbF10, kbShift),                   // 0x5D - Shift+F10
        new(KeyConstants.kbF1, KeyConstants.kbCtrlShift),   // 0x5E - Ctrl+F1
        new(KeyConstants.kbF2, KeyConstants.kbCtrlShift),   // 0x5F - Ctrl+F2
        new(KeyConstants.kbF3, KeyConstants.kbCtrlShift),   // 0x60 - Ctrl+F3
        new(KeyConstants.kbF4, KeyConstants.kbCtrlShift),   // 0x61 - Ctrl+F4
        new(KeyConstants.kbF5, KeyConstants.kbCtrlShift),   // 0x62 - Ctrl+F5
        new(KeyConstants.kbF6, KeyConstants.kbCtrlShift),   // 0x63 - Ctrl+F6
        new(KeyConstants.kbF7, KeyConstants.kbCtrlShift),   // 0x64 - Ctrl+F7
        new(KeyConstants.kbF8, KeyConstants.kbCtrlShift),   // 0x65 - Ctrl+F8
        new(KeyConstants.kbF9, KeyConstants.kbCtrlShift),   // 0x66 - Ctrl+F9
        new(KeyConstants.kbF10, KeyConstants.kbCtrlShift),  // 0x67 - Ctrl+F10
        new(KeyConstants.kbF1, KeyConstants.kbAltShift),    // 0x68 - Alt+F1
        new(KeyConstants.kbF2, KeyConstants.kbAltShift),    // 0x69 - Alt+F2
        new(KeyConstants.kbF3, KeyConstants.kbAltShift),    // 0x6A - Alt+F3
        new(KeyConstants.kbF4, KeyConstants.kbAltShift),    // 0x6B - Alt+F4
        new(KeyConstants.kbF5, KeyConstants.kbAltShift),    // 0x6C - Alt+F5
        new(KeyConstants.kbF6, KeyConstants.kbAltShift),    // 0x6D - Alt+F6
        new(KeyConstants.kbF7, KeyConstants.kbAltShift),    // 0x6E - Alt+F7
        new(KeyConstants.kbF8, KeyConstants.kbAltShift),    // 0x6F - Alt+F8
        new(KeyConstants.kbF9, KeyConstants.kbAltShift),    // 0x70 - Alt+F9
        new(KeyConstants.kbF10, KeyConstants.kbAltShift),   // 0x71 - Alt+F10
        new(kbCtrlPrtSc, KeyConstants.kbCtrlShift),         // 0x72 - Ctrl+PrtSc
        new(KeyConstants.kbLeft, KeyConstants.kbCtrlShift), // 0x73 - Ctrl+Left
        new(KeyConstants.kbRight, KeyConstants.kbCtrlShift),// 0x74 - Ctrl+Right
        new(KeyConstants.kbEnd, KeyConstants.kbCtrlShift),  // 0x75 - Ctrl+End
        new(KeyConstants.kbPgDn, KeyConstants.kbCtrlShift), // 0x76 - Ctrl+PgDn
        new(KeyConstants.kbHome, KeyConstants.kbCtrlShift), // 0x77 - Ctrl+Home
        new((ushort)'1', KeyConstants.kbAltShift),          // 0x78 - Alt+1
        new((ushort)'2', KeyConstants.kbAltShift),          // 0x79 - Alt+2
        new((ushort)'3', KeyConstants.kbAltShift),          // 0x7A - Alt+3
        new((ushort)'4', KeyConstants.kbAltShift),          // 0x7B - Alt+4
        new((ushort)'5', KeyConstants.kbAltShift),          // 0x7C - Alt+5
        new((ushort)'6', KeyConstants.kbAltShift),          // 0x7D - Alt+6
        new((ushort)'7', KeyConstants.kbAltShift),          // 0x7E - Alt+7
        new((ushort)'8', KeyConstants.kbAltShift),          // 0x7F - Alt+8
        new((ushort)'9', KeyConstants.kbAltShift),          // 0x80 - Alt+9
        new((ushort)'0', KeyConstants.kbAltShift),          // 0x81 - Alt+0
        new((ushort)'-', KeyConstants.kbAltShift),          // 0x82 - Alt+-
        new((ushort)'=', KeyConstants.kbAltShift),          // 0x83 - Alt+=
        new(KeyConstants.kbPgUp, KeyConstants.kbCtrlShift), // 0x84 - Ctrl+PgUp
        new(KeyConstants.kbF11, 0),                         // 0x85 - F11
        new(KeyConstants.kbF12, 0),                         // 0x86 - F12
        new(KeyConstants.kbF11, kbShift),                   // 0x87 - Shift+F11
        new(KeyConstants.kbF12, kbShift),                   // 0x88 - Shift+F12
        new(KeyConstants.kbF11, KeyConstants.kbCtrlShift),  // 0x89 - Ctrl+F11
        new(KeyConstants.kbF12, KeyConstants.kbCtrlShift),  // 0x8A - Ctrl+F12
        new(KeyConstants.kbF11, KeyConstants.kbAltShift),   // 0x8B - Alt+F11
        new(KeyConstants.kbF12, KeyConstants.kbAltShift),   // 0x8C - Alt+F12
        new(KeyConstants.kbUp, KeyConstants.kbCtrlShift),   // 0x8D - Ctrl+Up
        new(0, 0),                                          // 0x8E
        new(0, 0),                                          // 0x8F
        new(0, 0),                                          // 0x90
        new(KeyConstants.kbDown, KeyConstants.kbCtrlShift), // 0x91 - Ctrl+Down
        new(0, 0),                                          // 0x92
        new(0, 0),                                          // 0x93
        new(KeyConstants.kbTab, KeyConstants.kbCtrlShift),  // 0x94 - Ctrl+Tab
        new(0, 0),                                          // 0x95
        new(0, 0),                                          // 0x96
        new(KeyConstants.kbHome, KeyConstants.kbAltShift),  // 0x97 - Alt+Home
        new(KeyConstants.kbUp, KeyConstants.kbAltShift),    // 0x98 - Alt+Up
        new(KeyConstants.kbPgUp, KeyConstants.kbAltShift),  // 0x99 - Alt+PgUp
        new(0, 0),                                          // 0x9A
        new(KeyConstants.kbLeft, KeyConstants.kbAltShift),  // 0x9B - Alt+Left
        new(0, 0),                                          // 0x9C
        new(KeyConstants.kbRight, KeyConstants.kbAltShift), // 0x9D - Alt+Right
        new(0, 0),                                          // 0x9E
        new(KeyConstants.kbEnd, KeyConstants.kbAltShift),   // 0x9F - Alt+End
        new(KeyConstants.kbDown, KeyConstants.kbAltShift),  // 0xA0 - Alt+Down
        new(KeyConstants.kbPgDn, KeyConstants.kbAltShift),  // 0xA1 - Alt+PgDn
        new(KeyConstants.kbIns, KeyConstants.kbAltShift),   // 0xA2 - Alt+Ins
        new(KeyConstants.kbDel, KeyConstants.kbAltShift),   // 0xA3 - Alt+Del
        new(0, 0),                                          // 0xA4
        new(KeyConstants.kbTab, KeyConstants.kbAltShift),   // 0xA5 - Alt+Tab
        new(KeyConstants.kbEnter, KeyConstants.kbAltShift), // 0xA6 - Alt+Enter
    ];

    // Special key code constants
    private const ushort kbCtrlZ = 0x001A;
    private const ushort kbCtrlPrtSc = 0x7200;
    private const ushort kbCtrlBack = 0x0E7F;
    private const ushort kbCtrlEnter = 0x1C0A;
    private const ushort kbShift = 0x0010;  // Windows SHIFT_PRESSED

    public ushort KeyCode { get; }
    public ushort ControlKeyState { get; }

    public TKey(ushort keyCode, ushort shiftState = 0)
    {
        ushort code = keyCode;
        byte scanCode = (byte)(keyCode >> 8);
        byte charCode = (byte)(keyCode & 0xFF);

        KeyCodeLookupEntry? entry = null;

        // Determine the key code normalization
        if (keyCode <= kbCtrlZ || IsRawCtrlKey(scanCode, charCode))
        {
            entry = CtrlKeyLookup[charCode];
        }
        else if (charCode == 0)
        {
            if (scanCode < ExtKeyLookup.Length)
            {
                entry = ExtKeyLookup[scanCode];
            }
        }
        else if (IsPrintableCharacter(charCode))
        {
            if (charCode >= 'a' && charCode <= 'z')
            {
                code = (ushort)(charCode - 'a' + 'A');
            }
            else if (!IsKeypadCharacter(scanCode))
            {
                code = (ushort)(keyCode & 0xFF);
            }
        }
        else
        {
            entry = keyCode switch
            {
                kbCtrlBack => new(KeyConstants.kbBackSpace, KeyConstants.kbCtrlShift),
                kbCtrlEnter => new(KeyConstants.kbEnter, KeyConstants.kbCtrlShift),
                _ => null
            };
        }

        // Normalize modifiers using BIOS-style:
        // - kbShift = 0x0010 (for compatibility with Windows SHIFT_PRESSED)
        // - kbCtrlShift = 0x0004
        // - kbAltShift = 0x0008
        ushort mods = (ushort)(
            ((shiftState & kbShift) != 0 ? kbShift : 0) |
            ((shiftState & KeyConstants.kbCtrlShift) != 0 ? KeyConstants.kbCtrlShift : 0) |
            ((shiftState & KeyConstants.kbAltShift) != 0 ? KeyConstants.kbAltShift : 0));

        if (entry.HasValue)
        {
            mods |= entry.Value.ShiftState;
            if (entry.Value.NormalKeyCode != 0)
            {
                code = entry.Value.NormalKeyCode;
            }
        }

        KeyCode = code;
        ControlKeyState = (code != KeyConstants.kbNoKey) ? mods : (ushort)0;
    }

    private static bool IsRawCtrlKey(byte scanCode, byte charCode)
    {
        const string scanKeys = "QWERTYUIOP\0\0\0\0ASDFGHJKL\0\0\0\0\0ZXCVBNM";
        if (scanCode >= 16 && scanCode < 16 + 35)
        {
            int index = scanCode - 16;
            char expected = scanKeys[index];
            return expected != '\0' && expected == (char)(charCode - 1 + 'A');
        }
        return false;
    }

    private static bool IsPrintableCharacter(byte charCode)
    {
        return charCode >= ' ' && charCode != 0x7F && charCode != 0xFF;
    }

    private static bool IsKeypadCharacter(byte scanCode)
    {
        return scanCode == 0x35 || scanCode == 0x37 || scanCode == 0x4A || scanCode == 0x4E;
    }

    public byte ScanCode => (byte)(KeyCode >> 8);
    public byte CharCode => (byte)(KeyCode & 0xFF);

    public static implicit operator TKey(ushort keyCode) => new(keyCode);

    public bool Equals(TKey other) => KeyCode == other.KeyCode && ControlKeyState == other.ControlKeyState;
    public override bool Equals(object? obj) => obj is TKey other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(KeyCode, ControlKeyState);
    public static bool operator ==(TKey left, TKey right) => left.Equals(right);
    public static bool operator !=(TKey left, TKey right) => !left.Equals(right);
}
