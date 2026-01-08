using System.Text;
using TurboVision.Core;
using static TurboVision.Platform.Win32Interop;

namespace TurboVision.Platform;

/// <summary>
/// Win32 input adapter with surrogate pair handling for emoji and non-BMP characters.
/// Matches upstream Win32Input in win32con.cpp:477-604
/// </summary>
internal sealed class Win32Input : InputAdapter
{
    private readonly ConsoleCtl _con;
    private InputState _state;

    // Key code conversion tables (convert NT virtual scan codes to BIOS key codes)
    private const ushort kbShift = KeyConstants.kbRightShift | KeyConstants.kbLeftShift;

    private static readonly ushort[] NormalCvt =
    [
        0,      0,      0,      0,      0,      0,      0,      0,
        0,      0,      0,      0,      0,      0,      0,      0,
        0,      0,      0,      0,      0,      0,      0,      0,
        0,      0,      0,      0,      0,      0,      0,      0,
        0,      0,      0,      0,      0,      0,      0,      0,
        0,      0,      0,      0,      0,      0,      0,      0,
        0,      0,      0,      0,      0,      0,      0,      0,
        0,      0,      0,      0,      0,      0,      0,      0,
        0,      0,      0,      0,      0,      0,      0,      0,
        0,      0,      0,      0,      0,      0,      0,      0,
        0,      0,      0,      0,      0,      0,      0, 0x8500,
        0x8600
    ];

    private static readonly ushort[] ShiftCvt =
    [
        0,      0,      0,      0,      0,      0,      0,      0,
        0,      0,      0,      0,      0,      0,      0, 0x0F00,
        0,      0,      0,      0,      0,      0,      0,      0,
        0,      0,      0,      0,      0,      0,      0,      0,
        0,      0,      0,      0,      0,      0,      0,      0,
        0,      0,      0,      0,      0,      0,      0,      0,
        0,      0,      0,      0,      0,      0,      0,      0,
        0,      0,      0, 0x5400, 0x5500, 0x5600, 0x5700, 0x5800,
        0x5900, 0x5A00, 0x5B00, 0x5C00, 0x5D00,      0,      0,      0,
        0,      0,      0,      0,      0,      0,      0,      0,
        0,      0, 0x0500, 0x0700,      0,      0,      0, 0x8700,
        0x8800
    ];

    private static readonly ushort[] CtrlCvt =
    [
        0,      0, 0x0231, 0x0332, 0x0433, 0x0534, 0x0635, 0x0736,
        0x0837, 0x0938, 0x0A39, 0x0B30,      0,      0,      0, 0x9400,
        0x0011, 0x0017, 0x0005, 0x0012, 0x0014, 0x0019, 0x0015, 0x0009,
        0x000F, 0x0010,      0,      0,      0,      0, 0x0001, 0x0013,
        0x0004, 0x0006, 0x0007, 0x0008, 0x000A, 0x000B, 0x000C,      0,
        0,      0,      0,      0, 0x001A, 0x0018, 0x0003, 0x0016,
        0x0002, 0x000E, 0x000D,      0,      0, 0x352F,      0, 0x372A,
        0,      0,      0, 0x5E00, 0x5F00, 0x6000, 0x6100, 0x6200,
        0x6300, 0x6400, 0x6500, 0x6600, 0x6700,      0,      0, 0x7700,
        0x8D00, 0x8400, 0x4A2D, 0x7300,      0, 0x7400, 0x4E2B, 0x7500,
        0x9100, 0x7600, 0x0400, 0x0600,      0,      0,      0, 0x8900,
        0x8A00
    ];

