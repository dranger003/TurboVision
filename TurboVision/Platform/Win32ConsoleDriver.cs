using System.Runtime.InteropServices;
using TurboVision.Core;

namespace TurboVision.Platform;

/// <summary>
/// Windows Console API driver implementing screen output and input events.
/// </summary>
public sealed class Win32ConsoleDriver : IScreenDriver, IEventSource, IDisposable
{
    private readonly nint _inputHandle;
    private readonly nint _outputHandle;
    private readonly uint _originalInputMode;
    private readonly uint _originalOutputMode;
    private readonly uint _originalInputCodePage;
    private readonly uint _originalOutputCodePage;

    private int _cols;
    private int _rows;
    private bool _disposed;

    // Mouse state tracking
    private byte _lastButtons;
    private TPoint _lastMousePos;
    private DateTime _lastClickTime;
    private int _clickCount;

    public int Cols
    {
        get { return _cols; }
    }

    public int Rows
    {
        get { return _rows; }
    }

    public bool MousePresent
    {
        get { return true; }
    }

    public Win32ConsoleDriver()
    {
        _inputHandle = GetStdHandle(STD_INPUT_HANDLE);
        _outputHandle = GetStdHandle(STD_OUTPUT_HANDLE);

        // Save original modes
        GetConsoleMode(_inputHandle, out _originalInputMode);
        GetConsoleMode(_outputHandle, out _originalOutputMode);
        _originalInputCodePage = GetConsoleCP();
        _originalOutputCodePage = GetConsoleOutputCP();

        // Configure input mode
        uint inputMode = _originalInputMode;
        inputMode |= ENABLE_WINDOW_INPUT;
        inputMode |= ENABLE_MOUSE_INPUT;
        inputMode &= ~ENABLE_PROCESSED_INPUT;
        inputMode &= ~ENABLE_ECHO_INPUT;
        inputMode &= ~ENABLE_LINE_INPUT;
        inputMode |= ENABLE_EXTENDED_FLAGS;
        inputMode &= ~ENABLE_QUICK_EDIT_MODE;
        SetConsoleMode(_inputHandle, inputMode);

        // Configure output mode
        uint outputMode = _originalOutputMode;
        outputMode &= ~ENABLE_WRAP_AT_EOL_OUTPUT;
        outputMode |= DISABLE_NEWLINE_AUTO_RETURN;
        outputMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
        SetConsoleMode(_outputHandle, outputMode);

        // Set UTF-8 code pages
        SetConsoleCP(CP_UTF8);
        SetConsoleOutputCP(CP_UTF8);

        // Get initial size
        RefreshScreenInfo();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        // Restore original settings
        SetConsoleMode(_inputHandle, _originalInputMode);
        SetConsoleMode(_outputHandle, _originalOutputMode);
        SetConsoleCP(_originalInputCodePage);
        SetConsoleOutputCP(_originalOutputCodePage);
    }

    private void RefreshScreenInfo()
    {
        if (GetConsoleScreenBufferInfo(_outputHandle, out var info))
        {
            _cols = info.srWindow.Right - info.srWindow.Left + 1;
            _rows = info.srWindow.Bottom - info.srWindow.Top + 1;
        }
        else
        {
            _cols = 80;
            _rows = 25;
        }
    }

    public void ClearScreen(char c, TColorAttr attr)
    {
        var coord = new COORD { X = 0, Y = 0 };
        uint length = (uint)(_cols * _rows);
        ushort biosAttr = (ushort)attr.Value;

        FillConsoleOutputAttribute(_outputHandle, biosAttr, length, coord, out _);
        FillConsoleOutputCharacterW(_outputHandle, c, length, coord, out _);
    }

    public void WriteBuffer(int x, int y, int width, int height, ReadOnlySpan<TScreenCell> buffer)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        // Allocate CHAR_INFO array
        var charInfos = new CHAR_INFO[width * height];

        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                int srcIndex = row * width + col;
                int dstIndex = row * width + col;