    private static readonly ushort[] AltCvt =
    [
        0, 0x0100, 0x7800, 0x7900, 0x7A00, 0x7B00, 0x7C00, 0x7D00,
        0x7E00, 0x7F00, 0x8000, 0x8100, 0x8200, 0x8300, 0x0E00, 0xA500,
        0x1000, 0x1100, 0x1200, 0x1300, 0x1400, 0x1500, 0x1600, 0x1700,
        0x1800, 0x1900,      0,      0, 0xA600,      0, 0x1E00, 0x1F00,
        0x2000, 0x2100, 0x2200, 0x2300, 0x2400, 0x2500, 0x2600,      0,
        0,      0,      0,      0, 0x2C00, 0x2D00, 0x2E00, 0x2F00,
        0x3000, 0x3100, 0x3200,      0,      0,      0,      0,      0,
        0, 0x0200,      0, 0x6800, 0x6900, 0x6A00, 0x6B00, 0x6C00,
        0x6D00, 0x6E00, 0x6F00, 0x7000, 0x7100,      0,      0, 0x9700,
        0x9800, 0x9900, 0x8200, 0x9B00,      0, 0x9D00,      0, 0x9F00,
        0xA000, 0xA100, 0xA200, 0xA300,      0,      0,      0, 0x8B00,
        0x8C00
    ];

    public Win32Input(ConsoleCtl con) : base(con.In())
    {
        _con = con;
    }

    /// <summary>
    /// Gets the next event from the input source.
    /// Matches upstream Win32Input::getEvent() in win32con.cpp:542-558
    /// </summary>
    public override bool GetEvent(out TEvent ev)
    {
        ev = default;

        // Check if there are any events available
        if (!GetNumberOfConsoleInputEvents(_con.In(), out uint events) || events == 0)
            return false;

        // Process events until we get a valid one
        while (events-- > 0)
        {
            INPUT_RECORD ir = default;
            if (!ReadConsoleInputW(_con.In(), ref ir, 1, out uint read) || read == 0)
                return false;

            if (GetEvent(ir, out ev))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Processes a single input record.
    /// Matches upstream Win32Input::getEvent(const INPUT_RECORD&) in win32con.cpp:560-580
    /// </summary>
    private bool GetEvent(INPUT_RECORD ir, out TEvent ev)
    {
        ev = default;

        switch (ir.EventType)
        {
            case KEY_EVENT:
                // Process key event (only on key down, or VK_MENU with text for pasted surrogate)
                if (ir.KeyEvent.bKeyDown != 0 ||
                    (ir.KeyEvent.wVirtualKeyCode == VK_MENU && ir.KeyEvent.UnicodeChar != 0))
                {
                    return GetWin32Key(ir.KeyEvent, out ev, ref _state);
                }
                break;

            case MOUSE_EVENT:
                GetWin32Mouse(ir.MouseEvent, out ev, ref _state);
                return true;

            case WINDOW_BUFFER_SIZE_EVENT:
                ev.What = EventConstants.evCommand;
                ev.Message.Command = CommandConstants.cmScreenChanged;
                ev.Message.InfoPtr = 0;
                return true;
        }

        return false;
    }

    /// <summary>
    /// Processes a key event with surrogate pair handling.
    /// Matches upstream getWin32Key in win32con.cpp:527-604
    /// </summary>
    private static bool GetWin32Key(KEY_EVENT_RECORD keyEvent, out TEvent ev, ref InputState state)
    {
        ev = default;

        // First handle surrogate pairs and text
        // Matches upstream line 529-530
        if (!GetWin32KeyText(keyEvent, out ev, ref state))
            return false; // Need next event for surrogate pair completion

        ev.What = EventConstants.evKeyDown;

        // Initialize keyCode with scanCode and charCode
        // Matches upstream lines 533-534
        byte scanCode = (byte)keyEvent.wVirtualScanCode;
        byte charCode = (byte)keyEvent.UnicodeChar;  // Use low byte as ASCII char
        ushort keyCode = (ushort)((scanCode << 8) | charCode);

        // Convert Windows control key state to TV format
        // On Windows, dwControlKeyState flags match TV constants directly
        // Matches upstream lines 535-538
        ushort controlKeyState = (ushort)(keyEvent.dwControlKeyState & (
            kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift |
            KeyConstants.kbScrollState | KeyConstants.kbNumState | KeyConstants.kbCapsState | KeyConstants.kbEnhanced
        ));

        ev.KeyDown.ControlKeyState = controlKeyState;

        // Get text from event
        ReadOnlySpan<char> text = ev.KeyDown.GetText();

        // If there's text, update charCode from UTF-8
        // Matches upstream lines 540-552
        if (text.Length > 0)
        {
            charCode = (byte)text[0];  // First character as charCode
            keyCode = (ushort)((scanCode << 8) | charCode);

            // Handle pasted surrogate characters (VK_MENU special case)
            // Matches upstream lines 543-546
            if (keyEvent.wVirtualKeyCode == VK_MENU)
            {
                scanCode = 0;
                keyCode = 0;
            }

            // If charCode is null or would trigger Ctrl+Key accidentally, clear keyCode
            // Matches upstream lines 547-551
            if (charCode == 0 || keyCode <= KeyConstants.kbCtrlZ)
                keyCode = KeyConstants.kbNoKey;
        }

        // Discard standalone modifier keys
        // Matches upstream lines 554-559
        if (keyCode == 0x2A00 || keyCode == 0x1D00 || keyCode == 0x3600 ||
            keyCode == 0x3800 || keyCode == 0x3A00 || keyCode == 0x5B00 || keyCode == 0x5C00)
        {
            keyCode = KeyConstants.kbNoKey;
        }
        // Detect AltGr without text (left Ctrl + right Alt, no text produced)
        // Matches upstream lines 560-568
        else if ((controlKeyState & KeyConstants.kbLeftCtrl) != 0 &&
                 (controlKeyState & KeyConstants.kbRightAlt) != 0 &&
                 text.Length == 0)
        {
            // Discard AltGr without text
            keyCode = KeyConstants.kbNoKey;
        }
        // Detect AltGr with text (Ctrl+Alt produces text)
        // Matches upstream lines 569-574
        else if ((controlKeyState & KeyConstants.kbCtrlShift) != 0 &&
                 (controlKeyState & KeyConstants.kbAltShift) != 0 &&
                 text.Length > 0)
        {
            // AltGr: discard Ctrl and Alt modifiers
            controlKeyState &= unchecked((ushort)~(KeyConstants.kbCtrlShift | KeyConstants.kbAltShift));
            ev.KeyDown.ControlKeyState = controlKeyState;
        }
        // Convert scan code to key code using lookup tables
        // Matches upstream lines 575-601
        else if (keyEvent.wVirtualScanCode < 89)
        {
            byte index = (byte)keyEvent.wVirtualScanCode;
            ushort tableKeyCode = 0;

            // Priority: Alt > Ctrl > Shift > Normal
            // Matches upstream lines 580-588
            if ((controlKeyState & KeyConstants.kbAltShift) != 0 && AltCvt[index] != 0)
                tableKeyCode = AltCvt[index];
            else if ((controlKeyState & KeyConstants.kbCtrlShift) != 0 && CtrlCvt[index] != 0)
                tableKeyCode = CtrlCvt[index];
            else if ((controlKeyState & kbShift) != 0 && ShiftCvt[index] != 0)
                tableKeyCode = ShiftCvt[index];
            else if ((controlKeyState & (kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift)) == 0 &&
                     NormalCvt[index] != 0)
                tableKeyCode = NormalCvt[index];

            // If we found a key code in the table, use it
            // Matches upstream lines 590-600
            if (tableKeyCode != 0)
            {
                keyCode = tableKeyCode;
                // Clear text if charCode is non-printable
                if (charCode < ' ')
                {
                    ev.KeyDown.SetText(ReadOnlySpan<char>.Empty);
                }
                // Add text if charCode is printable ASCII and we don't have text yet
                else if (charCode < 0x7F && text.Length == 0)
                {
                    Span<char> singleChar = stackalloc char[1];
                    singleChar[0] = (char)charCode;
                    ev.KeyDown.SetText(singleChar);
                }
            }
        }

        ev.KeyDown.KeyCode = keyCode;

        // Return true if we have a key code or text
        // Matches upstream line 603
        return keyCode != KeyConstants.kbNoKey || ev.KeyDown.GetText().Length > 0;
    }

    /// <summary>
    /// Handles text extraction and surrogate pair reconstruction.
    /// Matches upstream getWin32KeyText in win32con.cpp:492-525
    /// </summary>
    private static bool GetWin32KeyText(KEY_EVENT_RECORD keyEvent, out TEvent ev, ref InputState state)
    {
        ev = default;
        ev.What = EventConstants.evKeyDown;

        uint ch = keyEvent.UnicodeChar;

        // Do not treat non-printable characters as text
        if (ch < ' ' || ch == 0x7F)
            return true;

        // Handle UTF-16 surrogate pairs
        if (ch >= 0xD800 && ch <= 0xDBFF)
        {
            // High surrogate - need to wait for low surrogate
            state.Surrogate = (char)ch;
            return false; // Signal that we need the next event
        }

        // Build UTF-16 character(s)
        char[] utf16;
        int utf16Length;

        if (state.Surrogate != 0)
        {
            // We have a surrogate pair
            if (ch >= 0xDC00 && ch <= 0xDFFF)
            {
                // Valid low surrogate
                utf16 = new char[] { state.Surrogate, (char)ch };
                utf16Length = 2;
            }
            else
            {
                // Invalid pair, just use current character
                utf16 = new char[] { (char)ch };
                utf16Length = 1;
            }
            state.Surrogate = '\0';
        }
        else
        {
            // Single character (BMP)
            utf16 = new char[] { (char)ch };
            utf16Length = 1;
        }

        // Convert UTF-16 to UTF-8
        string text = new string(utf16, 0, utf16Length);
        ev.KeyDown.SetText(text);

        return true;
    }

    /// <summary>
    /// Processes a mouse event.
    /// Matches upstream getWin32Mouse in win32con.cpp:606-633
    /// </summary>
    private static void GetWin32Mouse(MOUSE_EVENT_RECORD mouseEvent, out TEvent ev, ref InputState state)
    {
        ev = default;
        ev.What = EventConstants.evMouse;

        ev.Mouse.Where = new TPoint(mouseEvent.dwMousePosition.X, mouseEvent.dwMousePosition.Y);
        ev.Mouse.Buttons = state.Buttons = (byte)(mouseEvent.dwButtonState & 0xFF);
        ev.Mouse.EventFlags = (ushort)mouseEvent.dwEventFlags;

        // Convert Windows control key state to TV format
        // On Windows, dwControlKeyState flags match TV constants directly
        // Matches upstream getWin32Mouse
        ushort controlKeyState = (ushort)(mouseEvent.dwControlKeyState & (
            KeyConstants.kbShift | KeyConstants.kbCtrlShift | KeyConstants.kbAltShift |
            KeyConstants.kbScrollState | KeyConstants.kbNumState | KeyConstants.kbCapsState
        ));

        ev.Mouse.ControlKeyState = controlKeyState;

        // Handle wheel events
        bool positive = (mouseEvent.dwButtonState & 0x80000000) == 0;
        if ((mouseEvent.dwEventFlags & MOUSE_WHEELED) != 0)
            ev.Mouse.Wheel = positive ? EventConstants.mwUp : EventConstants.mwDown;
        else if ((mouseEvent.dwEventFlags & MOUSE_HWHEELED) != 0)
            ev.Mouse.Wheel = positive ? EventConstants.mwRight : EventConstants.mwLeft;
        else
            ev.Mouse.Wheel = 0;
    }
}