                if (srcIndex < buffer.Length)
                {
                    var cell = buffer[srcIndex];
                    charInfos[dstIndex].UnicodeChar = cell.Char;
                    charInfos[dstIndex].Attributes = (ushort)cell.Attr.Value;
                }
                else
                {
                    charInfos[dstIndex].UnicodeChar = ' ';
                    charInfos[dstIndex].Attributes = 0x07;
                }
            }
        }

        var bufferSize = new COORD { X = (short)width, Y = (short)height };
        var bufferCoord = new COORD { X = 0, Y = 0 };
        var writeRegion = new SMALL_RECT
        {
            Left = (short)x,
            Top = (short)y,
            Right = (short)(x + width - 1),
            Bottom = (short)(y + height - 1)
        };

        WriteConsoleOutputW(_outputHandle, charInfos, bufferSize, bufferCoord, ref writeRegion);
    }

    public void Flush()
    {
        // WriteConsoleOutput is unbuffered, nothing to flush
    }

    public void SetCursorPosition(int x, int y)
    {
        var coord = new COORD { X = (short)x, Y = (short)y };
        SetConsoleCursorPosition(_outputHandle, coord);
    }

    public void SetCursorType(ushort cursorType)
    {
        var info = new CONSOLE_CURSOR_INFO
        {
            dwSize = cursorType == 0 ? 1u : (cursorType > 100 ? 100u : cursorType),
            bVisible = cursorType != 0
        };
        SetConsoleCursorInfo(_outputHandle, ref info);
    }

    public ushort GetCursorType()
    {
        if (GetConsoleCursorInfo(_outputHandle, out var info))
        {
            return info.bVisible ? (ushort)info.dwSize : (ushort)0;
        }
        return 0;
    }

    public void Suspend()
    {
        SetConsoleMode(_inputHandle, _originalInputMode);
        SetConsoleMode(_outputHandle, _originalOutputMode);
    }

    public void Resume()
    {
        uint inputMode = _originalInputMode;
        inputMode |= ENABLE_WINDOW_INPUT;
        inputMode |= ENABLE_MOUSE_INPUT;
        inputMode &= ~ENABLE_PROCESSED_INPUT;
        inputMode &= ~ENABLE_ECHO_INPUT;
        inputMode &= ~ENABLE_LINE_INPUT;
        inputMode |= ENABLE_EXTENDED_FLAGS;
        inputMode &= ~ENABLE_QUICK_EDIT_MODE;
        SetConsoleMode(_inputHandle, inputMode);

        uint outputMode = _originalOutputMode;
        outputMode &= ~ENABLE_WRAP_AT_EOL_OUTPUT;
        outputMode |= DISABLE_NEWLINE_AUTO_RETURN;
        outputMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
        SetConsoleMode(_outputHandle, outputMode);

        RefreshScreenInfo();
    }

    public bool GetEvent(out TEvent ev)
    {
        ev = default;

        if (!GetNumberOfConsoleInputEvents(_inputHandle, out uint numEvents) || numEvents == 0)
        {
            return false;
        }

        var inputRecord = new INPUT_RECORD();
        if (!ReadConsoleInputW(_inputHandle, ref inputRecord, 1, out uint read) || read == 0)
        {
            return false;
        }

        return ProcessInputRecord(ref inputRecord, out ev);
    }

    private bool ProcessInputRecord(ref INPUT_RECORD ir, out TEvent ev)
    {
        ev = default;

        switch (ir.EventType)
        {
            case KEY_EVENT:
                if (ir.KeyEvent.bKeyDown || (ir.KeyEvent.wVirtualKeyCode == VK_MENU && ir.KeyEvent.UnicodeChar != 0))
                {
                    return ProcessKeyEvent(ref ir.KeyEvent, out ev);
                }
                break;

            case MOUSE_EVENT:
                return ProcessMouseEvent(ref ir.MouseEvent, out ev);

            case WINDOW_BUFFER_SIZE_EVENT:
                RefreshScreenInfo();
                ev.What = EventConstants.evCommand;
                ev.Message = new MessageEvent(CommandConstants.cmScreenChanged);
                return true;
        }

        return false;
    }

    private bool ProcessKeyEvent(ref KEY_EVENT_RECORD keyEvent, out TEvent ev)
    {
        ev = default;
        ev.What = EventConstants.evKeyDown;

        // Build key code from scan code and character
        byte scanCode = (byte)keyEvent.wVirtualScanCode;
        byte charCode = (byte)keyEvent.UnicodeChar;
        ev.KeyDown.KeyCode = (ushort)((scanCode << 8) | charCode);

        // Set control key state
        ev.KeyDown.ControlKeyState = (ushort)(keyEvent.dwControlKeyState & 0xFF);

        // Set text for printable characters
        if (keyEvent.UnicodeChar >= ' ' && keyEvent.UnicodeChar != 0x7F)
        {
            Span<char> text = stackalloc char[1];
            text[0] = keyEvent.UnicodeChar;
            ev.KeyDown.SetText(text);
        }

        // Discard standalone modifier keys
        ushort keyCode = ev.KeyDown.KeyCode;
        if (keyCode == 0x2A00 || keyCode == 0x1D00 || keyCode == 0x3600 ||
            keyCode == 0x3800 || keyCode == 0x3A00 || keyCode == 0x5B00 || keyCode == 0x5C00)
        {
            return false;
        }

        return ev.KeyDown.KeyCode != KeyConstants.kbNoKey || ev.KeyDown.GetText().Length > 0;
    }

    private bool ProcessMouseEvent(ref MOUSE_EVENT_RECORD mouseEvent, out TEvent ev)
    {
        ev = default;

        byte buttons = (byte)(mouseEvent.dwButtonState & 0x07);
        var where = new TPoint(mouseEvent.dwMousePosition.X, mouseEvent.dwMousePosition.Y);

        // Determine event type
        if ((mouseEvent.dwEventFlags & MOUSE_MOVED) != 0)
        {
            ev.What = EventConstants.evMouseMove;
        }
        else if (buttons != 0 && _lastButtons == 0)
        {
            ev.What = EventConstants.evMouseDown;

            // Check for double/triple click
            var now = DateTime.UtcNow;
            if ((now - _lastClickTime).TotalMilliseconds < 400 && where.Equals(_lastMousePos))
            {
                _clickCount++;
            }
            else
            {
                _clickCount = 1;
            }
            _lastClickTime = now;
        }
        else if (buttons == 0 && _lastButtons != 0)
        {
            ev.What = EventConstants.evMouseUp;
        }
        else if ((mouseEvent.dwEventFlags & (MOUSE_WHEELED | MOUSE_HWHEELED)) != 0)
        {
            ev.What = EventConstants.evMouseWheel;
        }
        else
        {
            ev.What = EventConstants.evMouseAuto;
        }

        ev.Mouse.Where = where;
        ev.Mouse.Buttons = buttons;
        ev.Mouse.ControlKeyState = (ushort)(mouseEvent.dwControlKeyState & 0xFF);

        // Event flags
        ushort eventFlags = 0;
        if ((mouseEvent.dwEventFlags & MOUSE_MOVED) != 0)
        {
            eventFlags |= EventConstants.meMouseMoved;
        }
        if (_clickCount == 2)
        {
            eventFlags |= EventConstants.meDoubleClick;
        }
        else if (_clickCount >= 3)
        {
            eventFlags |= EventConstants.meTripleClick;
        }
        ev.Mouse.EventFlags = eventFlags;

        // Wheel
        if ((mouseEvent.dwEventFlags & MOUSE_WHEELED) != 0)
        {
            bool up = (mouseEvent.dwButtonState & 0x80000000) == 0;
            ev.Mouse.Wheel = up ? EventConstants.mwUp : EventConstants.mwDown;
        }
        else if ((mouseEvent.dwEventFlags & MOUSE_HWHEELED) != 0)
        {
            bool right = (mouseEvent.dwButtonState & 0x80000000) == 0;
            ev.Mouse.Wheel = right ? EventConstants.mwRight : EventConstants.mwLeft;
        }

        _lastButtons = buttons;
        _lastMousePos = where;

        return true;
    }

    public void WaitForEvents(int timeoutMs)
    {
        WaitForSingleObject(_inputHandle, timeoutMs < 0 ? 0xFFFFFFFFu : (uint)timeoutMs);
    }

    public void WakeUp()
    {
        // Post a null input record to wake up the wait
        var ir = new INPUT_RECORD { EventType = 0 };
        WriteConsoleInputW(_inputHandle, ref ir, 1, out _);
    }

    #region Win32 API Declarations

    private const int STD_INPUT_HANDLE = -10;
    private const int STD_OUTPUT_HANDLE = -11;

    private const uint ENABLE_PROCESSED_INPUT = 0x0001;
    private const uint ENABLE_LINE_INPUT = 0x0002;
    private const uint ENABLE_ECHO_INPUT = 0x0004;
    private const uint ENABLE_WINDOW_INPUT = 0x0008;
    private const uint ENABLE_MOUSE_INPUT = 0x0010;
    private const uint ENABLE_EXTENDED_FLAGS = 0x0080;
    private const uint ENABLE_QUICK_EDIT_MODE = 0x0040;

    private const uint ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002;
    private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    private const uint CP_UTF8 = 65001;

    private const ushort KEY_EVENT = 0x0001;
    private const ushort MOUSE_EVENT = 0x0002;
    private const ushort WINDOW_BUFFER_SIZE_EVENT = 0x0004;

    private const uint MOUSE_MOVED = 0x0001;
    private const uint MOUSE_WHEELED = 0x0004;
    private const uint MOUSE_HWHEELED = 0x0008;

    private const ushort VK_MENU = 0x12; // Alt key

    [StructLayout(LayoutKind.Sequential)]
    private struct COORD
    {
        public short X;
        public short Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SMALL_RECT
    {
        public short Left;
        public short Top;
        public short Right;
        public short Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CONSOLE_SCREEN_BUFFER_INFO
    {
        public COORD dwSize;
        public COORD dwCursorPosition;
        public ushort wAttributes;
        public SMALL_RECT srWindow;
        public COORD dwMaximumWindowSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CONSOLE_CURSOR_INFO
    {
        public uint dwSize;
        public bool bVisible;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CHAR_INFO
    {
        public char UnicodeChar;
        public ushort Attributes;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUT_RECORD
    {
        [FieldOffset(0)]
        public ushort EventType;
        [FieldOffset(4)]
        public KEY_EVENT_RECORD KeyEvent;
        [FieldOffset(4)]
        public MOUSE_EVENT_RECORD MouseEvent;
        [FieldOffset(4)]
        public WINDOW_BUFFER_SIZE_RECORD WindowBufferSizeEvent;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEY_EVENT_RECORD
    {
        public bool bKeyDown;
        public ushort wRepeatCount;
        public ushort wVirtualKeyCode;
        public ushort wVirtualScanCode;
        public char UnicodeChar;
        public uint dwControlKeyState;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSE_EVENT_RECORD
    {
        public COORD dwMousePosition;
        public uint dwButtonState;
        public uint dwControlKeyState;
        public uint dwEventFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOW_BUFFER_SIZE_RECORD
    {
        public COORD dwSize;
    }

    [DllImport("kernel32.dll")]
    private static extern nint GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleMode(nint hConsoleHandle, uint dwMode);

    [DllImport("kernel32.dll")]
    private static extern uint GetConsoleCP();

    [DllImport("kernel32.dll")]
    private static extern uint GetConsoleOutputCP();

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleCP(uint wCodePageID);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleOutputCP(uint wCodePageID);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetConsoleScreenBufferInfo(nint hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FillConsoleOutputAttribute(nint hConsoleOutput, ushort wAttribute, uint nLength, COORD dwWriteCoord, out uint lpNumberOfAttrsWritten);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FillConsoleOutputCharacterW(nint hConsoleOutput, char cCharacter, uint nLength, COORD dwWriteCoord, out uint lpNumberOfCharsWritten);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteConsoleOutputW(nint hConsoleOutput, [In] CHAR_INFO[] lpBuffer, COORD dwBufferSize, COORD dwBufferCoord, ref SMALL_RECT lpWriteRegion);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleCursorPosition(nint hConsoleOutput, COORD dwCursorPosition);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleCursorInfo(nint hConsoleOutput, ref CONSOLE_CURSOR_INFO lpConsoleCursorInfo);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetConsoleCursorInfo(nint hConsoleOutput, out CONSOLE_CURSOR_INFO lpConsoleCursorInfo);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNumberOfConsoleInputEvents(nint hConsoleInput, out uint lpcNumberOfEvents);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadConsoleInputW(nint hConsoleInput, ref INPUT_RECORD lpBuffer, uint nLength, out uint lpNumberOfEventsRead);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteConsoleInputW(nint hConsoleInput, ref INPUT_RECORD lpBuffer, uint nLength, out uint lpNumberOfEventsWritten);

    [DllImport("kernel32.dll")]
    private static extern uint WaitForSingleObject(nint hHandle, uint dwMilliseconds);

    #endregion
}
